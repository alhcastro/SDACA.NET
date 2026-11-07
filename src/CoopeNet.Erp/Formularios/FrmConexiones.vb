Imports System.IO
Imports MySqlConnector
Imports CoopeNet.Core.Configuracion
Imports CoopeNet.Core.Datos
Imports CoopeNet.Ui

''' <summary>
''' Crea, cambia y elimina los perfiles de sdcnnNNN.ini. Migración de SDACAERP\FrmConfigurarServidor.frm
''' y FrmNewPerfil.frm, que se abrían con el botón "..." de la entrada.
''' </summary>
''' <remarks>
''' No se migraron las revisiones que el original hacía al validar: SD_VerificarUsuariosdaca borraba
''' usuarios de mysql.user y daba GRANT a root con contraseña fija, y SD_VerificarLocalCero /
''' SD_VerificarModuloLocalCero insertaban el local 0. Aquí probar una conexión no escribe en la base.
''' </remarks>
Partial Public Class FrmConexiones
    Inherits Form

    Private Const Titulo As String = Tema.Producto

    Private ReadOnly _rutaIni As String
    Private _nueva As Boolean
    Private _cargando As Boolean

    ''' <summary>True si se guardó o eliminó algún perfil: la entrada debe volver a leer el archivo.</summary>
    Public ReadOnly Property HuboCambios As Boolean

    ''' <summary>El último perfil guardado o elegido, para que la entrada lo deje seleccionado.</summary>
    Public ReadOnly Property PerfilElegido As String = ""

    Public Sub New(rutaIni As String, perfilActual As String, usuario As String, clave As String)
        _rutaIni = rutaIni
        InitializeComponent()
        Tema.AplicarControles(Me)
        Tema.ColorearVentana(Me)
        txtUsuario.Text = usuario
        txtClave.Text = clave
        CargarPerfiles(perfilActual)
    End Sub

    Private Sub CargarPerfiles(elegir As String)
        Dim perfiles As IReadOnlyList(Of PerfilConexion) = Array.Empty(Of PerfilConexion)()
        If File.Exists(_rutaIni) Then perfiles = ConexionesIni.Leer(_rutaIni).Where(Function(p) p.Servidor.Length > 0).ToList()
        _cargando = True
        cmbPerfil.DisplayMember = NameOf(PerfilConexion.Nombre)
        cmbPerfil.DataSource = perfiles
        cmbPerfil.SelectedIndex = -1
        _cargando = False
        Dim indice = If(String.IsNullOrEmpty(elegir), -1, cmbPerfil.FindStringExact(elegir))
        If indice >= 0 Then
            cmbPerfil.SelectedIndex = indice
        ElseIf perfiles.Count > 0 Then
            cmbPerfil.SelectedIndex = 0
        Else
            EmpezarNueva()
        End If
    End Sub

    Private Sub CmbPerfil_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbPerfil.SelectedIndexChanged
        If _cargando Then Return
        Dim perfil = TryCast(cmbPerfil.SelectedItem, PerfilConexion)
        If perfil Is Nothing Then Return
        _nueva = False
        txtNombre.Text = perfil.Nombre
        txtNombre.ReadOnly = True
        txtServidor.Text = perfil.Servidor
        txtPuerto.Text = perfil.Puerto.ToString()
        cmbCatalogo.Items.Clear()
        cmbCatalogo.Text = perfil.Catalogo
        Dim predeterminada = String.Equals(ConexionesIni.LeerValor(_rutaIni, "GENERAL", "DEFAULT"), perfil.Nombre, StringComparison.OrdinalIgnoreCase)
        chkPredeterminada.Checked = predeterminada
        chkPredeterminada.Enabled = Not predeterminada
        btnBorrar.Enabled = True
        Estado("")
        _PerfilElegido = perfil.Nombre
        ActualizarApariencia()
    End Sub

    ''' <summary>Como FrmNewPerfil: arranca con el servidor, el puerto y el catálogo que estaban a la vista.</summary>
    Private Sub EmpezarNueva()
        _nueva = True
        _cargando = True
        cmbPerfil.SelectedIndex = -1
        _cargando = False
        txtNombre.ReadOnly = False
        txtNombre.Clear()
        If txtPuerto.TextLength = 0 Then txtPuerto.Text = "3306"
        chkPredeterminada.Checked = cmbPerfil.Items.Count = 0
        chkPredeterminada.Enabled = True
        btnBorrar.Enabled = False
        Estado("Escriba un nombre y los datos de la conexión nueva.")
        ActualizarApariencia()
        If Visible Then txtNombre.Focus()
    End Sub

    Private Sub ActualizarApariencia()
        txtNombre.BackColor = If(txtNombre.ReadOnly, Tema.CampoLectura, SystemColors.Window)
    End Sub

    Private Sub FrmConexiones_Shown(sender As Object, e As EventArgs) Handles Me.Shown
        If _nueva Then txtNombre.Focus() Else txtServidor.Focus()
    End Sub

    Private Sub BtnNueva_Click(sender As Object, e As EventArgs) Handles btnNueva.Click
        EmpezarNueva()
    End Sub

    Private Sub BtnBorrar_Click(sender As Object, e As EventArgs) Handles btnBorrar.Click
        Dim perfil = TryCast(cmbPerfil.SelectedItem, PerfilConexion)
        If perfil Is Nothing Then Return
        If MessageBox.Show(Me, $"¿Está seguro de que desea eliminar el perfil ""{perfil.Nombre}""?", Titulo,
                           MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) <> DialogResult.Yes Then Return
        If Not Escribir(Sub() ConexionesIni.EliminarPerfil(_rutaIni, perfil.Nombre)) Then Return
        _PerfilElegido = ""
        CargarPerfiles(ConexionesIni.LeerValor(_rutaIni, "GENERAL", "DEFAULT"))
        Estado($"Se eliminó la conexión ""{perfil.Nombre}"".")
    End Sub

    Private Function Puerto() As UInteger
        Dim valor As UInteger
        If UInteger.TryParse(txtPuerto.Text.Trim(), valor) AndAlso valor > 0UI AndAlso valor <= 65535UI Then Return valor
        Return 0UI
    End Function

    Private Function FabricaPara(catalogo As String) As FabricaConexiones
        Return New FabricaConexiones(CadenaConexion.Crear(txtServidor.Text.Trim(), catalogo, Puerto(), txtUsuario.Text.Trim(), txtClave.Text))
    End Function

    Private Function ServidorValido() As Boolean
        If txtServidor.Text.Trim().Length = 0 Then
            Avisar("Indique el servidor de datos.", txtServidor)
            Return False
        End If
        If Puerto() = 0UI Then
            Avisar("El puerto debe ser un número entre 1 y 65535. El predeterminado es 3306.", txtPuerto)
            Return False
        End If
        Return True
    End Function

    ''' <summary>Command1_Click del original: lista las bases del servidor que son del sistema.</summary>
    Private Async Sub BtnBuscar_Click(sender As Object, e As EventArgs) Handles btnBuscar.Click
        If Not ServidorValido() Then Return
        Ocupado(True)
        Try
            Dim catalogos = Await CatalogosServidor.ListarAsync(FabricaPara(""))
            Dim actual = cmbCatalogo.Text.Trim()
            cmbCatalogo.Items.Clear()
            cmbCatalogo.Items.AddRange(catalogos.Cast(Of Object)().ToArray())
            If catalogos.Count = 0 Then
                Estado("No se encontró ningún catálogo del sistema en el servidor.")
                MessageBox.Show(Me, $"No se encuentra ningún catálogo del sistema en el servidor ""{txtServidor.Text.Trim()}"".{vbCrLf}" &
                                "Verifique el nombre del servidor, el puerto y que el usuario tenga acceso.", Titulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If
            Dim indice = cmbCatalogo.FindStringExact(actual)
            cmbCatalogo.SelectedIndex = If(indice >= 0, indice, 0)
            Estado($"{catalogos.Count} catálogo(s) encontrado(s).")
            cmbCatalogo.Focus()
        Catch ex As MySqlException
            Estado("Sin conexión con el servidor.")
            MessageBox.Show(Me, $"No se ha podido tener contacto con el servidor de datos ""{txtServidor.Text.Trim()}"", puerto {Puerto()}.{vbCrLf}{vbCrLf}{ex.Message}",
                            Titulo, MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
        Finally
            Ocupado(False)
        End Try
    End Sub

    ''' <summary>
    ''' CmdPrueba_Click del original, sin las escrituras: abre el catálogo con el usuario y la
    ''' contraseña indicados y confirma que tenga las tablas del sistema.
    ''' </summary>
    Private Async Sub BtnProbar_Click(sender As Object, e As EventArgs) Handles btnProbar.Click
        If Not ServidorValido() OrElse Not CatalogoValido() Then Return
        Dim catalogo = cmbCatalogo.Text.Trim()
        Ocupado(True)
        Try
            Dim fabrica = FabricaPara(catalogo)
            Using cn = Await fabrica.AbrirAsync()
            End Using
            Dim delSistema = (Await CatalogosServidor.ListarAsync(fabrica)).Contains(catalogo, StringComparer.OrdinalIgnoreCase)
            If delSistema Then
                Estado($"Conexión correcta con {txtServidor.Text.Trim()}.", True)
            Else
                Estado("Conecta, pero el catálogo no tiene las tablas del sistema.")
                MessageBox.Show(Me, $"La conexión funciona, pero ""{catalogo}"" no tiene las tablas del sistema.", Titulo, MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            End If
        Catch ex As MySqlException
            Estado("No se pudo conectar.")
            MessageBox.Show(Me, $"Error al conectar al servidor ""{txtServidor.Text.Trim()}"", puerto {Puerto()}, catálogo ""{catalogo}"" con el usuario ""{txtUsuario.Text.Trim()}"".{vbCrLf}{vbCrLf}" &
                            $"Verifique que los datos estén escritos correctamente y que haya conexión con el servidor.{vbCrLf}{vbCrLf}{ex.Message}",
                            Titulo, MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
        Finally
            Ocupado(False)
        End Try
    End Sub

    Private Function CatalogoValido() As Boolean
        If cmbCatalogo.Text.Trim().Length > 0 Then Return True
        Avisar("Indique el catálogo de datos. Puede buscarlo con ""Buscar catálogos"".", cmbCatalogo)
        Return False
    End Function

    Private Sub BtnGuardar_Click(sender As Object, e As EventArgs) Handles btnGuardar.Click
        Dim nombre = txtNombre.Text.Trim()
        If _nueva Then
            Dim motivo = ConexionesIni.ValidarNombre(nombre, ConexionesIni.LeerSecciones(_rutaIni))
            If motivo.Length > 0 Then
                Avisar(motivo, txtNombre)
                Return
            End If
        End If
        If Not ServidorValido() OrElse Not CatalogoValido() Then Return
        Dim servidor = txtServidor.Text.Trim()
        Dim catalogo = cmbCatalogo.Text.Trim()
        Dim predeterminada = chkPredeterminada.Checked AndAlso chkPredeterminada.Enabled
        If Not Escribir(Sub()
                            ConexionesIni.GuardarPerfil(_rutaIni, nombre, servidor, Puerto(), catalogo)
                            If predeterminada Then ConexionesIni.FijarValor(_rutaIni, "GENERAL", "DEFAULT", nombre)
                        End Sub) Then Return
        CargarPerfiles(nombre)
        Estado($"Se guardó la conexión ""{nombre}"".", True)
    End Sub

    ''' <summary>El .ini vive en Program Files: sin permiso de modificar la carpeta, la escritura falla.</summary>
    Private Function Escribir(accion As Action) As Boolean
        Try
            accion()
            _HuboCambios = True
            Return True
        Catch ex As Exception When TypeOf ex Is IOException OrElse TypeOf ex Is UnauthorizedAccessException
            MessageBox.Show(Me, $"No se pudo guardar en ""{_rutaIni}"".{vbCrLf}Verifique que el usuario de Windows tenga permiso de modificar esa carpeta.{vbCrLf}{vbCrLf}{ex.Message}",
                            Titulo, MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return False
        End Try
    End Function

    Private Sub Avisar(texto As String, campo As Control)
        MessageBox.Show(Me, texto, Titulo, MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
        campo.Focus()
    End Sub

    Private Sub Estado(texto As String, Optional correcto As Boolean = False)
        lblEstado.Text = texto
        lblEstado.ForeColor = If(correcto, Tema.Acento, Tema.TextoTenue)
    End Sub

    Private Sub Ocupado(si As Boolean)
        UseWaitCursor = si
        For Each c In New Control() {btnBuscar, btnProbar, btnGuardar, btnNueva, cmbPerfil}
            c.Enabled = Not si
        Next
        btnBorrar.Enabled = Not si AndAlso Not _nueva
        If si Then Estado("Conectando...")
    End Sub

    Private Sub BtnCerrar_Click(sender As Object, e As EventArgs) Handles btnCerrar.Click
        Close()
    End Sub

End Class
