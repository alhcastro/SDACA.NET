Imports System.Globalization
Imports MySqlConnector
Imports CoopeNet.Core.Pos
Imports CoopeNet.Ui

Namespace SD0502

    ''' <summary>
    ''' Conexiones remotas del POS: el servidor, catálogo y departamento de cada conexión del local. Migración de
    ''' SD0502\FrmConfigurarRemotasPOS.frm (Mantenimiento, opción 075, privilegio 5000).
    ''' </summary>
    ''' <remarks>
    ''' Diferencias con VB6: al elegir una conexión se muestran sus datos sin conectarse al servidor remoto (VB6 se
    ''' conectaba cada vez y esperaba si no respondía); los catálogos y departamentos remotos se traen con Cargar,
    ''' mientras se edita. Servidor, catálogo y departamento son obligatorios, el puerto debe ser un número y Agregar
    ''' no repite nombres. Cancelar vuelve a los datos guardados.
    ''' </remarks>
    Public Class FrmConexionesRemotas
        Inherits Form

        Private ReadOnly _repo As ConexionesRemotasRepositorio
        Private ReadOnly _local As Long
        Private _actual As ConexionRemota
        Private _editando As Boolean

        Private ReadOnly lstConexiones As New ListBox With {.Name = "lstConexiones", .AccessibleName = "Conexiones remotas", .Location = New Point(12, 32), .Size = New Size(200, 244), .IntegralHeight = False}
        Private ReadOnly txtNueva As New TextBox With {.Name = "txtNueva", .AccessibleName = "Nombre de la conexión nueva", .MaxLength = ConexionesRemotasRepositorio.LargoNombre, .CharacterCasing = CharacterCasing.Upper, .Location = New Point(12, 284), .Width = 120}
        Private ReadOnly btnAgregar As New Button With {.Name = "btnAgregar", .Text = "&Agregar", .Location = New Point(136, 282), .Size = New Size(76, 27)}

        Private ReadOnly gbDatos As New GroupBox With {.Name = "gbDatos", .Text = "Datos de la conexión", .Location = New Point(224, 8), .Size = New Size(448, 248)}
        Private ReadOnly lblConexion As New Label With {.Name = "lblConexion", .AutoSize = False, .AutoEllipsis = True, .Location = New Point(124, 26), .Size = New Size(308, 20)}
        Private ReadOnly txtServidor As New TextBox With {.Name = "txtServidor", .AccessibleName = "Servidor", .MaxLength = ConexionesRemotasRepositorio.LargoServidor, .Location = New Point(124, 54), .Width = 180}
        Private ReadOnly cmbPuerto As New ComboBox With {.Name = "cmbPuerto", .AccessibleName = "Puerto", .DropDownStyle = ComboBoxStyle.DropDown, .MaxLength = 5, .Location = New Point(124, 88), .Width = 100}
        Private ReadOnly cmbCatalogo As New ComboBox With {.Name = "cmbCatalogo", .AccessibleName = "Catálogo", .DropDownStyle = ComboBoxStyle.DropDown, .MaxLength = ConexionesRemotasRepositorio.LargoCatalogo, .Location = New Point(124, 122), .Width = 180}
        Private ReadOnly btnCargar As New Button With {.Name = "btnCargar", .Text = "Ca&rgar", .Location = New Point(312, 120), .Size = New Size(120, 27)}
        Private ReadOnly cmbDepartamento As New ComboBox With {.Name = "cmbDepartamento", .AccessibleName = "Departamento", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(124, 156), .Width = 308}
        Private ReadOnly txtCedula As New TextBox With {.Name = "txtCedula", .AccessibleName = "Cliente o proveedor", .MaxLength = ConexionesRemotasRepositorio.LargoCedula, .Location = New Point(124, 190), .Width = 140}
        Private ReadOnly lblPersona As New Label With {.Name = "lblPersona", .AutoSize = False, .AutoEllipsis = True, .Location = New Point(124, 218), .Size = New Size(308, 20)}
        Private ReadOnly lblInfo As New Label With {.Name = "lblInfo", .AutoSize = False, .Location = New Point(224, 262), .Size = New Size(448, 48)}
        Private ReadOnly btnEditar As New Button With {.Name = "btnEditar", .Text = "&Editar", .Location = New Point(360, 318), .Size = New Size(100, 30)}
        Private ReadOnly btnCancelar As New Button With {.Name = "btnCancelar", .Text = "&Cancelar", .Location = New Point(466, 318), .Size = New Size(100, 30)}
        Private ReadOnly btnSalir As New Button With {.Name = "btnSalir", .Text = "&Salir", .Location = New Point(572, 318), .Size = New Size(100, 30)}

        Public Sub New(repo As ConexionesRemotasRepositorio, local As Long)
            _repo = repo
            _local = local
            Text = "Configuración de conexiones remotas"
            ClientSize = New Size(684, 360)
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent
            CancelButton = btnSalir

            cmbPuerto.Items.Add(ConexionesRemotasRepositorio.PuertoPredeterminado.ToString(CultureInfo.InvariantCulture))
            gbDatos.Controls.AddRange(New Control() {
                Rotulo("Nombre", 26), lblConexion, Rotulo("Servidor", 57), txtServidor, Rotulo("Puerto", 91), cmbPuerto,
                Rotulo("Catálogo", 125), cmbCatalogo, btnCargar, Rotulo("Departamento", 159), cmbDepartamento,
                Rotulo("Cliente/Proveedor", 193), txtCedula, lblPersona})
            Controls.AddRange(New Control() {
                New Label With {.Text = "Conexiones remotas", .AutoSize = True, .Location = New Point(12, 12)}, lstConexiones, txtNueva, btnAgregar,
                gbDatos, lblInfo, btnEditar, btnCancelar, btnSalir})

            AddHandler lstConexiones.SelectedIndexChanged, Async Sub() Await ElegirAsync()
            AddHandler btnAgregar.Click, AddressOf Agregar_Clic
            AddHandler btnEditar.Click, Async Sub() Await EditarGuardarAsync()
            AddHandler btnCancelar.Click, Async Sub() Await CancelarAsync()
            AddHandler btnSalir.Click, Sub() Close()
            AddHandler btnCargar.Click, Async Sub() Await CargarCatalogosAsync()
            AddHandler cmbCatalogo.SelectionChangeCommitted, Async Sub() Await CargarDepartamentosAsync(TryCast(cmbCatalogo.SelectedItem, String))
            AddHandler txtCedula.KeyDown, Async Sub(s, e)
                                              If e.KeyCode <> Keys.Enter Then Return
                                              e.SuppressKeyPress = True
                                              Await BuscarPersonaAsync()
                                          End Sub
            Ayuda(txtServidor, "Indique el nombre del servidor o la dirección IP del mismo. Ejemplo: sdaca, 192.168.1.2, HPServer")
            Ayuda(cmbPuerto, "Indique el número de puerto de conexión; si no lo conoce, use el predeterminado 3306.")
            Ayuda(cmbCatalogo, "Cargar trae los catálogos de SDACA del servidor, con el usuario sdaca.")
            Ayuda(txtCedula, "Escriba la identificación y pulse Enter; si no existe, se abre el buscador.")
            Limpiar()
            Bloquear(True)
        End Sub

        Private Shared Function Rotulo(texto As String, y As Integer) As Label
            Return New Label With {.Text = texto, .AutoSize = True, .Location = New Point(12, y)}
        End Function

        Private Sub Ayuda(control As Control, texto As String)
            AddHandler control.Enter, Sub() lblInfo.Text = texto
            AddHandler control.Leave, Sub() lblInfo.Text = ""
        End Sub

        ''' <summary>Form_Load: SD_Cargar_Lista_Estaciones.</summary>
        Protected Overrides Async Sub OnLoad(e As EventArgs)
            MyBase.OnLoad(e)
            Try
                lstConexiones.Items.AddRange((Await _repo.NombresAsync(_local)).ToArray())
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
            Bloquear(True)
        End Sub

        Private ReadOnly Property NombreElegido As String
            Get
                Return TryCast(lstConexiones.SelectedItem, String)
            End Get
        End Property

        ''' <summary>SD_Limpiar.</summary>
        Private Sub Limpiar()
            _actual = Nothing
            lblConexion.Text = ""
            txtServidor.Text = ""
            cmbPuerto.Text = ConexionesRemotasRepositorio.PuertoPredeterminado.ToString(CultureInfo.InvariantCulture)
            cmbCatalogo.Items.Clear()
            cmbCatalogo.Text = ""
            cmbDepartamento.Items.Clear()
            txtCedula.Text = ""
            lblPersona.Text = ""
        End Sub

        ''' <summary>Bloquear: con pEstado los datos quedan de solo lectura y la lista activa.</summary>
        Private Sub Bloquear(bloqueado As Boolean)
            _editando = Not bloqueado
            For Each c As Control In New Control() {txtServidor, cmbPuerto, cmbCatalogo, btnCargar, cmbDepartamento, txtCedula}
                c.Enabled = Not bloqueado
            Next
            lstConexiones.Enabled = bloqueado
            txtNueva.Enabled = bloqueado
            btnAgregar.Enabled = bloqueado
            btnEditar.Text = If(bloqueado, "&Editar", "&Guardar")
            btnEditar.Enabled = Not bloqueado OrElse NombreElegido IsNot Nothing
            btnCancelar.Enabled = Not bloqueado
            btnSalir.Enabled = bloqueado
        End Sub

        ''' <summary>LstEst_Click: SD_Cargar_Estacion, sin conectarse al servidor remoto.</summary>
        Private Async Function ElegirAsync() As Task
            If _editando Then Return
            Limpiar()
            Dim nombre = NombreElegido
            If nombre Is Nothing Then
                Bloquear(True)
                Return
            End If
            Try
                Dim c = Await _repo.CargarAsync(_local, nombre)
                If nombre <> NombreElegido Then Return
                If c Is Nothing Then
                    lblConexion.Text = $"{nombre} (Sin definir)"
                    lblInfo.Text = "Las propiedades de esta conexión no han sido configuradas. Haga clic en 'Editar' para definirlas."
                Else
                    Mostrar(c)
                    lblInfo.Text = "Haga clic en 'Editar' para cambiar las propiedades de esta conexión."
                    lblPersona.Text = If(c.Cedula = "", "", Await _repo.NombrePersonaAsync(c.Cedula))
                End If
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
            Bloquear(True)
        End Function

        Private Sub Mostrar(c As ConexionRemota)
            _actual = c
            lblConexion.Text = c.Nombre
            txtServidor.Text = c.Servidor
            cmbPuerto.Text = c.Puerto.ToString(CultureInfo.InvariantCulture)
            cmbCatalogo.Items.Clear()
            cmbCatalogo.Text = c.Catalogo
            cmbDepartamento.Items.Clear()
            If c.Departamento <> 0 Then
                cmbDepartamento.Items.Add(New DepartamentoRemoto With {.Local = c.Departamento, .Descripcion = "(sin cargar del servidor)"})
                cmbDepartamento.SelectedIndex = 0
            End If
            txtCedula.Text = c.Cedula
        End Sub

        ''' <summary>cmdAgregar: un nombre nuevo en la lista, que se configura con Editar.</summary>
        Private Sub Agregar_Clic(sender As Object, e As EventArgs)
            Dim nombre = ConexionesRemotasRepositorio.Recortar(txtNueva.Text, ConexionesRemotasRepositorio.LargoNombre)
            If nombre = "" Then Return
            Dim existente = lstConexiones.Items.Cast(Of String)().ToList().FindIndex(Function(n) String.Equals(n, nombre, StringComparison.OrdinalIgnoreCase))
            If existente < 0 Then existente = lstConexiones.Items.Add(nombre)
            lstConexiones.SelectedIndex = existente
            txtNueva.Text = ""
        End Sub

        ''' <summary>CmdGuaEdi.</summary>
        Private Async Function EditarGuardarAsync() As Task
            If Not _editando Then
                If NombreElegido Is Nothing Then Return
                Bloquear(False)
                txtServidor.Focus()
                Return
            End If
            Dim puerto As Integer
            If txtServidor.Text.Trim() = "" Then
                txtServidor.Focus()
                Return
            End If
            If Not Integer.TryParse(cmbPuerto.Text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, puerto) OrElse puerto < 1 OrElse puerto > 65535 Then
                cmbPuerto.Focus()
                Return
            End If
            If cmbCatalogo.Text.Trim() = "" Then
                cmbCatalogo.Focus()
                Return
            End If
            Dim departamento = TryCast(cmbDepartamento.SelectedItem, DepartamentoRemoto)
            If departamento Is Nothing Then
                MessageBox.Show(Me, "Elija el departamento: use Cargar para traerlos del servidor.", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
                btnCargar.Focus()
                Return
            End If
            Dim c As New ConexionRemota With {
                .Nombre = NombreElegido, .Servidor = txtServidor.Text, .Puerto = puerto, .Catalogo = cmbCatalogo.Text,
                .Cedula = txtCedula.Text, .Departamento = departamento.Local}
            Try
                Await _repo.GuardarAsync(_local, c)
                MessageBox.Show(Me, $"Se ha guardado la configuración de la conexión '{c.Nombre}'.", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
                Bloquear(True)
                Await ElegirAsync()
            Catch ex As MySqlException
                Mensajes.NoGuardo(ex.Message, "Conexiones remotas", Modulo.TituloModulo)
            End Try
        End Function

        ''' <summary>CmdCan: vuelve a lo guardado.</summary>
        Private Async Function CancelarAsync() As Task
            Bloquear(True)
            Await ElegirAsync()
        End Function

        ''' <summary>CmdCat: los catálogos de SDACA del servidor, y los departamentos del elegido.</summary>
        Private Async Function CargarCatalogosAsync() As Task
            Dim servidor = txtServidor.Text.Trim()
            Dim puerto As Integer
            If servidor = "" OrElse Not Integer.TryParse(cmbPuerto.Text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, puerto) Then Return
            Dim guardado = cmbCatalogo.Text.Trim()
            Cursor = Cursors.WaitCursor
            btnCargar.Enabled = False
            Try
                Dim catalogos = Await _repo.CatalogosRemotosAsync(servidor, puerto)
                cmbCatalogo.Items.Clear()
                cmbCatalogo.Items.AddRange(catalogos.ToArray())
                If catalogos.Count = 0 Then
                    MessageBox.Show(Me, $"No se encuentran catálogos del sistema en {servidor}:{puerto}.", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                    Return
                End If
                Dim i = catalogos.FindIndex(Function(x) String.Equals(x, guardado, StringComparison.OrdinalIgnoreCase))
                cmbCatalogo.SelectedIndex = Math.Max(0, i)
                Await CargarDepartamentosAsync(TryCast(cmbCatalogo.SelectedItem, String))
            Catch ex As Exception When TypeOf ex Is MySqlException OrElse TypeOf ex Is TimeoutException OrElse TypeOf ex Is InvalidOperationException
                MessageBox.Show(Me, $"Error al efectuar la conexión a la base de datos en {servidor}:{puerto}.{Environment.NewLine}{ex.Message}", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            Finally
                btnCargar.Enabled = _editando
                Cursor = Cursors.Default
            End Try
        End Function

        ''' <summary>CmbCat_Click: los departamentos con módulos del catálogo remoto; queda elegido el guardado.</summary>
        Private Async Function CargarDepartamentosAsync(catalogo As String) As Task
            If String.IsNullOrEmpty(catalogo) Then Return
            Dim guardado = If(TryCast(cmbDepartamento.SelectedItem, DepartamentoRemoto)?.Local, _actual?.Departamento)
            Dim puerto As Integer
            Integer.TryParse(cmbPuerto.Text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, puerto)
            Try
                Dim departamentos = Await _repo.DepartamentosRemotosAsync(txtServidor.Text.Trim(), puerto, catalogo)
                cmbDepartamento.Items.Clear()
                cmbDepartamento.Items.AddRange(departamentos.ToArray())
                Dim i = departamentos.FindIndex(Function(d) guardado.HasValue AndAlso d.Local = guardado.Value)
                If cmbDepartamento.Items.Count > 0 Then cmbDepartamento.SelectedIndex = Math.Max(0, i)
            Catch ex As Exception When TypeOf ex Is MySqlException OrElse TypeOf ex Is TimeoutException OrElse TypeOf ex Is InvalidOperationException
                MessageBox.Show(Me, $"No se pudieron leer los departamentos de {catalogo}.{Environment.NewLine}{ex.Message}", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            End Try
        End Function

        ''' <summary>sdtID_KeyDown: la persona por identificación, o el buscador.</summary>
        Private Async Function BuscarPersonaAsync() As Task
            Try
                Dim id = txtCedula.Text.Trim()
                Dim nombre = If(id = "", "", Await _repo.NombrePersonaAsync(id))
                If nombre <> "" Then
                    lblPersona.Text = nombre
                    Return
                End If
                Using datos = Await _repo.PersonasAsync()
                    Using buscador As New FrmBuscador("Seleccione el cliente o proveedor", datos)
                        If buscador.ShowDialog(Me) = DialogResult.OK Then
                            txtCedula.Text = buscador.Resultado(1)
                            lblPersona.Text = buscador.Resultado(0)
                        End If
                    End Using
                End Using
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Function

    End Class

End Namespace
