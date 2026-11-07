Imports System.Globalization
Imports MySqlConnector
Imports CoopeNet.Core.Pos
Imports CoopeNet.Ui

Namespace SD0502

    ''' <summary>
    ''' Catálogo de Artículos. Migración de Fuentes\SD0502(Coopepalmares)\frmMantArticulos.frm: la ficha, la
    ''' navegación, la búsqueda, los precios y el alta, la edición y el borrado. Proveedores, imágenes, cambio
    ''' de código, catálogo CABYS, códigos de barras, alternos, etiquetas, movimientos, utilidades históricas y
    ''' existencias por local todavía no están. La pestaña Lista no se migra: en VB6 nunca se llenaba.
    ''' </summary>
    Public Class FrmArticulos
        Inherits Form

        Private Enum ModoFicha
            Normal
            Nuevo
            Editar
        End Enum

        Private Const CaracteresCodigo As String = "-|;'.,"
        Private Const CaracteresTexto As String = "-|;'"

        Private ReadOnly _repo As ArticulosRepositorio
        Private ReadOnly _local As Long
        Private ReadOnly _uid As Long
        Private ReadOnly _login As String
        Private ReadOnly _estacion As String
        Private ReadOnly _localDescripcion As String
        Private ReadOnly _puedeEditar As Boolean
        Private ReadOnly _puedeUtilidades As Boolean
        Private _catalogos As New CatalogosArticulo()
        Private _articulo As Articulo
        Private _foto As Dictionary(Of String, String)
        Private _modo As ModoFicha = ModoFicha.Normal
        Private _pintando As Boolean
        Private _versionCarga As Integer
        ''' <summary>El artículo que se veía al pulsar Nuevo, para volver a él si se cancela.</summary>
        Private _previo As String = ""

        Private Shared ReadOnly Cultura As CultureInfo = CultureInfo.CurrentCulture

        ' ---- Encabezado ----
        Private ReadOnly txtCodigo As New TextBox With {.Name = "txtCodigo", .AccessibleName = "Código", .Location = New Point(90, 12), .Width = 110, .MaxLength = 16, .Enabled = False}
        Private ReadOnly btnAnterior As New Button With {.Name = "btnAnterior", .Text = "◀", .AccessibleName = "Artículo anterior", .Location = New Point(206, 10), .Size = New Size(34, 26)}
        Private ReadOnly btnSiguiente As New Button With {.Name = "btnSiguiente", .Text = "▶", .AccessibleName = "Artículo siguiente", .Location = New Point(244, 10), .Size = New Size(34, 26)}
        Private ReadOnly lblCorto As New Label With {.Name = "lblCorto", .AutoSize = True, .Location = New Point(292, 15)}
        Private ReadOnly lblExistencia As New Label With {.Name = "lblExistencia", .AutoSize = True, .Location = New Point(410, 15)}
        Private ReadOnly lblPrecioDia As New Label With {.Name = "lblPrecioDia", .AutoSize = False, .TextAlign = ContentAlignment.MiddleRight, .Location = New Point(560, 4), .Size = New Size(320, 34)}
        Private ReadOnly txtDescripcion As New TextBox With {.Name = "txtDescripcion", .AccessibleName = "Descripción", .CharacterCasing = CharacterCasing.Upper, .Location = New Point(90, 46), .Width = 470, .MaxLength = 255}
        Private ReadOnly txtDescCorta As New TextBox With {.Name = "txtDescCorta", .AccessibleName = "Descripción corta", .CharacterCasing = CharacterCasing.Upper, .Location = New Point(660, 46), .Width = 220, .MaxLength = 30}

        ' ---- Filtro y búsqueda ----
        Private ReadOnly gbFiltro As New GroupBox With {.Name = "gbFiltro", .Text = "Filtrado por categorías y búsqueda por código de barras", .Location = New Point(12, 78), .Size = New Size(868, 56)}
        Private ReadOnly cmbFiltroCat As New ComboBox With {.Name = "cmbFiltroCat", .AccessibleName = "Filtro de categoría", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(10, 22), .Width = 190}
        Private ReadOnly cmbFiltroSCat As New ComboBox With {.Name = "cmbFiltroSCat", .AccessibleName = "Filtro de subcategoría", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(206, 22), .Width = 190}
        Private ReadOnly cmbFiltroSSCat As New ComboBox With {.Name = "cmbFiltroSSCat", .AccessibleName = "Filtro de sub-subcategoría", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(402, 22), .Width = 190}
        Private ReadOnly txtBarras As New TextBox With {.Name = "txtBarras", .AccessibleName = "Código de barras o de artículo", .Location = New Point(660, 22), .Width = 196}

        Private ReadOnly tabs As New Pestanas With {.Name = "tabs", .Location = New Point(12, 142), .Size = New Size(868, 404)}
        Private ReadOnly tabDetalle As New TabPage("Detalle")
        Private ReadOnly tabPrecio As New TabPage("Precio")
        Private ReadOnly tabOtros As New TabPage("Impuestos y otros")

        ' ---- Detalle ----
        Private ReadOnly cmbUnidad As New ComboBox With {.Name = "cmbUnidad", .AccessibleName = "Unidad de medida", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(130, 14), .Width = 260}
        Private ReadOnly cmbMarca As New ComboBox With {.Name = "cmbMarca", .AccessibleName = "Marca", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(530, 14), .Width = 300}
        Private ReadOnly cmbCat As New ComboBox With {.Name = "cmbCat", .AccessibleName = "Categoría", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(130, 48), .Width = 230}
        Private ReadOnly cmbSCat As New ComboBox With {.Name = "cmbSCat", .AccessibleName = "Subcategoría", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(366, 48), .Width = 230}
        Private ReadOnly cmbSSCat As New ComboBox With {.Name = "cmbSSCat", .AccessibleName = "Sub-subcategoría", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(602, 48), .Width = 228}
        Private ReadOnly txtPresentacion As New TextBox With {.Name = "txtPresentacion", .AccessibleName = "Presentación", .Location = New Point(130, 82), .Width = 100}
        Private ReadOnly txtEmpaque As New TextBox With {.Name = "txtEmpaque", .AccessibleName = "Código de empaque", .Location = New Point(370, 82), .Width = 140, .MaxLength = 16}
        Private ReadOnly txtMayor As New TextBox With {.Name = "txtMayor", .AccessibleName = "Cantidad por mayor", .Location = New Point(650, 82), .Width = 80}
        Private ReadOnly txtCabys As New TextBox With {.Name = "txtCabys", .AccessibleName = "Código de Hacienda (CABYS)", .Location = New Point(130, 116), .Width = 180, .MaxLength = 32}
        Private ReadOnly txtEspecificacion As New TextBox With {.Name = "txtEspecificacion", .AccessibleName = "Especificación", .Multiline = True, .ScrollBars = ScrollBars.Vertical, .Location = New Point(130, 150), .Size = New Size(700, 120)}
        Private ReadOnly txtCreadoPor As New TextBox With {.Name = "txtCreadoPor", .AccessibleName = "Creado por", .ReadOnly = True, .TabStop = False, .Location = New Point(130, 284), .Width = 160}
        Private ReadOnly txtCreado As New TextBox With {.Name = "txtCreado", .AccessibleName = "Creado el", .ReadOnly = True, .TabStop = False, .Location = New Point(390, 284), .Width = 160}
        Private ReadOnly txtModificado As New TextBox With {.Name = "txtModificado", .AccessibleName = "Modificado el", .ReadOnly = True, .TabStop = False, .Location = New Point(670, 284), .Width = 160}
        Private ReadOnly lblSugerido As New Label With {.Name = "lblSugerido", .AutoSize = True, .Location = New Point(130, 322)}

        ' ---- Precio ----
        Private ReadOnly txtCosto As New TextBox With {.Name = "txtCosto", .AccessibleName = "Precio de costo", .ReadOnly = True, .TabStop = False, .TextAlign = HorizontalAlignment.Right, .Location = New Point(130, 14), .Width = 120}
        Private ReadOnly txtPonderado As New TextBox With {.Name = "txtPonderado", .AccessibleName = "Costo ponderado", .ReadOnly = True, .TabStop = False, .TextAlign = HorizontalAlignment.Right, .Location = New Point(130, 46), .Width = 120}
        Private ReadOnly txtPromedio As New TextBox With {.Name = "txtPromedio", .AccessibleName = "Costo promedio", .ReadOnly = True, .TabStop = False, .TextAlign = HorizontalAlignment.Right, .Location = New Point(130, 78), .Width = 120}
        Private ReadOnly txtFlete As New TextBox With {.Name = "txtFlete", .AccessibleName = "Flete", .TextAlign = HorizontalAlignment.Right, .Location = New Point(130, 110), .Width = 120}
        Private ReadOnly cmbIva As New ComboBox With {.Name = "cmbIva", .AccessibleName = "Porcentaje de IVA", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(130, 142), .Width = 80}
        Private ReadOnly chkConIva As New CheckBox With {.Name = "chkConIva", .Text = "Precios con I.V.A.", .AutoSize = True, .Checked = True, .Location = New Point(130, 176)}
        Private ReadOnly lblCostoNeto As New Label With {.Name = "lblCostoNeto", .AutoSize = True, .Location = New Point(14, 214)}
        Private ReadOnly gridPrecios As New DataGridView With {.Name = "gridPrecios", .AccessibleName = "Precios", .Location = New Point(290, 14), .Size = New Size(540, 216),
            .AllowUserToAddRows = False, .AllowUserToDeleteRows = False, .AllowUserToResizeRows = False, .MultiSelect = False, .RowHeadersVisible = False,
            .SelectionMode = DataGridViewSelectionMode.CellSelect, .EditMode = DataGridViewEditMode.EditOnEnter, .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill}
        Private ReadOnly colEtiqueta As New DataGridViewTextBoxColumn With {.Name = "colEtiqueta", .HeaderText = "Precio", .ReadOnly = True, .FillWeight = 140}
        Private ReadOnly colUtilidad As New DataGridViewTextBoxColumn With {.Name = "colUtilidad", .HeaderText = "Utilidad %", .FillWeight = 80}
        Private ReadOnly colExtra As New DataGridViewTextBoxColumn With {.Name = "colExtra", .HeaderText = "Cargo extra", .FillWeight = 80}
        Private ReadOnly colPrecio As New DataGridViewTextBoxColumn With {.Name = "colPrecio", .HeaderText = "Precio de venta", .FillWeight = 100}

        ' ---- Impuestos y otros ----
        Private ReadOnly txtDescCompra As New TextBox With {.Name = "txtDescCompra", .AccessibleName = "Descuento en compra", .TextAlign = HorizontalAlignment.Right, .Location = New Point(200, 14), .Width = 90}
        Private ReadOnly txtDescVenta As New TextBox With {.Name = "txtDescVenta", .AccessibleName = "Descuento a aplicar en venta", .TextAlign = HorizontalAlignment.Right, .Location = New Point(200, 46), .Width = 90}
        Private ReadOnly txtDescMax As New TextBox With {.Name = "txtDescMax", .AccessibleName = "Descuento de venta máximo", .TextAlign = HorizontalAlignment.Right, .Location = New Point(200, 78), .Width = 90}
        Private ReadOnly txtPorcOtros As New TextBox With {.Name = "txtPorcOtros", .AccessibleName = "Porcentaje de otros impuestos", .TextAlign = HorizontalAlignment.Right, .Location = New Point(200, 110), .Width = 90}
        Private ReadOnly txtMontoOtros As New TextBox With {.Name = "txtMontoOtros", .AccessibleName = "Monto de otros impuestos", .TextAlign = HorizontalAlignment.Right, .Location = New Point(200, 142), .Width = 90}
        Private ReadOnly gbFacturar As New GroupBox With {.Name = "gbFacturar", .Text = "Parámetros para facturar", .Location = New Point(330, 10), .Size = New Size(240, 150)}
        Private ReadOnly chkSinCantidad As New CheckBox With {.Name = "chkSinCantidad", .Text = "Sin cantidad definida", .AutoSize = True, .Location = New Point(14, 26)}
        Private ReadOnly chkSinPrecio As New CheckBox With {.Name = "chkSinPrecio", .Text = "Sin precio definido", .AutoSize = True, .Location = New Point(14, 54)}
        Private ReadOnly chkSinPeso As New CheckBox With {.Name = "chkSinPeso", .Text = "Sin peso definido", .AutoSize = True, .Location = New Point(14, 82)}
        Private ReadOnly chkServicio As New CheckBox With {.Name = "chkServicio", .Text = "Es un servicio", .AutoSize = True, .Location = New Point(14, 110)}
        Private ReadOnly gbOtros As New GroupBox With {.Name = "gbOtros", .Text = "Otros", .Location = New Point(590, 10), .Size = New Size(240, 190)}
        Private ReadOnly chkCanasta As New CheckBox With {.Name = "chkCanasta", .Text = "Es canasta básica", .AutoSize = True, .Location = New Point(14, 26)}
        Private ReadOnly chkNoFacturar As New CheckBox With {.Name = "chkNoFacturar", .Text = "No facturar", .AutoSize = True, .Location = New Point(14, 54)}
        Private ReadOnly chkNegativos As New CheckBox With {.Name = "chkNegativos", .Text = "Permite negativos", .AutoSize = True, .Location = New Point(14, 82)}
        Private ReadOnly chkConsignacion As New CheckBox With {.Name = "chkConsignacion", .Text = "Consignación", .AutoSize = True, .Location = New Point(14, 110)}
        Private ReadOnly cmbMoneda As New ComboBox With {.Name = "cmbMoneda", .AccessibleName = "Moneda", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(90, 146), .Width = 70}

        Private ReadOnly barra As New BarraComandos With {.Name = "barra", .Location = New Point(420, 556), .Size = New Size(460, 32)}

        Public Sub New(repositorio As ArticulosRepositorio, local As Long, uid As Long, login As String, estacion As String, localDescripcion As String, puedeEditar As Boolean, puedeUtilidades As Boolean)
            _repo = repositorio
            _local = local
            _uid = uid
            _login = If(login, "")
            _estacion = If(estacion, "")
            _localDescripcion = If(localDescripcion, "")
            _puedeEditar = puedeEditar
            _puedeUtilidades = puedeUtilidades
            Text = "Mantenimiento de Artículos"
            ClientSize = New Size(892, 598)
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent
            KeyPreview = True

            gbFiltro.Controls.AddRange(New Control() {cmbFiltroCat, cmbFiltroSCat, cmbFiltroSSCat, New Label With {.Text = "Código", .AutoSize = True, .Location = New Point(606, 25)}, txtBarras})
            tabDetalle.Controls.AddRange(New Control() {
                Etiqueta("Unidad", 14, 17), cmbUnidad, Etiqueta("Marca", 470, 17), cmbMarca,
                Etiqueta("Categorías", 14, 51), cmbCat, cmbSCat, cmbSSCat,
                Etiqueta("Presentación", 14, 85), txtPresentacion, Etiqueta("Cód. empaque", 270, 85), txtEmpaque, Etiqueta("Cant. por mayor", 540, 85), txtMayor,
                Etiqueta("Cód. Hacienda", 14, 119), txtCabys,
                Etiqueta("Especificación", 14, 153), txtEspecificacion,
                Etiqueta("Creado por", 14, 287), txtCreadoPor, Etiqueta("Creado el", 310, 287), txtCreado, Etiqueta("Modificado", 580, 287), txtModificado, lblSugerido})
            gridPrecios.Columns.AddRange(colEtiqueta, colUtilidad, colExtra, colPrecio)
            For Each col In {colUtilidad, colExtra, colPrecio}
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
            Next
            tabPrecio.Controls.AddRange(New Control() {
                Etiqueta("Precio de costo", 14, 17), txtCosto, Etiqueta("C. ponderado", 14, 49), txtPonderado, Etiqueta("C. promedio", 14, 81), txtPromedio,
                Etiqueta("Flete", 14, 113), txtFlete, Etiqueta("I.V.A. %", 14, 145), cmbIva, chkConIva, lblCostoNeto, gridPrecios})
            gbFacturar.Controls.AddRange(New Control() {chkSinCantidad, chkSinPrecio, chkSinPeso, chkServicio})
            gbOtros.Controls.AddRange(New Control() {chkCanasta, chkNoFacturar, chkNegativos, chkConsignacion, Etiqueta("Moneda", 14, 149), cmbMoneda})
            tabOtros.Controls.AddRange(New Control() {
                Etiqueta("Descuento en compra %", 14, 17), txtDescCompra, Etiqueta("Descuento a aplicar en venta %", 14, 49), txtDescVenta,
                Etiqueta("Descuento de venta máximo %", 14, 81), txtDescMax, Etiqueta("Otros impuestos %", 14, 113), txtPorcOtros,
                Etiqueta("Monto de otros impuestos", 14, 145), txtMontoOtros, gbFacturar, gbOtros})
            tabs.TabPages.AddRange({tabDetalle, tabPrecio, tabOtros})

            Controls.AddRange(New Control() {
                Etiqueta("Código", 12, 15), txtCodigo, btnAnterior, btnSiguiente, lblCorto, lblExistencia, lblPrecioDia,
                Etiqueta("Descripción", 12, 49), txtDescripcion, Etiqueta("Desc. corta", 580, 49), txtDescCorta,
                gbFiltro, tabs, barra})

            barra.AlHacerClic = AddressOf Barra_Clic
            barra.Estado(BotonBarra.Nuevo, _puedeEditar)
            barra.Estado(BotonBarra.Editar, _puedeEditar)
            barra.Estado(BotonBarra.Borrar, _puedeEditar)

            AddHandler Load, AddressOf Cargar
            AddHandler Shown, Sub()
                                  lblPrecioDia.Font = New Font(Font.FontFamily, Font.Size * 1.8F, FontStyle.Bold)
                                  txtBarras.Focus()
                              End Sub
            AddHandler btnAnterior.Click, Async Sub() Await NavegarAsync(False)
            AddHandler btnSiguiente.Click, Async Sub() Await NavegarAsync(True)
            AddHandler txtBarras.KeyDown, AddressOf Barras_Tecla
            AddHandler cmbFiltroCat.SelectedIndexChanged, Async Sub() Await CascadaAsync(cmbFiltroCat, cmbFiltroSCat, cmbFiltroSSCat, True)
            AddHandler cmbFiltroSCat.SelectedIndexChanged, Async Sub() Await CascadaAsync(cmbFiltroSCat, cmbFiltroSSCat, Nothing, True)
            AddHandler cmbCat.SelectedIndexChanged, Async Sub() Await CascadaAsync(cmbCat, cmbSCat, cmbSSCat, False)
            AddHandler cmbSCat.SelectedIndexChanged, Async Sub() Await CascadaAsync(cmbSCat, cmbSSCat, Nothing, False)
            AddHandler txtCodigo.Leave, AddressOf Codigo_Salir
            AddHandler txtDescripcion.Leave, Sub() If txtDescCorta.Text.Trim().Length = 0 AndAlso _modo <> ModoFicha.Normal Then txtDescCorta.Text = Strings.Left(txtDescripcion.Text.Trim(), 20)
            AddHandler gridPrecios.CellEndEdit, AddressOf Precio_Editado
            AddHandler gridPrecios.CellValidating, AddressOf Precio_Validar
            For Each caja In {txtFlete, txtDescVenta, txtPorcOtros, txtMontoOtros}
                AddHandler caja.Leave, Sub() RecalcularPrecios()
            Next
            AddHandler txtDescCompra.Leave, AddressOf DescuentoCompra_Salir
            AddHandler txtDescVenta.Validating, AddressOf DescuentoVenta_Validar
            AddHandler cmbIva.SelectedIndexChanged, Sub() RecalcularPrecios()
            AddHandler chkConIva.CheckedChanged, AddressOf ConIva_Cambio
            AddHandler chkSinCantidad.CheckedChanged, Sub() Excluyente(chkSinCantidad)
            AddHandler chkSinPrecio.CheckedChanged, Sub() Excluyente(chkSinPrecio)
            AddHandler chkSinPeso.CheckedChanged, Sub() Excluyente(chkSinPeso)
            Habilitar(False)
        End Sub

        Private Shared Function Etiqueta(texto As String, x As Integer, y As Integer) As Label
            Return New Label With {.Text = texto, .AutoSize = True, .Location = New Point(x, y)}
        End Function

        ''' <summary>Enter pasa al siguiente campo, como EnviarTab de VB6, salvo en la rejilla y los cuadros de varias líneas.</summary>
        Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
            If keyData = Keys.Enter AndAlso ActiveControl IsNot txtBarras Then
                Dim actual = ActiveControl
                Dim esCampo = TypeOf actual Is ComboBox OrElse (TypeOf actual Is TextBox AndAlso Not CType(actual, TextBox).Multiline)
                If esCampo AndAlso TypeOf actual IsNot IDataGridViewEditingControl Then
                    SelectNextControl(actual, True, True, True, True)
                    Return True
                End If
            End If
            Return MyBase.ProcessCmdKey(msg, keyData)
        End Function

        ' ---- Carga ----

        Private Async Sub Cargar(sender As Object, e As EventArgs)
            Try
                _catalogos = Await _repo.CatalogosAsync(_local)
                cmbUnidad.Items.AddRange(_catalogos.Unidades.ToArray())
                cmbMarca.Items.Add(New OpcionArticulo With {.Codigo = "0", .Descripcion = "(SIN MARCA)"})
                cmbMarca.Items.AddRange(_catalogos.Marcas.ToArray())
                cmbMoneda.Items.AddRange(_catalogos.Monedas.ToArray())
                For Each iva In _catalogos.Ivas
                    cmbIva.Items.Add(iva)
                Next
                For i = 0 To 5
                    gridPrecios.Rows.Add($"{i + 1}. {_catalogos.EtiquetasPrecio(i)}", "0", "0", "0")
                Next
                Dim delDia = gridPrecios.Rows(_catalogos.PrecioDelDia - 1)
                delDia.Cells(colEtiqueta.Index).Style.BackColor = Color.FromArgb(198, 239, 206)
                delDia.Cells(colEtiqueta.Index).Style.Font = New Font(gridPrecios.Font, FontStyle.Bold)
                Await LlenarAsync(cmbFiltroCat, 1, 0, 0, True)
                Await LlenarAsync(cmbCat, 1, 0, 0, False)
                Await SugeridoAsync()
                Limpiar()
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        Private Async Function SugeridoAsync() As Task
            lblSugerido.Text = $"Próximo código corto sugerido: {Await _repo.ProximoSugeridoAsync(_local)}"
        End Function

        ''' <summary>Llena un nivel de categorías. El filtro empieza con (Todas), que no filtra.</summary>
        Private Async Function LlenarAsync(combo As ComboBox, nivel As Integer, categ As Integer, scateg As Integer, filtro As Boolean) As Task
            Dim opciones = If(nivel = 1 OrElse categ > 0, Await _repo.CategoriasAsync(_local, nivel, categ, scateg), New List(Of OpcionArticulo))
            Dim anterior = _pintando
            _pintando = True
            combo.Items.Clear()
            If filtro Then combo.Items.Add(New OpcionArticulo With {.Codigo = "0", .Descripcion = "(Todas)"})
            combo.Items.AddRange(opciones.ToArray())
            If filtro Then combo.SelectedIndex = 0
            _pintando = anterior
        End Function

        Private Shared Function CodigoDe(combo As ComboBox) As Integer
            Dim item = TryCast(combo.SelectedItem, OpcionArticulo)
            Return If(item Is Nothing, 0, CInt(Core.Vb6.Val(item.Codigo)))
        End Function

        Private Shared Sub Seleccionar(combo As ComboBox, valor As String)
            valor = If(valor, "").Trim()
            combo.SelectedIndex = combo.Items.Cast(Of Object)().ToList().FindIndex(Function(i) TypeOf i Is OpcionArticulo AndAlso CType(i, OpcionArticulo).Codigo.Trim() = valor)
        End Sub

        ''' <summary>Al cambiar un nivel se recargan los de abajo, como SDCategorias.</summary>
        Private Async Function CascadaAsync(padre As ComboBox, hijo As ComboBox, nieto As ComboBox, filtro As Boolean) As Task
            If _pintando Then Return
            Try
                Dim nivelHijo = If(nieto Is Nothing, 3, 2)
                Dim categ = If(nivelHijo = 2, CodigoDe(padre), CodigoDe(If(filtro, cmbFiltroCat, cmbCat)))
                Dim scateg = If(nivelHijo = 3, CodigoDe(padre), 0)
                Await LlenarAsync(hijo, nivelHijo, categ, scateg, filtro)
                If nieto IsNot Nothing Then Await LlenarAsync(nieto, 3, 0, 0, filtro)
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Function

        ''' <summary>Pinta las categorías del artículo en los tres combos, cargando cada nivel.</summary>
        Private Async Function MostrarCategoriasAsync(categ As Integer, scateg As Integer, sscateg As Integer) As Task
            _pintando = True
            Seleccionar(cmbCat, categ.ToString(CultureInfo.InvariantCulture))
            Await LlenarAsync(cmbSCat, 2, categ, 0, False)
            _pintando = True
            Seleccionar(cmbSCat, scateg.ToString(CultureInfo.InvariantCulture))
            Await LlenarAsync(cmbSSCat, 3, categ, scateg, False)
            _pintando = True
            Seleccionar(cmbSSCat, sscateg.ToString(CultureInfo.InvariantCulture))
            _pintando = False
        End Function

        ' ---- Números ----

        Private Shared Function Numero(texto As String) As Double
            Dim valor As Double
            Return If(Double.TryParse(If(texto, "").Trim(), NumberStyles.Number, Cultura, valor), valor, 0)
        End Function

        Private Shared Function Monto(valor As Double) As String
            Return valor.ToString("N2", Cultura)
        End Function

        Private Shared Function Porcentaje(valor As Double) As String
            Return valor.ToString("0.###", Cultura)
        End Function

        Private Function IvaActual() As Double
            Return If(cmbIva.SelectedItem Is Nothing, 0, CDbl(cmbIva.SelectedItem))
        End Function

        ''' <summary>El IVA con que se muestran los precios: 0 si la casilla I.V.A. está desmarcada.</summary>
        Private Function IvaMostrado() As Double
            Return If(chkConIva.Checked, IvaActual(), 0)
        End Function

        Private Function Celda(fila As Integer, columna As DataGridViewColumn) As Double
            Return Numero(Convert.ToString(gridPrecios.Rows(fila).Cells(columna.Index).Value, Cultura))
        End Function

        Private Sub FijarCelda(fila As Integer, columna As DataGridViewColumn, valor As Double)
            gridPrecios.Rows(fila).Cells(columna.Index).Value = If(columna Is colUtilidad, Porcentaje(valor), Monto(valor))
        End Sub

        ''' <summary>SDT1_LostFocus: el precio de una fila desde su utilidad y su extra.</summary>
        Private Sub PrecioDesdeUtilidad(fila As Integer)
            Dim precio = PreciosArticulo.PrecioDesdeUtilidad(Numero(txtCosto.Text), Numero(txtDescVenta.Text), Numero(txtPorcOtros.Text), Numero(txtMontoOtros.Text),
                                                             Celda(fila, colExtra) + Numero(txtFlete.Text), Celda(fila, colUtilidad), IvaMostrado())
            FijarCelda(fila, colPrecio, precio)
        End Sub

        Private Sub RecalcularPrecios()
            If _pintando OrElse _modo = ModoFicha.Normal Then Return
            For i = 0 To 5
                PrecioDesdeUtilidad(i)
            Next
            MostrarPrecioDelDia()
            MostrarCostoNeto()
        End Sub

        Private Sub MostrarCostoNeto()
            Dim neto = PreciosArticulo.CostoNeto(Numero(txtCosto.Text), Numero(txtDescCompra.Text), Numero(txtPorcOtros.Text), Numero(txtFlete.Text), Numero(txtMontoOtros.Text))
            lblCostoNeto.Text = $"Costo neto: {Monto(neto)}"
        End Sub

        ''' <summary>Moneda1_Change: el precio del día, redondeado hacia arriba con POSFacRedon si pasa de 10.</summary>
        Private Sub MostrarPrecioDelDia()
            Dim precio = Celda(_catalogos.PrecioDelDia - 1, colPrecio)
            If precio > 10 Then precio = PreciosArticulo.Redondear(precio, _catalogos.FactorRedondeo, DireccionRedondeo.Ascendente)
            lblPrecioDia.Text = $"{If(cmbMoneda.SelectedItem, "")}{Monto(precio)}"
        End Sub

        Private Sub Precio_Validar(sender As Object, e As DataGridViewCellValidatingEventArgs)
            If e.ColumnIndex = colEtiqueta.Index Then Return
            Dim valor As Double
            If Not Double.TryParse(Convert.ToString(e.FormattedValue, Cultura).Trim(), NumberStyles.Number, Cultura, valor) Then
                MessageBox.Show(Me, "Digite un número.", Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                e.Cancel = True
            End If
        End Sub

        ''' <summary>
        ''' Cambiar la utilidad o el extra recalcula el precio; cambiar el precio recalcula la utilidad
        ''' (Moneda1_LostFocus), con el mismo IVA con que se muestra el precio.
        ''' </summary>
        Private Sub Precio_Editado(sender As Object, e As DataGridViewCellEventArgs)
            If _pintando OrElse e.RowIndex < 0 Then Return
            If e.ColumnIndex = colPrecio.Index Then
                Dim precio = Celda(e.RowIndex, colPrecio)
                If precio <> 0 Then
                    Dim utilidad = PreciosArticulo.UtilidadDesdePrecio(Numero(txtCosto.Text), Numero(txtDescVenta.Text), precio, Numero(txtFlete.Text) + Celda(e.RowIndex, colExtra), IvaMostrado())
                    FijarCelda(e.RowIndex, colUtilidad, utilidad)
                End If
                FijarCelda(e.RowIndex, colPrecio, precio)
            ElseIf e.ColumnIndex = colUtilidad.Index OrElse e.ColumnIndex = colExtra.Index Then
                FijarCelda(e.RowIndex, CType(gridPrecios.Columns(e.ColumnIndex), DataGridViewColumn), Celda(e.RowIndex, CType(gridPrecios.Columns(e.ColumnIndex), DataGridViewColumn)))
                PrecioDesdeUtilidad(e.RowIndex)
            End If
            MostrarPrecioDelDia()
        End Sub

        ''' <summary>
        ''' Check1_Click: en VB6 la casilla está siempre activa. Al consultar solo cambia cómo se muestran los
        ''' precios guardados; al editar los recalcula desde las utilidades.
        ''' </summary>
        Private Sub ConIva_Cambio(sender As Object, e As EventArgs)
            If _pintando Then Return
            If _modo = ModoFicha.Normal Then
                If _articulo Is Nothing Then Return
                For i = 0 To 5
                    FijarCelda(i, colPrecio, PreciosArticulo.PrecioParaMostrar(_articulo.Precio(i), _articulo.Exento, chkConIva.Checked))
                Next
                MostrarPrecioDelDia()
            Else
                RecalcularPrecios()
            End If
        End Sub

        ''' <summary>sdtDescuent_LostFocus: el descuento de venta no puede pasar al de compra.</summary>
        Private Sub DescuentoCompra_Salir(sender As Object, e As EventArgs)
            If Numero(txtDescCompra.Text) < Numero(txtDescVenta.Text) Then txtDescVenta.Text = txtDescCompra.Text
            RecalcularPrecios()
        End Sub

        Private Sub DescuentoVenta_Validar(sender As Object, e As ComponentModel.CancelEventArgs)
            If _modo = ModoFicha.Normal Then Return
            If Numero(txtDescVenta.Text) > Numero(txtDescCompra.Text) Then
                MessageBox.Show(Me, $"El descuento a aplicar no puede ser mayor a {txtDescCompra.Text}.", Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                e.Cancel = True
            End If
        End Sub

        ''' <summary>Sin cantidad, sin precio y sin peso se excluyen entre sí.</summary>
        Private Sub Excluyente(marcada As CheckBox)
            If Not marcada.Checked Then Return
            For Each c In {chkSinCantidad, chkSinPrecio, chkSinPeso}
                If c IsNot marcada Then c.Checked = False
            Next
        End Sub

        ' ---- Ficha ----

        ''' <summary>SD_Borrar: deja la ficha vacía.</summary>
        Private Sub Limpiar()
            _pintando = True
            _articulo = Nothing
            _foto = Nothing
            For Each caja In {txtCodigo, txtDescripcion, txtDescCorta, txtPresentacion, txtEmpaque, txtCabys, txtEspecificacion, txtCreadoPor, txtCreado, txtModificado}
                caja.Clear()
            Next
            For Each caja In {txtCosto, txtPonderado, txtPromedio, txtFlete}
                caja.Text = Monto(0)
            Next
            For Each caja In {txtMayor, txtDescCompra, txtDescVenta, txtDescMax, txtPorcOtros, txtMontoOtros}
                caja.Text = "0"
            Next
            For Each c In {chkSinCantidad, chkSinPrecio, chkSinPeso, chkServicio, chkCanasta, chkNoFacturar, chkNegativos, chkConsignacion}
                c.Checked = False
            Next
            cmbUnidad.SelectedIndex = -1
            cmbMarca.SelectedIndex = -1
            cmbCat.SelectedIndex = -1
            cmbSCat.Items.Clear()
            cmbSSCat.Items.Clear()
            cmbMoneda.SelectedIndex = If(cmbMoneda.Items.Count > 0, 0, -1)
            cmbIva.SelectedIndex = -1
            For i = 0 To gridPrecios.Rows.Count - 1
                FijarCelda(i, colUtilidad, 0)
                FijarCelda(i, colExtra, 0)
                FijarCelda(i, colPrecio, 0)
            Next
            lblCorto.Text = ""
            lblExistencia.Text = ""
            lblPrecioDia.Text = ""
            lblCostoNeto.Text = ""
            _pintando = False
        End Sub

        Private Sub SeleccionarIva(iva As Double)
            Dim i = cmbIva.Items.Cast(Of Object)().ToList().FindIndex(Function(x) CDbl(x) = iva)
            If i < 0 Then i = cmbIva.Items.Add(iva)
            cmbIva.SelectedIndex = i
        End Sub

        ''' <summary>SD_CargarArt: pinta el artículo. Avisa si no existe o no está en el local.</summary>
        Private Async Function CargarArticuloAsync(codigo As String) As Task(Of Boolean)
            codigo = If(codigo, "").Trim()
            If codigo.Length = 0 Then Return False
            _versionCarga += 1
            Dim version = _versionCarga
            Dim a = Await _repo.ObtenerAsync(codigo, _local)
            If version <> _versionCarga Then Return False
            If a Is Nothing Then
                MessageBox.Show(Me, "No se encontró ningún artículo con este código.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return False
            End If
            If Not ArticulosRepositorio.SePuedeMostrar(a) Then
                MessageBox.Show(Me, $"El artículo [{a.Descripcion}] no se encuentra registrado en el local {_localDescripcion}.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return False
            End If
            Limpiar()
            _pintando = True
            _articulo = a
            txtCodigo.Text = a.CodigoArt
            txtDescripcion.Text = a.Descripcion
            txtDescCorta.Text = a.DescCorta
            Seleccionar(cmbUnidad, a.UnidadMedida)
            Seleccionar(cmbMarca, a.CodArtProv.ToString(CultureInfo.InvariantCulture))
            txtPresentacion.Text = a.Grupo.ToString(Cultura)
            txtEmpaque.Text = a.ValorEmpaque
            txtMayor.Text = a.MXMayor.ToString(Cultura)
            txtCabys.Text = a.CodAlterno
            txtEspecificacion.Text = a.Especificacion
            txtCreadoPor.Text = If(String.IsNullOrEmpty(a.UsuarioCrea), "N/R", a.UsuarioCrea)
            txtCreado.Text = If(a.FC.HasValue, a.FC.Value.ToString("g", Cultura), "")
            txtModificado.Text = If(a.FM.HasValue, a.FM.Value.ToString("g", Cultura), "")
            Dim moneda = If(a.Moneda, "").Trim()
            If moneda.Length = 0 OrElse moneda = "0" Then
                cmbMoneda.SelectedIndex = If(cmbMoneda.Items.Count > 0, 0, -1)
            Else
                Dim i = cmbMoneda.Items.IndexOf(moneda)
                If i < 0 Then i = cmbMoneda.Items.Add(moneda)
                cmbMoneda.SelectedIndex = i
            End If
            txtCosto.Text = Monto(a.PrecioCosto)
            txtPonderado.Text = Monto(a.CostoPonderado)
            txtPromedio.Text = Monto(If(a.CostoPromedio, 0))
            txtFlete.Text = Monto(a.Flete)
            SeleccionarIva(a.Exento)
            txtDescCompra.Text = Porcentaje(a.Descuento)
            txtDescVenta.Text = Porcentaje(a.DescuentoA)
            txtDescMax.Text = Porcentaje(a.Porcentaje)
            txtPorcOtros.Text = Porcentaje(a.PorcentajeOtros)
            txtMontoOtros.Text = Monto(a.Recomendada)
            chkSinCantidad.Checked = Articulo.EsMarcado(a.SinCantidad)
            chkSinPrecio.Checked = Articulo.EsMarcado(a.SinPrecio)
            chkSinPeso.Checked = Articulo.EsMarcado(a.SinPeso)
            chkServicio.Checked = Articulo.EsMarcado(a.Servicio)
            chkCanasta.Checked = a.SubGrupo <> 0
            chkNoFacturar.Checked = Articulo.EsMarcado(a.Consumo)
            chkNegativos.Checked = Articulo.EsMarcado(a.Gasto)
            chkConsignacion.Checked = Articulo.EsMarcado(a.Consignacion)
            For i = 0 To 5
                FijarCelda(i, colUtilidad, a.Utilidad(i))
                FijarCelda(i, colExtra, a.Extra(i))
                FijarCelda(i, colPrecio, PreciosArticulo.PrecioParaMostrar(a.Precio(i), a.Exento, chkConIva.Checked))
            Next
            lblCorto.Text = $"Cód: {a.CodigoCorto}"
            lblExistencia.Text = $"Existencia: {a.Stock.ToString("N2", Cultura)}"
            _pintando = False
            Await MostrarCategoriasAsync(a.Categ, a.SCateg, a.SSCateg)
            MostrarPrecioDelDia()
            MostrarCostoNeto()
            btnAnterior.Enabled = True
            btnSiguiente.Enabled = True
            Return True
        End Function

        ''' <summary>SD_Habilitar y SD_Mandos: qué se puede tocar según el modo y los privilegios 5037 y 5056.</summary>
        Private Sub Habilitar(editando As Boolean)
            For Each c As Control In New Control() {txtDescripcion, txtDescCorta, cmbUnidad, cmbMarca, cmbCat, cmbSCat, cmbSSCat, txtPresentacion, txtEmpaque, txtMayor, txtCabys, txtEspecificacion,
                                                     cmbIva, txtDescMax, txtPorcOtros, txtMontoOtros, gbFacturar, gbOtros}
                c.Enabled = editando
            Next
            Dim precios = editando AndAlso _puedeUtilidades
            txtFlete.Enabled = precios
            txtDescCompra.Enabled = precios
            txtDescVenta.Enabled = precios
            colUtilidad.ReadOnly = Not precios
            colExtra.ReadOnly = Not precios
            colPrecio.ReadOnly = Not precios
            txtCodigo.Enabled = editando AndAlso _modo = ModoFicha.Nuevo
            gbFiltro.Enabled = Not editando
            btnAnterior.Enabled = Not editando
            btnSiguiente.Enabled = Not editando
        End Sub

        ' ---- Navegación y búsqueda ----

        Private Async Function NavegarAsync(siguiente As Boolean) As Task
            Try
                Dim codigo = Await _repo.VecinoAsync(txtCodigo.Text.Trim(), _local, siguiente, CodigoDe(cmbFiltroCat), CodigoDe(cmbFiltroSCat), CodigoDe(cmbFiltroSSCat))
                If codigo.Length = 0 Then
                    If siguiente Then btnSiguiente.Enabled = False Else btnAnterior.Enabled = False
                    Return
                End If
                Await CargarArticuloAsync(codigo)
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Function

        ''' <summary>Buscar: el listado del local, filtrado por las categorías elegidas.</summary>
        Private Async Function BuscarAsync() As Task
            UseWaitCursor = True
            Try
                Using datos = Await _repo.BuscarAsync(_local, CodigoDe(cmbFiltroCat), CodigoDe(cmbFiltroSCat), CodigoDe(cmbFiltroSSCat))
                    UseWaitCursor = False
                    Using buscador As New FrmBuscador("Seleccione el artículo", datos)
                        If buscador.ShowDialog(Me) = DialogResult.OK AndAlso buscador.Resultado(1).Length > 0 Then
                            Await CargarArticuloAsync(buscador.Resultado(1))
                        End If
                    End Using
                End Using
            Finally
                UseWaitCursor = False
            End Try
        End Function

        ''' <summary>SDTCodBar_KeyDown: un código de barras, un código corto o el del artículo. Vacío abre el buscador.</summary>
        Private Async Sub Barras_Tecla(sender As Object, e As KeyEventArgs)
            If e.KeyCode <> Keys.Enter Then Return
            e.SuppressKeyPress = True
            Try
                Dim texto = txtBarras.Text.Trim()
                If texto.Length = 0 Then
                    Await BuscarAsync()
                    Return
                End If
                Dim resultado = Await _repo.BuscarCodigoBarrasAsync(texto, _local)
                Dim codigo = resultado.Articulo
                If resultado.Opciones IsNot Nothing Then
                    Using buscador As New FrmBuscador("Seleccione el código correcto", resultado.Opciones)
                        codigo = If(buscador.ShowDialog(Me) = DialogResult.OK, buscador.Resultado(0), "")
                    End Using
                    resultado.Opciones.Dispose()
                    If codigo.Length = 0 Then Return
                End If
                If codigo.Length = 0 Then codigo = texto
                Await CargarArticuloAsync(codigo)
                txtBarras.Clear()
                txtBarras.Focus()
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        ''' <summary>txtCodigoArt_Validate: un código nuevo no puede estar usado ni llevar caracteres prohibidos.</summary>
        Private Async Sub Codigo_Salir(sender As Object, e As EventArgs)
            If _modo <> ModoFicha.Nuevo Then Return
            Dim codigo = txtCodigo.Text.Trim()
            If codigo.Length = 0 Then Return
            Dim prohibido = codigo.FirstOrDefault(Function(ch) CaracteresCodigo.Contains(ch))
            If prohibido <> Nothing Then
                MessageBox.Show(Me, $"El carácter ( {prohibido} ) no está permitido.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information)
                txtCodigo.Focus()
                Return
            End If
            Try
                Dim usado = Await _repo.DescripcionDeAsync(codigo)
                If usado.Length > 0 AndAlso _modo = ModoFicha.Nuevo AndAlso txtCodigo.Text.Trim() = codigo Then
                    MessageBox.Show(Me, $"El código {codigo} ya fue asignado al artículo:{Environment.NewLine}{usado}", Text, MessageBoxButtons.OK, MessageBoxIcon.Information)
                    txtCodigo.Clear()
                    txtCodigo.Focus()
                End If
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        ' ---- Guardar ----

        Private Function Prohibido(texto As String) As Char
            Return If(texto, "").FirstOrDefault(Function(ch) CaracteresTexto.Contains(ch))
        End Function

        ''' <summary>Las validaciones de SDBoton_Guardar, más las categorías, que en VB6 se perdían por un error.</summary>
        Private Function DatosValidos() As Boolean
            Dim aviso As String = Nothing
            If txtCodigo.Text.Trim().Length = 0 Then
                aviso = "Digite el código del artículo."
            ElseIf txtDescripcion.Text.Trim().Length = 0 Then
                aviso = "Digite la descripción del artículo."
            ElseIf CodigoDe(cmbCat) = 0 OrElse CodigoDe(cmbSCat) = 0 OrElse CodigoDe(cmbSSCat) = 0 OrElse cmbUnidad.SelectedItem Is Nothing Then
                aviso = "Para un correcto funcionamiento del sistema, se requiere que asigne categorías al artículo y unidad de medida."
            ElseIf txtCabys.Text.Trim().Length = 0 Then
                aviso = "El código de hacienda es obligatorio."
            End If
            If aviso Is Nothing Then
                For Each caja In {txtDescripcion, txtDescCorta, txtEspecificacion}
                    Dim c = Prohibido(caja.Text)
                    If c <> Nothing Then
                        aviso = $"El carácter ( {c} ) no está permitido en {caja.AccessibleName.ToLower(Cultura)}."
                        Exit For
                    End If
                Next
            End If
            If aviso Is Nothing Then
                Dim negativa = Enumerable.Range(0, 6).Any(Function(i) Celda(i, colUtilidad) < 0)
                If negativa AndAlso Not _catalogos.UsuariosUtilidadNegativa.Contains(_login) Then aviso = "No está permitido guardar utilidades en negativo."
            End If
            If aviso IsNot Nothing Then
                MessageBox.Show(Me, aviso, Text, MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return False
            End If
            Return True
        End Function

        Private Function ArticuloDigitado() As Articulo
            Dim a = If(_articulo, New Articulo())
            Dim iva = IvaActual()
            a.CodigoArt = txtCodigo.Text.Trim()
            a.CodAlterno = txtCabys.Text.Trim()
            a.Descripcion = txtDescripcion.Text.Trim()
            a.DescCorta = If(txtDescCorta.Text.Trim().Length = 0, Strings.Left(a.Descripcion, 20), txtDescCorta.Text.Trim())
            ' VB6 guarda en Ubicacion lo que muestra "Creado por" y en UsuarioCrea el usuario si está vacío.
            a.Ubicacion = txtCreadoPor.Text.Trim()
            a.UsuarioCrea = If(a.Ubicacion.Length = 0, _login, a.Ubicacion)
            a.Grupo = Numero(txtPresentacion.Text)
            a.SubGrupo = If(chkCanasta.Checked, 1, 0)
            a.SinCantidad = Articulo.Marca(chkSinCantidad.Checked)
            a.UnidadMedida = CType(cmbUnidad.SelectedItem, OpcionArticulo).Codigo
            a.ValorEmpaque = txtEmpaque.Text.Trim()
            a.Moneda = If(cmbMoneda.SelectedItem, "").ToString()
            a.Flete = Numero(txtFlete.Text)
            a.Exento = iva
            a.Porcentaje = Numero(txtDescMax.Text)
            a.SinPeso = Articulo.Marca(chkSinPeso.Checked)
            a.PorcentajeOtros = Numero(txtPorcOtros.Text)
            a.Consignacion = Articulo.Marca(chkConsignacion.Checked)
            a.SinPrecio = Articulo.Marca(chkSinPrecio.Checked)
            a.Gasto = Articulo.Marca(chkNegativos.Checked)
            a.Servicio = Articulo.Marca(chkServicio.Checked)
            a.Descuento = Numero(txtDescCompra.Text)
            a.Recomendada = Numero(txtMontoOtros.Text)
            a.DescuentoA = Numero(txtDescVenta.Text)
            a.CodArtProv = If(cmbMarca.SelectedItem Is Nothing, 0, CInt(Core.Vb6.Val(CType(cmbMarca.SelectedItem, OpcionArticulo).Codigo)))
            a.Especificacion = txtEspecificacion.Text.Trim()
            a.Consumo = Articulo.Marca(chkNoFacturar.Checked)
            a.Usuario = _uid.ToString(CultureInfo.InvariantCulture)
            a.MXMayor = CLng(Math.Max(0, Numero(txtMayor.Text)))
            If _modo = ModoFicha.Nuevo Then a.FC = Date.Now
            Dim utilidades = Enumerable.Range(0, 6).Select(Function(i) Celda(i, colUtilidad)).ToArray()
            Dim extras = Enumerable.Range(0, 6).Select(Function(i) Celda(i, colExtra)).ToArray()
            Dim precios = Enumerable.Range(0, 6).Select(Function(i) PreciosArticulo.PrecioParaGuardar(Celda(i, colPrecio), iva, chkConIva.Checked)).ToArray()
            a.FijarPrecios(utilidades, extras, precios)
            Return a
        End Function

        ''' <summary>SD_CompReg: si otro usuario cambió el artículo, lo muestra y pregunta si se reemplaza.</summary>
        Private Async Function ConfirmarCambiosDeOtrosAsync(codigo As String) As Task(Of Boolean)
            Dim cambios = Await _repo.CambiosDeOtrosAsync(codigo, _foto)
            If cambios.Count = 0 Then Return True
            Dim texto = ArticulosRepositorio.TextoCambios("El sistema detectó un cambio en el registro actual realizado por otro usuario. Los datos modificados son los siguientes:", cambios)
            texto &= Environment.NewLine & "¿Desea guardar los datos? Si selecciona Sí, sus datos reemplazarán los modificados por el otro usuario."
            Return MessageBox.Show(Me, texto, Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) = DialogResult.Yes
        End Function

        ' ---- Barra ----

        ''' <summary>Devuelve True para que la barra no cambie de estado.</summary>
        Private Async Function Barra_Clic(boton As BotonBarra) As Task(Of Boolean)
            Try
                Select Case boton
                    Case BotonBarra.Nuevo
                        _previo = If(_articulo?.CodigoArt, "")
                        _modo = ModoFicha.Nuevo
                        Limpiar()
                        txtCreado.Text = Date.Now.ToString("g", Cultura)
                        txtCodigo.Text = (Await _repo.CodigoNuevoAsync(_local)).ToString(CultureInfo.InvariantCulture)
                        Habilitar(True)
                        tabs.SelectedTab = tabDetalle
                        txtCodigo.Focus()
                    Case BotonBarra.Editar
                        If _articulo Is Nothing Then Return True
                        _foto = Await _repo.FotoAsync(_articulo.CodigoArt)
                        _modo = ModoFicha.Editar
                        Habilitar(True)
                        txtDescripcion.Focus()
                    Case BotonBarra.Guardar
                        If Not ValidateChildren() OrElse Not DatosValidos() Then Return True
                        Dim codigo = txtCodigo.Text.Trim()
                        If _modo = ModoFicha.Nuevo Then
                            Dim usado = Await _repo.DescripcionDeAsync(codigo)
                            If usado.Length > 0 Then
                                MessageBox.Show(Me, $"El código {codigo} ya fue asignado al artículo:{Environment.NewLine}{usado}", Text, MessageBoxButtons.OK, MessageBoxIcon.Information)
                                Return True
                            End If
                        ElseIf Not Await ConfirmarCambiosDeOtrosAsync(codigo) Then
                            Return True
                        End If
                        Dim a = ArticuloDigitado()
                        Await _repo.GuardarAsync(a, _modo = ModoFicha.Nuevo, _local, _uid, _estacion, CodigoDe(cmbCat), CodigoDe(cmbSCat), CodigoDe(cmbSSCat), _foto)
                        _modo = ModoFicha.Normal
                        Habilitar(False)
                        Await CargarArticuloAsync(codigo)
                        Await SugeridoAsync()
                        txtBarras.Focus()
                    Case BotonBarra.Cancelar
                        Dim codigo = If(_modo = ModoFicha.Nuevo, _previo, If(_articulo?.CodigoArt, ""))
                        _modo = ModoFicha.Normal
                        Habilitar(False)
                        Limpiar()
                        If codigo.Length > 0 Then Await CargarArticuloAsync(codigo)
                        txtBarras.Focus()
                    Case BotonBarra.Buscar
                        Await BuscarAsync()
                    Case BotonBarra.Borrar
                        If _articulo Is Nothing Then Return True
                        Dim codigo = _articulo.CodigoArt
                        Dim aviso = Await _repo.ValidarBorrarAsync(codigo, _local)
                        If aviso.Length > 0 Then
                            MessageBox.Show(Me, aviso, Text, MessageBoxButtons.OK, MessageBoxIcon.Information)
                            Return True
                        End If
                        If MessageBox.Show(Me, $"¿Desea eliminar el artículo {codigo} - {_articulo.Descripcion}?", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) <> DialogResult.Yes Then Return True
                        Await _repo.BorrarAsync(codigo, _local, _uid, _estacion)
                        Limpiar()
                    Case BotonBarra.Salir
                        Close()
                End Select
            Catch ex As MySqlException
                Mensajes.NoGuardo(ex.Message, "ARTICULOS", Modulo.TituloModulo)
                Return True
            End Try
            Return False
        End Function

    End Class

End Namespace
