Imports System.Globalization
Imports MySqlConnector
Imports CoopeNet.Core.Bancos
Imports CoopeNet.Ui

Namespace SD1401

    ''' <summary>
    ''' Opción Saldos por Cuenta, privilegio 14017. Migración de Fuentes\SD1401\FrmSaldosBanco.frm. Solo
    ''' lee. Allá el combo de monedas se queda en la primera; aquí se ve la moneda de la cuenta.
    ''' </summary>
    Partial Public Class FrmSaldosCuenta
        Inherits Form

        Private Shared ReadOnly Cultura As New CultureInfo("es-CR")

        Private ReadOnly _cierre As CierreMes
        Private ReadOnly _saldos As SaldosCuenta
        Private ReadOnly _uid As Long
        Private ReadOnly _local As Long
        Private ReadOnly _cedula As String
        Private ReadOnly _todasLasCuentas As Boolean
        Private _consulta As Integer

        Public Sub New(cierre As CierreMes, saldos As SaldosCuenta, uid As Long, local As Long, cedula As String, todasLasCuentas As Boolean)
            _cierre = cierre
            _saldos = saldos
            _uid = uid
            _local = local
            _cedula = cedula
            _todasLasCuentas = todasLasCuentas
            InitializeComponent()
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

        Private ReadOnly Property Periodo As PeriodoFiscal
            Get
                Return TryCast(cmbPeriodos.SelectedItem, PeriodoFiscal)
            End Get
        End Property

        Private Async Sub FrmSaldosCuenta_Load(sender As Object, e As EventArgs) Handles MyBase.Load
            Try
                Dim mesInicio = Await _saldos.ObtenerMesInicioAsync(_cedula)
                Dim periodos = Await _saldos.ListarPeriodosAsync(mesInicio)
                cmbPeriodos.Items.AddRange(periodos.ToArray())
                Dim bancos = Await _cierre.ListarBancosAsync(_uid, _todasLasCuentas)
                cmbBancos.Items.AddRange(bancos.ToArray())
                If cmbPeriodos.Items.Count > 0 Then cmbPeriodos.SelectedIndex = 0
                If cmbBancos.Items.Count > 0 Then cmbBancos.SelectedIndex = 0
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        Private Async Sub CmbBancos_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbBancos.SelectedIndexChanged
            cmbCuentas.Items.Clear()
            Limpiar()
            If Banco Is Nothing Then Return
            Try
                Dim cuentas = Await _saldos.ListarCuentasAsync(Banco.Codigo, _local, _uid, _todasLasCuentas)
                cmbCuentas.Items.AddRange(cuentas.ToArray())
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        Private Async Sub CmbCuentas_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbCuentas.SelectedIndexChanged
            Await Refrescar()
        End Sub

        Private Async Sub CmbPeriodos_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbPeriodos.SelectedIndexChanged
            Await Refrescar()
        End Sub

        Private Sub Limpiar()
            _consulta += 1
            txtMoneda.Clear()
            rejilla.Rows.Clear()
            txtSaldoActual.Clear()
        End Sub

        Private Async Function Refrescar() As Task
            Limpiar()
            If Banco Is Nothing OrElse Cuenta Is Nothing Then Return
            txtMoneda.Text = Cuenta.Moneda
            Dim consulta = _consulta
            Try
                Dim saldoActual = Await _saldos.SaldoActualAsync(Banco.Codigo, Cuenta.Cuenta, Date.Today)
                Dim meses = If(Periodo Is Nothing, New List(Of SaldoMes)(), Await _saldos.ListarMesesAsync(Banco.Codigo, Cuenta.Cuenta, Periodo))
                If consulta <> _consulta Then Return
                For Each mes In meses
                    rejilla.Rows.Add(mes.Mes.ToString("MMM-yyyy", Cultura).Replace(".", ""), mes.SaldoInicial, mes.Debito, mes.Credito, mes.SaldoFinal)
                Next
                txtSaldoActual.Text = saldoActual.ToString("#,##0.00")
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Function

        Private Sub BtnSalir_Click(sender As Object, e As EventArgs) Handles btnSalir.Click
            Close()
        End Sub

    End Class

End Namespace
