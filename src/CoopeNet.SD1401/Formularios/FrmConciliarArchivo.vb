Imports System.IO
Imports System.Text
Imports MySqlConnector
Imports CoopeNet.Core.Bancos
Imports CoopeNet.Ui

Namespace SD1401

    ''' <summary>
    ''' Pasos #1.a y #1.b, privilegio 14004: importar el archivo del banco y conciliar. Migración de
    ''' Fuentes\SD1401\FrmConciliacion.frm (CSV, en un asistente de tres pasos) y FrmConciliacion2.frm (TEC).
    ''' Aquí son una sola pantalla. Bloquea un mes cerrado y, si algo falla, lo avisa.
    ''' </summary>
    Public Class FrmConciliarArchivo
        Inherits FrmConCuenta

        Private Const ColumnasArchivo As Integer = 10
        Private Const PaginaDeCodigos As Integer = 1252

        Private ReadOnly _conciliacion As ConciliacionBancaria
        Private ReadOnly _cierre As CierreMes
        Private ReadOnly _local As Long
        Private ReadOnly _empresa As String
        Private ReadOnly _esTec As Boolean
        Private _filasCsv As New List(Of String())
        Private _movimientosTec As New List(Of MovimientoArchivo)
        Private _mesCerrado As Boolean

        Private ReadOnly dtpInicio As New DateTimePicker With {.Name = "dtpInicio", .AccessibleName = "Fecha inicial", .Format = DateTimePickerFormat.Short, .Location = New Point(90, 142), .Width = 130}
        Private ReadOnly dtpFin As New DateTimePicker With {.Name = "dtpFin", .AccessibleName = "Fecha final", .Format = DateTimePickerFormat.Short, .Location = New Point(280, 142), .Width = 130}
        Private ReadOnly lblInfo As New Label With {.Name = "lblInfo", .AutoSize = True, .ForeColor = Tema.Peligro, .Location = New Point(430, 145)}
        Private ReadOnly cmbSeparador As New ComboBox With {.Name = "cmbSeparador", .AccessibleName = "Separador", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(90, 172), .Width = 130}
        Private ReadOnly btnArchivo As New Button With {.Name = "btnArchivo", .Text = "&Abrir archivo...", .Location = New Point(236, 170), .Size = New Size(130, 28), .UseVisualStyleBackColor = True}
        Private ReadOnly lblArchivo As New Label With {.Name = "lblArchivo", .AutoEllipsis = True, .AutoSize = False, .Location = New Point(376, 176), .Size = New Size(400, 18)}
        Private ReadOnly cmbColumnas As ComboBox() = Enumerable.Range(0, 5).Select(Function(i) New ComboBox With {.DropDownStyle = ComboBoxStyle.DropDownList, .Width = 100}).ToArray()
        Private ReadOnly rejilla As New DataGridView With {
            .Name = "rejilla", .ReadOnly = True, .AllowUserToAddRows = False, .AllowUserToDeleteRows = False, .AllowUserToResizeRows = False, .RowHeadersVisible = False,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect, .MultiSelect = False
        }
        Private ReadOnly chkMigrar As New CheckBox With {.Name = "chkMigrar", .Text = "Conciliar todos los movimientos que procedan del banco", .AutoSize = True}
        Private ReadOnly barraProgreso As New ProgressBar With {.Name = "barraProgreso", .Style = ProgressBarStyle.Marquee, .Visible = False, .Size = New Size(200, 24)}
        Private ReadOnly btnConciliar As New Button With {.Name = "btnGuardar", .Text = "&Conciliar", .Enabled = False, .Size = New Size(110, 30), .UseVisualStyleBackColor = True}
        Private ReadOnly btnSalir As New Button With {.Name = "btnSalir", .Text = "&Salir", .Size = New Size(100, 30), .UseVisualStyleBackColor = True}

        Public Sub New(conciliacion As ConciliacionBancaria, cierre As CierreMes, uid As Long, local As Long, todasLasCuentas As Boolean, empresa As String, esTec As Boolean)
            MyBase.New(cierre, uid, todasLasCuentas, If(esTec, "Conciliación Bancaria TEC", "Conciliación Bancaria CSV"), New Size(790, 620))
            _conciliacion = conciliacion
            _cierre = cierre
            _local = local
            _empresa = empresa
            _esTec = esTec
            Dim hoy = Date.Today
            dtpInicio.Value = New Date(hoy.Year, hoy.Month, 1)
            dtpFin.Value = dtpInicio.Value.AddMonths(1).AddDays(-1)
            cmbSeparador.Items.AddRange(New Object() {"Coma ( , )", "Punto y coma ( ; )", "Tabulador"})
            cmbSeparador.SelectedIndex = 0
            CancelButton = btnSalir

            Controls.AddRange(New Control() {Etiqueta("Desde", 12, 145), dtpInicio, Etiqueta("Hasta", 236, 145), dtpFin, lblInfo, btnArchivo, lblArchivo, rejilla, barraProgreso, btnConciliar, btnSalir})
            Dim yRejilla = 206
            If esTec Then
                btnArchivo.Location = New Point(90, 170)
                lblArchivo.Location = New Point(230, 176)
                lblArchivo.Width = 546
                chkMigrar.Location = New Point(12, 578)
                Controls.Add(chkMigrar)
                For Each c In {("Fecha", 90), ("Tipo", 80), ("Documento", 100), ("Detalle", 0), ("Débitos", 110), ("Créditos", 110)}
                    rejilla.Columns.Add(Columna(c.Item1, c.Item2))
                Next
                rejilla.Columns(4).DefaultCellStyle.Format = "#,##0.00"
                rejilla.Columns(5).DefaultCellStyle.Format = "#,##0.00"
                rejilla.Columns(4).DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
                rejilla.Columns(5).DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
            Else
                Controls.AddRange(New Control() {Etiqueta("Separador", 12, 175), cmbSeparador})
                Dim nombres = {"Fecha", "Documento", "Detalle", "Débitos", "Créditos"}
                For i = 0 To 4
                    Dim x = 12 + i * 154
                    cmbColumnas(i).Name = "cmbColumna" & nombres(i)
                    cmbColumnas(i).AccessibleName = "Columna de " & nombres(i)
                    cmbColumnas(i).Location = New Point(x, 226)
                    cmbColumnas(i).Items.AddRange(Enumerable.Range(0, ColumnasArchivo).Select(Function(n) CObj("Columna " & ChrW(65 + n))).ToArray())
                    Controls.AddRange(New Control() {Etiqueta(nombres(i), x, 206), cmbColumnas(i)})
                Next
                yRejilla = 256
                For n = 0 To ColumnasArchivo - 1
                    rejilla.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = ChrW(65 + n).ToString(), .Width = 100, .SortMode = DataGridViewColumnSortMode.NotSortable})
                Next
            End If
            rejilla.Location = New Point(10, yRejilla)
            rejilla.Size = New Size(770, 572 - yRejilla - If(esTec, 0, -2))
            barraProgreso.Location = New Point(350, 582)
            btnConciliar.Location = New Point(560, 580)
            btnSalir.Location = New Point(680, 580)

            AddHandler dtpInicio.ValueChanged, AddressOf InicioCambio
            AddHandler btnArchivo.Click, AddressOf AbrirArchivo
            AddHandler btnConciliar.Click, AddressOf Conciliar
            AddHandler btnSalir.Click, Sub() Close()
        End Sub

        Private Shared Function Columna(titulo As String, ancho As Integer) As DataGridViewTextBoxColumn
            Dim c As New DataGridViewTextBoxColumn With {.HeaderText = titulo, .SortMode = DataGridViewColumnSortMode.NotSortable}
            If ancho = 0 Then c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill Else c.Width = ancho
            Return c
        End Function

        Private Sub Limpiar()
            rejilla.Rows.Clear()
            _filasCsv = New List(Of String())
            _movimientosTec = New List(Of MovimientoArchivo)
            lblArchivo.Text = ""
            Habilitar()
        End Sub

        Private Sub Habilitar()
            btnConciliar.Enabled = HayCuenta AndAlso Not _mesCerrado AndAlso rejilla.Rows.Count > 0
            btnArchivo.Enabled = HayCuenta AndAlso Not _mesCerrado
        End Sub

        Protected Overrides Sub AlCambiarCuenta()
            Limpiar()
            RevisarMes()
            If Not _esTec Then CargarColumnas()
        End Sub

        Private Async Sub CargarColumnas()
            If Banco Is Nothing Then Return
            Try
                Dim c = Await _conciliacion.ObtenerColumnasAsync(Banco.Codigo)
                Dim valores = {c.Fecha, c.Documento, c.Detalle, c.Debitos, c.Creditos}
                For i = 0 To 4
                    cmbColumnas(i).SelectedIndex = valores(i)
                Next
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        Private Sub InicioCambio(sender As Object, e As EventArgs)
            dtpFin.Value = New Date(dtpInicio.Value.Year, dtpInicio.Value.Month, 1).AddMonths(1).AddDays(-1)
            RevisarMes()
        End Sub

        Private Async Sub RevisarMes()
            _mesCerrado = False
            lblInfo.Text = ""
            If HayCuenta Then
                Try
                    _mesCerrado = Await _cierre.EstaCerradoAsync(Banco.Codigo, Cuenta.Cuenta, dtpInicio.Value)
                    lblInfo.Text = If(_mesCerrado, "El mes está cerrado.", "")
                Catch ex As MySqlException
                    Mensajes.NoConecta()
                End Try
            End If
            Habilitar()
        End Sub

        Private Sub AbrirArchivo(sender As Object, e As EventArgs)
            If Not HayCuenta Then Return
            Using dialogo As New OpenFileDialog With {.Title = "Archivo del banco", .Filter = If(_esTec, "Archivos TEC (*.tec;*.txt)|*.tec;*.txt|Todos (*.*)|*.*", "Archivos CSV (*.csv;*.txt)|*.csv;*.txt|Todos (*.*)|*.*")}
                If dialogo.ShowDialog(Me) <> DialogResult.OK Then Return
                Limpiar()
                Try
                    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)
                    Dim lineas = File.ReadAllLines(dialogo.FileName, Encoding.GetEncoding(PaginaDeCodigos))
                    If _esTec Then CargarTec(lineas) Else CargarCsv(lineas)
                    If rejilla.Rows.Count > 0 Then lblArchivo.Text = $"{Path.GetFileName(dialogo.FileName)}: {rejilla.Rows.Count} filas"
                Catch ex As IOException
                    Aviso($"No se pudo leer el archivo: {ex.Message}", MessageBoxIcon.Error)
                Catch ex As FormatException
                    Aviso($"El archivo no es un archivo TEC válido: {ex.Message}", MessageBoxIcon.Error)
                End Try
            End Using
            Habilitar()
        End Sub

        Private Sub CargarTec(lineas As String())
            Dim cuentaBanco = Cuenta.CuentaB.Trim()
            If cuentaBanco.Length <> 15 Then
                Aviso("El número de cuenta no está definido correctamente.")
                Return
            End If
            Select Case ArchivoTec.Verificar(lineas, cuentaBanco)
                Case EstadoArchivoTec.NoValido
                    Aviso("El archivo no es un archivo TEC válido.", MessageBoxIcon.Error)
                Case EstadoArchivoTec.SinCuenta
                    Aviso($"El archivo no tiene registros para la cuenta {Cuenta.Cuenta}.", MessageBoxIcon.Error)
                Case Else
                    _movimientosTec = ArchivoTec.Leer(lineas, cuentaBanco)
                    Dim nombres = {"Débito", "Crédito", "Cheque", "Depósito"}
                    For Each m In _movimientosTec
                        rejilla.Rows.Add(m.Fecha.ToString("dd/MM/yyyy"), nombres(m.TipoMov), m.Documento, m.Detalle, If(m.Tipo = 0, m.Monto, 0.0), If(m.Tipo = 1, m.Monto, 0.0))
                    Next
            End Select
        End Sub

        Private Sub CargarCsv(lineas As String())
            Dim separador = {","c, ";"c, ControlChars.Tab}(cmbSeparador.SelectedIndex)
            _filasCsv = lineas.Where(Function(l) l.Trim().Length > 0).Select(Function(l) ArchivoCsv.Separar(l, separador)).ToList()
            For Each fila In _filasCsv
                rejilla.Rows.Add(fila.Take(ColumnasArchivo).Cast(Of Object)().ToArray())
            Next
        End Sub

        Private Function ColumnasElegidas() As ColumnasCsv
            Return New ColumnasCsv With {
                .Fecha = cmbColumnas(0).SelectedIndex, .Documento = cmbColumnas(1).SelectedIndex, .Detalle = cmbColumnas(2).SelectedIndex,
                .Debitos = cmbColumnas(3).SelectedIndex, .Creditos = cmbColumnas(4).SelectedIndex
            }
        End Function

        Private Sub Ocupado(activo As Boolean)
            barraProgreso.Visible = activo
            gbCuenta.Enabled = Not activo
            btnArchivo.Enabled = Not activo
            btnConciliar.Enabled = Not activo
            btnSalir.Enabled = Not activo
        End Sub

        Private Async Sub Conciliar(sender As Object, e As EventArgs)
            If Not HayCuenta Then Return
            Dim inicio = dtpInicio.Value.Date
            Dim fin = dtpFin.Value.Date
            Dim movimientos = _movimientosTec
            If Not _esTec Then
                Dim columnas = ColumnasElegidas()
                If columnas.Repetidas Then
                    Aviso("Columna repetida: cada dato debe estar en una columna distinta.")
                    Return
                End If
                movimientos = ArchivoCsv.Leer(_filasCsv, columnas, inicio, fin)
                If movimientos.Count = 0 Then
                    Aviso("El archivo no tiene movimientos válidos con esas columnas.")
                    Return
                End If
            End If
            If chkMigrar.Checked AndAlso Not Pregunta("Todo lo que el banco reporte y no esté en libros pasará a libros como conciliado. ¿Desea continuar?") Then Return

            Ocupado(True)
            Try
                If Not _esTec Then Await _conciliacion.GuardarColumnasAsync(Banco.Codigo, ColumnasElegidas())
                Dim importados = Await _conciliacion.ImportarAsync(movimientos, _local, Banco.Codigo, Cuenta.Cuenta, inicio, fin, Not _esTec)
                Dim conciliados = Await _conciliacion.ConciliarAsync(_local, Banco.Codigo, Cuenta.Cuenta, inicio, Date.Today, _esTec, chkMigrar.Checked)
                Dim informe = Await _conciliacion.InformeAsync(Banco.Codigo, Cuenta.Cuenta, inicio, fin, OrdenInforme.FechaLibrosCreditosDocumento, False)
                Aviso($"Importados: {importados.Insertados}. Repetidos omitidos: {importados.Omitidos}.{Environment.NewLine}Movimientos conciliados: {conciliados}.")
                Dim subtitulo = $"Conciliación Bancaria cuenta# {Cuenta} del {Banco}. Entre el {inicio:dd/MMM/yyyy} y el {fin:dd/MMM/yyyy}"
                Using documento As New InformeConciliacionImpreso(informe, _empresa, subtitulo, True)
                    InformeConciliacionImpreso.Mostrar(Me, documento)
                End Using
                Limpiar()
            Catch ex As MySqlException
                Mensajes.NoGuardo(ex.Message, "CONCILIANDO", Modulo.TituloModulo)
            Finally
                If Not IsDisposed Then
                    Ocupado(False)
                    Habilitar()
                End If
            End Try
        End Sub

    End Class

End Namespace
