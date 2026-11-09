Imports System.Globalization
Imports System.Net
Imports System.Text
Imports MySqlConnector
Imports CoopeNet.Core.Configuracion
Imports CoopeNet.Core.Datos
Imports CoopeNet.Core.Pos
Imports CoopeNet.Ui

Namespace SD0502

    ''' <summary>
    ''' Existencias del artículo por local. Migración de SD0502\FrmMantLocales.frm, que se abre con un clic en la
    ''' existencia del Catálogo de Artículos (lblStock_Click, privilegio 5008).
    ''' </summary>
    ''' <remarks>
    ''' Otro local se consulta en su servidor (poslocalescnn) y solo se ve: Nuevo, Editar y Borrar son para el local
    ''' del usuario. Si el local no tiene servidor propio se consulta en la base principal. El historial va en dos
    ''' rejillas en lugar del HTML de VB6, que se arma igual solo para imprimir. No se migran los enlaces del
    ''' historial (abrían la compra o reimprimían la factura, pantallas que aún no están en .NET) ni el lector serial.
    ''' </remarks>
    Public Class FrmExistencias
        Inherits Form

        Private Enum ModoFicha
            Normal
            Nuevo
            Editar
        End Enum

        Private ReadOnly _repo As ExistenciasLocalRepositorio
        Private ReadOnly _articulos As ArticulosRepositorio
        Private ReadOnly _local As Long
        Private ReadOnly _uid As Long
        Private ReadOnly _estacion As String
        Private ReadOnly _configIni As String
        Private ReadOnly _codigoInicial As String
        ''' <summary>La conexión del local que se ve (GCNN de VB6).</summary>
        Private _datos As IFabricaConexiones
        Private _existencia As ExistenciaLocal
        Private _foto As Dictionary(Of String, String)
        Private _modo As ModoFicha = ModoFicha.Normal
        Private _modificandoInicial As Boolean
        Private _inicialAnterior As String = ""
        Private _pintando As Boolean
        Private _compras As New List(Of MovimientoExistencia)
        Private _ventas As New List(Of MovimientoExistencia)
        Private _hayLocalizaciones As Boolean = True
        Private _navegador As WebBrowser

        Private Shared ReadOnly Cultura As CultureInfo = CultureInfo.CurrentCulture

        ' ---- Filtro ----
        Private ReadOnly gbFiltro As New GroupBox With {.Name = "gbFiltro", .Text = "Filtrado por local, categoría y subcategoría", .Location = New Point(12, 8), .Size = New Size(876, 56)}
        Private ReadOnly cmbLocal As New ComboBox With {.Name = "cmbLocal", .AccessibleName = "Local", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(10, 22), .Width = 250}
        Private ReadOnly cmbFiltroCat As New ComboBox With {.Name = "cmbFiltroCat", .AccessibleName = "Filtro de categoría", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(270, 22), .Width = 196}
        Private ReadOnly cmbFiltroSCat As New ComboBox With {.Name = "cmbFiltroSCat", .AccessibleName = "Filtro de subcategoría", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(472, 22), .Width = 196}
        Private ReadOnly cmbFiltroSSCat As New ComboBox With {.Name = "cmbFiltroSSCat", .AccessibleName = "Filtro de sub-subcategoría", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(674, 22), .Width = 192}

        ' ---- Artículo ----
        Private ReadOnly txtCodigo As New TextBox With {.Name = "txtCodigo", .AccessibleName = "Artículo", .Location = New Point(120, 76), .Width = 130, .MaxLength = 16}
        Private ReadOnly btnAnterior As New Button With {.Name = "btnAnterior", .Text = "◀", .AccessibleName = "Artículo anterior", .Location = New Point(256, 74), .Size = New Size(34, 26)}
        Private ReadOnly btnSiguiente As New Button With {.Name = "btnSiguiente", .Text = "▶", .AccessibleName = "Artículo siguiente", .Location = New Point(294, 74), .Size = New Size(34, 26)}
        Private ReadOnly lblDescripcion As New Label With {.Name = "lblDescripcion", .AutoSize = False, .AutoEllipsis = True, .Location = New Point(340, 79), .Size = New Size(548, 20)}
        Private ReadOnly txtStock As New TextBox With {.Name = "txtStock", .AccessibleName = "Stock", .ReadOnly = True, .TabStop = False, .TextAlign = HorizontalAlignment.Right, .Location = New Point(120, 112), .Width = 110}
        Private ReadOnly txtMin As New TextBox With {.Name = "txtMin", .AccessibleName = "Mínimo", .TextAlign = HorizontalAlignment.Right, .Location = New Point(340, 112), .Width = 110}
        Private ReadOnly txtMax As New TextBox With {.Name = "txtMax", .AccessibleName = "Máximo", .TextAlign = HorizontalAlignment.Right, .Location = New Point(560, 112), .Width = 110}
        Private ReadOnly txtRecom As New TextBox With {.Name = "txtRecom", .AccessibleName = "Recomendado", .TextAlign = HorizontalAlignment.Right, .Location = New Point(778, 112), .Width = 110}
        Private ReadOnly cmbLinea As New ComboBox With {.Name = "cmbLinea", .AccessibleName = "Línea de crédito", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(120, 146), .Width = 330}
        Private ReadOnly cmbLocalizacion As New ComboBox With {.Name = "cmbLocalizacion", .AccessibleName = "Localización", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(560, 146), .Width = 328}
        Private ReadOnly txtModificado As New TextBox With {.Name = "txtModificado", .AccessibleName = "Última modificación", .ReadOnly = True, .TabStop = False, .Location = New Point(120, 188), .Width = 330}

        ' ---- Inventario inicial ----
        Private ReadOnly gbInicial As New GroupBox With {.Name = "gbInicial", .Text = "Inventario inicial", .Location = New Point(470, 174), .Size = New Size(418, 56)}
        Private ReadOnly txtInicial As New TextBox With {.Name = "txtInicial", .AccessibleName = "Inventario inicial", .TextAlign = HorizontalAlignment.Right, .Location = New Point(70, 22), .Width = 90}
        Private ReadOnly dtpInicial As New DateTimePicker With {.Name = "dtpInicial", .AccessibleName = "Fecha del inventario inicial", .Format = DateTimePickerFormat.Short, .Location = New Point(214, 22), .Width = 110}
        Private ReadOnly btnInicial As New Button With {.Name = "btnInicial", .Text = "Modificar", .Location = New Point(330, 20), .Size = New Size(80, 28)}

        ' ---- Historial ----
        Private ReadOnly gbHistorial As New GroupBox With {.Name = "gbHistorial", .Text = "Historial", .Location = New Point(12, 238), .Size = New Size(876, 312)}
        Private ReadOnly cmbMeses As New ComboBox With {.Name = "cmbMeses", .AccessibleName = "Meses a consultar", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(130, 22), .Width = 60}
        Private ReadOnly nudRegistros As New NumericUpDown With {.Name = "nudRegistros", .AccessibleName = "Registros consultados", .Minimum = 1, .Maximum = 500, .Value = 3, .Location = New Point(350, 22), .Width = 64}
        Private ReadOnly btnRefrescar As New Button With {.Name = "btnRefrescar", .Text = "Actualizar", .Location = New Point(424, 20), .Size = New Size(100, 28)}
        Private ReadOnly gridCompras As DataGridView = Rejilla("gridCompras", "Últimas compras", "Proveedor", "Usuario")
        Private ReadOnly gridVentas As DataGridView = Rejilla("gridVentas", "Últimas ventas", "Cliente", "Vendedor")
        Private ReadOnly lblCompras As New Label With {.Name = "lblCompras", .Text = "Últimas compras", .AutoSize = True, .Margin = New Padding(0, 4, 0, 2)}
        Private ReadOnly lblVentas As New Label With {.Name = "lblVentas", .Text = "Últimas ventas", .AutoSize = True, .Margin = New Padding(0, 6, 0, 2)}

        Private ReadOnly barra As New BarraComandos With {.Name = "barra", .Modo = ModoBarra.Imprimir, .Location = New Point(428, 562), .Size = New Size(460, 32)}

        ''' <param name="codigo">El artículo que se ve en el catálogo; vacío para empezar sin artículo.</param>
        Public Sub New(repositorio As ExistenciasLocalRepositorio, articulos As ArticulosRepositorio, codigo As String, local As Long, uid As Long, estacion As String, directorioDatos As String)
            _repo = repositorio
            _articulos = articulos
            _datos = repositorio.Principal
            _codigoInicial = If(codigo, "").Trim()
            _local = local
            _uid = uid
            _estacion = If(estacion, "")
            _configIni = IO.Path.Combine(If(directorioDatos, ""), "CONFIG.INI")

            Text = "Mantenimiento de existencias en local"
            ClientSize = New Size(900, 606)
            MinimumSize = New Size(916, 520)
            MinimizeBox = False
            ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent

            gbFiltro.Controls.AddRange(New Control() {cmbLocal, cmbFiltroCat, cmbFiltroSCat, cmbFiltroSSCat})
            gbInicial.Controls.AddRange(New Control() {Etiqueta("Cantidad", 10, 25), txtInicial, Etiqueta("Fecha", 170, 25), dtpInicial, btnInicial})
            Dim rejillas As New TableLayoutPanel With {.Name = "rejillas", .Location = New Point(10, 54), .Size = New Size(856, 250), .ColumnCount = 1, .RowCount = 4,
                .Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right}
            rejillas.RowStyles.Add(New RowStyle(SizeType.AutoSize))
            rejillas.RowStyles.Add(New RowStyle(SizeType.Percent, 50))
            rejillas.RowStyles.Add(New RowStyle(SizeType.AutoSize))
            rejillas.RowStyles.Add(New RowStyle(SizeType.Percent, 50))
            gridCompras.Dock = DockStyle.Fill
            gridVentas.Dock = DockStyle.Fill
            rejillas.Controls.Add(lblCompras, 0, 0)
            rejillas.Controls.Add(gridCompras, 0, 1)
            rejillas.Controls.Add(lblVentas, 0, 2)
            rejillas.Controls.Add(gridVentas, 0, 3)
            gbHistorial.Controls.AddRange(New Control() {Etiqueta("Meses a consultar", 10, 25), cmbMeses, Etiqueta("Registros consultados", 210, 25), nudRegistros, btnRefrescar, rejillas})

            Controls.AddRange(New Control() {gbFiltro,
                Etiqueta("Artículo", 12, 79), txtCodigo, btnAnterior, btnSiguiente, lblDescripcion,
                Etiqueta("Stock", 12, 115), txtStock, Etiqueta("Mínimo", 250, 115), txtMin, Etiqueta("Máximo", 470, 115), txtMax, Etiqueta("Recomendado", 682, 115), txtRecom,
                Etiqueta("Línea de crédito", 12, 149), cmbLinea, Etiqueta("Localización", 470, 149), cmbLocalizacion,
                Etiqueta("Modificado", 12, 191), txtModificado, gbInicial,
                gbHistorial, barra})

            gbFiltro.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
            gbHistorial.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
            lblDescripcion.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
            barra.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
            Ventanas.Recordar(Me)

            barra.AlHacerClic = AddressOf Barra_Clic
            AddHandler Load, AddressOf Cargar
            AddHandler FormClosed, Sub() _navegador?.Dispose()
            AddHandler cmbLocal.SelectedIndexChanged, Async Sub() Await CambiarLocalAsync()
            AddHandler cmbFiltroCat.SelectedIndexChanged, Async Sub() Await CascadaAsync(cmbFiltroSCat, 2)
            AddHandler cmbFiltroSCat.SelectedIndexChanged, Async Sub() Await CascadaAsync(cmbFiltroSSCat, 3)
            AddHandler txtCodigo.KeyDown, AddressOf Codigo_Tecla
            AddHandler btnAnterior.Click, Async Sub() Await NavegarAsync(False)
            AddHandler btnSiguiente.Click, Async Sub() Await NavegarAsync(True)
            AddHandler btnInicial.Click, Async Sub() Await InventarioInicialAsync()
            AddHandler btnRefrescar.Click, Async Sub() Await MostrarHistorialAsync()
            AddHandler cmbMeses.SelectedIndexChanged, AddressOf Meses_Cambio
            Habilitar(False)
            btnAnterior.Enabled = False
        End Sub

        Private Shared Function Etiqueta(texto As String, x As Integer, y As Integer) As Label
            Return New Label With {.Text = texto, .AutoSize = True, .Location = New Point(x, y)}
        End Function

        Private Shared Function Rejilla(nombre As String, titulo As String, tercero As String, usuario As String) As DataGridView
            Dim g As New DataGridView With {.Name = nombre, .AccessibleName = titulo, .ReadOnly = True, .AllowUserToAddRows = False, .AllowUserToDeleteRows = False,
                .AllowUserToResizeRows = False, .MultiSelect = False, .RowHeadersVisible = False, .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, .Margin = Padding.Empty}
            g.Columns.Add(Columna("Factura", 70, DataGridViewContentAlignment.MiddleCenter))
            g.Columns.Add(Columna(tercero, 220, DataGridViewContentAlignment.MiddleLeft))
            g.Columns.Add(Columna("Fecha", 110, DataGridViewContentAlignment.MiddleLeft))
            g.Columns.Add(Columna("Cantidad", 70, DataGridViewContentAlignment.MiddleRight))
            g.Columns.Add(Columna("P.Costo", 80, DataGridViewContentAlignment.MiddleRight))
            g.Columns.Add(Columna("P.Venta", 80, DataGridViewContentAlignment.MiddleRight))
            g.Columns.Add(Columna(usuario, 70, DataGridViewContentAlignment.MiddleCenter))
            Return g
        End Function

        Private Shared Function Columna(titulo As String, peso As Integer, alineacion As DataGridViewContentAlignment) As DataGridViewTextBoxColumn
            Dim c As New DataGridViewTextBoxColumn With {.HeaderText = titulo, .FillWeight = peso, .SortMode = DataGridViewColumnSortMode.NotSortable}
            c.DefaultCellStyle.Alignment = alineacion
            Return c
        End Function

        ' ---- Carga ----

        Private ReadOnly Property LocalVisto As Long
            Get
                Dim item = TryCast(cmbLocal.SelectedItem, OpcionArticulo)
                Return If(item Is Nothing, _local, CLng(Core.Vb6.Val(item.Codigo)))
            End Get
        End Property

        Private ReadOnly Property EsMiLocal As Boolean
            Get
                Return LocalVisto = _local
            End Get
        End Property

        ''' <summary>Form_Load: los meses de CONFIG.INI, los locales, las líneas y localizaciones del local del usuario y el artículo recibido.</summary>
        Private Async Sub Cargar(sender As Object, e As EventArgs)
            For m = 1 To 12
                cmbMeses.Items.Add(m.ToString(CultureInfo.InvariantCulture))
            Next
            Dim meses = ConexionesIni.LeerValor(_configIni, "POS", "MESES")
            _pintando = True
            cmbMeses.SelectedIndex = Math.Max(0, cmbMeses.FindStringExact(If(meses.Length = 0, "1", meses)))
            _pintando = False
            Try
                Dim locales = Await _repo.LocalesAsync()
                _pintando = True
                cmbLocal.Items.AddRange(locales.ToArray())
                cmbLocal.SelectedIndex = locales.FindIndex(Function(l) Core.Vb6.Val(l.Codigo) = _local)
                _pintando = False
                Await LlenarCategoriasAsync(cmbFiltroCat, 1, 0, 0)
                Dim opciones = Await _repo.OpcionesAsync(_local)
                cmbLinea.Items.AddRange(opciones.Lineas.ToArray())
                cmbLinea.SelectedIndex = 0
                cmbLocalizacion.Items.AddRange(opciones.Localizaciones.ToArray())
                If cmbLocalizacion.Items.Count > 0 Then
                    cmbLocalizacion.SelectedIndex = 0
                Else
                    _hayLocalizaciones = False
                    MessageBox.Show(Me, "No se encontraron localizaciones para los artículos." & vbCrLf & vbCrLf &
                                    "Para el correcto funcinamiento del sistema se requiere que exista al menos una localización." & vbCrLf & vbCrLf &
                                    "Puede crearla en [ INVENTARIO >> LOCALIZACIONES ]", Text, MessageBoxButtons.OK, MessageBoxIcon.Error)
                    For Each c As Control In Controls
                        c.Enabled = False
                    Next
                    Return
                End If
                Permisos()
                If _codigoInicial.Length > 0 Then
                    txtCodigo.Text = _codigoInicial
                    Await CargarAsync(_codigoInicial, BusquedaExistencia.Consulta)
                End If
            Catch ex As MySqlException
                Mensajes.NoConecta()
            Finally
                _pintando = False
            End Try
            txtCodigo.Focus()
        End Sub

        ''' <summary>Nuevo, Editar y Borrar solo en el local del usuario (SDCMD1.EnabledNuevo y compañía).</summary>
        Private Sub Permisos()
            barra.Estado(BotonBarra.Nuevo, EsMiLocal)
            barra.Estado(BotonBarra.Editar, EsMiLocal)
            barra.Estado(BotonBarra.Borrar, EsMiLocal)
        End Sub

        ''' <summary>CmbLocal_Click: el local del usuario usa la conexión principal; otro, la de su servidor.</summary>
        Private Async Function CambiarLocalAsync() As Task
            If _pintando Then Return
            Limpiar()
            Permisos()
            Try
                _datos = If(EsMiLocal, _repo.Principal, If(Await _repo.ConexionDeLocalAsync(LocalVisto), _repo.Principal))
                If txtCodigo.Text.Trim().Length > 0 Then Await CargarAsync(txtCodigo.Text.Trim(), BusquedaExistencia.Consulta)
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Function

        Private Async Function LlenarCategoriasAsync(combo As ComboBox, nivel As Integer, categ As Integer, scateg As Integer) As Task
            Dim opciones = If(nivel = 1 OrElse categ > 0, Await _articulos.CategoriasAsync(_local, nivel, categ, scateg), New List(Of OpcionArticulo))
            Dim anterior = _pintando
            _pintando = True
            combo.Items.Clear()
            combo.Items.Add(New OpcionArticulo With {.Codigo = "0", .Descripcion = "(Todas)"})
            combo.Items.AddRange(opciones.ToArray())
            combo.SelectedIndex = 0
            _pintando = anterior
        End Function

        Private Async Function CascadaAsync(hijo As ComboBox, nivel As Integer) As Task
            If _pintando Then Return
            Try
                Await LlenarCategoriasAsync(hijo, nivel, CodigoDe(cmbFiltroCat), If(nivel = 3, CodigoDe(cmbFiltroSCat), 0))
                If nivel = 2 Then Await LlenarCategoriasAsync(cmbFiltroSSCat, 3, 0, 0)
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Function

        Private Shared Function CodigoDe(combo As ComboBox) As Integer
            Dim item = TryCast(combo.SelectedItem, OpcionArticulo)
            Return If(item Is Nothing, 0, CInt(Core.Vb6.Val(item.Codigo)))
        End Function

        Private Shared Function TextoDe(combo As ComboBox) As String
            Dim item = TryCast(combo.SelectedItem, OpcionArticulo)
            Return If(item Is Nothing, "", item.Codigo.Trim())
        End Function

        ''' <summary>SD_SeleccionaDelCombo: elige por código; si no está, deja lo que había.</summary>
        Private Shared Sub Seleccionar(combo As ComboBox, codigo As String)
            Dim indice = combo.Items.Cast(Of Object)().ToList().FindIndex(Function(i) String.Equals(CType(i, OpcionArticulo).Codigo.Trim(), If(codigo, "").Trim(), StringComparison.OrdinalIgnoreCase))
            If indice >= 0 Then combo.SelectedIndex = indice
        End Sub

        ' ---- Consulta ----

        ''' <summary>
        ''' SD_CargarLocal. Si el artículo no está en el local avisa, salvo al navegar o en un registro nuevo, donde
        ''' deja el código escrito. Devuelve False si no hay artículo.
        ''' </summary>
        Private Async Function CargarAsync(codigo As String, busqueda As BusquedaExistencia) As Task(Of Boolean)
            Dim e = Await ExistenciasLocalRepositorio.CargarAsync(_datos, LocalVisto, codigo, busqueda, CodigoDe(cmbFiltroCat), CodigoDe(cmbFiltroSCat), CodigoDe(cmbFiltroSSCat))
            If e Is Nothing Then
                If busqueda = BusquedaExistencia.Consulta AndAlso _modo <> ModoFicha.Nuevo Then
                    If txtCodigo.Text.Trim().Length > 0 Then
                        Dim descripcion = Await ExistenciasLocalRepositorio.DescripcionAsync(_datos, codigo)
                        MessageBox.Show(Me, $"No se encuentra el artículo {descripcion} en el inventario.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information)
                    End If
                ElseIf busqueda = BusquedaExistencia.Consulta Then
                    txtCodigo.Text = codigo
                    lblDescripcion.Text = Await ExistenciasLocalRepositorio.DescripcionAsync(_datos, codigo)
                End If
                Return False
            End If
            Mostrar(e)
            lblDescripcion.Text = Await ExistenciasLocalRepositorio.DescripcionAsync(_datos, e.CArticulo)
            Await MostrarHistorialAsync()
            Return True
        End Function

        Private Sub Mostrar(e As ExistenciaLocal)
            _existencia = e
            txtCodigo.Text = e.CArticulo
            txtStock.Text = Math.Round(e.Stock, 2).ToString("N2", Cultura)
            MostrarNumero(txtMin, e.Min)
            MostrarNumero(txtMax, e.Max)
            MostrarNumero(txtRecom, e.Recom)
            MostrarNumero(txtInicial, e.InvInicial)
            dtpInicial.Value = If(e.InvInicialF.Year < 1900, New Date(1900, 1, 1), e.InvInicialF)
            txtModificado.Text = If(e.FM.Year < 1900, "", e.FM.ToString("dddd, dd 'de' MMMM 'del' yyyy hh:mm:ss tt", Cultura))
            Seleccionar(cmbLinea, e.LinCredito.ToString(CultureInfo.InvariantCulture))
            Seleccionar(cmbLocalizacion, e.Localiz)
        End Sub

        ''' <summary>SD_Limpiar.</summary>
        Private Sub Limpiar()
            _existencia = Nothing
            For Each t In New TextBox() {txtStock, txtMin, txtMax, txtRecom, txtModificado, txtInicial}
                t.Clear()
                t.Tag = Nothing
            Next
            lblDescripcion.Text = ""
            _compras.Clear()
            _ventas.Clear()
            gridCompras.Rows.Clear()
            gridVentas.Rows.Clear()
        End Sub

        ''' <summary>SD_Detalle: las últimas compras y ventas del artículo en el local que se ve.</summary>
        Private Async Function MostrarHistorialAsync() As Task
            Dim codigo = txtCodigo.Text.Trim()
            If codigo.Length = 0 Then Return
            Try
                Dim limite = CInt(nudRegistros.Value)
                Dim meses = If(_local = 4, CType(Nothing, Integer?), CInt(cmbMeses.Text))
                _compras = Await ExistenciasLocalRepositorio.ComprasAsync(_datos, LocalVisto, codigo, limite)
                _ventas = Await ExistenciasLocalRepositorio.VentasAsync(_datos, LocalVisto, codigo, limite, meses)
                Llenar(gridCompras, _compras)
                Llenar(gridVentas, _ventas)
                lblCompras.Text = $"Últimas {limite} compras"
                lblVentas.Text = $"Últimas {limite} ventas"
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Function

        Private Shared Sub Llenar(grid As DataGridView, filas As List(Of MovimientoExistencia))
            grid.Rows.Clear()
            For Each m In filas
                grid.Rows.Add(m.Documento, m.Tercero, Fecha(m.Fecha), m.Cantidad.ToString(Cultura), Monto(m.Costo), Monto(m.Precio), m.Usuario)
            Next
        End Sub

        Private Shared Function Fecha(valor As Date?) As String
            Return If(valor.HasValue AndAlso valor.Value.Year >= 1900, valor.Value.ToString("dd/MMM/yyyy HH:mm:ss", Cultura), "")
        End Function

        Private Shared Function Monto(valor As Double?) As String
            Return If(valor.HasValue, valor.Value.ToString("N2", Cultura), "")
        End Function

        ''' <summary>ComMeses_Click: guarda los meses en CONFIG.INI y vuelve a consultar.</summary>
        Private Async Sub Meses_Cambio(sender As Object, e As EventArgs)
            If _pintando Then Return
            Try
                ConexionesIni.FijarValor(_configIni, "POS", "MESES", cmbMeses.Text)
            Catch ex As Exception When TypeOf ex Is IO.IOException OrElse TypeOf ex Is UnauthorizedAccessException
                ' Sin permiso sobre CONFIG.INI solo se pierde el recuerdo de los meses.
            End Try
            Await MostrarHistorialAsync()
        End Sub

        ' ---- Navegación y búsqueda ----

        Private Async Function NavegarAsync(siguiente As Boolean) As Task
            Try
                Dim hay = Await CargarAsync(txtCodigo.Text.Trim(), If(siguiente, BusquedaExistencia.Siguiente, BusquedaExistencia.Anterior))
                If siguiente Then
                    btnAnterior.Enabled = True
                    btnSiguiente.Enabled = hay
                Else
                    btnSiguiente.Enabled = True
                    btnAnterior.Enabled = hay
                End If
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Function

        ''' <summary>
        ''' txtCodigoArticulo_KeyDown: vacío abre el buscador del local con el filtro; si no, busca el código de
        ''' barras o alterno (SD_CodAlterno) y si no lo encuentra lo toma como código de artículo.
        ''' </summary>
        Private Async Sub Codigo_Tecla(sender As Object, e As KeyEventArgs)
            If e.KeyCode <> Keys.Enter Then Return
            e.SuppressKeyPress = True
            Try
                Dim texto = txtCodigo.Text.Trim()
                If texto.Length = 0 Then
                    UseWaitCursor = True
                    Using datos = Await _repo.BuscarAsync(LocalVisto, CodigoDe(cmbFiltroCat), CodigoDe(cmbFiltroSCat), CodigoDe(cmbFiltroSSCat))
                        UseWaitCursor = False
                        Using buscador As New FrmBuscador("Seleccione el artículo", datos)
                            If buscador.ShowDialog(Me) = DialogResult.OK AndAlso buscador.Resultado(1).Length > 0 Then
                                txtCodigo.Text = buscador.Resultado(1)
                                Await CargarAsync(buscador.Resultado(1), BusquedaExistencia.Consulta)
                            End If
                        End Using
                    End Using
                    txtCodigo.Focus()
                    Return
                End If
                Dim resultado = Await New ArticulosRepositorio(_datos).BuscarCodigoBarrasAsync(texto, _local)
                Dim codigo = resultado.Articulo
                If resultado.Opciones IsNot Nothing Then
                    Using buscador As New FrmBuscador("Seleccione el código correcto", resultado.Opciones)
                        codigo = If(buscador.ShowDialog(Me) = DialogResult.OK, buscador.Resultado(0), "")
                    End Using
                    resultado.Opciones.Dispose()
                    If codigo.Length = 0 Then Return
                End If
                If codigo.Length = 0 Then codigo = texto
                Await CargarAsync(codigo, BusquedaExistencia.Consulta)
            Catch ex As MySqlException
                Mensajes.NoConecta()
            Finally
                UseWaitCursor = False
            End Try
        End Sub

        ' ---- Edición ----

        ''' <summary>SD_Habilitar.</summary>
        Private Sub Habilitar(editando As Boolean)
            txtCodigo.Enabled = Not editando
            For Each t In New TextBox() {txtMin, txtMax, txtRecom}
                t.ReadOnly = Not editando
                t.TabStop = editando
                t.BackColor = If(editando, SystemColors.Window, Tema.CampoLectura)
            Next
            txtInicial.ReadOnly = True
            txtInicial.BackColor = Tema.CampoLectura
            dtpInicial.Enabled = False
            btnInicial.Enabled = editando
            btnAnterior.Enabled = Not editando
            btnSiguiente.Enabled = Not editando
            cmbLinea.Enabled = editando AndAlso cmbLinea.Items.Count > 1
            cmbLocalizacion.Enabled = editando AndAlso cmbLocalizacion.Items.Count > 1
            cmbLocal.Enabled = Not editando
        End Sub

        ''' <summary>
        ''' Muestra hasta cuatro decimales (en la base hay restos como 270.280999999984) y guarda el valor exacto en
        ''' Tag, para que guardar sin tocar el campo no lo redondee.
        ''' </summary>
        Private Shared Sub MostrarNumero(caja As TextBox, valor As Double)
            caja.Text = valor.ToString("0.####", Cultura)
            caja.Tag = valor
        End Sub

        Private Shared Function Numero(caja As TextBox) As Double
            If TypeOf caja.Tag Is Double AndAlso caja.Text = CDbl(caja.Tag).ToString("0.####", Cultura) Then Return CDbl(caja.Tag)
            Dim valor As Double
            Return If(Double.TryParse(caja.Text, NumberStyles.Number, Cultura, valor), valor, 0)
        End Function

        ''' <summary>Lo que hay en pantalla, sobre lo que se cargó (Stock, FC y CosInicial no se editan).</summary>
        Private Function Digitado() As ExistenciaLocal
            Dim base = If(_existencia, New ExistenciaLocal())
            Return New ExistenciaLocal With {
                .CArticulo = txtCodigo.Text.Trim(), .Stock = If(_existencia Is Nothing, 0, base.Stock), .Min = Numero(txtMin), .Max = Numero(txtMax), .Recom = Numero(txtRecom),
                .InvInicial = Numero(txtInicial), .InvInicialF = dtpInicial.Value, .FC = base.FC, .LinCredito = CodigoDe(cmbLinea), .Localiz = TextoDe(cmbLocalizacion), .CosInicial = base.CosInicial}
        End Function

        ''' <summary>Devuelve True para que la barra no cambie de estado.</summary>
        Private Async Function Barra_Clic(boton As BotonBarra) As Task(Of Boolean)
            Try
                Select Case boton
                    Case BotonBarra.Nuevo
                        If Not EsMiLocal OrElse Not _hayLocalizaciones Then Return True
                        _modo = ModoFicha.Nuevo
                        _foto = Nothing
                        Habilitar(True)
                        Limpiar()
                        txtCodigo.Clear()
                        txtCodigo.Enabled = True
                        txtCodigo.Focus()
                    Case BotonBarra.Editar
                        If Not EsMiLocal OrElse _existencia Is Nothing Then Return True
                        _foto = Await _repo.FotoAsync(_local, _existencia.CArticulo)
                        _modo = ModoFicha.Editar
                        Habilitar(True)
                        txtMin.Focus()
                    Case BotonBarra.Guardar
                        Return Not Await GuardarAsync()
                    Case BotonBarra.Cancelar
                        _modo = ModoFicha.Normal
                        Habilitar(False)
                        Dim codigo = txtCodigo.Text.Trim()
                        Limpiar()
                        If codigo.Length > 0 Then Await CargarAsync(codigo, BusquedaExistencia.Consulta)
                        txtCodigo.Focus()
                    Case BotonBarra.Borrar
                        Await BorrarAsync()
                    Case BotonBarra.Imprimir
                        Await ImprimirAsync()
                    Case BotonBarra.Salir
                        Close()
                End Select
            Catch ex As MySqlException
                Mensajes.NoGuardo(ex.Message, "Existencias en local", Modulo.TituloModulo)
                Return True
            End Try
            Return False
        End Function

        ''' <summary>SDBoton_Guardar: avisa si otro usuario cambió el registro y guarda con SD_iGuardarLocal.</summary>
        Private Async Function GuardarAsync() As Task(Of Boolean)
            Dim codigo = txtCodigo.Text.Trim()
            If codigo.Length = 0 Then
                MessageBox.Show(Me, "Indique el artículo.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information)
                txtCodigo.Focus()
                Return False
            End If
            If _modo = ModoFicha.Editar Then
                Dim cambios = Await _repo.CambiosDeOtrosAsync(_local, codigo, _foto)
                If cambios.Count > 0 Then
                    Dim texto = ArticulosRepositorio.TextoCambios("El sistema detectó un cambio en el registro actual realizado por otro usuario. Los datos modificados son los siguientes:", cambios) &
                                Environment.NewLine & "¿Desea guardar los datos? Si selecciona Sí, sus datos reemplazarán los modificados por el otro usuario."
                    If MessageBox.Show(Me, texto, Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) <> DialogResult.Yes Then Return False
                End If
            End If
            UseWaitCursor = True
            Try
                Await _repo.GuardarAsync(Digitado(), _modo = ModoFicha.Nuevo, _local, _uid)
            Finally
                UseWaitCursor = False
            End Try
            _modo = ModoFicha.Normal
            Habilitar(False)
            Limpiar()
            Await CargarAsync(codigo, BusquedaExistencia.Consulta)
            txtCodigo.Focus()
            Return True
        End Function

        ''' <summary>SD_iBorrarLocal.</summary>
        Private Async Function BorrarAsync() As Task
            Dim codigo = txtCodigo.Text.Trim()
            If Not EsMiLocal OrElse codigo.Length = 0 Then Return
            If MessageBox.Show(Me, $"¿Desea eliminar el Articulo {codigo} del Local?", "Confirme", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) <> DialogResult.Yes Then Return
            Await _repo.BorrarAsync(_local, codigo)
            Limpiar()
            Habilitar(False)
            btnAnterior.Enabled = False
        End Function

        ''' <summary>
        ''' cmdModif_Click: Modificar abre la cantidad y la fecha; Guardar graba en el momento si la cantidad cambió
        ''' (con la fecha de hoy) y vuelve a la edición del registro.
        ''' </summary>
        Private Async Function InventarioInicialAsync() As Task
            If Not _modificandoInicial Then
                _modificandoInicial = True
                For Each c As Control In New Control() {txtMin, txtMax, txtRecom}
                    CType(c, TextBox).ReadOnly = True
                Next
                cmbLinea.Enabled = False
                cmbLocalizacion.Enabled = False
                txtInicial.ReadOnly = False
                txtInicial.BackColor = SystemColors.Window
                _inicialAnterior = txtInicial.Text
                dtpInicial.Enabled = True
                txtCodigo.Enabled = False
                barra.Enabled = False
                btnInicial.Text = "Guardar"
                txtInicial.Focus()
                txtInicial.SelectAll()
                Return
            End If
            Dim anterior = _inicialAnterior
            If anterior <> txtInicial.Text Then
                Dim valorAnterior As Double
                Double.TryParse(anterior, NumberStyles.Number, Cultura, valorAnterior)
                Try
                    dtpInicial.Value = Await _repo.GuardarInventarioInicialAsync(_local, txtCodigo.Text.Trim(), valorAnterior, If(_existencia?.InvInicialF, Date.MinValue), Numero(txtInicial), _uid, _estacion)
                Catch ex As MySqlException
                    Mensajes.NoGuardo(ex.Message, "Guar.Inv.Inicial", Modulo.TituloModulo)
                    Return
                End Try
            End If
            _modificandoInicial = False
            btnInicial.Text = "Modificar"
            barra.Enabled = True
            Habilitar(True)
            If _modo = ModoFicha.Nuevo Then txtCodigo.Enabled = True
        End Function

        ' ---- Impresión ----

        ''' <summary>SDBoton_Imprimir: la vista previa del mismo HTML que VB6 mostraba en WebB.</summary>
        Private Async Function ImprimirAsync() As Task
            If txtCodigo.Text.Trim().Length = 0 Then Return
            Dim html = ArmarHtml(Await ExistenciasLocalRepositorio.EstiloHtmlAsync(_datos))
            _navegador?.Dispose()
            _navegador = New WebBrowser With {.Visible = False, .ScriptErrorsSuppressed = True}
            AddHandler _navegador.DocumentCompleted, Sub() _navegador.ShowPrintPreviewDialog()
            _navegador.DocumentText = html
        End Function

        Private Function ArmarHtml(estilo As String) As String
            Dim h = Function(t As String) WebUtility.HtmlEncode(If(t, ""))
            Dim sb As New StringBuilder()
            sb.Append("<!DOCTYPE html><html><head><meta http-equiv=""Content-Type"" content=""text/html; charset=utf-8"" /><title>Listado de movimientos</title>")
            sb.Append(estilo).Append("</head><body>")
            sb.Append($"<table width=""100%""><tr><td><span class=""Estilo3"">Codigo:</span> {h(txtCodigo.Text.Trim())} </td><td><span class=""Estilo3"">Descripci&oacute;n:</span> {h(lblDescripcion.Text)} </td></tr></table>")
            Dim limite = CInt(nudRegistros.Value)
            Tabla(sb, $"Ultimas {limite} Compras", "Proveedor", "Usuario", _compras, h)
            Tabla(sb, $"Ultimas {limite} Ventas", "Cliente", "Vendedor", _ventas, h)
            sb.Append($"<p>Consulta realizada el {Date.Now.ToString("D", Cultura)}, {Date.Now.ToString("T", Cultura)}</p><p>&nbsp;</p></body></html>")
            Return sb.ToString()
        End Function

        Private Shared Sub Tabla(sb As StringBuilder, titulo As String, tercero As String, usuario As String, filas As List(Of MovimientoExistencia), h As Func(Of String, String))
            sb.Append($"<table class=""table1"" width=""100%"" border=""1""><caption align=""center"">{h(titulo)}</caption><thead><tr bgcolor=""#CCCCCC"" class=""Estilo2"">")
            For Each c In {"Factura", tercero, "Fecha", "Cantidad", "P.Costo", "P.Venta", usuario}
                sb.Append($"<th scope=""col"">{h(c)}</th>")
            Next
            sb.Append("</tr></thead><tbody>")
            For Each m In filas
                sb.Append($"<tr><td><div align=""center"">{h(m.Documento)}</div></td><td><div align=""left"">{h(m.Tercero)}</div></td><td><div align=""left"">{h(Fecha(m.Fecha))}</div></td>")
                sb.Append($"<td><div align=""right"">{h(m.Cantidad.ToString(Cultura))}</div></td><td><div align=""right"">{h(Monto(m.Costo))}</div></td><td><div align=""right"">{h(Monto(m.Precio))}</div></td>")
                sb.Append($"<td><div align=""center"">{h(m.Usuario)}</div></td></tr>")
            Next
            sb.Append("</tbody></table>")
        End Sub

    End Class

End Namespace
