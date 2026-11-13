Imports System.Data
Imports System.Globalization
Imports MySqlConnector
Imports CoopeNet.Core
Imports CoopeNet.Core.Cheques
Imports CoopeNet.Core.Pos
Imports CoopeNet.Ui

Namespace SD0502

    ''' <summary>
    ''' Proveedores: registra a una persona o empresa como proveedor del local y mantiene sus cuentas de pago
    ''' (forma de pago, días de visita y de pago, contactos, observaciones y cuenta contable). Migración de
    ''' SD0502\FrmMantProveedores.frm (Mantenimiento, opción 016, privilegio 5022).
    ''' </summary>
    ''' <remarks>
    ''' Las cuentas se crean, editan o marcan para borrar en la lista y se escriben todas con Guardar, como en
    ''' VB6. Diferencias: no se ofrece el local "G - Global" (en VB6 escribe -1 en una columna sin signo y no
    ''' encuentra nada); con pago por cheque se guarda la cuenta bancaria elegida (VB6 la cortaba en un "-" que
    ''' la lista no tiene y guardaba vacío); el código libre se calcula sin crear tablas en la base; solo se
    ''' actualizan las cuentas que cambiaron.
    ''' </remarks>
    Public Class FrmProveedores
        Inherits Form

        Private Shared ReadOnly Cultura As CultureInfo = CultureInfo.CurrentCulture
        Private Shared ReadOnly NombresDias As String() = {"Domingo", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado"}

        Private ReadOnly _repo As ProveedoresRepositorio
        Private ReadOnly _formatoCuentas As Func(Of Task(Of FormatoCuenta))
        Private ReadOnly _localSesion As Long
        Private ReadOnly _uid As Long
        Private ReadOnly _cedula As String
        Private ReadOnly _exigeContable As Boolean
        Private _formato As FormatoCuenta = FormatoCuenta.Predeterminado
        Private _cuentas As New List(Of CuentaProveedor)
        ''' <summary>La cuenta que se está creando o editando; Nothing fuera de esa edición.</summary>
        Private _editada As CuentaProveedor
        Private _ultimo As String = ""
        Private _editandoProveedor As Boolean
        Private _nuevoProveedor As Boolean
        Private _mostrando As Boolean

        ''' <summary>Abre Datos personales para registrar a una persona que no existe (DP_Agregar_Return de VB6).</summary>
        Public Property AbrirDatosPersonales As Func(Of Form)

        ' ---- Encabezado ----
        Private ReadOnly cmbLocal As New ComboBox With {.Name = "cmbLocal", .AccessibleName = "Departamento", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(110, 10), .Width = 640}
        Private ReadOnly tabsProveedor As New Pestanas With {.Name = "tabsProveedor", .Location = New Point(10, 42), .Size = New Size(740, 142)}
        Private ReadOnly tabProveedor As New TabPage("Proveedor") With {.Name = "tabProveedor"}
        Private ReadOnly tabLocalizacion As New TabPage("Localización") With {.Name = "tabLocalizacion"}
        Private ReadOnly txtId As New TextBox With {.Name = "txtId", .AccessibleName = "Identificación del proveedor", .MaxLength = 16, .Location = New Point(110, 15), .Width = 170}
        Private ReadOnly btnPersona As New Button With {.Name = "btnPersona", .Text = "...", .AccessibleName = "Buscar persona", .Location = New Point(284, 14), .Size = New Size(32, 26)}
        Private ReadOnly txtNombre As New TextBox With {.Name = "txtNombre", .AccessibleName = "Nombre del proveedor", .ReadOnly = True, .TabStop = False, .Location = New Point(110, 50), .Width = 610}
        Private ReadOnly lblProvincia As New Label With {.Name = "lblProvincia", .AutoSize = True, .Location = New Point(90, 8)}
        Private ReadOnly lblCanton As New Label With {.Name = "lblCanton", .AutoSize = True, .Location = New Point(90, 34)}
        Private ReadOnly lblDistrito As New Label With {.Name = "lblDistrito", .AutoSize = True, .Location = New Point(90, 60)}
        Private ReadOnly lblSector As New Label With {.Name = "lblSector", .AutoSize = True, .Location = New Point(90, 86)}
        Private ReadOnly gridTelefonos As New DataGridView With {.Name = "gridTelefonos", .AccessibleName = "Teléfonos y correos", .Location = New Point(370, 6), .Size = New Size(355, 100),
            .ReadOnly = True, .AllowUserToAddRows = False, .AllowUserToDeleteRows = False, .RowHeadersVisible = False, .TabStop = False,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, .SelectionMode = DataGridViewSelectionMode.FullRowSelect}

        ' ---- Cuentas: botones y lista ----
        Private ReadOnly btnCrear As New Button With {.Name = "btnCrear", .Text = "C&rear", .Location = New Point(10, 190), .Size = New Size(75, 28)}
        Private ReadOnly btnEditarCuenta As New Button With {.Name = "btnEditarCuenta", .Text = "&Editar", .Location = New Point(88, 190), .Size = New Size(75, 28)}
        Private ReadOnly btnAplicar As New Button With {.Name = "btnAplicar", .Text = "&Aplicar", .Location = New Point(166, 190), .Size = New Size(75, 28)}
        Private ReadOnly btnBorrarCuenta As New Button With {.Name = "btnBorrarCuenta", .Text = "&Borrar", .Location = New Point(244, 190), .Size = New Size(75, 28)}
        Private ReadOnly txtCodigo As New TextBox With {.Name = "txtCodigo", .AccessibleName = "Código de la cuenta", .MaxLength = 16, .Location = New Point(470, 192), .Width = 70}
        Private ReadOnly txtDetalle As New TextBox With {.Name = "txtDetalle", .AccessibleName = "Detalle de la cuenta", .MaxLength = 64, .Location = New Point(544, 192), .Width = 206}
        Private ReadOnly gridCuentas As New DataGridView With {.Name = "gridCuentas", .AccessibleName = "Cuentas del proveedor", .Location = New Point(10, 226), .Size = New Size(250, 332),
            .ReadOnly = True, .AllowUserToAddRows = False, .AllowUserToDeleteRows = False, .RowHeadersVisible = False, .MultiSelect = False,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, .SelectionMode = DataGridViewSelectionMode.FullRowSelect}
        Private ReadOnly colCodigo As New DataGridViewTextBoxColumn With {.Name = "colCodigo", .HeaderText = "Código", .FillWeight = 35}
        Private ReadOnly colDetalle As New DataGridViewTextBoxColumn With {.Name = "colDetalle", .HeaderText = "Detalle", .FillWeight = 65}
        Private ReadOnly tabsCuenta As New Pestanas With {.Name = "tabsCuenta", .Padding = New Point(8, 5), .Location = New Point(266, 226), .Size = New Size(484, 332)}
        Private ReadOnly tabCuenta As New TabPage("Cuenta") With {.Name = "tabCuenta"}
        Private ReadOnly tabVisita As New TabPage("Días visita") With {.Name = "tabVisita"}
        Private ReadOnly tabContactos As New TabPage("Contactos") With {.Name = "tabContactos"}
        Private ReadOnly tabObservaciones As New TabPage("Obser.") With {.Name = "tabObservaciones"}
        Private ReadOnly tabContable As New TabPage("Contable") With {.Name = "tabContable"}

        ' ---- Pestaña Cuenta ----
        Private ReadOnly cmbTipoPago As New ComboBox With {.Name = "cmbTipoPago", .AccessibleName = "Tipo de pago", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(140, 10), .Width = 320}
        Private ReadOnly cmbBanco As New ComboBox With {.Name = "cmbBanco", .AccessibleName = "Banco", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(140, 42), .Width = 320}
        Private ReadOnly cmbMoneda As New ComboBox With {.Name = "cmbMoneda", .AccessibleName = "Moneda de pago", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(140, 74), .Width = 320}
        Private ReadOnly cmbCuentaBanco As New ComboBox With {.Name = "cmbCuentaBanco", .AccessibleName = "Cuenta bancaria", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(140, 106), .Width = 320}
        Private ReadOnly txtCuenta As New TextBox With {.Name = "txtCuenta", .AccessibleName = "Número de cuenta", .MaxLength = 16, .Location = New Point(140, 106), .Width = 320}
        Private ReadOnly cmbConfirmar As New ComboBox With {.Name = "cmbConfirmar", .AccessibleName = "Confirmar a", .DropDownStyle = ComboBoxStyle.DropDown, .MaxLength = 16, .Location = New Point(140, 138), .Width = 320}
        Private ReadOnly txtCredito As New TextBox With {.Name = "txtCredito", .AccessibleName = "Límite de crédito", .TextAlign = HorizontalAlignment.Right, .Location = New Point(140, 170), .Width = 110}
        Private ReadOnly txtPlazo As New TextBox With {.Name = "txtPlazo", .AccessibleName = "Días de plazo", .TextAlign = HorizontalAlignment.Right, .Location = New Point(340, 170), .Width = 80}
        Private ReadOnly cmbCategoria As New ComboBox With {.Name = "cmbCategoria", .AccessibleName = "Categoría", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(140, 202), .Width = 70}
        Private ReadOnly cmbDevoluciones As New ComboBox With {.Name = "cmbDevoluciones", .AccessibleName = "Acepta devoluciones", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(350, 202), .Width = 70}
        Private ReadOnly txtPromocional As New TextBox With {.Name = "txtPromocional", .AccessibleName = "Monto promocional", .TextAlign = HorizontalAlignment.Right, .Location = New Point(140, 234), .Width = 110}
        Private ReadOnly txtUltimoPago As New TextBox With {.Name = "txtUltimoPago", .AccessibleName = "Fecha del último pago", .ReadOnly = True, .TabStop = False, .Location = New Point(140, 266), .Width = 320}

        ' ---- Pestaña Días visita ----
        Private ReadOnly txtVisita As New TextBox With {.Name = "txtVisita", .AccessibleName = "Periodicidad de visita en días", .TextAlign = HorizontalAlignment.Right, .Location = New Point(12, 36), .Width = 70}
        Private ReadOnly txtEntrega As New TextBox With {.Name = "txtEntrega", .AccessibleName = "Duración de entrega en días", .TextAlign = HorizontalAlignment.Right, .Location = New Point(12, 100), .Width = 70}
        Private ReadOnly gbDiasPago As New GroupBox With {.Name = "gbDiasPago", .Text = "Días de pago", .Location = New Point(250, 8), .Size = New Size(210, 200)}
        Private ReadOnly lstDiasPago As New CheckedListBox With {.Name = "lstDiasPago", .AccessibleName = "Días de pago", .CheckOnClick = True, .Dock = DockStyle.Fill, .IntegralHeight = False}

        ' ---- Pestaña Contactos ----
        Private ReadOnly txtContactoId As TextBox() = New TextBox(2) {}
        Private ReadOnly txtContactoNombre As TextBox() = New TextBox(2) {}
        Private ReadOnly btnContactoBuscar As Button() = New Button(2) {}
        Private ReadOnly btnContactoQuitar As Button() = New Button(2) {}

        ' ---- Pestaña Observaciones ----
        Private ReadOnly txtObservaciones As New TextBox With {.Name = "txtObservaciones", .AccessibleName = "Observaciones", .Multiline = True, .AcceptsReturn = True, .ScrollBars = ScrollBars.Vertical, .MaxLength = 255, .Dock = DockStyle.Fill}

        ' ---- Pestaña Contable ----
        Private ReadOnly txtContable As New TextBox With {.Name = "txtContable", .AccessibleName = "Cuenta contable", .Location = New Point(140, 15), .Width = 180}
        Private ReadOnly btnContable As New Button With {.Name = "btnContable", .Text = "Buscar", .Location = New Point(326, 14), .Size = New Size(80, 26)}
        Private ReadOnly txtDescContable As New TextBox With {.Name = "txtDescContable", .AccessibleName = "Descripción de la cuenta contable", .ReadOnly = True, .TabStop = False, .Location = New Point(12, 74), .Width = 450}
        ''' <summary>Código completo de la cuenta contable elegida (TxtCContable.Tag de VB6).</summary>
        Private _codigoContable As String = ""

        ' ---- Navegación y barra ----
        Private ReadOnly btnPrimero As New Button With {.Name = "btnPrimero", .Text = "<<", .AccessibleName = "Primer proveedor", .Location = New Point(10, 566), .Size = New Size(42, 32)}
        Private ReadOnly btnAnterior As New Button With {.Name = "btnAnterior", .Text = "<", .AccessibleName = "Proveedor anterior", .Location = New Point(53, 566), .Size = New Size(42, 32)}
        Private ReadOnly btnSiguiente As New Button With {.Name = "btnSiguiente", .Text = ">", .AccessibleName = "Proveedor siguiente", .Location = New Point(96, 566), .Size = New Size(42, 32)}
        Private ReadOnly btnUltimo As New Button With {.Name = "btnUltimo", .Text = ">>", .AccessibleName = "Último proveedor", .Location = New Point(139, 566), .Size = New Size(42, 32)}
        Private ReadOnly barra As New BarraComandos With {.Name = "barra", .Location = New Point(190, 566), .Size = New Size(560, 32)}

        Public Sub New(repo As ProveedoresRepositorio, formatoCuentas As Func(Of Task(Of FormatoCuenta)), localSesion As Long, uid As Long, cedula As String, exigeContable As Boolean)
            _repo = repo
            _formatoCuentas = formatoCuentas
            _localSesion = localSesion
            _uid = uid
            _cedula = cedula
            _exigeContable = exigeContable
            Text = "Mantenimiento de Proveedores"
            ClientSize = New Size(760, 608)
            MinimumSize = New Size(760, 608)
            MinimizeBox = False
            ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent

            tabProveedor.Controls.AddRange(New Control() {Rotulo("Identificación", 12, 18), txtId, btnPersona, Rotulo("Descripción", 12, 53), txtNombre})
            tabLocalizacion.Controls.AddRange(New Control() {
                Rotulo("Provincia", 12, 8), lblProvincia, Rotulo("Cantón", 12, 34), lblCanton,
                Rotulo("Distrito", 12, 60), lblDistrito, Rotulo("Sector", 12, 86), lblSector, gridTelefonos})
            gridTelefonos.Columns.Add("colTipo", "Tipo")
            gridTelefonos.Columns.Add("colDato", "Dato")
            tabsProveedor.TabPages.AddRange({tabProveedor, tabLocalizacion})

            gridCuentas.Columns.AddRange(colCodigo, colDetalle)

            cmbCategoria.Items.AddRange({"", "A", "B", "C"})
            cmbCategoria.Enabled = False ' Locked en VB6: se ve, no se cambia.
            cmbDevoluciones.Items.AddRange({"No", "Sí"})
            cmbConfirmar.Items.AddRange({"FAX:", "TELEFONO:", "E-MAIL:", "OTRO:"})
            tabCuenta.Controls.AddRange(New Control() {
                Rotulo("Tipo de pago", 8, 13), cmbTipoPago, Rotulo("Banco", 8, 45), cmbBanco, Rotulo("Moneda de pago", 8, 77), cmbMoneda,
                Rotulo("Nº cuenta", 8, 109), cmbCuentaBanco, txtCuenta, Rotulo("Confirmar a:", 8, 141), cmbConfirmar,
                Rotulo("Límite de crédito", 8, 173), txtCredito, Rotulo("Días plazo", 262, 173), txtPlazo,
                Rotulo("Categoría", 8, 205), cmbCategoria, Rotulo("Acepta devoluciones", 220, 205), cmbDevoluciones,
                Rotulo("Monto promocional", 8, 237), txtPromocional, Rotulo("Último pago", 8, 269), txtUltimoPago})
            txtCuenta.Visible = False

            lstDiasPago.Items.AddRange(NombresDias)
            gbDiasPago.Controls.Add(lstDiasPago)
            tabVisita.Controls.AddRange(New Control() {
                Rotulo("Periodicidad de visita", 12, 14), txtVisita, Rotulo("días", 88, 39),
                Rotulo("Duración de entrega", 12, 78), txtEntrega, Rotulo("días", 88, 103), gbDiasPago})

            Dim titulos = {"Encargado de compras", "Agente de ventas", "Repartidor"}
            For i = 0 To 2
                Dim indice = i
                txtContactoId(i) = New TextBox With {.Name = $"txtContactoId{i}", .AccessibleName = $"Identificación: {titulos(i)}", .MaxLength = 16, .Location = New Point(10, 24), .Width = 160}
                txtContactoNombre(i) = New TextBox With {.Name = $"txtContactoNombre{i}", .AccessibleName = $"Nombre: {titulos(i)}", .ReadOnly = True, .TabStop = False, .Location = New Point(10, 56), .Width = 440}
                btnContactoBuscar(i) = New Button With {.Name = $"btnContactoBuscar{i}", .Text = "Buscar", .Location = New Point(285, 23), .Size = New Size(80, 26)}
                btnContactoQuitar(i) = New Button With {.Name = $"btnContactoQuitar{i}", .Text = "Quitar", .Location = New Point(370, 23), .Size = New Size(80, 26)}
                Dim grupo As New GroupBox With {.Name = $"gbContacto{i}", .Text = titulos(i), .Location = New Point(6, 4 + i * 98), .Size = New Size(462, 92)}
                grupo.Controls.AddRange(New Control() {txtContactoId(i), btnContactoBuscar(i), btnContactoQuitar(i), txtContactoNombre(i)})
                tabContactos.Controls.Add(grupo)
                AddHandler btnContactoBuscar(i).Click, Async Sub() Await BuscarContactoAsync(indice)
                AddHandler btnContactoQuitar(i).Click, Sub()
                                                           txtContactoId(indice).Text = ""
                                                           txtContactoNombre(indice).Text = ""
                                                       End Sub
                AddHandler txtContactoId(i).Leave, Async Sub() Await NombreContactoAsync(indice)
            Next

            tabObservaciones.Controls.Add(txtObservaciones)
            tabContable.Controls.AddRange(New Control() {Rotulo("Cuenta contable", 12, 18), txtContable, btnContable, Rotulo("Descripción", 12, 52), txtDescContable})
            tabsCuenta.TabPages.AddRange({tabCuenta, tabVisita, tabContactos, tabObservaciones, tabContable})

            Controls.AddRange(New Control() {
                Rotulo("Departamento", 10, 13), cmbLocal, tabsProveedor,
                btnCrear, btnEditarCuenta, btnAplicar, btnBorrarCuenta, Rotulo("Detalle de la cuenta", 330, 195), txtCodigo, txtDetalle,
                gridCuentas, tabsCuenta, btnPrimero, btnAnterior, btnSiguiente, btnUltimo, barra})

            ' Anclajes: la lista y las pestañas de la cuenta crecen con la ventana.
            For Each c As Control In New Control() {cmbLocal, tabsProveedor, txtDetalle}
                c.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
            Next
            gridCuentas.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left
            tabsCuenta.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
            gridTelefonos.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
            txtNombre.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
            For Each c As Control In {btnPrimero, btnAnterior, btnSiguiente, btnUltimo}
                c.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
            Next
            barra.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right

            barra.AlHacerClic = AddressOf Barra_Clic
            barra.Estado(BotonBarra.Borrar, False) ' VB6 no atiende Borrar de la barra.
            AddHandler cmbLocal.SelectionChangeCommitted, Async Sub() Await LocalElegidoAsync()
            AddHandler btnPrimero.Click, Async Sub() Await NavegarAsync(DireccionNavegacion.Primero)
            AddHandler btnAnterior.Click, Async Sub() Await NavegarAsync(DireccionNavegacion.Anterior)
            AddHandler btnSiguiente.Click, Async Sub() Await NavegarAsync(DireccionNavegacion.Siguiente)
            AddHandler btnUltimo.Click, Async Sub() Await NavegarAsync(DireccionNavegacion.Ultimo)
            AddHandler btnPersona.Click, Async Sub() Await BuscarPersonaAsync()
            AddHandler txtId.KeyDown, Async Sub(s, e)
                                          If e.KeyCode <> Keys.Enter Then Return
                                          e.SuppressKeyPress = True
                                          If txtId.Text.Trim().Length = 0 Then
                                              Await BuscarPersonaAsync()
                                          Else
                                              Await PersonaEscritaAsync(txtId.Text.Trim())
                                          End If
                                      End Sub
            AddHandler gridCuentas.SelectionChanged, AddressOf Cuentas_Seleccion
            AddHandler btnCrear.Click, Async Sub() Await CrearCuentaAsync()
            AddHandler btnEditarCuenta.Click, AddressOf EditarCuenta_Clic
            AddHandler btnAplicar.Click, Async Sub() Await AplicarCuentaAsync()
            AddHandler btnBorrarCuenta.Click, AddressOf BorrarCuenta_Clic
            AddHandler cmbTipoPago.SelectedIndexChanged, Sub() TipoPagoCambiado()
            AddHandler cmbBanco.SelectionChangeCommitted, Async Sub() Await CargarCuentasBancariasAsync("")
            AddHandler cmbMoneda.SelectionChangeCommitted, Async Sub() Await CargarCuentasBancariasAsync("")
            AddHandler txtDetalle.KeyDown, Sub(s, e)
                                               If e.KeyCode <> Keys.Enter Then Return
                                               e.SuppressKeyPress = True
                                               tabsCuenta.SelectedTab = tabCuenta
                                               cmbTipoPago.Focus()
                                           End Sub
            AddHandler txtContable.KeyDown, Async Sub(s, e)
                                                If e.KeyCode <> Keys.Enter Then Return
                                                e.SuppressKeyPress = True
                                                If txtContable.Text.Trim().Length = 0 Then
                                                    Await BuscarContableAsync()
                                                Else
                                                    Await ValidarContableAsync()
                                                End If
                                            End Sub
            AddHandler txtContable.Leave, Async Sub() Await ValidarContableAsync()
            AddHandler btnContable.Click, Async Sub() Await BuscarContableAsync()
            For Each caja In {txtVisita, txtEntrega, txtPlazo}
                AddHandler caja.KeyPress, AddressOf SoloDigitos
            Next
            For Each caja In {txtCredito, txtPromocional}
                AddHandler caja.KeyPress, AddressOf SoloNumero
            Next

            LimpiarProveedor()
            LimpiarCuenta()
            Estados()
            Ventanas.Recordar(Me)
        End Sub

        Private Shared Function Rotulo(texto As String, x As Integer, y As Integer) As Label
            Return New Label With {.Text = texto, .AutoSize = True, .Location = New Point(x, y)}
        End Function

        Private Shared Sub SoloDigitos(sender As Object, e As KeyPressEventArgs)
            If Not Char.IsControl(e.KeyChar) AndAlso Not Char.IsDigit(e.KeyChar) Then e.Handled = True
        End Sub

        Private Sub SoloNumero(sender As Object, e As KeyPressEventArgs)
            If Char.IsControl(e.KeyChar) OrElse Char.IsDigit(e.KeyChar) Then Return
            Dim caja = CType(sender, TextBox)
            If e.KeyChar.ToString() = Cultura.NumberFormat.NumberDecimalSeparator AndAlso Not caja.Text.Contains(e.KeyChar) Then Return
            e.Handled = True
        End Sub

        Protected Overrides Async Sub OnLoad(e As EventArgs)
            MyBase.OnLoad(e)
            Try
                Dim catalogos = Await _repo.CatalogosAsync()
                cmbLocal.Items.AddRange(catalogos.Locales.ToArray())
                cmbTipoPago.Items.AddRange(catalogos.TiposPago.ToArray())
                cmbBanco.Items.AddRange(catalogos.Bancos.ToArray())
                cmbMoneda.Items.AddRange(catalogos.Monedas.ToArray())
                _formato = Await _formatoCuentas()
                Elegir(cmbLocal, _localSesion.ToString(CultureInfo.InvariantCulture))
                LimpiarCuenta()
                Await NavegarAsync(DireccionNavegacion.Primero)
            Catch ex As MySqlException
                Mensajes.NoConecta()
                barra.Enabled = False
            End Try
        End Sub

        ' ---- Utilidades de combos ----

        Private Shared Sub Elegir(combo As ComboBox, codigo As String)
            combo.SelectedIndex = -1
            For i = 0 To combo.Items.Count - 1
                Dim opcion = TryCast(combo.Items(i), OpcionProveedor)
                If opcion IsNot Nothing AndAlso opcion.Codigo.Trim() = codigo.Trim() Then
                    combo.SelectedIndex = i
                    Return
                End If
            Next
            If combo.Items.Count > 0 Then combo.SelectedIndex = 0
        End Sub

        Private Shared Function Codigo(combo As ComboBox) As String
            Return If(TryCast(combo.SelectedItem, OpcionProveedor)?.Codigo, "")
        End Function

        Private Shared Function Entero(combo As ComboBox) As Integer
            Return CInt(Vb6.Val(Codigo(combo)))
        End Function

        Private ReadOnly Property LocalElegido As Long
            Get
                Return Vb6.Val(Codigo(cmbLocal))
            End Get
        End Property

        ' ---- Proveedor ----

        ''' <summary>PLimpiar.</summary>
        Private Sub LimpiarProveedor()
            txtId.Text = ""
            txtNombre.Text = ""
            MostrarUbicacion(Nothing, New List(Of ContactoProveedor)())
            _cuentas = New List(Of CuentaProveedor)()
            LlenarLista()
        End Sub

        Private Sub MostrarUbicacion(p As PersonaProveedor, telefonos As List(Of ContactoProveedor))
            lblProvincia.Text = If(p?.Provincia, "")
            lblCanton.Text = If(p?.Canton, "")
            lblDistrito.Text = If(p?.Distrito, "")
            lblSector.Text = If(p?.Sector, "")
            gridTelefonos.Rows.Clear()
            For Each t In telefonos
                gridTelefonos.Rows.Add(t.Tipo, t.Dato)
            Next
        End Sub

        ''' <summary>CargarDatosDP: muestra la persona. Si no existe, ofrece registrarla en Datos personales.</summary>
        Private Async Function CargarPersonaAsync(id As String, preguntar As Boolean) As Task(Of Boolean)
            Dim p = Await _repo.PersonaAsync(id)
            If p Is Nothing Then
                txtNombre.Text = ""
                MostrarUbicacion(Nothing, New List(Of ContactoProveedor)())
                If Not preguntar OrElse AbrirDatosPersonales Is Nothing Then Return False
                If MessageBox.Show(Me, $"La persona/Empresa que esta buscando con la identificación {id} no esta registrada..." & vbCrLf & "¿Desea agregarla?",
                                   Modulo.TituloModulo, MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Return False
                Using ventana = AbrirDatosPersonales.Invoke()
                    Tema.Aplicar(ventana, Modulo.TituloModulo)
                    ventana.ShowDialog(Me)
                End Using
                p = Await _repo.PersonaAsync(id)
                If p Is Nothing Then
                    MessageBox.Show(Me, "La persona o empresa no se encuentra registrada en datos personales...", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Return False
                End If
            End If
            txtId.Text = p.Id
            txtNombre.Text = p.Nombre
            MostrarUbicacion(p, Await _repo.ContactosAsync(p.Id))
            Return True
        End Function

        ''' <summary>Carga la persona y sus cuentas en el local elegido.</summary>
        Private Async Function MostrarProveedorAsync(id As String) As Task
            If Not Await CargarPersonaAsync(id, False) Then txtId.Text = id
            _ultimo = id
            Await CargarCuentasAsync()
        End Function

        ''' <summary>CXP_Cargar_CuentasProv.</summary>
        Private Async Function CargarCuentasAsync() As Task
            _cuentas = If(txtId.Text.Trim().Length = 0, New List(Of CuentaProveedor)(), Await _repo.CuentasAsync(LocalElegido, txtId.Text.Trim()))
            LlenarLista()
        End Function

        Private Sub LlenarLista()
            _mostrando = True
            gridCuentas.Rows.Clear()
            For Each c In _cuentas
                Dim i = gridCuentas.Rows.Add(c.Codigo, c.Descripcion)
                gridCuentas.Rows(i).Tag = c
                PintarFila(gridCuentas.Rows(i))
            Next
            _mostrando = False
            If gridCuentas.Rows.Count > 0 Then
                gridCuentas.Rows(0).Selected = True
                gridCuentas.CurrentCell = gridCuentas.Rows(0).Cells(0)
            End If
            Cuentas_Seleccion(Nothing, EventArgs.Empty)
        End Sub

        ''' <summary>Una cuenta marcada para borrar se ve en rojo suave, tachada en gris.</summary>
        Private Shared Sub PintarFila(fila As DataGridViewRow)
            Dim c = CType(fila.Tag, CuentaProveedor)
            fila.DefaultCellStyle.BackColor = If(c.Borrar, Tema.PeligroSuave, Color.Empty)
            fila.DefaultCellStyle.ForeColor = If(c.Borrar, Tema.TextoTenue, Color.Empty)
            fila.DefaultCellStyle.SelectionBackColor = If(c.Borrar, Tema.Peligro, Color.Empty)
            fila.DefaultCellStyle.SelectionForeColor = If(c.Borrar, Color.White, Color.Empty)
        End Sub

        Private Function CuentaActual() As CuentaProveedor
            Return TryCast(gridCuentas.CurrentRow?.Tag, CuentaProveedor)
        End Function

        Private Async Function NavegarAsync(direccion As DireccionNavegacion) As Task
            Try
                Dim id = Await _repo.VecinoAsync(LocalElegido, txtId.Text, direccion)
                If id.Length = 0 Then
                    If direccion = DireccionNavegacion.Primero OrElse direccion = DireccionNavegacion.Ultimo Then
                        LimpiarProveedor()
                        _ultimo = ""
                    Else
                        System.Media.SystemSounds.Beep.Play()
                    End If
                    Return
                End If
                Await MostrarProveedorAsync(id)
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Function

        ''' <summary>CmbLocal_Click: otro local, su primer proveedor.</summary>
        Private Async Function LocalElegidoAsync() As Task
            LimpiarProveedor()
            LimpiarCuenta()
            Await NavegarAsync(DireccionNavegacion.Primero)
        End Function

        ''' <summary>CmdBuscarDP_Click: elige la persona que se registra como proveedor.</summary>
        Private Async Function BuscarPersonaAsync() As Task
            Try
                Using datos = Await _repo.PersonasAsync()
                    Using buscador As New FrmBuscador("Seleccione la persona o empresa", datos)
                        If buscador.ShowDialog(Me) <> DialogResult.OK OrElse buscador.Resultado(1).Length = 0 Then Return
                        Await PersonaEscritaAsync(buscador.Resultado(1))
                    End Using
                End Using
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Function

        ''' <summary>Con Nuevo: la persona elegida; si ya es proveedor del local, se cargan sus cuentas.</summary>
        Private Async Function PersonaEscritaAsync(id As String) As Task
            Try
                If Not Await CargarPersonaAsync(id, True) Then Return
                If Await _repo.EsProveedorAsync(LocalElegido, txtId.Text) Then
                    Await CargarCuentasAsync()
                Else
                    _cuentas = New List(Of CuentaProveedor)()
                    LlenarLista()
                End If
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Function

        ' ---- Detalle de la cuenta ----

        ''' <summary>GLimpiar.</summary>
        Private Sub LimpiarCuenta()
            _mostrando = True
            txtCodigo.Text = ""
            txtDetalle.Text = ""
            If cmbTipoPago.Items.Count > 0 Then cmbTipoPago.SelectedIndex = 0
            If cmbBanco.Items.Count > 0 Then cmbBanco.SelectedIndex = 0
            If cmbMoneda.Items.Count > 0 Then cmbMoneda.SelectedIndex = 0
            cmbCuentaBanco.Items.Clear()
            txtCuenta.Text = ""
            cmbConfirmar.Text = ""
            MarcarDias(0)
            txtVisita.Text = "7"
            txtCredito.Text = "0"
            txtPlazo.Text = "30"
            cmbCategoria.SelectedIndex = 0
            cmbDevoluciones.SelectedIndex = 0
            txtPromocional.Text = (0.0).ToString("N2", Cultura)
            For i = 0 To 2
                txtContactoId(i).Text = ""
                txtContactoNombre(i).Text = ""
            Next
            txtUltimoPago.Text = ""
            txtObservaciones.Text = ""
            txtEntrega.Text = "7"
            txtContable.Text = ""
            _codigoContable = ""
            txtDescContable.Text = ""
            _mostrando = False
            TipoPagoCambiado()
        End Sub

        Private Sub MarcarDias(valor As Integer)
            For i = 0 To 6
                lstDiasPago.SetItemChecked(i, (valor And (1 << i)) <> 0)
            Next
        End Sub

        ''' <summary>SD_DiasValorLista: bit 0 domingo... bit 6 sábado.</summary>
        Private Function DiasMarcados() As Integer
            Dim total = 0
            For i = 0 To 6
                If lstDiasPago.GetItemChecked(i) Then total += 1 << i
            Next
            Return total
        End Function

        Private Sub Cuentas_Seleccion(sender As Object, e As EventArgs)
            If _mostrando OrElse _editada IsNot Nothing Then Return
            Dim c = CuentaActual()
            If c Is Nothing Then
                LimpiarCuenta()
            Else
                Dim tarea = MostrarCuentaAsync(c)
            End If
            Estados()
        End Sub

        ''' <summary>GrdCuentas_RowColChange: muestra la cuenta elegida.</summary>
        Private Async Function MostrarCuentaAsync(c As CuentaProveedor) As Task
            LimpiarCuenta()
            _mostrando = True
            Try
                txtCodigo.Text = c.Codigo
                txtDetalle.Text = c.Descripcion
                Elegir(cmbTipoPago, c.TipoPago.ToString(CultureInfo.InvariantCulture))
                Elegir(cmbBanco, c.Banco.ToString(CultureInfo.InvariantCulture))
                Elegir(cmbMoneda, c.Moneda.ToString(CultureInfo.InvariantCulture))
                cmbConfirmar.Text = c.Confirmacion
                MarcarDias(c.DiaPago)
                txtVisita.Text = c.DiaVisita.ToString(Cultura)
                txtCredito.Text = c.Credito.ToString("0.##", Cultura)
                txtPlazo.Text = c.DiasPlazo.ToString(Cultura)
                cmbCategoria.SelectedIndex = Math.Max(cmbCategoria.Items.IndexOf(c.Categ.ToUpperInvariant()), 0)
                cmbDevoluciones.SelectedIndex = If(c.Devoluciones, 1, 0)
                txtPromocional.Text = c.MontoPromocional.ToString("N2", Cultura)
                txtUltimoPago.Text = If(c.FUltPago.HasValue, c.FUltPago.Value.ToString("dddd, dd/MMM/yyyy", Cultura), "Sin Compras")
                txtObservaciones.Text = c.Observaciones
                txtEntrega.Text = c.DiasEntrega.ToString(Cultura)
                txtContactoId(0).Text = c.EncargadoCompras
                txtContactoId(1).Text = c.Agente
                txtContactoId(2).Text = c.Vendedor
                _codigoContable = c.CC
                txtContable.Text = If(c.CC.Length = 0, "", _formato.Mostrar(c.CC))
            Finally
                _mostrando = False
            End Try
            TipoPagoCambiado()
            Try
                Await CargarCuentasBancariasAsync(c.Cuenta, primeraSiNoEsta:=False)
                If Not EsCheque() Then txtCuenta.Text = c.Cuenta
                For i = 0 To 2
                    txtContactoNombre(i).Text = Await _repo.NombreAsync(txtContactoId(i).Text)
                Next
                txtDescContable.Text = If(c.CC.Length = 0, "", Await _repo.DescripcionContableAsync(c.CC, _cedula))
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Function

        ''' <summary>CmbTipoPago_Click: con el primer tipo (cheque) la cuenta se elige de las bancarias; con otro se escribe.</summary>
        Private Function EsCheque() As Boolean
            Return cmbTipoPago.SelectedIndex = 0
        End Function

        Private Sub TipoPagoCambiado()
            cmbCuentaBanco.Visible = EsCheque()
            txtCuenta.Visible = Not EsCheque()
        End Sub

        ''' <summary>Cargar_Cuentas: las cuentas de cheques del local, el banco y la moneda elegidos.</summary>
        ''' <param name="elegida">
        ''' La cuenta que se quiere ver elegida.
        ''' </param>
        ''' <param name="primeraSiNoEsta">
        ''' Con una cuenta nueva o al cambiar de banco o moneda se elige la primera, como VB6. Al mostrar una
        ''' cuenta guardada que no está en la lista (o vacía), el combo queda sin elegir para no cambiarla sin querer.
        ''' </param>
        Private Async Function CargarCuentasBancariasAsync(elegida As String, Optional primeraSiNoEsta As Boolean = True) As Task
            cmbCuentaBanco.Items.Clear()
            Try
                Dim cuentas = Await _repo.CuentasBancariasAsync(LocalElegido, Entero(cmbBanco), Entero(cmbMoneda))
                cmbCuentaBanco.Items.AddRange(cuentas.ToArray())
                If cmbCuentaBanco.Items.Count > 0 Then Elegir(cmbCuentaBanco, elegida)
                If Not primeraSiNoEsta AndAlso Codigo(cmbCuentaBanco) <> elegida.Trim() Then cmbCuentaBanco.SelectedIndex = -1
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Function

        Private Async Function NombreContactoAsync(indice As Integer) As Task
            If _editada Is Nothing Then Return
            Try
                txtContactoNombre(indice).Text = Await _repo.NombreAsync(txtContactoId(indice).Text.Trim())
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Function

        ''' <summary>CmdBuscarContacto_Click: personas físicas.</summary>
        Private Async Function BuscarContactoAsync(indice As Integer) As Task
            Try
                Using datos = Await _repo.ContactosBuscarAsync()
                    Using buscador As New FrmBuscador("Seleccione el contacto", datos)
                        If buscador.ShowDialog(Me) <> DialogResult.OK OrElse buscador.Resultado(0).Length = 0 Then Return
                        txtContactoId(indice).Text = buscador.Resultado(0)
                        txtContactoNombre(indice).Text = buscador.Resultado(1)
                    End Using
                End Using
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Function

        ' ---- Cuenta contable ----

        ''' <summary>TxtCContable_KeyPress con la caja vacía: las cuentas auxiliares del catálogo.</summary>
        Private Async Function BuscarContableAsync() As Task
            Try
                Using datos = Await _repo.BuscarCuentasContablesAsync(_formato, _cedula)
                    Using buscador As New FrmBuscador("Seleccione la cuenta contable", datos)
                        If buscador.ShowDialog(Me) <> DialogResult.OK OrElse buscador.Resultado(1).Length = 0 Then Return
                        txtContable.Text = buscador.Resultado(1)
                        Await ValidarContableAsync()
                    End Using
                End Using
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Function

        ''' <summary>CargarCuenta: la cuenta debe existir y ser auxiliar; si es un padre se rechaza.</summary>
        Private Async Function ValidarContableAsync() As Task
            If _editada Is Nothing Then Return
            Dim texto = txtContable.Text.Trim()
            If texto.Length = 0 Then
                _codigoContable = ""
                txtDescContable.Text = ""
                Return
            End If
            Try
                Dim hallada = Await _repo.CuentaContableAsync(texto, _formato, _cedula)
                If hallada Is Nothing Then
                    txtContable.Text = ""
                    _codigoContable = ""
                    txtDescContable.Text = ""
                ElseIf hallada.Auxiliar <> "1" Then
                    MessageBox.Show(Me, "La cuenta es un padre...", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
                    txtContable.Text = ""
                    _codigoContable = ""
                    txtDescContable.Text = ""
                Else
                    _codigoContable = hallada.Codigo
                    txtDescContable.Text = hallada.Descripcion
                End If
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Function

        ' ---- Edición de cuentas ----

        ''' <summary>Habilita según el estado: proveedor en vista o en edición, y cuenta en edición.</summary>
        Private Sub Estados()
            Dim cuentaEnEdicion = _editada IsNot Nothing
            cmbLocal.Enabled = Not _editandoProveedor
            txtId.Enabled = _nuevoProveedor AndAlso Not cuentaEnEdicion
            btnPersona.Enabled = txtId.Enabled
            For Each b In {btnPrimero, btnAnterior, btnSiguiente, btnUltimo}
                b.Enabled = Not _editandoProveedor
            Next
            btnCrear.Enabled = _editandoProveedor AndAlso Not cuentaEnEdicion
            btnEditarCuenta.Enabled = _editandoProveedor AndAlso Not cuentaEnEdicion AndAlso CuentaActual() IsNot Nothing
            btnBorrarCuenta.Enabled = _editandoProveedor AndAlso (cuentaEnEdicion OrElse CuentaActual() IsNot Nothing)
            btnBorrarCuenta.Text = If(cuentaEnEdicion, "&Cancelar", "&Borrar")
            btnAplicar.Enabled = cuentaEnEdicion
            txtCodigo.Enabled = cuentaEnEdicion
            txtDetalle.Enabled = cuentaEnEdicion
            gridCuentas.Enabled = Not cuentaEnEdicion
            barra.Enabled = Not cuentaEnEdicion
            For Each pagina As TabPage In tabsCuenta.TabPages
                For Each c As Control In pagina.Controls
                    If Not TypeOf c Is Label Then c.Enabled = cuentaEnEdicion
                Next
            Next
            cmbCategoria.Enabled = False
            txtUltimoPago.Enabled = True
            txtDescContable.Enabled = True
            For i = 0 To 2
                txtContactoNombre(i).Enabled = True
            Next
        End Sub

        ''' <summary>CmdNuevaCuenta_Click: una cuenta en blanco con el primer código libre.</summary>
        Private Async Function CrearCuentaAsync() As Task
            LimpiarCuenta()
            _editada = New CuentaProveedor()
            tabsCuenta.SelectedTab = tabCuenta
            Estados()
            Try
                txtCodigo.Text = Await _repo.CodigoDisponibleAsync()
                Await CargarCuentasBancariasAsync("")
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
            txtDetalle.Focus()
        End Function

        ''' <summary>CmdGEditar_Click.</summary>
        Private Sub EditarCuenta_Clic(sender As Object, e As EventArgs)
            Dim c = CuentaActual()
            If c Is Nothing Then Return
            _editada = c
            tabsCuenta.SelectedTab = tabCuenta
            Estados()
            txtDetalle.Focus()
        End Sub

        ''' <summary>CmdCancelar_Click: en edición cancela; si no, marca o desmarca la cuenta para borrarla al guardar.</summary>
        Private Sub BorrarCuenta_Clic(sender As Object, e As EventArgs)
            If _editada IsNot Nothing Then
                _editada = Nothing
                Estados()
                Cuentas_Seleccion(Nothing, EventArgs.Empty)
                Return
            End If
            Dim fila = gridCuentas.CurrentRow
            If fila Is Nothing Then
                MessageBox.Show(Me, "No ha seleccionado ninguna cuenta", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If
            Dim c = CType(fila.Tag, CuentaProveedor)
            c.Borrar = Not c.Borrar
            PintarFila(fila)
        End Sub

        Private Function Numero(caja As TextBox) As Double
            Dim valor As Double
            Return If(Double.TryParse(caja.Text.Trim(), NumberStyles.Number, Cultura, valor), valor, 0)
        End Function

        ''' <summary>CmdGCuenta_Click: valida y pasa la cuenta a la lista; se escribe al guardar el proveedor.</summary>
        Private Async Function AplicarCuentaAsync() As Task
            If _editada Is Nothing Then Return
            If txtDetalle.Text.Trim().Length = 0 Then
                MessageBox.Show(Me, "Especifique el Nombre de la Cuenta...", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
                txtDetalle.Focus()
                Return
            End If
            Dim codigoCuenta = txtCodigo.Text.Trim()
            If codigoCuenta.Length = 0 Then
                MessageBox.Show(Me, "Especifique el código de la cuenta...", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
                txtCodigo.Focus()
                Return
            End If
            If _cuentas.Any(Function(x) x IsNot _editada AndAlso x.Codigo.Trim() = codigoCuenta) Then
                MessageBox.Show(Me, $"La Cuenta {codigoCuenta} ya existe...", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
                txtCodigo.Focus()
                Return
            End If
            Await ValidarContableAsync()
            If _exigeContable AndAlso _codigoContable.Length = 0 Then
                MessageBox.Show(Me, "Especifique la Cuenta Contable...", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
                tabsCuenta.SelectedTab = tabContable
                txtContable.Focus()
                Return
            End If
            Dim c = _editada
            c.Codigo = codigoCuenta
            c.Descripcion = txtDetalle.Text.Trim()
            c.TipoPago = Entero(cmbTipoPago)
            c.Cuenta = If(EsCheque(), Codigo(cmbCuentaBanco), txtCuenta.Text.Trim())
            c.Banco = Entero(cmbBanco)
            c.Moneda = Entero(cmbMoneda)
            c.Confirmacion = cmbConfirmar.Text.Trim()
            c.DiaPago = DiasMarcados()
            c.DiaVisita = CInt(Vb6.Val(txtVisita.Text))
            c.Credito = Numero(txtCredito)
            c.DiasPlazo = CInt(Vb6.Val(txtPlazo.Text))
            c.Categ = Convert.ToString(cmbCategoria.SelectedItem)
            c.Devoluciones = cmbDevoluciones.SelectedIndex = 1
            c.MontoPromocional = Numero(txtPromocional)
            c.Observaciones = txtObservaciones.Text
            c.DiasEntrega = CInt(Vb6.Val(txtEntrega.Text))
            c.EncargadoCompras = txtContactoId(0).Text.Trim()
            c.Agente = txtContactoId(1).Text.Trim()
            c.Vendedor = txtContactoId(2).Text.Trim()
            c.CC = _codigoContable
            c.Modificada = True
            If Not _cuentas.Contains(c) Then _cuentas.Add(c)
            _editada = Nothing
            LlenarLista()
            For Each fila As DataGridViewRow In gridCuentas.Rows
                If fila.Tag Is c Then
                    gridCuentas.CurrentCell = fila.Cells(0)
                    Exit For
                End If
            Next
            Estados()
        End Function

        ' ---- Barra ----

        Private Async Function Barra_Clic(boton As BotonBarra) As Task(Of Boolean)
            Try
                Select Case boton
                    Case BotonBarra.Nuevo
                        LimpiarProveedor()
                        LimpiarCuenta()
                        _editandoProveedor = True
                        _nuevoProveedor = True
                        tabsProveedor.SelectedTab = tabProveedor
                        Estados()
                        txtId.Focus()
                    Case BotonBarra.Editar
                        If txtId.Text.Trim().Length = 0 OrElse txtNombre.Text.Length = 0 Then Return True
                        _editandoProveedor = True
                        _nuevoProveedor = False
                        Estados()
                    Case BotonBarra.Guardar
                        If Not Await GuardarAsync() Then Return True
                        _editandoProveedor = False
                        _nuevoProveedor = False
                        Estados()
                    Case BotonBarra.Cancelar
                        _editandoProveedor = False
                        _nuevoProveedor = False
                        If _ultimo.Length = 0 Then
                            LimpiarProveedor()
                        Else
                            Await MostrarProveedorAsync(_ultimo)
                        End If
                        Estados()
                    Case BotonBarra.Buscar
                        Await BuscarAsync()
                    Case BotonBarra.Salir
                        Close()
                End Select
            Catch ex As MySqlException
                Mensajes.NoGuardo(ex.Message, "PROVEEDORES", Modulo.TituloModulo)
                Return True
            End Try
            Return False
        End Function

        ''' <summary>
        ''' Save_Proveedor: revisa los códigos de las cuentas nuevas o cambiadas contra la base (la llave es única en
        ''' todos los locales) y escribe todo.
        ''' </summary>
        Private Async Function GuardarAsync() As Task(Of Boolean)
            Dim id = txtId.Text.Trim()
            If id.Length = 0 OrElse txtNombre.Text.Length = 0 Then
                MessageBox.Show(Me, "Indique la persona o empresa que es el proveedor.", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
                txtId.Focus()
                Return False
            End If
            For Each c In _cuentas.Where(Function(x) Not x.Borrar AndAlso x.Modificada)
                If Not c.Nueva AndAlso c.Codigo.Trim() = c.CodigoOriginal.Trim() Then Continue For
                Dim dueno = Await _repo.DuenoCodigoAsync(c.Codigo)
                Do While dueno.Length > 0
                    If Not c.Nueva Then
                        MessageBox.Show(Me, $"La Cuenta {c.Codigo} ya existe...", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
                        Return False
                    End If
                    If MessageBox.Show(Me, $"La Cuenta {c.Codigo} ya existe, esta asignada al proveedor {dueno}" & vbCrLf & "¿Asignar un Codigo Automatico?",
                                       Modulo.TituloModulo, MessageBoxButtons.YesNo, MessageBoxIcon.Information) <> DialogResult.Yes Then Return False
                    c.Codigo = Await _repo.CodigoDisponibleAsync()
                    dueno = Await _repo.DuenoCodigoAsync(c.Codigo)
                Loop
            Next
            Dim noBorradas = Await _repo.GuardarAsync(LocalElegido, id, _cuentas, _uid, _localSesion)
            For Each codigoCuenta In noBorradas
                MessageBox.Show(Me, $"La cuenta {codigoCuenta} no se puede borrar porque tiene CXP asignadas...", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
            Next
            _ultimo = id
            Await CargarCuentasAsync()
            Return True
        End Function

        Private Async Function BuscarAsync() As Task
            Using datos = Await _repo.BuscarAsync(LocalElegido)
                Using buscador As New FrmBuscador("Seleccione el proveedor", datos)
                    If buscador.ShowDialog(Me) <> DialogResult.OK OrElse buscador.Resultado(1).Length = 0 Then Return
                    Await MostrarProveedorAsync(buscador.Resultado(1))
                End Using
            End Using
        End Function

    End Class

End Namespace
