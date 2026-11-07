Imports System.IO
Imports System.Net
Imports System.Net.Sockets
Imports MySqlConnector
Imports CoopeNet.Core.Configuracion
Imports CoopeNet.Core.Datos
Imports CoopeNet.Core.Licencias
Imports CoopeNet.Core.Seguridad
Imports CoopeNet.Ui

''' <summary>
''' Inicio de sesión. Ofrece los perfiles de sdcnn007.ini (se administran en <see cref="FrmConexiones"/>) y aplica las reglas de
''' <see cref="ServicioEntrada"/>. Migración de SDACAERP\FrmEntrada.frm.
''' </summary>
Partial Public Class FrmEntrada
    Inherits Form

    Private Const Titulo As String = Tema.Producto

    Private ReadOnly _licencia As ILicencia
    Private ReadOnly _directorioIni As String
    Private ReadOnly _preferencias As Preferencias
    Private _localesDe As String = ""
    Private _conexionVisible As Boolean

    Public ReadOnly Property Sesion As Sesion
    Public ReadOnly Property Servicio As ServicioEntrada

    Public Sub New(licencia As ILicencia, directorioIni As String, preferencias As Preferencias)
        _licencia = licencia
        _directorioIni = directorioIni
        _preferencias = preferencias
        InitializeComponent()
        Tema.AplicarControles(Me)
        Tema.ColorearVentana(Me)
        lblLicencia.Text = licencia.Registro.NombreCorto
        CargarPerfiles()
        ' Sin un perfil elegido no hay servidor: hay que poder escribirlo.
        If cmbPerfil.SelectedIndex < 0 Then MostrarConexion(True)
    End Sub

    Private Sub MostrarConexion(mostrar As Boolean)
        If _conexionVisible = mostrar Then Return
        _conexionVisible = mostrar
        pnlConexion.Visible = mostrar
        lnkConexion.Text = If(mostrar, "Ocultar datos de conexión", "Mostrar datos de conexión")
        Dim diferencia = pnlConexion.Height * If(mostrar, 1, -1)
        ClientSize = New Size(ClientSize.Width, ClientSize.Height + diferencia)
    End Sub

    Private Sub LnkConexion_LinkClicked(sender As Object, e As LinkLabelLinkClickedEventArgs) Handles lnkConexion.LinkClicked
        MostrarConexion(Not _conexionVisible)
    End Sub

    Private Sub FrmEntrada_Shown(sender As Object, e As EventArgs) Handles Me.Shown
        If txtUsuario.TextLength = 0 Then txtUsuario.Focus() Else txtClave.Focus()
    End Sub

    Private Function RutaIni() As String
        Return Path.Combine(_directorioIni, $"sdcnn{_licencia.Registro.NumeroCliente:000}.ini")
    End Function

    ''' <summary>CmdOpciones_Click del original ("..."): abre la configuración del servidor.</summary>
    Private Sub LnkAdministrar_LinkClicked(sender As Object, e As LinkLabelLinkClickedEventArgs) Handles lnkAdministrar.LinkClicked
        Using ventana As New FrmConexiones(RutaIni(), cmbPerfil.Text, txtUsuario.Text, txtClave.Text)
            ventana.ShowDialog(Me)
            If Not ventana.HuboCambios Then Return
            Dim usuario = txtUsuario.Text
            CargarPerfiles(ventana.PerfilElegido)
            If txtUsuario.TextLength = 0 Then txtUsuario.Text = usuario
        End Using
        If cmbPerfil.SelectedIndex >= 0 Then txtClave.Focus()
    End Sub

    ''' <param name="elegir">Perfil que debe quedar seleccionado; si no se indica, el último usado o el predeterminado.</param>
    Private Sub CargarPerfiles(Optional elegir As String = Nothing)
        Dim ini = RutaIni()
        If Not File.Exists(ini) Then Return
        cmbPerfil.DisplayMember = NameOf(PerfilConexion.Nombre)
        cmbPerfil.DataSource = ConexionesIni.Leer(ini).Where(Function(p) p.Servidor.Length > 0).ToList()
        cmbPerfil.SelectedIndex = -1
        If Not String.IsNullOrEmpty(elegir) AndAlso cmbPerfil.FindStringExact(elegir) >= 0 Then
            cmbPerfil.SelectedIndex = cmbPerfil.FindStringExact(elegir)
            Return
        End If
        Dim ultimo = cmbPerfil.FindStringExact(_preferencias.UltimoPerfil)
        If ultimo >= 0 Then
            cmbPerfil.SelectedIndex = ultimo
            If _preferencias.UltimoUsuario.Length > 0 Then txtUsuario.Text = _preferencias.UltimoUsuario
        Else
            cmbPerfil.SelectedIndex = cmbPerfil.FindStringExact(ConexionesIni.LeerValor(ini, "GENERAL", "DEFAULT"))
        End If
    End Sub

    Private Sub CmbPerfil_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbPerfil.SelectedIndexChanged
        Dim perfil = TryCast(cmbPerfil.SelectedItem, PerfilConexion)
        If perfil Is Nothing Then Return
        txtServidor.Text = perfil.Servidor
        txtPuerto.Text = perfil.Puerto.ToString()
        txtCatalogo.Text = perfil.Catalogo
        txtUsuario.Text = perfil.UltimoUsuario
    End Sub

    Private Sub Datos_Changed(sender As Object, e As EventArgs) Handles txtServidor.TextChanged, txtPuerto.TextChanged, txtCatalogo.TextChanged, txtUsuario.TextChanged, txtClave.TextChanged
        _localesDe = ""
        cmbLocal.DataSource = Nothing
    End Sub

    Private Function Fabrica() As FabricaConexiones
        Dim puerto As UInteger
        UInteger.TryParse(txtPuerto.Text, puerto)
        Return New FabricaConexiones(CadenaConexion.Crear(txtServidor.Text.Trim(), txtCatalogo.Text.Trim(), puerto, txtUsuario.Text.Trim(), txtClave.Text))
    End Function

    Private Async Sub CmbLocal_Enter(sender As Object, e As EventArgs) Handles cmbLocal.Enter
        Await CargarLocales()
    End Sub

    ''' <summary>Los locales dependen del usuario, así que se piden al tener usuario y contraseña.</summary>
    Private Async Function CargarLocales() As Task(Of Boolean)
        Dim llave = LlaveConexion()
        If _localesDe = llave Then Return True
        If txtUsuario.Text.Trim().Length = 0 Then Return False
        Try
            Dim locales = Await New DatosEntradaMySql(Fabrica()).ListarLocalesAsync(txtUsuario.Text.Trim())
            cmbLocal.DisplayMember = NameOf(LocalEntrada.Descripcion)
            cmbLocal.DataSource = locales
            Dim ultimo = _preferencias.LocalDe(llave)
            Dim recordado = locales.Find(Function(l) ultimo.HasValue AndAlso l.Local = ultimo.Value)
            If recordado IsNot Nothing Then cmbLocal.SelectedItem = recordado
            _localesDe = llave
            Return True
        Catch ex As MySqlException
            MessageBox.Show($"El sistema no se ha podido conectar con la base de datos ubicada en el servidor ""{txtServidor.Text}"".{vbCrLf}Verifique el usuario y la contraseña.",
                            Titulo, MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            Return False
        End Try
    End Function

    ''' <summary>Identifica los locales de un usuario en una base: los mismos datos dan la misma lista.</summary>
    Private Function LlaveConexion() As String
        Return String.Join("|", txtServidor.Text.Trim(), txtPuerto.Text.Trim(), txtCatalogo.Text.Trim(), txtUsuario.Text.Trim())
    End Function

    Private Async Sub BtnEntrar_Click(sender As Object, e As EventArgs) Handles btnEntrar.Click
        btnEntrar.Enabled = False
        Try
            If Not Await CargarLocales() Then Return
            Dim local = TryCast(cmbLocal.SelectedItem, LocalEntrada)
            If local Is Nothing Then
                MessageBox.Show("Debe de seleccionar un departamento para ingresar al sistema.", Titulo, MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return
            End If
            If txtCatalogo.Text.Trim().Length = 0 Then
                MessageBox.Show("Debe de indicar un catálogo de datos para poder ingresar al sistema.", Titulo, MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return
            End If
            Await Entrar(local)
        Finally
            btnEntrar.Enabled = True
        End Try
    End Sub

    Private Async Function Entrar(local As LocalEntrada) As Task
        Dim fabrica = Me.Fabrica()
        Dim servicio As New ServicioEntrada(New DatosEntradaMySql(fabrica), _licencia)
        Dim login = txtUsuario.Text.Trim()
        Dim solicitud As New SolicitudEntrada With {
            .Login = login,
            .Local = local.Local,
            .LocalDescripcion = local.Descripcion,
            .Catalogo = txtCatalogo.Text.Trim(),
            .Equipo = Environment.MachineName,
            .Ip = IpLocal(),
            .EstacionGuardada = ConexionesIni.LeerValor(Path.Combine(_directorioIni, "sdconfig.ini"), "GENERAL", "IDEstacion"),
            .Avisar = Sub(texto) MessageBox.Show(Me, texto, Titulo, MessageBoxButtons.OK, MessageBoxIcon.Information),
            .ConfirmarOtraEstacion = Function(detalle) ConfirmarOtraEstacion(login, detalle)
        }
        Dim respuesta As RespuestaEntrada
        Try
            respuesta = Await servicio.EntrarAsync(solicitud)
        Catch ex As MySqlException
            MessageBox.Show("Error iniciando la configuración general del sistema..." & vbCrLf & ex.Message, Titulo, MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            Return
        End Try
        If respuesta.Resultado = ResultadoEntrada.Correcto Then
            respuesta.Sesion.Fabrica = fabrica
            _preferencias.RecordarLocal(LlaveConexion(), local.Local)
            _preferencias.UltimoPerfil = cmbPerfil.Text
            _preferencias.UltimoUsuario = login
            _preferencias.Guardar()
            _Sesion = respuesta.Sesion
            _Servicio = servicio
            DialogResult = DialogResult.OK
            Close()
        ElseIf respuesta.Resultado <> ResultadoEntrada.Cancelado Then
            MessageBox.Show(MensajeDe(respuesta, login), Titulo, MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
        End If
    End Function

    Private Function ConfirmarOtraEstacion(login As String, detalle As String) As Boolean
        Dim texto = $"El usuario {{{login}}} ya esta trabajando en la estación {{{detalle}}}.{vbCrLf}" &
                    "Se recomienda cerrar el sistema en ese equipo o ingresar con otro nombre de usuario." & vbCrLf &
                    $"¿Desea ingresar al sistema con el usuario {{{login}}}?"
        Return MessageBox.Show(Me, texto, Titulo, MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes
    End Function

    Private Function MensajeDe(respuesta As RespuestaEntrada, login As String) As String
        Select Case respuesta.Resultado
            Case ResultadoEntrada.SinConexion
                Return $"El sistema no se ha podido conectar con la base de datos ubicada en el servidor ""{txtServidor.Text}""."
            Case ResultadoEntrada.UsuarioNoExiste
                Return $"El usuario '{login}' no se encuentra autorizado en las bases de datos maestras del sistema."
            Case ResultadoEntrada.UsuarioInactivo
                Return $"Se ha denegado el acceso al usuario {login} por no estar autorizado para ingresar al sistema."
            Case ResultadoEntrada.DatosInconsistentes
                Return "Acceso denegado por inconsistencias encontrada en el usuario!!!!"
            Case ResultadoEntrada.FueraDeHorario
                Return $"Acceso denegado por restricciones en el horario del usuario.{vbCrLf}El usuario no puede iniciar sesión durante esta hora ({Date.Now:HH:mm})."
            Case ResultadoEntrada.SesionEnOtraEstacion
                Return $"El usuario {{{login}}} ya esta trabajando en la estación {{{respuesta.Detalle}}}."
            Case ResultadoEntrada.EstacionInactiva
                Return $"Este equipo {{{Environment.MachineName}}} ha sido deshabilitado para la ejecución del sistema por el Administrador del sistema."
            Case Else
                Return "Error iniciando la configuración general del sistema..."
        End Select
    End Function

    Private Shared Function IpLocal() As String
        Dim ip = Dns.GetHostAddresses(Dns.GetHostName()).FirstOrDefault(Function(a) a.AddressFamily = AddressFamily.InterNetwork AndAlso Not IPAddress.IsLoopback(a))
        Return If(ip?.ToString(), "0.0.0.0")
    End Function

    Private Sub BtnSalir_Click(sender As Object, e As EventArgs) Handles btnSalir.Click
        DialogResult = DialogResult.Cancel
        Close()
    End Sub

End Class
