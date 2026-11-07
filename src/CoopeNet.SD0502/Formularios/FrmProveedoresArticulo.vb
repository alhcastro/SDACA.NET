Imports System.Globalization
Imports MySqlConnector
Imports CoopeNet.Core.Pos
Imports CoopeNet.Ui

Namespace SD0502

    ''' <summary>
    ''' Proveedores del artículo. Migra FraProveedores y grdProvee de frmMantArticulos.frm, que VB6 mostraba
    ''' en lugar de las pestañas al pulsar Agregar: Listo graba (GuardaProv) y Cancelar descarta.
    ''' </summary>
    Public Class FrmProveedoresArticulo
        Inherits Form

        Private ReadOnly _repo As ProveedoresArticuloRepositorio
        Private ReadOnly _articulo As String
        Private ReadOnly _local As Long
        Private _id As String = ""

        Private ReadOnly lblArticulo As New Label With {.Name = "lblArticulo", .AutoSize = False, .Location = New Point(12, 12), .Size = New Size(736, 20)}
        Private ReadOnly grid As New DataGridView With {.Name = "grid", .AccessibleName = "Proveedores del artículo", .Location = New Point(12, 38), .Size = New Size(736, 250),
            .ReadOnly = True, .AllowUserToAddRows = False, .AllowUserToDeleteRows = False, .AllowUserToResizeRows = False, .MultiSelect = False, .RowHeadersVisible = False,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect, .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill}
        Private ReadOnly colProveedor As New DataGridViewTextBoxColumn With {.Name = "colProveedor", .HeaderText = "Cód.Prov", .FillWeight = 70}
        Private ReadOnly colNombre As New DataGridViewTextBoxColumn With {.Name = "colNombre", .HeaderText = "Proveedor", .FillWeight = 160}
        Private ReadOnly colCuenta As New DataGridViewTextBoxColumn With {.Name = "colCuenta", .HeaderText = "Cuenta", .FillWeight = 60}
        Private ReadOnly colCodigo As New DataGridViewTextBoxColumn With {.Name = "colCodigo", .HeaderText = "Cód.Artíc", .FillWeight = 80}
        Private ReadOnly colEmpaque As New DataGridViewTextBoxColumn With {.Name = "colEmpaque", .HeaderText = "Cant / Empaq", .FillWeight = 60}

        Private ReadOnly gbAgregar As New GroupBox With {.Name = "gbAgregar", .Text = "Proveedores", .Location = New Point(12, 296), .Size = New Size(736, 96)}
        Private ReadOnly txtId As New TextBox With {.Name = "txtId", .AccessibleName = "Identificación del proveedor", .Location = New Point(14, 44), .Width = 110, .MaxLength = ProveedoresArticuloRepositorio.LargoCodigo}
        Private ReadOnly txtNombre As New TextBox With {.Name = "txtNombre", .AccessibleName = "Nombre del proveedor", .ReadOnly = True, .TabStop = False, .Location = New Point(130, 44), .Width = 200}
        Private ReadOnly cmbCuenta As New ComboBox With {.Name = "cmbCuenta", .AccessibleName = "Cuenta del proveedor", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(336, 44), .Width = 140, .Enabled = False}
        Private ReadOnly txtCodigo As New TextBox With {.Name = "txtCodigo", .AccessibleName = "Código de proveedor", .Location = New Point(482, 44), .Width = 100, .MaxLength = ProveedoresArticuloRepositorio.LargoCodigo}
        Private ReadOnly txtEmpaque As New TextBox With {.Name = "txtEmpaque", .AccessibleName = "Cantidad por empaque", .Text = "1", .TextAlign = HorizontalAlignment.Right, .Location = New Point(588, 44), .Width = 46}
        Private ReadOnly btnAgregar As New Button With {.Name = "btnAgregar", .Text = "Agregar", .Location = New Point(640, 42), .Size = New Size(84, 27)}

        Private ReadOnly btnQuitar As New Button With {.Name = "btnQuitar", .Text = "Quitar", .Location = New Point(12, 404), .Size = New Size(100, 30)}
        Private ReadOnly btnListo As New Button With {.Name = "btnListo", .Text = "&Listo", .Location = New Point(542, 404), .Size = New Size(100, 30)}
        Private ReadOnly btnCancelar As New Button With {.Name = "btnCancelar", .Text = "Cancelar", .DialogResult = DialogResult.Cancel, .Location = New Point(648, 404), .Size = New Size(100, 30)}

        Public Sub New(repositorio As ProveedoresArticuloRepositorio, articulo As String, descripcion As String, local As Long)
            _repo = repositorio
            _articulo = articulo
            _local = local
            Text = "Proveedores del artículo"
            ClientSize = New Size(760, 446)
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent
            CancelButton = btnCancelar
            lblArticulo.Text = $"{articulo}  -  {descripcion}"
            grid.Columns.AddRange(colProveedor, colNombre, colCuenta, colCodigo, colEmpaque)
            colEmpaque.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
            gbAgregar.Controls.AddRange(New Control() {
                New Label With {.Text = "Identificación", .AutoSize = True, .Location = New Point(14, 24)}, txtId,
                New Label With {.Text = "Nombre", .AutoSize = True, .Location = New Point(130, 24)}, txtNombre,
                New Label With {.Text = "Cuenta", .AutoSize = True, .Location = New Point(336, 24)}, cmbCuenta,
                New Label With {.Text = "Código", .AutoSize = True, .Location = New Point(482, 24)}, txtCodigo,
                New Label With {.Text = "Empaque", .AutoSize = True, .Location = New Point(588, 24)}, txtEmpaque, btnAgregar})
            Controls.AddRange(New Control() {lblArticulo, grid, gbAgregar, btnQuitar, btnListo, btnCancelar})
            lblArticulo.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
            grid.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
            gbAgregar.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
            btnQuitar.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
            btnListo.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
            btnCancelar.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
            Ventanas.Recordar(Me)

            AddHandler Load, AddressOf Cargar
            AddHandler txtId.KeyDown, AddressOf Id_Tecla
            AddHandler txtId.TextChanged, Sub() If Not String.Equals(txtId.Text.Trim(), _id, StringComparison.Ordinal) Then LimpiarProveedor(False)
            AddHandler btnAgregar.Click, Sub() Agregar()
            AddHandler btnQuitar.Click, Sub() Quitar()
            AddHandler grid.KeyDown, Sub(s, e) If e.KeyCode = Keys.Delete Then Quitar()
            AddHandler btnListo.Click, AddressOf Listo_Clic
        End Sub

        Private Async Sub Cargar(sender As Object, e As EventArgs)
            Try
                For Each p In Await _repo.ListarAsync(_articulo, _local)
                    AgregarFila(p)
                Next
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
            txtId.Focus()
        End Sub

        Private Sub AgregarFila(p As ProveedorArticulo)
            Dim i = grid.Rows.Add(p.Proveedor, p.Nombre, p.Cuenta, p.CodArtProv, p.CantEmpaq.ToString(CultureInfo.CurrentCulture))
            grid.Rows(i).Tag = p
        End Sub

        Private Sub LimpiarProveedor(conId As Boolean)
            _id = ""
            If conId Then txtId.Clear()
            txtNombre.Clear()
            cmbCuenta.Items.Clear()
            cmbCuenta.Enabled = False
        End Sub

        ''' <summary>sdtID_KeyPress: con Enter busca la persona; si no se encuentra, abre la lista de proveedores.</summary>
        Private Async Sub Id_Tecla(sender As Object, e As KeyEventArgs)
            If e.KeyCode <> Keys.Enter Then Return
            e.SuppressKeyPress = True
            Try
                Dim id = Await _repo.ResolverIdAsync(txtId.Text)
                Dim nombre = If(id.Length = 0, "", Await _repo.NombreAsync(id))
                If nombre.Length = 0 Then
                    Using datos = Await _repo.ProveedoresAsync(_local)
                        Using buscador As New FrmBuscador("Seleccione el proveedor", datos)
                            If buscador.ShowDialog(Me) <> DialogResult.OK OrElse buscador.Resultado(1).Length = 0 Then Return
                            id = buscador.Resultado(1)
                            nombre = buscador.Resultado(0)
                        End Using
                    End Using
                End If
                Await MostrarProveedorAsync(id, nombre)
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        ''' <summary>CargaCuentas: con una sola cuenta el combo queda deshabilitado, como en VB6.</summary>
        Private Async Function MostrarProveedorAsync(id As String, nombre As String) As Task
            txtId.Text = id
            _id = id
            txtNombre.Text = nombre.Trim()
            cmbCuenta.Items.Clear()
            For Each c In Await _repo.CuentasAsync(id, _local)
                cmbCuenta.Items.Add(c)
            Next
            If cmbCuenta.Items.Count > 0 Then cmbCuenta.SelectedIndex = 0
            cmbCuenta.Enabled = cmbCuenta.Items.Count > 1
            If cmbCuenta.Enabled Then cmbCuenta.Focus() Else txtCodigo.Focus()
        End Function

        ''' <summary>cmdAgregar_Click. Además rechaza una fila repetida, que la llave de poscodprove no admite.</summary>
        Private Sub Agregar()
            Dim codigo = txtCodigo.Text.Trim()
            Dim empaque As Double
            If _id.Length = 0 OrElse codigo.Length = 0 Then
                MessageBox.Show(Me, "Faltan datos necesarios para agregar el proveedor", Text, MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If
            If Not Double.TryParse(txtEmpaque.Text, NumberStyles.Number, CultureInfo.CurrentCulture, empaque) OrElse empaque <= 0 Then
                MessageBox.Show(Me, "La cantidad por empaque debe ser un número mayor que cero.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information)
                txtEmpaque.Focus()
                Return
            End If
            If Filas().Any(Function(p) p.Proveedor = _id AndAlso String.Equals(p.CodArtProv, codigo, StringComparison.OrdinalIgnoreCase)) Then
                MessageBox.Show(Me, "Ese proveedor ya tiene ese código para el artículo.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If
            AgregarFila(New ProveedorArticulo With {.Proveedor = _id, .Nombre = txtNombre.Text, .Cuenta = ProveedoresArticuloRepositorio.CuentaDe(CStr(cmbCuenta.SelectedItem)), .CodArtProv = codigo, .CantEmpaq = empaque})
            LimpiarProveedor(True)
            txtCodigo.Clear()
            txtEmpaque.Text = "1"
            txtId.Focus()
        End Sub

        ''' <summary>grdProvee_BeforeDelete.</summary>
        Private Sub Quitar()
            Dim fila = grid.CurrentRow
            If fila Is Nothing Then Return
            If MessageBox.Show(Me, "Este proceso elimina el proveedor del artículo. ¿Desea continuar?", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) <> DialogResult.Yes Then Return
            grid.Rows.Remove(fila)
        End Sub

        Private Function Filas() As List(Of ProveedorArticulo)
            Return grid.Rows.Cast(Of DataGridViewRow)().Select(Function(r) CType(r.Tag, ProveedorArticulo)).ToList()
        End Function

        Private Async Sub Listo_Clic(sender As Object, e As EventArgs)
            btnListo.Enabled = False
            UseWaitCursor = True
            Try
                Await _repo.GuardarAsync(_articulo, _local, Filas())
                DialogResult = DialogResult.OK
            Catch ex As MySqlException
                Mensajes.NoGuardo(ex.Message, "PROVEEDORES", Modulo.TituloModulo)
            Finally
                UseWaitCursor = False
                btnListo.Enabled = True
            End Try
        End Sub

    End Class

End Namespace
