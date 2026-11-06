Imports MySqlConnector
Imports SDACA.Core.Bancos
Imports SDACA.Ui

Namespace SD1401

    ''' <summary>Base de las pantallas de conciliación: banco, cuenta y moneda en la parte superior.</summary>
    Public Class FrmConCuenta
        Inherits Form

        Private ReadOnly _cierre As CierreMes
        Private ReadOnly _uid As Long
        Private ReadOnly _todasLasCuentas As Boolean

        Protected ReadOnly gbCuenta As New GroupBox With {.Name = "gbCuenta", .Text = "Información de la Cuenta", .Location = New Point(10, 8)}
        Protected ReadOnly cmbBancos As New ComboBox With {.Name = "cmbBancos", .AccessibleName = "Banco", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(90, 30)}
        Protected ReadOnly cmbCuentas As New ComboBox With {.Name = "cmbCuentas", .AccessibleName = "Cuenta", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(90, 60)}
        Protected ReadOnly txtMoneda As New TextBox With {.Name = "txtMoneda", .AccessibleName = "Moneda", .ReadOnly = True, .TabStop = False, .Location = New Point(90, 90), .Width = 220}

        ''' <summary>Solo para el diseñador.</summary>
        Public Sub New()
        End Sub

        Public Sub New(cierre As CierreMes, uid As Long, todasLasCuentas As Boolean, titulo As String, tamano As Size)
            _cierre = cierre
            _uid = uid
            _todasLasCuentas = todasLasCuentas
            Text = titulo
            ClientSize = tamano
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent
            gbCuenta.Size = New Size(tamano.Width - 20, 124)
            cmbBancos.Width = tamano.Width - 124
            cmbCuentas.Width = tamano.Width - 124
            gbCuenta.Controls.AddRange(New Control() {Etiqueta("Banco", 12, 33), cmbBancos, Etiqueta("Cuenta #", 12, 63), cmbCuentas, Etiqueta("Moneda", 12, 93), txtMoneda})
            Controls.Add(gbCuenta)
            AddHandler Load, AddressOf CargarBancos
            AddHandler cmbBancos.SelectedIndexChanged, AddressOf BancoCambio
            AddHandler cmbCuentas.SelectedIndexChanged, AddressOf CuentaElegida
        End Sub

        Protected Shared Function Etiqueta(texto As String, x As Integer, y As Integer) As Label
            Return New Label With {.Text = texto, .AutoSize = True, .Location = New Point(x, y)}
        End Function

        Protected ReadOnly Property Banco As BancoCierre
            Get
                Return TryCast(cmbBancos.SelectedItem, BancoCierre)
            End Get
        End Property

        Protected ReadOnly Property Cuenta As CuentaCierre
            Get
                Return TryCast(cmbCuentas.SelectedItem, CuentaCierre)
            End Get
        End Property

        Protected ReadOnly Property HayCuenta As Boolean
            Get
                Return Banco IsNot Nothing AndAlso Cuenta IsNot Nothing
            End Get
        End Property

        ''' <summary>Se llama al cambiar el banco o la cuenta. Cuenta es Nothing si no hay una elegida.</summary>
        Protected Overridable Sub AlCambiarCuenta()
        End Sub

        Protected Sub Aviso(texto As String, Optional icono As MessageBoxIcon = MessageBoxIcon.Information)
            MessageBox.Show(Me, texto, Modulo.TituloModulo, MessageBoxButtons.OK, icono)
        End Sub

        Protected Function Pregunta(texto As String) As Boolean
            Return MessageBox.Show(Me, texto, Modulo.TituloModulo, MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes
        End Function

        Private Async Sub CargarBancos(sender As Object, e As EventArgs)
            Try
                cmbBancos.Items.AddRange((Await _cierre.ListarBancosAsync(_uid, _todasLasCuentas)).ToArray())
                If cmbBancos.Items.Count > 0 Then cmbBancos.SelectedIndex = 0
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        Private Async Sub BancoCambio(sender As Object, e As EventArgs)
            cmbCuentas.Items.Clear()
            txtMoneda.Clear()
            AlCambiarCuenta()
            If Banco Is Nothing Then Return
            Try
                cmbCuentas.Items.AddRange((Await _cierre.ListarCuentasAsync(Banco.Codigo, _uid, _todasLasCuentas)).ToArray())
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        Private Sub CuentaElegida(sender As Object, e As EventArgs)
            txtMoneda.Text = If(Cuenta Is Nothing, "", Cuenta.Moneda)
            AlCambiarCuenta()
        End Sub

    End Class

End Namespace
