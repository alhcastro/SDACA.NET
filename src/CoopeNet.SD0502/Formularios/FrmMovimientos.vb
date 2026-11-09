Imports System.Globalization
Imports MySqlConnector
Imports CoopeNet.Core.Configuracion
Imports CoopeNet.Core.Pos
Imports CoopeNet.Ui

Namespace SD0502

    ''' <summary>
    ''' Movimientos del artículo. Migración de SD0502\REP\frmVentDetall.frm ("Ventas por artículo"), que abre el
    ''' botón "Ver movimientos" del Catálogo de Artículos: la lista de artículos de la misma subcategoría o por
    ''' descripción, el gráfico de compras y ventas de los últimos doce meses y los informes resumido (RPT0013) y
    ''' detallado (RPT0017).
    ''' </summary>
    ''' <remarks>
    ''' El gráfico es propio (MSChart no existe en .NET) y tiene los tipos de MSChart en 2D. No se migra el lector
    ''' serial. No escribe en la base: VB6 creaba posinventariotmp_UID y llenaba posmeses.
    ''' </remarks>
    Public Class FrmMovimientos
        Inherits Form

        Private ReadOnly _repo As MovimientosArticuloRepositorio
        Private ReadOnly _articulos As ArticulosRepositorio
        Private ReadOnly _local As Long
        Private ReadOnly _localDescripcion As String
        Private ReadOnly _empresa As String
        Private ReadOnly _configIni As String
        Private ReadOnly _codigoInicial As String
        Private _pintando As Boolean
        Private _versionLista As Integer

        Private Shared ReadOnly Cultura As CultureInfo = CultureInfo.CurrentCulture

        ''' <summary>Los tipos del combo de VB6 van en pares 3D/2D; aquí se ofrecen los 2D y se guarda su índice.</summary>
        Private Shared ReadOnly Tipos As (Nombre As String, Tipo As TipoGrafico)() = {
            ("Barras", TipoGrafico.Barras), ("Líneas", TipoGrafico.Lineas), ("Área", TipoGrafico.Area), ("Gradas", TipoGrafico.Gradas), ("Combinación", TipoGrafico.Combinacion)}

        Private ReadOnly txtCodigo As New TextBox With {.Name = "txtCodigo", .AccessibleName = "Código", .Location = New Point(70, 12), .Width = 130, .MaxLength = 32}
        Private ReadOnly txtDescripcion As New TextBox With {.Name = "txtDescripcion", .AccessibleName = "Descripción", .CharacterCasing = CharacterCasing.Upper, .Location = New Point(210, 12), .Width = 738}
        Private ReadOnly grid As New DataGridView With {.Name = "grid", .AccessibleName = "Artículos", .Dock = DockStyle.Fill, .ReadOnly = True, .AllowUserToAddRows = False,
            .AllowUserToDeleteRows = False, .AllowUserToResizeRows = False, .MultiSelect = False, .RowHeadersVisible = False,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect, .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, .Margin = New Padding(0, 0, 8, 8)}
        Private ReadOnly opciones As New Panel With {.Name = "opciones", .Dock = DockStyle.Fill, .Margin = New Padding(0, 0, 0, 8)}
        Private ReadOnly gbPeriodo As New GroupBox With {.Name = "gbPeriodo", .Text = "Período", .Location = New Point(0, 0), .Size = New Size(256, 128)}
        Private ReadOnly chkInicial As New CheckBox With {.Name = "chkInicial", .Text = "Ver informe desde inventario inicial", .AutoSize = True, .Location = New Point(10, 22)}
        Private ReadOnly dtpDesde As New DateTimePicker With {.Name = "dtpDesde", .AccessibleName = "Desde", .Format = DateTimePickerFormat.Short, .Location = New Point(70, 52), .Width = 176}
        Private ReadOnly dtpHasta As New DateTimePicker With {.Name = "dtpHasta", .AccessibleName = "Hasta", .Format = DateTimePickerFormat.Short, .Location = New Point(70, 88), .Width = 176}
        Private ReadOnly optMensual As New RadioButton With {.Name = "optMensual", .Text = "Mensual", .AutoSize = True, .Location = New Point(10, 138)}
        Private ReadOnly optDiario As New RadioButton With {.Name = "optDiario", .Text = "Diario", .AutoSize = True, .Location = New Point(130, 138), .Checked = True}
        Private ReadOnly btnResumido As New Button With {.Name = "btnResumido", .Text = "&Resumido", .Location = New Point(0, 168), .Size = New Size(256, 32)}
        Private ReadOnly btnDetallado As New Button With {.Name = "btnDetallado", .Text = "&Detallado", .Location = New Point(0, 206), .Size = New Size(256, 32)}
        Private ReadOnly btnSalir As New Button With {.Name = "btnSalir", .Text = "&Salir", .DialogResult = DialogResult.Cancel, .Location = New Point(0, 244), .Size = New Size(256, 32)}
        Private ReadOnly cmbTipo As New ComboBox With {.Name = "cmbTipo", .AccessibleName = "Tipo de gráfico", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(100, 290), .Width = 156}
        Private ReadOnly grafico As New GraficoSeries With {.Name = "grafico", .Dock = DockStyle.Fill, .Margin = Padding.Empty}

        Public Sub New(repositorio As MovimientosArticuloRepositorio, articulos As ArticulosRepositorio, codigo As String, local As Long, localDescripcion As String, empresa As String, directorioSdaca As String)
            _repo = repositorio
            _articulos = articulos
            _codigoInicial = If(codigo, "").Trim()
            _local = local
            _localDescripcion = If(localDescripcion, "")
            _empresa = If(empresa, "")
            _configIni = IO.Path.Combine(If(directorioSdaca, ""), "CONFIG.INI")

            Text = "Ventas por artículo"
            ClientSize = New Size(960, 640)
            MinimumSize = New Size(820, 560)
            MinimizeBox = False
            ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent
            CancelButton = btnSalir

            grid.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Código", .FillWeight = 25})
            grid.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Descripción", .FillWeight = 75})
            gbPeriodo.Controls.AddRange(New Control() {chkInicial, New Label With {.Text = "Desde", .AutoSize = True, .Location = New Point(10, 56)}, dtpDesde,
                                                       New Label With {.Text = "Hasta", .AutoSize = True, .Location = New Point(10, 92)}, dtpHasta})
            opciones.Controls.AddRange(New Control() {gbPeriodo, optMensual, optDiario, btnResumido, btnDetallado, btnSalir,
                                                      New Label With {.Text = "Tipo de gráfico", .AutoSize = True, .Location = New Point(0, 294)}, cmbTipo})
            Dim cuerpo As New TableLayoutPanel With {.Name = "cuerpo", .Location = New Point(12, 46), .Size = New Size(936, 582), .ColumnCount = 2, .RowCount = 2,
                .Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right}
            cuerpo.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
            cuerpo.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 260))
            cuerpo.RowStyles.Add(New RowStyle(SizeType.Absolute, 330))
            cuerpo.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
            cuerpo.Controls.Add(grid, 0, 0)
            cuerpo.Controls.Add(opciones, 1, 0)
            cuerpo.Controls.Add(grafico, 0, 1)
            cuerpo.SetColumnSpan(grafico, 2)
            Controls.AddRange(New Control() {New Label With {.Text = "Código", .AutoSize = True, .Location = New Point(12, 15)}, txtCodigo, txtDescripcion, cuerpo})
            txtDescripcion.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
            Ventanas.Recordar(Me)

            For Each t In Tipos
                cmbTipo.Items.Add(t.Nombre)
            Next
            Dim guardado = CInt(Core.Vb6.Val(ConexionesIni.LeerValor(_configIni, "POS", "TIPO_GRAF")))
            _pintando = True
            cmbTipo.SelectedIndex = Math.Min(Math.Max(guardado, 0) \ 2, Tipos.Length - 1)
            _pintando = False
            grafico.Tipo = Tipos(cmbTipo.SelectedIndex).Tipo
            dtpDesde.Value = Date.Today.AddDays(-30)
            dtpHasta.Value = Date.Today

            AddHandler Load, AddressOf Cargar
            AddHandler txtCodigo.KeyDown, AddressOf Codigo_Tecla
            AddHandler txtDescripcion.KeyUp, AddressOf Descripcion_Tecla
            AddHandler grid.CellClick, AddressOf Lista_Clic
            AddHandler chkInicial.CheckedChanged, Async Sub() Await AplicarInicialAsync()
            AddHandler cmbTipo.SelectedIndexChanged, AddressOf Tipo_Cambio
            AddHandler btnResumido.Click, Async Sub() Await ResumidoAsync()
            AddHandler btnDetallado.Click, Async Sub() Await DetalladoAsync()
        End Sub

        ''' <summary>Form_Load y Form_Activate: el artículo recibido y la casilla de inventario inicial marcada.</summary>
        Private Async Sub Cargar(sender As Object, e As EventArgs)
            Try
                If _codigoInicial.Length > 0 Then
                    txtCodigo.Text = _codigoInicial
                    Await ArticuloAsync(_codigoInicial)
                End If
                chkInicial.Checked = True
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        Private ReadOnly Property Codigo As String
            Get
                Return txtCodigo.Text.Trim()
            End Get
        End Property

        ' ---- Artículo ----

        ''' <summary>SD_DetArtic: el código (o su código de barras), la lista de su subcategoría y el gráfico.</summary>
        Private Async Function ArticuloAsync(texto As String) As Task
            Dim codigo = texto.Trim()
            Dim barras = Await _articulos.BuscarCodigoBarrasAsync(codigo, _local)
            If barras.Opciones IsNot Nothing Then
                Using buscador As New FrmBuscador("Seleccione el código correcto", barras.Opciones)
                    Dim elegido = If(buscador.ShowDialog(Me) = DialogResult.OK, buscador.Resultado(0), "")
                    If elegido.Length > 0 Then codigo = elegido
                End Using
                barras.Opciones.Dispose()
            ElseIf barras.Articulo.Length > 0 Then
                codigo = barras.Articulo
            End If
            Dim a = Await _repo.ArticuloAsync(codigo, _local)
            If a Is Nothing Then Return
            txtCodigo.Text = a.Codigo
            _pintando = True
            txtDescripcion.Text = a.Descripcion
            _pintando = False
            Llenar(Await _repo.MismaCategoriaAsync(_local, a.Categ, a.SCateg, a.SSCateg), a.Codigo)
            Await GraficoAsync()
        End Function

        Private Sub Llenar(lista As List(Of OpcionArticulo), Optional elegir As String = "")
            grid.Rows.Clear()
            For Each o In lista
                grid.Rows.Add(o.Codigo, o.Descripcion)
            Next
            Dim fila = grid.Rows.Cast(Of DataGridViewRow)().FirstOrDefault(Function(r) CStr(r.Cells(0).Value) = elegir)
            If fila IsNot Nothing Then
                grid.CurrentCell = fila.Cells(0)
            ElseIf grid.Rows.Count > 0 Then
                grid.ClearSelection()
            End If
        End Sub

        ''' <summary>SD_Grafico: compras y ventas de los doce meses anteriores.</summary>
        Private Async Function GraficoAsync() As Task
            If Codigo.Length = 0 Then
                grafico.Limpiar()
                Return
            End If
            Dim meses = Await _repo.GraficoAsync(Codigo, _local, Date.Today)
            grafico.Mostrar(meses.Select(Function(m) Cultura.TextInfo.ToTitleCase(m.Mes.ToString("MMM", Cultura).TrimEnd("."c))).ToList(), {"Compras", "Ventas"},
                            meses.Select(Function(m) m.Compras).ToList(), meses.Select(Function(m) m.Ventas).ToList())
        End Function

        ''' <summary>sdtCodigo_KeyDown: vacío abre el buscador de los artículos del local.</summary>
        Private Async Sub Codigo_Tecla(sender As Object, e As KeyEventArgs)
            If e.KeyCode <> Keys.Enter Then Return
            e.SuppressKeyPress = True
            Try
                If Codigo.Length = 0 Then
                    UseWaitCursor = True
                    Using datos = Await _repo.BuscarAsync(_local)
                        UseWaitCursor = False
                        Using buscador As New FrmBuscador("Seleccione el artículo", datos)
                            If buscador.ShowDialog(Me) = DialogResult.OK AndAlso buscador.Resultado(1).Length > 0 Then Await ArticuloAsync(buscador.Resultado(1))
                        End Using
                    End Using
                Else
                    Await ArticuloAsync(Codigo)
                End If
                Await AplicarInicialAsync()
                btnResumido.Focus()
            Catch ex As MySqlException
                Mensajes.NoConecta()
            Finally
                UseWaitCursor = False
            End Try
        End Sub

        ''' <summary>sdtDes_KeyUp: con más de dos letras, la lista muestra los artículos cuya descripción las contiene.</summary>
        Private Async Sub Descripcion_Tecla(sender As Object, e As KeyEventArgs)
            If _pintando OrElse txtDescripcion.Text.Trim().Length <= 2 Then Return
            If e.KeyCode = Keys.Enter OrElse e.KeyCode = Keys.Tab OrElse e.KeyCode = Keys.ShiftKey Then Return
            _versionLista += 1
            Dim version = _versionLista
            Try
                Dim lista = Await _repo.PorDescripcionAsync(_local, txtDescripcion.Text)
                If version = _versionLista Then Llenar(lista)
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        ''' <summary>grdArtic_Click: elige el artículo de la fila y redibuja el gráfico.</summary>
        Private Async Sub Lista_Clic(sender As Object, e As DataGridViewCellEventArgs)
            If e.RowIndex < 0 Then Return
            Dim fila = grid.Rows(e.RowIndex)
            txtCodigo.Text = CStr(fila.Cells(0).Value)
            _pintando = True
            txtDescripcion.Text = CStr(fila.Cells(1).Value)
            _pintando = False
            Try
                Await GraficoAsync()
                Await AplicarInicialAsync()
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        ''' <summary>chkInvInic_Click: con la casilla, el informe empieza en la fecha del inventario inicial.</summary>
        Private Async Function AplicarInicialAsync() As Task
            If Not chkInicial.Checked Then
                dtpDesde.Enabled = True
                Return
            End If
            If Codigo.Length = 0 Then Return
            dtpDesde.Enabled = False
            Try
                Dim inicial = Await _repo.InventarioInicialAsync(Codigo, _local)
                If inicial.HasValue AndAlso inicial.Value.Fecha.Year >= 1900 Then
                    dtpDesde.Value = inicial.Value.Fecha
                Else
                    MessageBox.Show(Me, "No se encuentra la fecha de inventario inicial para el artículo seleccionado.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information)
                End If
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Function

        Private Sub Tipo_Cambio(sender As Object, e As EventArgs)
            grafico.Tipo = Tipos(cmbTipo.SelectedIndex).Tipo
            If _pintando Then Return
            Try
                ConexionesIni.FijarValor(_configIni, "POS", "TIPO_GRAF", (cmbTipo.SelectedIndex * 2 + 1).ToString(CultureInfo.InvariantCulture))
            Catch ex As Exception When TypeOf ex Is IO.IOException OrElse TypeOf ex Is UnauthorizedAccessException
                ' Sin permiso sobre CONFIG.INI solo se pierde el recuerdo del tipo.
            End Try
        End Sub

        ' ---- Informes ----

        Private Function Titulo(clase As String) As String
            Dim t = $"{_empresa}, {_localDescripcion}, {clase}de movimientos por artículo "
            If dtpDesde.Value.Date = dtpHasta.Value.Date Then Return t & $"registrados el {dtpDesde.Value.ToString("D", Cultura)}"
            Return t & $"registrados del {dtpDesde.Value.ToString("D", Cultura)} al {dtpHasta.Value.ToString("D", Cultura)}"
        End Function

        Private Shared Function N(valor As Double) As String
            Return valor.ToString("#,##0.00", Cultura)
        End Function

        Private Sub Ocupado(si As Boolean)
            UseWaitCursor = si
            btnResumido.Enabled = Not si
            btnDetallado.Enabled = Not si
        End Sub

        ''' <summary>SD_Rept y RPT0013: por día o por mes, con el inventario al final de cada fila.</summary>
        Private Async Function ResumidoAsync() As Task
            If Codigo.Length = 0 Then Return
            Ocupado(True)
            Try
                Dim diario = optDiario.Checked
                Dim inicial = Await _repo.InicialResumenAsync(Codigo, _local, dtpDesde.Value, chkInicial.Checked)
                Dim filas = Await _repo.ResumenAsync(Codigo, _local, dtpDesde.Value, dtpHasta.Value, diario)
                Dim acumulado = 0.0
                Dim lineas As New List(Of String())
                For Each f In filas
                    acumulado += f.Total
                    Dim periodo = f.Periodo
                    If periodo.Length > 0 AndAlso periodo.Length < 3 Then periodo = Cultura.DateTimeFormat.GetMonthName(CInt(Core.Vb6.Val(periodo))).ToUpper(Cultura)
                    lineas.Add({periodo, N(f.Compras), N(f.Notas), N(f.Ajustes), N(f.Requisiciones), N(f.Ventas), N(f.Total), N(inicial + acumulado),
                                If(f.Historico.HasValue, f.Historico.Value.ToString("#,##0.0", Cultura), "")})
                Next
                Dim totales = {"Totales:", N(filas.Sum(Function(f) f.Compras)), N(filas.Sum(Function(f) f.Notas)), N(filas.Sum(Function(f) f.Ajustes)),
                               N(filas.Sum(Function(f) f.Requisiciones)), N(filas.Sum(Function(f) f.Ventas)), N(acumulado), N(inicial + acumulado), ""}
                Dim descripcion = If(filas.Count > 0, filas(0).Descripcion, txtDescripcion.Text)
                Dim columnas = {New ColumnaImpresa("Fecha", 13, False), New ColumnaImpresa("Compras", 10), New ColumnaImpresa("+ Not. Cr/De", 10), New ColumnaImpresa("+ Ajustes", 10),
                                New ColumnaImpresa("+ Requisic.", 10), New ColumnaImpresa("- Ventas", 10), New ColumnaImpresa("= Tot.movim.", 11), New ColumnaImpresa("Inv. final", 11), New ColumnaImpresa("Histor.", 9)}
                Dim encabezado = {$"Artículo: {Codigo}  {descripcion}", $"Inventario inicial al {dtpDesde.Value.ToString("dd/MMMM/yyyy", Cultura)}:  {N(inicial)}"}
                Using documento As New TablaImpresa(Titulo($"Reporte {If(diario, "diario", "mensual")} "), _empresa, "SD0502-RPT0013", encabezado, columnas, lineas, totales)
                    Ocupado(False)
                    TablaImpresa.Mostrar(Me, documento)
                End Using
            Catch ex As MySqlException
                MessageBox.Show(Me, $"No se puede mostrar el informe debido a:{vbCrLf}Error: {ex.Message}", Text, MessageBoxButtons.OK, MessageBoxIcon.Information)
            Finally
                Ocupado(False)
            End Try
        End Function

        ''' <summary>SD_ReptDetallado y RPT0017: cada documento, con el saldo después de cada uno.</summary>
        Private Async Function DetalladoAsync() As Task
            If Codigo.Length = 0 Then Return
            Ocupado(True)
            Try
                Dim inicial = 0.0
                If chkInicial.Checked Then
                    Dim i = Await _repo.InventarioInicialAsync(Codigo, _local)
                    If i.HasValue Then inicial = i.Value.Cantidad
                End If
                Dim filas = Await _repo.DetalleAsync(Codigo, _local, dtpDesde.Value, dtpHasta.Value)
                Dim saldo = inicial
                Dim lineas As New List(Of String())
                For Each f In filas
                    saldo += f.Entradas - f.Salidas
                    lineas.Add({f.Fecha.ToString("dd/MM/yyyy HH:mm:ss", Cultura), N(f.Entradas), N(f.Salidas), f.Documento, f.Tipo, N(saldo)})
                Next
                Dim entradas = filas.Sum(Function(f) f.Entradas)
                Dim salidas = filas.Sum(Function(f) f.Salidas)
                Dim totales = {"Totales", N(entradas), N(salidas), "", "Inventario Final:", N(inicial + entradas - salidas)}
                Dim columnas = {New ColumnaImpresa("Fecha", 20, False), New ColumnaImpresa("Entradas", 13), New ColumnaImpresa("Salidas", 13),
                                New ColumnaImpresa("Documento", 15, False), New ColumnaImpresa("Descripción", 20, False), New ColumnaImpresa("Saldo", 14)}
                Dim encabezado As New List(Of String) From {$"Artículo: {Codigo}  {txtDescripcion.Text}"}
                If inicial <> 0 Then encabezado.Add($"Inventario inicial al {dtpDesde.Value.ToString("dd/MMMM/yyyy HH:mm:ss", Cultura)} >>  {N(inicial)}")
                Using documento As New TablaImpresa(Titulo("Reporte detallado "), _empresa, "SD0502-RPT0017", encabezado, columnas, lineas, totales)
                    Ocupado(False)
                    TablaImpresa.Mostrar(Me, documento)
                End Using
            Catch ex As MySqlException
                MessageBox.Show(Me, $"No se puede mostrar el informe debido a:{vbCrLf}Error: {ex.Message}", Text, MessageBoxButtons.OK, MessageBoxIcon.Information)
            Finally
                Ocupado(False)
            End Try
        End Function

    End Class

End Namespace
