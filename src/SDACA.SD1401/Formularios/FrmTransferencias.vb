Imports System.Globalization
Imports MySqlConnector
Imports SDACA.Core
Imports SDACA.Core.Bancos
Imports SDACA.Ui

Namespace SD1401

    ''' <summary>
    ''' Opción Transferencias, privilegio 14014. Migración de Fuentes\SD1401\FrmMantTransferencias.frm. Allá
    ''' la fecha de aplicación se puede editar, pero ningún proceso la usa: aquí se conserva sin mostrarla.
    ''' </summary>
    Public Class FrmTransferencias
        Inherits Form

        Private Const ColumnaId As Integer = 1

        Private ReadOnly _transferencias As TransferenciasBancarias
        Private ReadOnly _uid As Long
        Private ReadOnly _local As Long
        Private _actual As Transferencia

        Private ReadOnly cmbLocal As New ComboBox With {.Name = "cmbLocal", .AccessibleName = "Departamento", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(110, 12), .Width = 290}
        Private ReadOnly cmbModulo As New ComboBox With {.Name = "cmbModulo", .AccessibleName = "Módulo", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(520, 12), .Width = 290}
        Private ReadOnly txtNumero As New TextBox With {.Name = "txtNumero", .AccessibleName = "Número de transferencia", .ReadOnly = True, .TabStop = False, .Location = New Point(110, 44), .Width = 120}
        Private ReadOnly txtEstado As New TextBox With {.Name = "txtEstado", .AccessibleName = "Anulada", .ReadOnly = True, .TabStop = False, .Location = New Point(320, 44), .Width = 80}
        Private ReadOnly dtpFecha As New DateTimePicker With {
            .Name = "dtpFecha", .AccessibleName = "Fecha de transferencia", .Format = DateTimePickerFormat.Custom, .CustomFormat = "dd/MM/yyyy HH:mm:ss", .Location = New Point(520, 44), .Width = 190
        }
        Private ReadOnly txtConcepto As New TextBox With {.Name = "txtConcepto", .AccessibleName = "Concepto", .MaxLength = 128, .Location = New Point(110, 76), .Width = 700}
        Private ReadOnly txtCuenta As New TextBox With {.Name = "txtCuenta", .AccessibleName = "Cuenta", .MaxLength = 32, .Location = New Point(110, 108), .Width = 290}
        Private ReadOnly txtCliente As New TextBox With {.Name = "txtCliente", .AccessibleName = "Número de cliente", .MaxLength = 32, .Location = New Point(520, 108), .Width = 190}
        Private ReadOnly txtInterna As New TextBox With {.Name = "txtInterna", .AccessibleName = "Transferencia interna", .MaxLength = 32, .Location = New Point(110, 140), .Width = 120}
        Private ReadOnly txtBanco As New TextBox With {.Name = "txtBanco", .AccessibleName = "Transferencia del banco", .MaxLength = 32, .Location = New Point(320, 140), .Width = 80}
        Private ReadOnly txtCompra As New TextBox With {.Name = "txtCompra", .AccessibleName = "Tipo de cambio de compra", .TextAlign = HorizontalAlignment.Right, .Location = New Point(520, 140), .Width = 90}
        Private ReadOnly txtVenta As New TextBox With {.Name = "txtVenta", .AccessibleName = "Tipo de cambio de venta", .TextAlign = HorizontalAlignment.Right, .Location = New Point(720, 140), .Width = 90}
        Private ReadOnly rejilla As New DataGridView With {
            .Name = "rejilla", .Location = New Point(10, 174), .Size = New Size(800, 300), .AllowUserToResizeRows = False, .RowHeadersVisible = False,
            .SelectionMode = DataGridViewSelectionMode.CellSelect, .MultiSelect = False
        }
        Private ReadOnly lblCreacion As New Label With {.Name = "lblCreacion", .AutoSize = True, .Location = New Point(12, 482)}
        Private ReadOnly btnAnular As New Button With {.Name = "btnAnular", .Text = "&Anular", .Location = New Point(10, 504), .Size = New Size(100, 32), .UseVisualStyleBackColor = True}
        Private ReadOnly barra As New BarraComandos With {.Name = "barra", .Location = New Point(370, 504), .Size = New Size(440, 32)}

        Public Sub New(transferencias As TransferenciasBancarias, uid As Long, local As Long)
            _transferencias = transferencias
            _uid = uid
            _local = local
            Text = "Mantenimiento de Transferencias Bancarias"
            ClientSize = New Size(820, 546)
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent
            rejilla.Columns.Add(Columna("Tipo", 50))
            rejilla.Columns.Add(Columna("ID", 110, 16))
            rejilla.Columns.Add(Columna("Cuenta", 170, 64))
            rejilla.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Concepto", .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, .MaxInputLength = 255})
            rejilla.Columns.Add(Columna("Comprobante", 110, 32))
            Dim monto = Columna("Monto", 110)
            monto.DefaultCellStyle.Format = "#,##0.00"
            monto.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
            monto.ValueType = GetType(Double)
            rejilla.Columns.Add(monto)
            rejilla.Columns.Add(New DataGridViewCheckBoxColumn With {.HeaderText = "Anulado", .Width = 60})
            Controls.AddRange(New Control() {
                Etiqueta("Departamento", 12, 15), cmbLocal, Etiqueta("Módulo", 420, 15), cmbModulo,
                Etiqueta("Nº", 12, 47), txtNumero, Etiqueta("Anulada", 244, 47), txtEstado, Etiqueta("Fecha", 420, 47), dtpFecha,
                Etiqueta("Concepto", 12, 79), txtConcepto, Etiqueta("Cuenta", 12, 111), txtCuenta, Etiqueta("Nº de cliente", 420, 111), txtCliente,
                Etiqueta("Interna", 12, 143), txtInterna, Etiqueta("Banco", 244, 143), txtBanco, Etiqueta("T.C. Compra", 420, 143), txtCompra, Etiqueta("Venta", 640, 143), txtVenta,
                rejilla, lblCreacion, btnAnular, barra
            })
            barra.AlHacerClic = AddressOf Barra_Clic
            AddHandler Load, AddressOf Cargar
            AddHandler btnAnular.Click, AddressOf Anular
            AddHandler rejilla.KeyDown, AddressOf Rejilla_KeyDown
            AddHandler rejilla.DataError, Sub(s, e) e.ThrowException = False
            Limpiar()
            Editable(False)
        End Sub

        Private Shared Function Columna(titulo As String, ancho As Integer, Optional maximo As Integer = 0) As DataGridViewTextBoxColumn
            Dim c As New DataGridViewTextBoxColumn With {.HeaderText = titulo, .Width = ancho}
            If maximo > 0 Then c.MaxInputLength = maximo
            Return c
        End Function

        Private Shared Function Etiqueta(texto As String, x As Integer, y As Integer) As Label
            Return New Label With {.Text = texto, .AutoSize = True, .Location = New Point(x, y)}
        End Function

        Private ReadOnly Property LocalActual As OpcionTransferencia
            Get
                Return TryCast(cmbLocal.SelectedItem, OpcionTransferencia)
            End Get
        End Property

        Private ReadOnly Property ModuloActual As OpcionTransferencia
            Get
                Return TryCast(cmbModulo.SelectedItem, OpcionTransferencia)
            End Get
        End Property

        Private Async Sub Cargar(sender As Object, e As EventArgs)
            Try
                cmbLocal.Items.AddRange((Await _transferencias.ListarLocalesAsync()).ToArray())
                cmbModulo.Items.AddRange((Await _transferencias.ListarModulosAsync()).ToArray())
                Elegir(cmbLocal, _local)
                If cmbLocal.SelectedIndex < 0 AndAlso cmbLocal.Items.Count > 0 Then cmbLocal.SelectedIndex = 0
                If cmbModulo.Items.Count > 0 Then cmbModulo.SelectedIndex = 0
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        Private Shared Sub Elegir(combo As ComboBox, codigo As Long)
            combo.SelectedItem = combo.Items.Cast(Of OpcionTransferencia)().FirstOrDefault(Function(o) o.Codigo = codigo)
        End Sub

        Private Sub Limpiar()
            _actual = Nothing
            txtNumero.Clear()
            txtEstado.Text = "NO"
            dtpFecha.Value = Date.Now
            For Each caja In {txtConcepto, txtCuenta, txtCliente, txtInterna, txtCompra, txtVenta}
                caja.Clear()
            Next
            txtBanco.Text = "0"
            lblCreacion.Text = ""
            rejilla.Rows.Clear()
            btnAnular.Enabled = False
        End Sub

        ''' <param name="selectores">Los combos solo se usan al crear: una transferencia guardada no cambia de origen.</param>
        Private Sub Editable(activo As Boolean, Optional selectores As Boolean = True)
            cmbLocal.Enabled = selectores
            cmbModulo.Enabled = selectores
            dtpFecha.Enabled = activo
            For Each caja In {txtConcepto, txtCuenta, txtCliente, txtInterna, txtBanco, txtCompra, txtVenta}
                caja.ReadOnly = Not activo
            Next
            rejilla.ReadOnly = Not activo
            rejilla.AllowUserToAddRows = activo
            rejilla.AllowUserToDeleteRows = activo
        End Sub

        Private Sub Mostrar(t As Transferencia)
            _actual = t
            Elegir(cmbLocal, t.Local)
            Elegir(cmbModulo, t.Modulo)
            txtNumero.Text = t.NumTra.ToString()
            txtEstado.Text = If(t.Anulado, "SI", "NO")
            dtpFecha.Value = t.Fecha
            txtConcepto.Text = t.Concepto
            txtCuenta.Text = t.Cuenta
            txtCliente.Text = t.NumCli
            txtInterna.Text = t.NumTraInt
            txtBanco.Text = t.NumTraBan
            txtCompra.Text = t.TcCompra.ToString("0.00")
            txtVenta.Text = t.TcVenta.ToString("0.00")
            lblCreacion.Text = t.Creacion
            rejilla.Rows.Clear()
            For Each l In t.Lineas
                Dim i = rejilla.Rows.Add(l.Tipo, l.Id, l.NumCuenta, l.Concepto, l.NumCom, l.Monto, l.Anulado)
                rejilla.Rows(i).Tag = l
            Next
            btnAnular.Enabled = Not t.Anulado
        End Sub

        Private Sub Aviso(texto As String)
            MessageBox.Show(Me, texto, Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
        End Sub

        Private Function Pregunta(texto As String) As Boolean
            Return MessageBox.Show(Me, texto, Modulo.TituloModulo, MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes
        End Function

        ''' <summary>Devuelve True para que la barra no cambie de estado.</summary>
        Private Async Function Barra_Clic(boton As BotonBarra) As Task(Of Boolean)
            Select Case boton
                Case BotonBarra.Nuevo
                    If LocalActual Is Nothing OrElse ModuloActual Is Nothing Then Return True
                    Limpiar()
                    Try
                        txtNumero.Text = (Await _transferencias.SiguienteAsync(LocalActual.Codigo, ModuloActual.Codigo)).ToString()
                    Catch ex As MySqlException
                        Mensajes.NoConecta()
                        Return True
                    End Try
                    Editable(True, False)
                    txtConcepto.Focus()
                Case BotonBarra.Editar
                    If _actual Is Nothing Then Return True
                    Editable(True, False)
                    txtConcepto.Focus()
                Case BotonBarra.Guardar
                    Return Await Guardar()
                Case BotonBarra.Cancelar
                    If _actual Is Nothing Then Limpiar() Else Mostrar(_actual)
                    Editable(False)
                Case BotonBarra.Buscar
                    Await Buscar()
                Case BotonBarra.Borrar
                    Await Borrar()
                Case BotonBarra.Salir
                    Close()
            End Select
            Return False
        End Function

        Private Function LeerLineas() As List(Of LineaTransferencia)
            Dim lineas As New List(Of LineaTransferencia)
            For Each fila In rejilla.Rows.Cast(Of DataGridViewRow)().Where(Function(f) Not f.IsNewRow)
                Dim anterior = TryCast(fila.Tag, LineaTransferencia)
                lineas.Add(New LineaTransferencia With {
                    .Tipo = If(Convert.ToString(fila.Cells(0).Value), "D").Trim(), .Id = Convert.ToString(fila.Cells(1).Value).Trim(), .NumCuenta = Convert.ToString(fila.Cells(2).Value).Trim(),
                    .Concepto = Convert.ToString(fila.Cells(3).Value).Trim(), .NumCom = Convert.ToString(fila.Cells(4).Value).Trim(), .Monto = Convert.ToDouble(If(fila.Cells(5).Value, 0.0)),
                    .Anulado = Convert.ToBoolean(If(fila.Cells(6).Value, False)), .Fa = If(anterior Is Nothing, Transferencia.SinAplicar, anterior.Fa)
                })
            Next
            For Each l In lineas.Where(Function(x) x.Tipo.Length = 0)
                l.Tipo = "D"
            Next
            Return lineas
        End Function

        Private Shared Function Numero(caja As TextBox) As Double
            Dim valor As Double
            Return If(Double.TryParse(caja.Text, NumberStyles.Number, CultureInfo.CurrentCulture, valor), valor, 0)
        End Function

        Private Async Function Guardar() As Task(Of Boolean)
            rejilla.EndEdit()
            If dtpFecha.Value.Date > Date.Today Then
                Aviso("La fecha de transferencia no puede ser mayor al día de hoy.")
                Return True
            End If
            Dim lineas = LeerLineas()
            If lineas.Any(Function(l) l.NumCuenta.Length = 0) Then
                Aviso("El campo Cuenta es un dato requerido.")
                Return True
            End If
            If lineas.Any(Function(l) l.NumCom.Length = 0) Then
                Aviso("El campo Comprobante es un dato requerido.")
                Return True
            End If
            Dim t As New Transferencia With {
                .Local = LocalActual.Codigo, .Modulo = ModuloActual.Codigo, .NumTra = Vb6.Val(txtNumero.Text), .Concepto = txtConcepto.Text.Trim(), .NumCli = txtCliente.Text.Trim(),
                .Cuenta = txtCuenta.Text.Trim(), .Fecha = dtpFecha.Value, .NumTraInt = txtInterna.Text.Trim(), .NumTraBan = txtBanco.Text.Trim(), .TcCompra = Numero(txtCompra), .TcVenta = Numero(txtVenta),
                .Anulado = _actual IsNot Nothing AndAlso _actual.Anulado, .FAplicada = If(_actual Is Nothing, Transferencia.SinAplicar, _actual.FAplicada),
                .TipTra = If(_actual Is Nothing, "1", _actual.TipTra), .Lineas = lineas
            }
            Try
                Await _transferencias.GuardarAsync(t, _uid, Date.Now)
                Mostrar(Await _transferencias.ObtenerAsync(t.Local, t.Modulo, t.NumTra))
            Catch ex As MySqlException
                Mensajes.NoGuardo(ex.Message, "GUARDANDO TRANSFERENCIA", Modulo.TituloModulo)
                Return True
            End Try
            Editable(False)
            Return False
        End Function

        Private Async Function Buscar() As Task
            Try
                Using datos = Await _transferencias.BuscarAsync()
                    Using buscador As New FrmBuscador("Seleccione la transferencia", datos)
                        If buscador.ShowDialog(Me) <> DialogResult.OK OrElse buscador.Resultado(3).Length = 0 Then Return
                        Dim t = Await _transferencias.ObtenerAsync(Vb6.Val(buscador.Resultado(0)), Vb6.Val(buscador.Resultado(1)), Vb6.Val(buscador.Resultado(3)))
                        If t IsNot Nothing Then Mostrar(t)
                    End Using
                End Using
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Function

        Private Async Function Borrar() As Task
            If _actual Is Nothing Then
                Aviso("Para poder borrar, seleccione una transferencia primero.")
                Return
            End If
            If Not Pregunta($"¿Está seguro de eliminar la transferencia {_actual.NumTra}?") Then Return
            Try
                Await _transferencias.BorrarAsync(_actual.Local, _actual.Modulo, _actual.NumTra)
                Limpiar()
            Catch ex As MySqlException
                Mensajes.NoGuardo(ex.Message, "BORRANDO TRANSFERENCIA", Modulo.TituloModulo)
            End Try
        End Function

        Private Async Sub Anular(sender As Object, e As EventArgs)
            If _actual Is Nothing OrElse barra.EnEdicion Then Return
            If Not Pregunta($"¿Está seguro de que desea anular la transferencia {_actual.NumTra}?") Then Return
            Try
                Await _transferencias.AnularAsync(_actual.Local, _actual.Modulo, _actual.NumTra, Date.Now)
                Mostrar(Await _transferencias.ObtenerAsync(_actual.Local, _actual.Modulo, _actual.NumTra))
                Aviso("Transferencia anulada con éxito.")
            Catch ex As MySqlException
                Mensajes.NoGuardo(ex.Message, "ANULANDO TRANSFERENCIA", Modulo.TituloModulo)
            End Try
        End Sub

        Private Async Sub Rejilla_KeyDown(sender As Object, e As KeyEventArgs)
            If e.KeyCode <> Keys.Enter OrElse rejilla.ReadOnly OrElse rejilla.CurrentCell Is Nothing OrElse rejilla.CurrentCell.ColumnIndex <> ColumnaId Then Return
            e.Handled = True
            Dim celda = rejilla.CurrentCell
            Try
                Using datos = Await _transferencias.BuscarPersonasAsync()
                    Using buscador As New FrmBuscador("Seleccione el registro", datos)
                        If buscador.ShowDialog(Me) = DialogResult.OK Then celda.Value = buscador.Resultado(1)
                    End Using
                End Using
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

    End Class

End Namespace
