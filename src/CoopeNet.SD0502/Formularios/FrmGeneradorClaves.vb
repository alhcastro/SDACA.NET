Imports System.Globalization
Imports MySqlConnector
Imports CoopeNet.Core.Pos
Imports CoopeNet.Ui

Namespace SD0502

    ''' <summary>
    ''' Generador de claves: un supervisor se valida y genera la clave temporal con que autoriza en el POS. Migración de
    ''' SD0502\frmGenerador.frm (Mantenimiento, opción 077, privilegio 5000).
    ''' </summary>
    ''' <remarks>
    ''' Diferencias con VB6: muestra cuándo vence la clave actual del supervisor y cuándo vence la nueva; cambiar la
    ''' contraseña también pide validar de nuevo; la clave se muestra grande. El marco fraCantidad de VB6 no tiene
    ''' código y no se migra.
    ''' </remarks>
    Public Class FrmGeneradorClaves
        Inherits Form

        Private ReadOnly _repo As ClavesSupervisorRepositorio
        Private ReadOnly _fabrica As Core.Datos.IFabricaConexiones
        Private ReadOnly _local As Long
        Private _uid As Long?
        Private _supervisor As String = ""

        Private ReadOnly txtUsuario As New TextBox With {.Name = "txtUsuario", .AccessibleName = "Usuario", .Location = New Point(110, 14), .Width = 180}
        Private ReadOnly txtContrasena As New TextBox With {.Name = "txtContrasena", .AccessibleName = "Contraseña", .UseSystemPasswordChar = True, .Location = New Point(110, 46), .Width = 180}
        Private ReadOnly btnValidar As New Button With {.Name = "btnValidar", .Text = "&Validar", .Location = New Point(300, 44), .Size = New Size(100, 28)}
        Private ReadOnly gbClave As New GroupBox With {.Name = "gbClave", .Text = "Clave", .Location = New Point(12, 84), .Size = New Size(388, 214)}
        Private ReadOnly lblVigente As New Label With {.Name = "lblVigente", .AutoSize = False, .AutoEllipsis = True, .Location = New Point(14, 24), .Size = New Size(360, 20)}
        Private ReadOnly opt3 As New RadioButton With {.Name = "opt3", .Text = "3 caracteres", .AutoSize = True, .Location = New Point(14, 52)}
        Private ReadOnly opt4 As New RadioButton With {.Name = "opt4", .Text = "4 caracteres", .AutoSize = True, .Location = New Point(134, 52), .Checked = True}
        Private ReadOnly opt5 As New RadioButton With {.Name = "opt5", .Text = "5 caracteres", .AutoSize = True, .Location = New Point(254, 52)}
        Private ReadOnly cmbDias As New ComboBox With {.Name = "cmbDias", .AccessibleName = "Validez de la clave en días", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(200, 84), .Width = 70}
        Private ReadOnly btnGenerar As New Button With {.Name = "btnGenerar", .Text = "&Generar", .Location = New Point(14, 124), .Size = New Size(110, 30)}
        Private ReadOnly txtClaveNueva As New TextBox With {
            .Name = "txtClaveNueva", .AccessibleName = "Clave generada", .ReadOnly = True, .TextAlign = HorizontalAlignment.Center,
            .Font = New Font("Segoe UI", 18.0F, FontStyle.Bold), .Location = New Point(134, 120), .Width = 130}
        Private ReadOnly btnGuardar As New Button With {.Name = "btnGuardar", .Text = "G&uardar", .Location = New Point(274, 124), .Size = New Size(100, 30)}
        Private ReadOnly lblVence As New Label With {.Name = "lblVence", .AutoSize = False, .AutoEllipsis = True, .Location = New Point(14, 172), .Size = New Size(360, 20)}
        Private ReadOnly btnSalir As New Button With {.Name = "btnSalir", .Text = "&Salir", .Location = New Point(300, 308), .Size = New Size(100, 30)}

        Public Sub New(repo As ClavesSupervisorRepositorio, fabrica As Core.Datos.IFabricaConexiones, local As Long)
            _repo = repo
            _fabrica = fabrica
            _local = local
            Text = "Generador de claves"
            ClientSize = New Size(412, 350)
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent
            AcceptButton = btnValidar
            CancelButton = btnSalir

            For d = 1 To ClavesSupervisorRepositorio.DiasMaximo
                cmbDias.Items.Add(d.ToString(CultureInfo.InvariantCulture))
            Next
            cmbDias.SelectedIndex = 0
            gbClave.Controls.AddRange(New Control() {
                lblVigente, opt3, opt4, opt5, New Label With {.Text = "Validez de la clave en días", .AutoSize = True, .Location = New Point(14, 87)}, cmbDias,
                btnGenerar, txtClaveNueva, btnGuardar, lblVence})
            Controls.AddRange(New Control() {
                New Label With {.Text = "Usuario", .AutoSize = True, .Location = New Point(12, 17)}, txtUsuario,
                New Label With {.Text = "Contraseña", .AutoSize = True, .Location = New Point(12, 49)}, txtContrasena, btnValidar,
                gbClave, btnSalir})

            AddHandler btnValidar.Click, Async Sub() Await ValidarAsync()
            AddHandler btnGenerar.Click, Async Sub() Await GenerarAsync()
            AddHandler btnGuardar.Click, Async Sub() Await GuardarAsync()
            AddHandler btnSalir.Click, Sub() Close()
            AddHandler txtUsuario.TextChanged, Sub() Desautorizar()
            AddHandler txtContrasena.TextChanged, Sub() Desautorizar()
            Desautorizar()
        End Sub

        ''' <summary>SDTUser_Change: sin supervisor validado el marco queda apagado.</summary>
        Private Sub Desautorizar()
            _uid = Nothing
            gbClave.Enabled = False
            txtClaveNueva.Text = ""
            lblVigente.Text = ""
            lblVence.Text = ""
            btnGuardar.Enabled = False
            AcceptButton = btnValidar
        End Sub

        ''' <summary>cmdValidar: Validar.</summary>
        Private Async Function ValidarAsync() As Task
            Dim login = txtUsuario.Text.Trim()
            If login = "" Then
                MessageBox.Show(Me, "El nombre de usuario es requerido.", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Error)
                txtUsuario.Focus()
                Return
            End If
            btnValidar.Enabled = False
            Try
                Dim r = Await AutorizacionSupervisor.AutorizarAsync(_fabrica, login, txtContrasena.Text.Trim(), _local)
                Select Case r.Resultado
                    Case ResultadoAutorizacion.Autorizado
                        _uid = r.Uid
                        _supervisor = login
                        gbClave.Enabled = True
                        btnGuardar.Enabled = False
                        AcceptButton = btnGenerar
                        Dim vence = Await _repo.VenceAsync(r.Uid, _local)
                        lblVigente.Text = If(Not vence.HasValue, $"{login} no tiene clave en este local.",
                                            If(vence.Value < Date.Now, $"La clave de {login} venció el {vence.Value:d}.", $"La clave actual de {login} vence el {vence.Value:d}."))
                        btnGenerar.Focus()
                    Case ResultadoAutorizacion.SinPrivilegio
                        MessageBox.Show(Me, $"El usuario {login} no tiene privilegios para modificar.", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Case Else
                        MessageBox.Show(Me, $"El usuario {login} no está autorizado a modificar.", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
                End Select
            Catch ex As MySqlException
                Mensajes.NoConecta()
            Finally
                btnValidar.Enabled = True
            End Try
        End Function

        Private ReadOnly Property Digitos As Integer
            Get
                Return If(opt3.Checked, 3, If(opt5.Checked, 5, 4))
            End Get
        End Property

        ''' <summary>cmdGenerar: una que no tenga otro supervisor del local.</summary>
        Private Async Function GenerarAsync() As Task
            If Not _uid.HasValue Then Return
            Try
                txtClaveNueva.Text = Await _repo.GenerarLibreAsync(Digitos, _local)
                lblVence.Text = ""
                btnGuardar.Enabled = True
                AcceptButton = btnGuardar
            Catch ex As Exception When TypeOf ex Is MySqlException OrElse TypeOf ex Is InvalidOperationException
                MessageBox.Show(Me, ex.Message, Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
            End Try
        End Function

        ''' <summary>cmdGuardar.</summary>
        Private Async Function GuardarAsync() As Task
            If Not _uid.HasValue OrElse txtClaveNueva.Text = "" Then Return
            btnGuardar.Enabled = False
            Try
                Dim vence = Await _repo.GuardarAsync(_local, _uid.Value, txtClaveNueva.Text, CInt(cmbDias.SelectedItem))
                lblVence.Text = $"Clave de {_supervisor} guardada: vence el {vence:d} a las {vence:HH:mm}."
                lblVigente.Text = $"La clave actual de {_supervisor} vence el {vence:d}."
                AcceptButton = Nothing
                MessageBox.Show(Me, "Clave guardada.", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
            Catch ex As MySqlException
                btnGuardar.Enabled = True
                Mensajes.NoGuardo(ex.Message, "Claves", Modulo.TituloModulo)
            End Try
        End Function

    End Class

End Namespace
