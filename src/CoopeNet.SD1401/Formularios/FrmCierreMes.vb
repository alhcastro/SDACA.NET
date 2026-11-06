Imports MySqlConnector
Imports CoopeNet.Core.Bancos
Imports CoopeNet.Ui

Namespace SD1401

    ''' <summary>
    ''' Opción 021 (Cerrar Mes), privilegio 14016. Migración de Fuentes\SD1401\FrmCierre.frm. El original
    ''' anuncia "cerrado correctamente" aunque el INSERT falle; aquí se avisa el error. Por cada cuenta y
    ''' mes cerrado, anular un movimiento genera una reversión en lugar de borrarlo.
    ''' </summary>
    Partial Public Class FrmCierreMes
        Inherits Form

        Private ReadOnly _cierre As CierreMes
        Private ReadOnly _uid As Long
        Private ReadOnly _todasLasCuentas As Boolean

        Public Sub New(cierre As CierreMes, uid As Long, todasLasCuentas As Boolean)
            _cierre = cierre
            _uid = uid
            _todasLasCuentas = todasLasCuentas
            InitializeComponent()
            Dim hoy = Date.Today
            dtpMes.MaxDate = hoy
            dtpMes.Value = New Date(hoy.Year, hoy.Month, 1).AddDays(-10)
            Botones(False, False)
        End Sub

        Private ReadOnly Property Banco As BancoCierre
            Get
                Return TryCast(cmbBancos.SelectedItem, BancoCierre)
            End Get
        End Property

        Private ReadOnly Property Cuenta As CuentaCierre
            Get
                Return TryCast(cmbCuentas.SelectedItem, CuentaCierre)
            End Get
        End Property

        Private Sub Botones(puedeCerrar As Boolean, puedeAbrir As Boolean)
            btnCerrarMes.Enabled = puedeCerrar
            btnAbrirMes.Enabled = puedeAbrir
            lblEstado.Text = If(puedeAbrir, "Mes cerrado", If(puedeCerrar, "Mes abierto", ""))
            lblEstado.ForeColor = If(puedeAbrir, Tema.Peligro, Tema.Acento)
        End Sub

        Private Async Sub FrmCierreMes_Load(sender As Object, e As EventArgs) Handles MyBase.Load
            Try
                Dim bancos = Await _cierre.ListarBancosAsync(_uid, _todasLasCuentas)
                cmbBancos.Items.AddRange(bancos.ToArray())
                If cmbBancos.Items.Count > 0 Then cmbBancos.SelectedIndex = 0
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        Private Async Sub CmbBancos_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbBancos.SelectedIndexChanged
            cmbCuentas.Items.Clear()
            txtMoneda.Clear()
            Botones(False, False)
            If Banco Is Nothing Then Return
            Try
                Dim cuentas = Await _cierre.ListarCuentasAsync(Banco.Codigo, _uid, _todasLasCuentas)
                cmbCuentas.Items.AddRange(cuentas.ToArray())
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        Private Async Sub CmbCuentas_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbCuentas.SelectedIndexChanged
            txtMoneda.Text = If(Cuenta Is Nothing, "", Cuenta.Moneda)
            Await Refrescar()
        End Sub

        Private Async Sub DtpMes_ValueChanged(sender As Object, e As EventArgs) Handles dtpMes.ValueChanged
            Await Refrescar()
        End Sub

        Private Async Function Refrescar() As Task
            If Banco Is Nothing OrElse Cuenta Is Nothing Then
                Botones(False, False)
                Return
            End If
            Try
                Dim cerrado = Await _cierre.EstaCerradoAsync(Banco.Codigo, Cuenta.Cuenta, dtpMes.Value)
                Botones(Not cerrado, cerrado)
            Catch ex As MySqlException
                Botones(False, False)
                Mensajes.NoConecta()
            End Try
        End Function

        Private Async Sub BtnCerrarMes_Click(sender As Object, e As EventArgs) Handles btnCerrarMes.Click
            If Banco Is Nothing OrElse Cuenta Is Nothing Then Return
            Try
                Await _cierre.CerrarAsync(Banco.Codigo, Cuenta.Cuenta, dtpMes.Value)
                MessageBox.Show(Me, "El mes ha sido cerrado correctamente...", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
            Catch ex As MySqlException
                Mensajes.NoGuardo(ex.Message, "CERRANDO MES", Modulo.TituloModulo)
            End Try
            Await Refrescar()
        End Sub

        Private Async Sub BtnAbrirMes_Click(sender As Object, e As EventArgs) Handles btnAbrirMes.Click
            If Banco Is Nothing OrElse Cuenta Is Nothing Then Return
            Try
                Await _cierre.AbrirAsync(Banco.Codigo, Cuenta.Cuenta, dtpMes.Value)
                MessageBox.Show(Me, "El mes Abierto correctamente...", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
            Catch ex As MySqlException
                Mensajes.NoGuardo(ex.Message, "ABRIENDO MES", Modulo.TituloModulo)
            End Try
            Await Refrescar()
        End Sub

        Private Sub BtnSalir_Click(sender As Object, e As EventArgs) Handles btnSalir.Click
            Close()
        End Sub

    End Class

End Namespace
