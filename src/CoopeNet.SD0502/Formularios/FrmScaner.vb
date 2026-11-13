Imports MySqlConnector
Imports CoopeNet.Core.Pos
Imports CoopeNet.Ui

Namespace SD0502

    ''' <summary>
    ''' Tipo de escáner de esta estación: por teclado o por puerto serial. Migración de SD0502\frmScaner.frm
    ''' (Mantenimiento, opción 030, privilegio 5047).
    ''' </summary>
    Public Class FrmScaner
        Inherits Form

        Private ReadOnly _repo As ScanerEstacionRepositorio
        Private ReadOnly _estacionId As String
        Private ReadOnly _local As Long

        Private ReadOnly lblEstacion As New Label With {.Name = "lblEstacion", .AutoSize = True, .Location = New Point(90, 16)}
        Private ReadOnly gbConexion As New GroupBox With {.Name = "gbConexion", .Text = "Conectado al", .Location = New Point(12, 48), .Size = New Size(340, 96)}
        Private ReadOnly optTeclado As New RadioButton With {.Name = "optTeclado", .Text = "Teclado", .AutoSize = True, .Location = New Point(18, 28), .Checked = True}
        Private ReadOnly optSerial As New RadioButton With {.Name = "optSerial", .Text = "Puerto serial", .AutoSize = True, .Location = New Point(18, 58)}
        Private ReadOnly btnGuardar As New Button With {.Name = "btnGuardar", .Text = "&Guardar", .Location = New Point(216, 22), .Size = New Size(108, 30)}
        Private ReadOnly btnCancelar As New Button With {.Name = "btnCancelar", .Text = "&Cancelar", .Location = New Point(216, 56), .Size = New Size(108, 30)}

        Public Sub New(repo As ScanerEstacionRepositorio, estacionId As String, estacion As String, local As Long)
            _repo = repo
            _estacionId = estacionId
            _local = local
            Text = "Tipo de escáner"
            ClientSize = New Size(364, 156)
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent
            AcceptButton = btnGuardar
            CancelButton = btnCancelar
            lblEstacion.Text = estacion

            gbConexion.Controls.AddRange(New Control() {optTeclado, optSerial, btnGuardar, btnCancelar})
            Controls.AddRange(New Control() {New Label With {.Text = "Estación", .AutoSize = True, .Location = New Point(12, 16)}, lblEstacion, gbConexion})
            AddHandler btnGuardar.Click, Async Sub() Await GuardarAsync()
            AddHandler btnCancelar.Click, Sub() Close()
        End Sub

        ''' <summary>SD_Cargar.</summary>
        Protected Overrides Async Sub OnLoad(e As EventArgs)
            MyBase.OnLoad(e)
            If String.IsNullOrEmpty(_estacionId) Then
                btnGuardar.Enabled = False
                MessageBox.Show(Me, "Esta estación no está registrada.", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If
            Try
                Dim serial = Await _repo.PuertoSerialAsync(_estacionId)
                optSerial.Checked = serial
                optTeclado.Checked = Not serial
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        ''' <summary>cmdGuardar: guarda y cierra.</summary>
        Private Async Function GuardarAsync() As Task
            If String.IsNullOrEmpty(_estacionId) Then Return
            Try
                btnGuardar.Enabled = False
                Await _repo.GuardarAsync(_estacionId, optSerial.Checked, _local)
                Close()
            Catch ex As MySqlException
                btnGuardar.Enabled = True
                Mensajes.NoGuardo(ex.Message, "ESCÁNER", Modulo.TituloModulo)
            End Try
        End Function

    End Class

End Namespace
