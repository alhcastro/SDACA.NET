Imports MySqlConnector
Imports SDACA.Core
Imports SDACA.Core.Bancos
Imports SDACA.Ui

Namespace SD1401

    ''' <summary>Usuarios que pueden ver una cuenta. Migración de Fuentes\SD1401\FrmCuentasUsuarios.frm.</summary>
    Public Class FrmCuentaUsuarios
        Inherits Form

        Private ReadOnly _repositorio As CuentasBancariasRepositorio
        Private ReadOnly _banco As Long
        Private ReadOnly _cuenta As String
        Private ReadOnly _localSesion As Long
        Private ReadOnly rejilla As New DataGridView With {
            .Name = "rejilla", .Location = New Point(10, 44), .Size = New Size(560, 280), .ReadOnly = True, .AllowUserToAddRows = False, .AllowUserToDeleteRows = False,
            .RowHeadersVisible = False, .SelectionMode = DataGridViewSelectionMode.FullRowSelect, .MultiSelect = False
        }
        Private ReadOnly btnAgregar As New Button With {.Name = "btnAgregar", .Text = "&Agregar", .Location = New Point(10, 332), .Size = New Size(100, 30), .UseVisualStyleBackColor = True}
        Private ReadOnly btnBorrar As New Button With {.Name = "btnQuitar", .Text = "&Borrar", .Location = New Point(116, 332), .Size = New Size(100, 30), .UseVisualStyleBackColor = True}
        Private ReadOnly btnAceptar As New Button With {.Name = "btnGuardar", .Text = "&Aceptar", .Location = New Point(364, 332), .Size = New Size(100, 30), .UseVisualStyleBackColor = True}
        Private ReadOnly btnCancelar As New Button With {.Name = "btnCancelar", .Text = "&Cancelar", .Location = New Point(470, 332), .Size = New Size(100, 30), .UseVisualStyleBackColor = True, .DialogResult = DialogResult.Cancel}

        Public Sub New(repositorio As CuentasBancariasRepositorio, banco As Long, descripcionBanco As String, cuenta As String, descripcionCuenta As String, localSesion As Long)
            _repositorio = repositorio
            _banco = banco
            _cuenta = cuenta
            _localSesion = localSesion
            Text = "Usuarios para Cuentas Bancarias"
            ClientSize = New Size(580, 372)
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent
            CancelButton = btnCancelar
            rejilla.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "Uid", .Visible = False})
            rejilla.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Usuario", .Width = 120})
            rejilla.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "ID", .Width = 130})
            rejilla.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Nombre", .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill})
            Dim titulo As New Label With {.Text = $"Cuenta # {cuenta} ({descripcionCuenta}) del {descripcionBanco}", .AutoEllipsis = True, .Location = New Point(10, 14), .Size = New Size(560, 22)}
            Controls.AddRange(New Control() {titulo, rejilla, btnAgregar, btnBorrar, btnAceptar, btnCancelar})
            AddHandler Load, AddressOf Ventana_Load
            AddHandler btnAgregar.Click, AddressOf Agregar
            AddHandler btnBorrar.Click, AddressOf Borrar
            AddHandler btnAceptar.Click, AddressOf Aceptar
        End Sub

        Private Async Sub Ventana_Load(sender As Object, e As EventArgs)
            Try
                For Each u In Await _repositorio.ListarUsuariosAsync(_banco, _cuenta)
                    rejilla.Rows.Add(u.Uid, u.Usuario, u.Id, u.Nombre)
                Next
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        Private Function YaEsta(uid As Long) As Boolean
            Return rejilla.Rows.Cast(Of DataGridViewRow)().Any(Function(f) CLng(f.Cells(0).Value) = uid)
        End Function

        Private Async Sub Agregar(sender As Object, e As EventArgs)
            Try
                Using datos = Await _repositorio.BuscarUsuariosAsync(_banco, _cuenta)
                    Using buscador As New FrmBuscador("Búsqueda de usuarios", datos)
                        If buscador.ShowDialog(Me) = DialogResult.OK AndAlso buscador.Resultado(0).Length > 0 Then
                            Dim uid = Vb6.Val(buscador.Resultado(0))
                            If Not YaEsta(uid) Then rejilla.Rows.Add(uid, buscador.Resultado(1), buscador.Resultado(2), buscador.Resultado(3))
                        End If
                    End Using
                End Using
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        Private Sub Borrar(sender As Object, e As EventArgs)
            If rejilla.CurrentRow IsNot Nothing Then rejilla.Rows.Remove(rejilla.CurrentRow)
        End Sub

        Private Async Sub Aceptar(sender As Object, e As EventArgs)
            btnAceptar.Enabled = False
            Try
                Dim usuarios = rejilla.Rows.Cast(Of DataGridViewRow)().Select(Function(f) CLng(f.Cells(0).Value)).ToList()
                Await _repositorio.GuardarUsuariosAsync(_banco, _cuenta, usuarios, _localSesion)
                MessageBox.Show(Me, "Información Actualizada...", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
                Close()
            Catch ex As MySqlException
                Mensajes.NoGuardo(ex.Message, "GUARDANDO USUARIOS DE LA CUENTA", Modulo.TituloModulo)
                btnAceptar.Enabled = True
            End Try
        End Sub

    End Class

End Namespace
