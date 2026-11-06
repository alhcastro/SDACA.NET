Imports System.Globalization
Imports MySqlConnector
Imports SDACA.Core.Bancos
Imports SDACA.Ui

Namespace SD1401

    ''' <summary>
    ''' Pasos #2, #3 y #4, privilegio 14004. Migración de Fuentes\SD1401\FrmMovBancoaLibros.frm,
    ''' FrmMovBancoaMal.frm y FrmConciliarManual.frm, que comparten pantalla y solo cambian de tabla y de acción.
    ''' Como allá, la columna Crédito muestra el tipo 0. El privilegio 14006 deja corregir los datos en los pasos 2 y 3.
    ''' </summary>
    Public Class FrmMovimientosPendientes
        Inherits FrmConCuenta

        Public Enum Paso
            ImportarAlLibro
            ErroresDeConciliacion
            ConciliacionManual
        End Enum

        Private Const ColFecha As Integer = 0
        Private Const ColDocumento As Integer = 1
        Private Const ColDetalle As Integer = 2
        Private Const ColCredito As Integer = 3
        Private Const ColDebito As Integer = 4
        Private Const ColSele As Integer = 5
        Private Const ColFechaBanco As Integer = 5
        Private Const ColDetalleBanco As Integer = 6

        Private Shared ReadOnly Cultura As New CultureInfo("es-CR")

        Private ReadOnly _conciliacion As ConciliacionBancaria
        Private ReadOnly _paso As Paso
        Private ReadOnly _uid As Long
        Private ReadOnly _local As Long
        Private ReadOnly _puedeCorregir As Boolean
        Private ReadOnly _empresa As String

        Private ReadOnly cmbFiltro As New ComboBox With {.Name = "cmbFiltro", .AccessibleName = "Filtrar por", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(70, 142), .Width = 120}
        Private ReadOnly txtFiltro As New TextBox With {.Name = "txtFiltro", .AccessibleName = "Valor del filtro", .Location = New Point(198, 142), .Width = 180}
        Private ReadOnly dtpFiltro As New DateTimePicker With {.Name = "dtpFiltro", .AccessibleName = "Fecha del filtro", .Format = DateTimePickerFormat.Short, .Location = New Point(198, 142), .Width = 180, .Visible = False}
        Private ReadOnly btnCargar As New Button With {.Name = "btnCargar", .Text = "&Cargar", .Location = New Point(386, 140), .Size = New Size(90, 28), .UseVisualStyleBackColor = True}
        Private ReadOnly cmbMeses As New ComboBox With {.Name = "cmbMeses", .AccessibleName = "Mes", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(540, 142), .Width = 200}
        Private ReadOnly rejilla As New DataGridView With {
            .Name = "rejilla", .Location = New Point(10, 176), .Size = New Size(840, 380), .AllowUserToAddRows = False, .AllowUserToDeleteRows = False, .AllowUserToResizeRows = False,
            .RowHeadersVisible = False, .SelectionMode = DataGridViewSelectionMode.CellSelect, .MultiSelect = False
        }
        Private ReadOnly btnTodos As New Button With {.Name = "btnTodos", .Text = "&Marcar todos", .Location = New Point(10, 566), .Size = New Size(110, 30), .UseVisualStyleBackColor = True}
        Private ReadOnly btnNinguno As New Button With {.Name = "btnNinguno", .Text = "&Desmarcar", .Location = New Point(126, 566), .Size = New Size(100, 30), .UseVisualStyleBackColor = True}
        Private ReadOnly btnImprimir As New Button With {.Name = "btnImprimir", .Text = "&Imprimir", .Location = New Point(232, 566), .Size = New Size(90, 30), .UseVisualStyleBackColor = True}
        Private ReadOnly btnAplicar As New Button With {.Name = "btnGuardar", .Location = New Point(530, 566), .Size = New Size(120, 30), .UseVisualStyleBackColor = True}
        Private ReadOnly btnBorrar As New Button With {.Name = "btnBorrar", .Text = "&Borrar", .Location = New Point(656, 566), .Size = New Size(90, 30), .UseVisualStyleBackColor = True}
        Private ReadOnly btnSalir As New Button With {.Name = "btnSalir", .Text = "&Salir", .Location = New Point(752, 566), .Size = New Size(98, 30), .UseVisualStyleBackColor = True}

        Public Sub New(conciliacion As ConciliacionBancaria, cierre As CierreMes, uid As Long, local As Long, todasLasCuentas As Boolean, puedeCorregir As Boolean, empresa As String, paso As Paso)
            MyBase.New(cierre, uid, todasLasCuentas, Titulo(paso), New Size(860, 606))
            _conciliacion = conciliacion
            _paso = paso
            _uid = uid
            _local = local
            _puedeCorregir = puedeCorregir
            _empresa = empresa
            cmbFiltro.Items.AddRange(New Object() {"Documento", "Fecha", "Detalle"})
            cmbFiltro.SelectedIndex = 0
            cmbMeses.Items.Add("Todos")
            cmbMeses.SelectedIndex = 0
            btnAplicar.Text = If(paso = Paso.ConciliacionManual, "&Conciliar", If(paso = Paso.ImportarAlLibro, "&Pasar a libros", "&Pasar al banco"))
            CancelButton = btnSalir

            Dim fecha = TextoCol("Fecha", 92, False)
            fecha.DefaultCellStyle.Format = "dd/MMM/yyyy"
            rejilla.Columns.Add(fecha)
            rejilla.Columns.Add(TextoCol("Documento", 100, Not CorrigeDatos))
            Dim detalle = TextoCol("Detalle", 0, Not CorrigeDatos)
            detalle.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            rejilla.Columns.Add(detalle)
            rejilla.Columns.Add(MontoCol("Crédito"))
            rejilla.Columns.Add(MontoCol("Debito"))
            fecha.ReadOnly = Not CorrigeDatos
            If paso = Paso.ConciliacionManual Then
                rejilla.Columns.Add(TextoCol("Fecha Banco", 96, False))
                rejilla.Columns.Add(TextoCol("Detalle Banco", 170, False))
                For Each boton In {btnTodos, btnNinguno, btnBorrar}
                    boton.Visible = False
                Next
            Else
                rejilla.Columns.Add(New DataGridViewCheckBoxColumn With {.HeaderText = "Sele", .Width = 44})
            End If
            btnImprimir.Visible = paso = Paso.ImportarAlLibro

            Controls.AddRange(New Control() {
                Etiqueta("Filtrar", 12, 145), cmbFiltro, txtFiltro, dtpFiltro, btnCargar, Etiqueta("Mes", 500, 145), cmbMeses, rejilla, btnTodos, btnNinguno, btnImprimir, btnAplicar, btnBorrar, btnSalir
            })
            AddHandler cmbFiltro.SelectedIndexChanged, AddressOf FiltroCambio
            AddHandler btnCargar.Click, AddressOf Cargar
            AddHandler cmbMeses.SelectedIndexChanged, AddressOf MesCambio
            AddHandler btnTodos.Click, Sub() Marcar(True)
            AddHandler btnNinguno.Click, Sub() Marcar(False)
            AddHandler btnAplicar.Click, AddressOf Aplicar
            AddHandler btnBorrar.Click, AddressOf Borrar
            AddHandler btnImprimir.Click, AddressOf Imprimir
            AddHandler btnSalir.Click, Sub() Close()
            AddHandler rejilla.DataError, Sub(s, e) e.ThrowException = False
        End Sub

        Private Shared Function Titulo(paso As Paso) As String
            Select Case paso
                Case Paso.ImportarAlLibro : Return "Importar Movimientos Bancarios"
                Case Paso.ErroresDeConciliacion : Return "Errores de Conciliación"
                Case Else : Return "Conciliación Manual"
            End Select
        End Function

        Private ReadOnly Property CorrigeDatos As Boolean
            Get
                Return _paso <> Paso.ConciliacionManual AndAlso _puedeCorregir
            End Get
        End Property

        Private ReadOnly Property Origen As OrigenPendiente
            Get
                Return If(_paso = Paso.ImportarAlLibro, OrigenPendiente.Banco, OrigenPendiente.Errores)
            End Get
        End Property

        Private Shared Function TextoCol(titulo As String, ancho As Integer, soloLectura As Boolean) As DataGridViewTextBoxColumn
            Dim c As New DataGridViewTextBoxColumn With {.HeaderText = titulo, .ReadOnly = soloLectura, .SortMode = DataGridViewColumnSortMode.NotSortable}
            If ancho > 0 Then c.Width = ancho
            Return c
        End Function

        Private Function MontoCol(titulo As String) As DataGridViewTextBoxColumn
            Dim c = TextoCol(titulo, 110, Not CorrigeDatos)
            c.ValueType = GetType(Double)
            c.DefaultCellStyle.Format = "#,##0.00"
            c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
            Return c
        End Function

        Protected Overrides Sub AlCambiarCuenta()
            rejilla.Rows.Clear()
            cmbMeses.Items.Clear()
            cmbMeses.Items.Add("Todos")
            cmbMeses.SelectedIndex = 0
        End Sub

        Private Sub FiltroCambio(sender As Object, e As EventArgs)
            Dim porFecha = cmbFiltro.SelectedIndex = FiltroPendiente.Fecha
            dtpFiltro.Visible = porFecha
            txtFiltro.Visible = Not porFecha
            txtFiltro.Clear()
        End Sub

        Private Async Sub Cargar(sender As Object, e As EventArgs)
            If Not HayCuenta Then
                cmbCuentas.Focus()
                Return
            End If
            Dim campo = CType(cmbFiltro.SelectedIndex, FiltroPendiente)
            Dim valor = If(campo = FiltroPendiente.Fecha, dtpFiltro.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), txtFiltro.Text.Trim())
            btnCargar.Enabled = False
            Try
                Dim filas = If(_paso = Paso.ConciliacionManual,
                               Await _conciliacion.ListarSinConciliarAsync(Banco.Codigo, Cuenta.Cuenta, campo, valor),
                               Await _conciliacion.ListarPendientesAsync(Origen, Banco.Codigo, Cuenta.Cuenta, campo, valor))
                Mostrar(filas)
            Catch ex As MySqlException
                Mensajes.NoConecta()
            Finally
                If Not IsDisposed Then btnCargar.Enabled = True
            End Try
        End Sub

        Private Sub Mostrar(filas As List(Of MovimientoPendiente))
            rejilla.Rows.Clear()
            cmbMeses.Items.Clear()
            cmbMeses.Items.Add("Todos")
            For Each m In filas
                Dim credito = If(m.Tipo = 0, m.Monto, 0.0)
                Dim debito = If(m.Tipo = 1, m.Monto, 0.0)
                Dim i = If(_paso = Paso.ConciliacionManual,
                           rejilla.Rows.Add(m.Fecha, m.Documento, m.Detalle, credito, debito, "", ""),
                           rejilla.Rows.Add(m.Fecha, m.Documento, m.Detalle, credito, debito, False))
                rejilla.Rows(i).Tag = m
                Dim mes = NombreMes(m.Fecha)
                If Not cmbMeses.Items.Contains(mes) Then cmbMeses.Items.Add(mes)
            Next
            Dim actual = NombreMes(Date.Today)
            cmbMeses.SelectedItem = If(cmbMeses.Items.Contains(actual), actual, "Todos")
        End Sub

        Private Shared Function NombreMes(fecha As Date) As String
            Return fecha.ToString("MMMM-yyyy", Cultura)
        End Function

        Private Sub MesCambio(sender As Object, e As EventArgs)
            Dim mes = TryCast(cmbMeses.SelectedItem, String)
            rejilla.CurrentCell = Nothing
            For Each fila In rejilla.Rows.Cast(Of DataGridViewRow)()
                Dim m = TryCast(fila.Tag, MovimientoPendiente)
                fila.Visible = cmbMeses.SelectedIndex <= 0 OrElse m Is Nothing OrElse NombreMes(m.Fecha) = mes
                If _paso <> Paso.ConciliacionManual Then fila.Cells(ColSele).Value = False
            Next
        End Sub

        Private Sub Marcar(valor As Boolean)
            rejilla.EndEdit()
            For Each fila In rejilla.Rows.Cast(Of DataGridViewRow)()
                fila.Cells(ColSele).Value = valor AndAlso fila.Visible
            Next
        End Sub

        Private Function Marcadas() As List(Of DataGridViewRow)
            rejilla.EndEdit()
            Return rejilla.Rows.Cast(Of DataGridViewRow)().Where(Function(f) Convert.ToBoolean(f.Cells(ColSele).Value)).ToList()
        End Function

        Private Shared Function Editado(fila As DataGridViewRow) As MovimientoPendiente
            Dim credito = Convert.ToDouble(If(fila.Cells(ColCredito).Value, 0.0))
            Dim debito = Convert.ToDouble(If(fila.Cells(ColDebito).Value, 0.0))
            Return New MovimientoPendiente With {
                .Fecha = Convert.ToDateTime(fila.Cells(ColFecha).Value), .Documento = Convert.ToString(fila.Cells(ColDocumento).Value).Trim(), .Detalle = Convert.ToString(fila.Cells(ColDetalle).Value).Trim(),
                .Tipo = If(credito <> 0, 0, 1), .Monto = If(credito <> 0, credito, debito)
            }
        End Function

        Private Async Sub Aplicar(sender As Object, e As EventArgs)
            If Not HayCuenta Then Return
            If _paso = Paso.ConciliacionManual Then
                Await ConciliarManual()
                Return
            End If
            Dim filas = Marcadas()
            If filas.Count = 0 Then
                Aviso("Marque los movimientos que desea pasar.")
                Return
            End If
            btnAplicar.Enabled = False
            Dim fallidos As New List(Of String)
            For Each fila In filas
                Dim original = CType(fila.Tag, MovimientoPendiente)
                Try
                    Await _conciliacion.AplicarAsync(Origen, original, Editado(fila), _local, Banco.Codigo, Cuenta.Cuenta, _uid, Date.Today)
                    rejilla.Rows.Remove(fila)
                Catch ex As MySqlException
                    fila.Cells(ColSele).Value = False
                    fallidos.Add($"{original.Documento} - {original.Detalle} - {original.Monto:#,##0.00}")
                End Try
            Next
            If Not IsDisposed Then btnAplicar.Enabled = True
            If fallidos.Count > 0 Then Aviso($"No se pudieron pasar estos movimientos, probablemente porque ya existen:{Environment.NewLine}{String.Join(Environment.NewLine, fallidos)}")
        End Sub

        Private Async Function ConciliarManual() As Task
            rejilla.EndEdit()
            Dim lineas As New List(Of ConciliacionManual)
            For Each fila In rejilla.Rows.Cast(Of DataGridViewRow)()
                Dim fechaTexto = Convert.ToString(fila.Cells(ColFechaBanco).Value).Trim()
                Dim detalle = Convert.ToString(fila.Cells(ColDetalleBanco).Value).Trim()
                If fechaTexto.Length = 0 AndAlso detalle.Length = 0 Then Continue For
                Dim fecha As Date
                If Not Date.TryParse(fechaTexto, Cultura, DateTimeStyles.None, fecha) OrElse fecha.Year <= 1900 OrElse detalle.Length = 0 Then
                    Aviso($"Revise el documento {fila.Cells(ColDocumento).Value}: la fecha del banco debe ser válida y el detalle no puede quedar vacío.")
                    Return
                End If
                lineas.Add(New ConciliacionManual With {.Movimiento = CType(fila.Tag, MovimientoPendiente), .BancoFecha = fecha, .BancoDetalle = detalle})
            Next
            If lineas.Count = 0 Then
                Aviso("Digite la fecha y el detalle del banco en los movimientos que desea conciliar.")
                Return
            End If
            If Not Pregunta($"¿Desea conciliar {lineas.Count} movimientos?") Then Return
            Try
                Await _conciliacion.ConciliarManualAsync(lineas, _local, Banco.Codigo, Cuenta.Cuenta, Date.Today)
                Aviso("Conciliación efectuada.")
                Cargar(Me, EventArgs.Empty)
            Catch ex As MySqlException
                Mensajes.NoGuardo(ex.Message, "CONCILIANDO", Modulo.TituloModulo)
            End Try
        End Function

        Private Async Sub Borrar(sender As Object, e As EventArgs)
            Dim filas = Marcadas()
            If filas.Count = 0 OrElse Not HayCuenta Then Return
            If Not Pregunta($"¿Realmente desea borrar {filas.Count} movimientos?") Then Return
            Try
                For Each fila In filas
                    Await _conciliacion.BorrarPendienteAsync(Origen, CType(fila.Tag, MovimientoPendiente), Banco.Codigo, Cuenta.Cuenta)
                    rejilla.Rows.Remove(fila)
                Next
            Catch ex As MySqlException
                Mensajes.NoGuardo(ex.Message, "BORRANDO MOVIMIENTOS", Modulo.TituloModulo)
            End Try
        End Sub

        ''' <summary>RPT0009: lo que está en bancos y no en libros.</summary>
        Private Sub Imprimir(sender As Object, e As EventArgs)
            If Not HayCuenta OrElse rejilla.Rows.Count = 0 Then Return
            Dim filas = rejilla.Rows.Cast(Of DataGridViewRow)().Where(Function(f) f.Visible).Select(
                Function(f)
                    Dim m = Editado(f)
                    Return New FilaConciliacion With {.Fecha = m.Fecha, .Documento = m.Documento, .Detalle = m.Detalle, .Monto = m.Monto, .Tipo = m.Tipo}
                End Function).ToList()
            Using documento As New ListadoImpreso($"Movimientos en Bancos y no en Libros: {Banco} - {Cuenta}", _empresa, filas)
                InformeConciliacionImpreso.Mostrar(Me, documento)
            End Using
        End Sub

    End Class

End Namespace
