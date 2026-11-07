Imports System.Globalization
Imports MySqlConnector
Imports CoopeNet.Core.Pos
Imports CoopeNet.Ui

Namespace SD0502

    ''' <summary>
    ''' Códigos de barras del artículo. Migración de SD0502\frmCodBarra.frm. Como en VB6, cada código se graba al
    ''' pulsar Enter; quitar uno (Supr o Quitar) se graba con Aceptar o con el siguiente Enter, y Cancelar descarta
    ''' solo lo que falte grabar. No migra el lector serial (posscanestac) ni los ALTER TABLE que VB6 corría al abrir.
    ''' </summary>
    Public Class FrmCodigosBarras
        Inherits Form

        Private ReadOnly _repo As CodigosBarrasRepositorio
        Private ReadOnly _articulo As String
        Private ReadOnly _local As Long
        ''' <summary>Se quitó algún código y falta grabarlo: entonces se borra y se vuelve a grabar la lista entera.</summary>
        Private _quitados As Boolean
        Private _ocupado As Boolean

        Private ReadOnly lblArticulo As New Label With {.Name = "lblArticulo", .AutoSize = False, .Location = New Point(12, 12), .Size = New Size(436, 20)}
        Private ReadOnly txtCodigo As New TextBox With {.Name = "txtCodigo", .AccessibleName = "Código de barras", .Location = New Point(80, 42), .Width = 200, .MaxLength = CodigosBarrasRepositorio.LargoCodigo}
        Private ReadOnly txtUnidades As New TextBox With {.Name = "txtUnidades", .AccessibleName = "Unidades", .Text = "1", .TextAlign = HorizontalAlignment.Right, .Location = New Point(368, 42), .Width = 80}
        Private ReadOnly grid As New DataGridView With {.Name = "grid", .AccessibleName = "Códigos de barras", .Location = New Point(12, 76), .Size = New Size(436, 300),
            .ReadOnly = True, .AllowUserToAddRows = False, .AllowUserToDeleteRows = False, .AllowUserToResizeRows = False, .MultiSelect = False, .RowHeadersVisible = False,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect, .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill}
        Private ReadOnly colCodigo As New DataGridViewTextBoxColumn With {.Name = "colCodigo", .HeaderText = "Cód.barra", .FillWeight = 200}
        Private ReadOnly colUnidades As New DataGridViewTextBoxColumn With {.Name = "colUnidades", .HeaderText = "Unidades", .FillWeight = 80}
        Private ReadOnly lblCantidad As New Label With {.Name = "lblCantidad", .AutoSize = True, .Location = New Point(12, 384)}
        Private ReadOnly btnQuitar As New Button With {.Name = "btnQuitar", .Text = "Quitar", .Location = New Point(12, 410), .Size = New Size(100, 30)}
        Private ReadOnly btnAceptar As New Button With {.Name = "btnAceptar", .Text = "&Aceptar", .Location = New Point(242, 410), .Size = New Size(100, 30)}
        Private ReadOnly btnCancelar As New Button With {.Name = "btnCancelar", .Text = "&Cancelar", .DialogResult = DialogResult.Cancel, .Location = New Point(348, 410), .Size = New Size(100, 30)}

        Public Sub New(repositorio As CodigosBarrasRepositorio, articulo As String, descripcion As String, local As Long, sugerido As String)
            _repo = repositorio
            _articulo = articulo
            _local = local
            Text = "Códigos de barras"
            ClientSize = New Size(460, 452)
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent
            CancelButton = btnCancelar
            lblArticulo.Text = $"{articulo}  -  {descripcion}"
            txtCodigo.Text = If(sugerido, "")
            grid.Columns.AddRange(colCodigo, colUnidades)
            colCodigo.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            colUnidades.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            colCodigo.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter
            colUnidades.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter
            Controls.AddRange(New Control() {lblArticulo,
                New Label With {.Text = "Código", .AutoSize = True, .Location = New Point(12, 45)}, txtCodigo,
                New Label With {.Text = "Unidades", .AutoSize = True, .Location = New Point(300, 45)}, txtUnidades,
                grid, lblCantidad, btnQuitar, btnAceptar, btnCancelar})

            AddHandler Load, AddressOf Cargar
            AddHandler txtCodigo.KeyDown, AddressOf Codigo_Tecla
            AddHandler btnQuitar.Click, Sub() Quitar()
            AddHandler grid.KeyDown, Sub(s, e) If e.KeyCode = Keys.Delete Then Quitar()
            AddHandler btnAceptar.Click, AddressOf Aceptar_Clic
        End Sub

        Private Async Sub Cargar(sender As Object, e As EventArgs)
            Try
                For Each c In Await _repo.ListarAsync(_articulo, _local)
                    AgregarFila(c)
                Next
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
            Contar()
            txtCodigo.SelectAll()
            txtCodigo.Focus()
        End Sub

        Private Sub AgregarFila(c As CodigoBarras)
            Dim i = grid.Rows.Add(c.CAlterno, c.Unidades.ToString(CultureInfo.CurrentCulture))
            grid.Rows(i).Tag = c
        End Sub

        Private Sub Contar()
            lblCantidad.Text = $"{grid.Rows.Count} código{If(grid.Rows.Count = 1, "", "s")}"
        End Sub

        Private Function Codigos() As List(Of CodigoBarras)
            Return grid.Rows.Cast(Of DataGridViewRow)().Select(Function(r) CType(r.Tag, CodigoBarras)).ToList()
        End Function

        ''' <summary>SDTCodigo_KeyDown: con Enter agrega el código y lo graba.</summary>
        Private Async Sub Codigo_Tecla(sender As Object, e As KeyEventArgs)
            If e.KeyCode <> Keys.Enter Then Return
            e.SuppressKeyPress = True
            Dim codigo = txtCodigo.Text.Trim()
            If codigo.Length = 0 OrElse _ocupado Then Return
            Dim unidades As Double
            If Not Double.TryParse(txtUnidades.Text, NumberStyles.Number, CultureInfo.CurrentCulture, unidades) OrElse unidades <= 0 Then
                MessageBox.Show(Me, "Las unidades deben ser un número mayor que cero.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information)
                txtUnidades.Focus()
                Return
            End If
            Ocupado(True)
            Try
                Dim usado = Await _repo.UsadoPorAsync(codigo, _articulo, _local)
                If usado.Length > 0 Then
                    MessageBox.Show(Me, $"El código '{codigo}' ya se encuentra asignado al artículo: {Environment.NewLine}{usado}", Text, MessageBoxButtons.OK, MessageBoxIcon.Error)
                    txtCodigo.Clear()
                    Return
                End If
                ' El mismo código en este artículo cambia sus unidades, como el REPLACE de VB6, sin repetir la fila.
                Dim fila = grid.Rows.Cast(Of DataGridViewRow)().FirstOrDefault(Function(r) String.Equals(CType(r.Tag, CodigoBarras).CAlterno, codigo, StringComparison.OrdinalIgnoreCase))
                Dim nuevo As New CodigoBarras With {.CAlterno = codigo, .Unidades = unidades}
                If fila IsNot Nothing Then grid.Rows.Remove(fila)
                AgregarFila(nuevo)
                If _quitados Then
                    Await _repo.GuardarAsync(_articulo, _local, Codigos())
                    _quitados = False
                Else
                    Await _repo.AgregarAsync(_articulo, _local, nuevo)
                End If
                txtCodigo.Clear()
                Contar()
            Catch ex As MySqlException
                Mensajes.NoGuardo(ex.Message, "CODIGOS DE BARRAS", Modulo.TituloModulo)
            Finally
                Ocupado(False)
                txtCodigo.Focus()
            End Try
        End Sub

        Private Sub Ocupado(valor As Boolean)
            _ocupado = valor
            UseWaitCursor = valor
            btnAceptar.Enabled = Not valor
            btnQuitar.Enabled = Not valor
        End Sub

        Private Sub Quitar()
            Dim fila = grid.CurrentRow
            If fila Is Nothing OrElse _ocupado Then Return
            If MessageBox.Show(Me, $"¿Desea quitar el código {fila.Cells(0).Value}?", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) <> DialogResult.Yes Then Return
            grid.Rows.Remove(fila)
            _quitados = True
            Contar()
        End Sub

        Private Async Sub Aceptar_Clic(sender As Object, e As EventArgs)
            If Not _quitados Then
                DialogResult = DialogResult.OK
                Return
            End If
            Ocupado(True)
            Try
                Await _repo.GuardarAsync(_articulo, _local, Codigos())
                _quitados = False
                DialogResult = DialogResult.OK
            Catch ex As MySqlException
                Mensajes.NoGuardo(ex.Message, "CODIGOS DE BARRAS", Modulo.TituloModulo)
            Finally
                Ocupado(False)
            End Try
        End Sub

    End Class

End Namespace
