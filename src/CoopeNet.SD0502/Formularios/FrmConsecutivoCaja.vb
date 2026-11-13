Imports System.Globalization
Imports MySqlConnector
Imports CoopeNet.Core
Imports CoopeNet.Core.Pos
Imports CoopeNet.Ui

Namespace SD0502

    ''' <summary>
    ''' Modificar consecutivo de una caja. Migración de SD0502\frmModConsec.frm: un usuario con el 5000 y el
    ''' 5030 en el local (o root) se valida con su contraseña y fija los seis dígitos del consecutivo, que no
    ''' pueden bajar. El cambio se guarda en el acto, se anota en posvitmovimientos y la estación recibe el
    ''' aviso de reiniciar el POS.
    ''' </summary>
    Public Class FrmConsecutivoCaja
        Inherits Form

        Private ReadOnly _repo As CajasRepositorio
        Private ReadOnly _estacion As String
        Private ReadOnly _caja As String
        Private ReadOnly _local As Long
        Private ReadOnly _uid As Long
        Private ReadOnly _estacionSesion As String

        Private ReadOnly txtUsuario As New TextBox With {.Name = "txtUsuario", .AccessibleName = "Usuario", .Location = New Point(110, 14), .Width = 180}
        Private ReadOnly txtClave As New TextBox With {.Name = "txtClave", .AccessibleName = "Contraseña", .UseSystemPasswordChar = True, .Location = New Point(110, 46), .Width = 180}
        Private ReadOnly btnValidar As New Button With {.Name = "btnValidar", .Text = "&Validar", .Location = New Point(300, 44), .Size = New Size(90, 28)}
        Private ReadOnly gbConsecutivo As New GroupBox With {.Name = "gbConsecutivo", .Text = "Consecutivo", .Location = New Point(12, 84), .Size = New Size(378, 96), .Enabled = False}
        Private ReadOnly txtActual As New TextBox With {.Name = "txtActual", .AccessibleName = "Consecutivo actual", .ReadOnly = True, .TabStop = False, .TextAlign = HorizontalAlignment.Right, .Location = New Point(98, 26), .Width = 100}
        Private ReadOnly txtNuevo As New TextBox With {.Name = "txtNuevo", .AccessibleName = "Consecutivo nuevo", .MaxLength = 6, .TextAlign = HorizontalAlignment.Right, .Location = New Point(98, 60), .Width = 100}
        Private ReadOnly btnAceptar As New Button With {.Name = "btnAceptar", .Text = "&Aceptar", .Enabled = False, .Location = New Point(204, 190), .Size = New Size(90, 28)}
        Private ReadOnly btnCancelar As New Button With {.Name = "btnCancelar", .Text = "&Cancelar", .DialogResult = DialogResult.Cancel, .Location = New Point(300, 190), .Size = New Size(90, 28)}

        ''' <summary>El consecutivo guardado, completo (caja y seis dígitos).</summary>
        Public ReadOnly Property Nuevo As Double

        Public Sub New(repo As CajasRepositorio, estacion As String, consecutivo As Double, local As Long, uid As Long, estacionSesion As String)
            _repo = repo
            _estacion = estacion
            _local = local
            _uid = uid
            _estacionSesion = estacionSesion
            Dim partes = CajasRepositorio.PartirConsecutivo(consecutivo).Value
            _caja = partes.Caja
            txtActual.Text = partes.Numero

            Text = "Modificar consecutivo"
            ClientSize = New Size(402, 230)
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent
            gbConsecutivo.Controls.AddRange(New Control() {
                New Label With {.Text = "Actual", .AutoSize = True, .Location = New Point(14, 29)}, txtActual,
                New Label With {.Text = "Nuevo", .AutoSize = True, .Location = New Point(14, 63)}, txtNuevo,
                New Label With {.Text = $"Caja {_caja}", .AutoSize = True, .Location = New Point(204, 63)}
            })
            Controls.AddRange(New Control() {
                New Label With {.Text = "Usuario", .AutoSize = True, .Location = New Point(12, 17)}, txtUsuario,
                New Label With {.Text = "Contraseña", .AutoSize = True, .Location = New Point(12, 49)}, txtClave, btnValidar,
                gbConsecutivo, btnAceptar, btnCancelar
            })
            AcceptButton = btnValidar
            CancelButton = btnCancelar
            AddHandler btnValidar.Click, AddressOf BtnValidar_Click
            AddHandler btnAceptar.Click, AddressOf BtnAceptar_Click
            AddHandler txtNuevo.KeyPress, Sub(s, e) If Not Char.IsControl(e.KeyChar) AndAlso Not Char.IsDigit(e.KeyChar) Then e.Handled = True
        End Sub

        Private Sub Avisar(texto As String, icono As MessageBoxIcon)
            MessageBox.Show(Me, texto, Modulo.TituloModulo, MessageBoxButtons.OK, icono)
        End Sub

        ''' <summary>cmdValidar_Click.</summary>
        Private Async Sub BtnValidar_Click(sender As Object, e As EventArgs)
            Dim login = txtUsuario.Text.Trim()
            If login.Length = 0 Then
                Avisar("El nombre de usuario es requerido", MessageBoxIcon.Error)
                txtUsuario.Focus()
                Return
            End If
            btnValidar.Enabled = False
            Try
                Select Case Await _repo.AutorizarAsync(login, txtClave.Text.Trim(), _local)
                    Case ResultadoAutorizacion.Autorizado
                        gbConsecutivo.Enabled = True
                        btnAceptar.Enabled = True
                        txtUsuario.Enabled = False
                        txtClave.Enabled = False
                        AcceptButton = btnAceptar
                        txtNuevo.Focus()
                        Return
                    Case ResultadoAutorizacion.SinPrivilegio
                        Avisar($"EL USUARIO {login} NO TIENE PRIVILEGIOS PARA MODIFICAR...", MessageBoxIcon.Information)
                    Case Else
                        Avisar($"EL USUARIO {login} NO ESTA AUTORIZADO A MODIFICAR...", MessageBoxIcon.Information)
                End Select
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
            btnValidar.Enabled = True
        End Sub

        ''' <summary>sdtNuevo_Validate y cmdAceptar_Click.</summary>
        Private Async Sub BtnAceptar_Click(sender As Object, e As EventArgs)
            Dim nuevo = txtNuevo.Text.Trim()
            If nuevo.Length = 0 OrElse Vb6.Val(nuevo) < Vb6.Val(txtActual.Text) Then
                Avisar("El consecutivo nuevo no puede ser menor al actual.", MessageBoxIcon.Error)
                txtNuevo.Focus()
                Return
            End If
            nuevo = nuevo.PadLeft(6, "0"c)
            txtNuevo.Text = nuevo
            btnAceptar.Enabled = False
            Try
                _Nuevo = Await _repo.CambiarConsecutivoAsync(_estacion, _caja, txtActual.Text, nuevo, _local, _uid, _estacionSesion)
                DialogResult = DialogResult.OK
                Close()
            Catch ex As MySqlException
                Mensajes.NoGuardo(ex.Message, "Error en cambio de consecutivo", Modulo.TituloModulo)
                btnAceptar.Enabled = True
            End Try
        End Sub

    End Class

End Namespace
