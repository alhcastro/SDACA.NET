Imports MySqlConnector
Imports CoopeNet.Core.Bancos
Imports CoopeNet.Core.Cheques
Imports CoopeNet.Ui

Namespace SD1401

    ''' <summary>
    ''' Cuenta contable de una cuenta bancaria en otros departamentos. Migración de
    ''' Fuentes\SD1401\FrmCuentasxDepartamento.frm. El original agregaba la fila aunque la cuenta fuera
    ''' inválida; aquí la cuenta contable debe existir y ser auxiliar.
    ''' </summary>
    Public Class FrmCuentaDepartamentos
        Inherits Form

        Private ReadOnly _repositorio As CuentasBancariasRepositorio
        Private ReadOnly _formato As FormatoCuenta
        Private ReadOnly _cedula As String
        Private ReadOnly _local As Long
        Private ReadOnly _banco As Long
        Private ReadOnly _cuenta As String
        Private ReadOnly _conversion As Boolean
        Private ReadOnly _uid As Long
        Private ReadOnly _localSesion As Long

        Private ReadOnly cmbLocal As New ComboBox With {.Name = "cmbLocal", .AccessibleName = "Departamento", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(110, 42), .Width = 300}
        Private ReadOnly txtCuenta As New TextBox With {.Name = "txtCuenta", .AccessibleName = "Cuenta contable", .MaxLength = 16, .Location = New Point(110, 72), .Width = 160}
        Private ReadOnly btnBuscar As New Button With {.Name = "btnBuscarCuenta", .Text = "...", .AccessibleName = "Buscar cuenta contable", .Location = New Point(274, 71), .Size = New Size(30, 25), .UseVisualStyleBackColor = True}
        Private ReadOnly txtConv As New TextBox With {.Name = "txtConv", .AccessibleName = "Cuenta de conversión", .MaxLength = 16, .Location = New Point(110, 102), .Width = 160}
        Private ReadOnly btnBuscarConv As New Button With {.Name = "btnBuscarConv", .Text = "...", .AccessibleName = "Buscar cuenta de conversión", .Location = New Point(274, 101), .Size = New Size(30, 25), .UseVisualStyleBackColor = True}
        Private ReadOnly btnAgregar As New Button With {.Name = "btnAgregar", .Text = "&Agregar", .Location = New Point(420, 41), .Size = New Size(100, 28), .UseVisualStyleBackColor = True}
        Private ReadOnly btnBorrar As New Button With {.Name = "btnQuitar", .Text = "&Borrar", .Location = New Point(526, 41), .Size = New Size(100, 28), .UseVisualStyleBackColor = True}
        Private ReadOnly rejilla As New DataGridView With {
            .Name = "rejilla", .Location = New Point(10, 136), .Size = New Size(616, 240), .ReadOnly = True, .AllowUserToAddRows = False, .AllowUserToDeleteRows = False,
            .RowHeadersVisible = False, .SelectionMode = DataGridViewSelectionMode.FullRowSelect, .MultiSelect = False
        }
        Private ReadOnly btnAceptar As New Button With {.Name = "btnGuardar", .Text = "&Aceptar", .Location = New Point(420, 384), .Size = New Size(100, 30), .UseVisualStyleBackColor = True}
        Private ReadOnly btnCancelar As New Button With {.Name = "btnCancelar", .Text = "&Cancelar", .Location = New Point(526, 384), .Size = New Size(100, 30), .UseVisualStyleBackColor = True, .DialogResult = DialogResult.Cancel}

        Public Sub New(repositorio As CuentasBancariasRepositorio, formato As FormatoCuenta, cedula As String, local As Long, banco As Long, descripcionBanco As String,
                       cuenta As String, descripcionCuenta As String, conversion As Boolean, uid As Long, localSesion As Long)
            _repositorio = repositorio
            _formato = formato
            _cedula = cedula
            _local = local
            _banco = banco
            _cuenta = cuenta
            _conversion = conversion
            _uid = uid
            _localSesion = localSesion
            Text = "Cuenta Contable x Departamento"
            ClientSize = New Size(636, 424)
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent
            CancelButton = btnCancelar
            txtConv.Enabled = conversion
            btnBuscarConv.Enabled = conversion
            rejilla.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "Local", .Visible = False})
            rejilla.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Departamento", .Width = 190})
            rejilla.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "#Cuenta", .Width = 130})
            rejilla.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "CC", .Visible = False})
            rejilla.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Descripcion", .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill})
            rejilla.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "#Cuenta Conv", .Width = 120})
            rejilla.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "CCC", .Visible = False})
            Dim titulo As New Label With {.Text = $"Cuenta # {cuenta} ({descripcionCuenta}) del {descripcionBanco}", .AutoEllipsis = True, .Location = New Point(10, 14), .Size = New Size(616, 22)}
            Controls.AddRange(New Control() {
                titulo, Etiqueta("Departamento", 45), cmbLocal, Etiqueta("Cuenta #", 75), txtCuenta, btnBuscar, Etiqueta("Conversion", 105), txtConv, btnBuscarConv,
                btnAgregar, btnBorrar, rejilla, btnAceptar, btnCancelar
            })
            AddHandler Load, AddressOf Ventana_Load
            AddHandler btnBuscar.Click, Sub() BuscarCatalogo(txtCuenta)
            AddHandler btnBuscarConv.Click, Sub() BuscarCatalogo(txtConv)
            AddHandler btnAgregar.Click, AddressOf Agregar
            AddHandler btnBorrar.Click, AddressOf Borrar
            AddHandler btnAceptar.Click, AddressOf Aceptar
        End Sub

        Private Shared Function Etiqueta(texto As String, y As Integer) As Label
            Return New Label With {.Text = texto, .AutoSize = True, .Location = New Point(10, y)}
        End Function

        Private Async Sub Ventana_Load(sender As Object, e As EventArgs)
            Try
                Dim locales = (Await _repositorio.ListarLocalesAsync()).Where(Function(l) l.Codigo <> _local).ToList()
                Dim filas = Await _repositorio.ListarContaAsync(_banco, _cuenta)
                For Each f In filas
                    rejilla.Rows.Add(f.Local, f.Departamento, _formato.Mostrar(f.Contable), f.Contable, f.Descripcion, _formato.Mostrar(f.Conversion), f.Conversion)
                Next
                cmbLocal.Items.AddRange(locales.Where(Function(l) Not filas.Any(Function(f) f.Local = l.Codigo)).ToArray())
                If cmbLocal.Items.Count > 0 Then cmbLocal.SelectedIndex = 0
                btnAgregar.Enabled = cmbLocal.Items.Count > 0
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        Private Async Sub BuscarCatalogo(caja As TextBox)
            Try
                Using datos = Await _repositorio.BuscarCatalogoAsync(_formato, _cedula)
                    Using buscador As New FrmBuscador("Catálogo contable", datos)
                        If buscador.ShowDialog(Me) = DialogResult.OK AndAlso buscador.Resultado(0).Length > 0 Then caja.Text = buscador.Resultado(1)
                    End Using
                End Using
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        ''' <summary>Devuelve Nothing y avisa si la cuenta no sirve.</summary>
        Private Async Function Validar(caja As TextBox) As Task(Of CuentaCatalogo)
            Dim fila = Await _repositorio.ObtenerCuentaCatalogoAsync(caja.Text.Trim(), _formato, _cedula)
            If fila Is Nothing OrElse fila.Auxiliar <> "1" Then
                Dim aviso = If(fila Is Nothing, "La cuenta contable no existe.", "La cuenta es un padre...")
                MessageBox.Show(Me, aviso, Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
                caja.Focus()
                Return Nothing
            End If
            Return fila
        End Function

        Private Async Sub Agregar(sender As Object, e As EventArgs)
            Dim opcion = TryCast(cmbLocal.SelectedItem, OpcionCodigo)
            If opcion Is Nothing Then Return
            Try
                Dim contable = Await Validar(txtCuenta)
                If contable Is Nothing Then Return
                Dim conversion = ""
                If _conversion AndAlso txtConv.Text.Trim().Length > 0 Then
                    Dim fila = Await Validar(txtConv)
                    If fila Is Nothing Then Return
                    conversion = fila.Codigo
                End If
                rejilla.Rows.Add(opcion.Codigo, opcion.Descripcion, _formato.Mostrar(contable.Codigo), contable.Codigo, contable.Descripcion, _formato.Mostrar(conversion), conversion)
                cmbLocal.Items.Remove(opcion)
                If cmbLocal.Items.Count > 0 Then cmbLocal.SelectedIndex = 0
                btnAgregar.Enabled = cmbLocal.Items.Count > 0
                txtCuenta.Clear()
                txtConv.Clear()
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        Private Sub Borrar(sender As Object, e As EventArgs)
            Dim fila = rejilla.CurrentRow
            If fila Is Nothing Then Return
            cmbLocal.Items.Add(New OpcionCodigo With {.Codigo = CLng(fila.Cells(0).Value), .Descripcion = CStr(fila.Cells(1).Value)})
            rejilla.Rows.Remove(fila)
            If cmbLocal.SelectedIndex < 0 Then cmbLocal.SelectedIndex = 0
            btnAgregar.Enabled = True
        End Sub

        Private Async Sub Aceptar(sender As Object, e As EventArgs)
            btnAceptar.Enabled = False
            Try
                Dim filas = rejilla.Rows.Cast(Of DataGridViewRow)().Select(Function(f) New ContaDepartamento With {
                    .Local = CLng(f.Cells(0).Value), .Contable = CStr(f.Cells(3).Value), .Descripcion = CStr(f.Cells(4).Value), .Conversion = CStr(f.Cells(6).Value)
                }).ToList()
                Await _repositorio.GuardarContaAsync(_banco, _cuenta, filas, _uid, _localSesion)
                MessageBox.Show(Me, "Información Actualizada...", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
                Close()
            Catch ex As MySqlException
                Mensajes.NoGuardo(ex.Message, "GUARDANDO CUENTA POR DEPARTAMENTO", Modulo.TituloModulo)
                btnAceptar.Enabled = True
            End Try
        End Sub

    End Class

End Namespace
