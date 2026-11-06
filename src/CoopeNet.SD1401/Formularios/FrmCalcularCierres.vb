Imports MySqlConnector
Imports CoopeNet.Core.Bancos
Imports CoopeNet.Ui

Namespace SD1401

    ''' <summary>
    ''' Opción Calcular Cierres, privilegio 14010. Migración de Fuentes\SD1401\FrmRCierre.frm. Rehace
    ''' ccbcierrediario desde la fecha de inicio; allá anuncia "Proceso Finalizado" aunque falle.
    ''' </summary>
    Partial Public Class FrmCalcularCierres
        Inherits Form

        Private ReadOnly _cierre As CierreMes
        Private ReadOnly _uid As Long
        Private ReadOnly _local As Long
        Private ReadOnly _todasLasCuentas As Boolean

        Public Sub New(cierre As CierreMes, uid As Long, local As Long, todasLasCuentas As Boolean)
            _cierre = cierre
            _uid = uid
            _local = local
            _todasLasCuentas = todasLasCuentas
            InitializeComponent()
            dtpInicio.Value = Date.Today.AddDays(-1)
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

        Private Async Sub FrmCalcularCierres_Load(sender As Object, e As EventArgs) Handles MyBase.Load
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
            If Banco Is Nothing Then Return
            Try
                Dim cuentas = Await _cierre.ListarCuentasAsync(Banco.Codigo, _uid, _todasLasCuentas)
                cmbCuentas.Items.AddRange(cuentas.ToArray())
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        Private Sub CmbCuentas_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbCuentas.SelectedIndexChanged
            txtMoneda.Text = If(Cuenta Is Nothing, "", Cuenta.Moneda)
        End Sub

        Private Async Sub BtnCalcular_Click(sender As Object, e As EventArgs) Handles btnCalcular.Click
            If Banco Is Nothing OrElse Cuenta Is Nothing Then Return
            Ocupado(True)
            Try
                Await _cierre.RecalcularAsync(_local, Banco.Codigo, Cuenta.Cuenta, dtpInicio.Value.Date, Date.Today)
                MessageBox.Show(Me, "Proceso finalizado.", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
            Catch ex As MySqlException
                Mensajes.NoGuardo(ex.Message, "CALCULANDO CIERRES", Modulo.TituloModulo)
            Finally
                Ocupado(False)
            End Try
        End Sub

        Private Sub Ocupado(activo As Boolean)
            barraProgreso.Visible = activo
            btnCalcular.Enabled = Not activo
            btnSalir.Enabled = Not activo
            gbDatos.Enabled = Not activo
            dtpInicio.Enabled = Not activo
        End Sub

        Private Sub BtnSalir_Click(sender As Object, e As EventArgs) Handles btnSalir.Click
            Close()
        End Sub

    End Class

End Namespace
