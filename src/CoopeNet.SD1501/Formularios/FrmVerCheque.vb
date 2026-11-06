Imports MySqlConnector
Imports CoopeNet.Core.Cheques
Imports CoopeNet.Ui

Namespace SD1501

    ''' <summary>
    ''' Opción 014 (Consultar Cheque), privilegio 15013. Migración de Fuentes\SD1501\FrmverCheque.frm.
    ''' Solo lectura. Falta "Imprimir Comprobante", que VB6 oculta si no existe ECCPrintComprobante.
    ''' </summary>
    Partial Public Class FrmVerCheque
        Inherits Form

        Private Const TituloCheque As String = "Información del Cheque # "

        Private ReadOnly _repositorio As ChequesRepositorio
        Private ReadOnly _local As Long
        Private ReadOnly _uid As Long
        Private ReadOnly _todasLasCuentas As Boolean
        Private ReadOnly _cedula As String
        Private _formato As FormatoCuenta = FormatoCuenta.Predeterminado

        Public Sub New(repositorio As ChequesRepositorio, local As Long, uid As Long, todasLasCuentas As Boolean, cedula As String)
            _repositorio = repositorio
            _local = local
            _uid = uid
            _todasLasCuentas = todasLasCuentas
            _cedula = cedula
            InitializeComponent()
            Limpiar()
        End Sub

        Private ReadOnly Property Banco As BancoCheque
            Get
                Return TryCast(cmbBancos.SelectedItem, BancoCheque)
            End Get
        End Property

        Private ReadOnly Property Cuenta As CuentaCheque
            Get
                Return TryCast(cmbCuentas.SelectedItem, CuentaCheque)
            End Get
        End Property

        Private Async Sub FrmVerCheque_Load(sender As Object, e As EventArgs) Handles MyBase.Load
            Try
                _formato = Await _repositorio.ObtenerFormatoCuentaAsync(_cedula)
                cmbBancos.DisplayMember = NameOf(BancoCheque.Etiqueta)
                cmbBancos.DataSource = Await _repositorio.ListarBancosAsync(_local, _uid, _todasLasCuentas)
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        Private Async Sub CmbBancos_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbBancos.SelectedIndexChanged
            Dim elegido = Banco
            cmbCuentas.DataSource = Nothing
            If elegido Is Nothing Then Return
            Try
                Dim cuentas = Await _repositorio.ListarCuentasAsync(_local, elegido.Codigo, _uid, _todasLasCuentas)
                If Banco IsNot elegido Then Return
                cmbCuentas.DisplayMember = NameOf(CuentaCheque.Etiqueta)
                cmbCuentas.DataSource = cuentas
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        Private Sub CmbCuentas_SelectionChangeCommitted(sender As Object, e As EventArgs) Handles cmbCuentas.SelectionChangeCommitted
            txtNumCheque.Focus()
        End Sub

        Private Async Sub TxtNumCheque_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtNumCheque.KeyPress
            If e.KeyChar <> ChrW(Keys.Return) Then Return
            e.Handled = True
            Await Consultar()
        End Sub

        Private Async Sub BtnBuscar_Click(sender As Object, e As EventArgs) Handles btnBuscar.Click
            If Banco Is Nothing OrElse Cuenta Is Nothing Then Return
            btnBuscar.Enabled = False
            Try
                Using datos = Await _repositorio.ListarChequesAsync(Banco.Codigo, Cuenta.Cuenta)
                    Using buscador As New FrmBuscador("Búsqueda de cheques", datos)
                        If buscador.ShowDialog(Me) = DialogResult.OK AndAlso buscador.Resultado(0).Length > 0 Then
                            txtNumCheque.Text = buscador.Resultado(0)
                            Await Consultar()
                        End If
                    End Using
                End Using
            Catch ex As MySqlException
                Mensajes.NoConecta()
            Finally
                btnBuscar.Enabled = True
            End Try
        End Sub

        Private Async Function Consultar() As Task
            If Banco Is Nothing OrElse Cuenta Is Nothing Then Return
            Dim numero As Integer
            Dim cheque As ChequeConsulta = Nothing
            Try
                If Integer.TryParse(txtNumCheque.Text.Trim(), numero) Then
                    cheque = Await _repositorio.ObtenerAsync(Banco.Codigo, Cuenta.Cuenta, numero)
                End If
            Catch ex As MySqlException
                Mensajes.NoConecta()
                Return
            End Try
            If cheque Is Nothing Then
                MessageBox.Show(Me, "El Cheque no Existe...", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
                txtNumCheque.Text = ""
                Limpiar()
                txtNumCheque.Focus()
                Return
            End If
            Mostrar(cheque)
        End Function

        Private Sub Limpiar()
            gbCheque.Text = TituloCheque
            gbCheque.ForeColor = Tema.Texto
            txtFecha.Text = ""
            txtNombre.Text = ""
            txtMonto.Text = "0.00"
            lblLetras.Text = ""
            txtCodigoSeguridad.Text = ""
            txtUsuario.Text = ""
            lblDetalle.Text = ""
            lblConciliado.Visible = False
            rejilla.Rows.Clear()
            tabs.SelectedIndex = 0
        End Sub

        Private Sub Mostrar(cheque As ChequeConsulta)
            gbCheque.Text = TituloCheque & cheque.Numero & If(cheque.EstaAnulado, "      ANULADO", "")
            gbCheque.ForeColor = If(cheque.EstaAnulado, Tema.Peligro, Tema.Texto)
            txtFecha.Text = If(cheque.Fecha = Date.MinValue, "", cheque.Fecha.ToString("dd/MM/yyyy"))
            txtNombre.Text = cheque.Nombre
            txtMonto.Text = cheque.Monto.ToString("N2")
            lblLetras.Text = MontoEnLetras.Convertir(cheque.Monto, True, True)
            txtCodigoSeguridad.Text = cheque.CodigoSeguridad
            txtUsuario.Text = cheque.Usuario
            lblDetalle.Text = cheque.Observaciones
            lblConciliado.Visible = cheque.Conciliado
            rejilla.Rows.Clear()
            For Each linea In cheque.Lineas
                rejilla.Rows.Add(_formato.Mostrar(linea.Cuenta), linea.Detalle, linea.Debe, linea.Haber)
            Next
        End Sub

        Private Sub BtnSalir_Click(sender As Object, e As EventArgs) Handles btnSalir.Click
            Close()
        End Sub

    End Class

End Namespace
