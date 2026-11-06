Imports MySqlConnector
Imports SDACA.Core.Cheques
Imports SDACA.Ui

Namespace SD1501

    ''' <summary>
    ''' Opción 010 (Impresión Masiva de Cheques), privilegio 15009. Migración de
    ''' Fuentes\SD1501\FrmPrintChequeM.frm: reimprime un rango y salta los anulados e inexistentes. No
    ''' graba nada. La pausa conserva la posición; "Detener" la descarta. En PDF, todo el rango queda
    ''' en un archivo que se guarda al terminar o detener. El check de comprobantes no se migró: solo
    ''' se muestra con ECCPrintComprobante, que no está definido.
    ''' </summary>
    Partial Public Class FrmPrintChequeM
        Inherits Form

        Private Const SalidaPdf As Integer = 1

        Private ReadOnly _repositorio As ChequesRepositorio
        Private ReadOnly _impresion As ImpresionCheques
        Private ReadOnly _local As Long
        Private ReadOnly _uid As Long
        Private ReadOnly _todasLasCuentas As Boolean
        Private ReadOnly _cedula As String
        Private ReadOnly _estacion As String
        Private ReadOnly _textosPdf As New List(Of String)
        Private _formato As FormatoCuenta = FormatoCuenta.Predeterminado
        Private _rutaPdf As String = ""
        Private _imprimiendo As Boolean
        Private _ultimo As Long = -1

        Public Sub New(repositorio As ChequesRepositorio, impresion As ImpresionCheques, local As Long, uid As Long,
                       todasLasCuentas As Boolean, cedula As String, estacion As String)
            _repositorio = repositorio
            _impresion = impresion
            _local = local
            _uid = uid
            _todasLasCuentas = todasLasCuentas
            _cedula = cedula
            _estacion = estacion
            InitializeComponent()
            cmbSalida.SelectedIndex = 0
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

        Private Sub Aviso(texto As String)
            MessageBox.Show(Me, texto, Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
        End Sub

        Private Async Sub FrmPrintChequeM_Load(sender As Object, e As EventArgs) Handles MyBase.Load
            Try
                If Not Await _impresion.UsaPlantillaAsync(_local) Then
                    Aviso("Este local imprime con el formato gráfico, que no está migrado. Use el sistema anterior.")
                    Close()
                    Return
                End If
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

        Private Async Sub TxtDesde_Validated(sender As Object, e As EventArgs) Handles txtDesde.Validated
            Dim numero As Long
            If Banco Is Nothing OrElse Cuenta Is Nothing OrElse Not Long.TryParse(txtDesde.Text.Trim(), numero) Then Return
            Try
                Dim cheque = Await _impresion.ObtenerVigenteAsync(Banco.Codigo, Cuenta.Cuenta, numero)
                txtDetalle.Text = If(cheque Is Nothing, "", ResumenCheque.Armar(cheque, _formato))
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        Private Sub TxtHasta_Validated(sender As Object, e As EventArgs) Handles txtHasta.Validated
            Dim desde, hasta As Long
            If Long.TryParse(txtDesde.Text, desde) AndAlso Long.TryParse(txtHasta.Text, hasta) AndAlso desde > hasta Then
                MessageBox.Show(Me, "Error con el Rango...", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Error)
                txtHasta.Text = desde.ToString()
            End If
        End Sub

        Private Sub Bloquear(imprimiendo As Boolean)
            gbDatos.Enabled = Not imprimiendo
            btnImprimir.Enabled = Not imprimiendo
            btnPausa.Enabled = imprimiendo
            btnDetener.Enabled = imprimiendo
        End Sub

        Private Async Sub BtnImprimir_Click(sender As Object, e As EventArgs) Handles btnImprimir.Click
            Dim desde, hasta As Long
            If Not Long.TryParse(txtDesde.Text.Trim(), desde) OrElse Not Long.TryParse(txtHasta.Text.Trim(), hasta) Then
                Aviso("Por favor especifique un rango...")
                Return
            End If
            If _ultimo = -1 AndAlso cmbSalida.SelectedIndex = SalidaPdf Then
                Using dialogo As New SaveFileDialog With {.Filter = "PDF|*.pdf", .FileName = $"Cheques {Cuenta.Cuenta} {desde}-{hasta}.pdf"}
                    If dialogo.ShowDialog(Me) <> DialogResult.OK Then Return
                    _rutaPdf = dialogo.FileName
                End Using
            End If
            Bloquear(True)
            _imprimiendo = True
            lblEstado.Text = ""
            Await Imprimir(desde, hasta)
        End Sub

        Private Async Function Imprimir(desde As Long, hasta As Long) As Task
            Dim banco = Me.Banco.Codigo
            Dim cuenta = Me.Cuenta.Cuenta
            barra.Maximum = CInt(Math.Max(1, hasta - desde + 1))
            Try
                Dim seImprime = Await _impresion.SeImprimeAsync(_local)
                For numero = If(_ultimo = -1, desde, _ultimo + 1) To hasta
                    If Not _imprimiendo Then Return
                    barra.Value = CInt(numero - desde + 1)
                    Dim cheque = Await _impresion.ObtenerVigenteAsync(banco, cuenta, numero)
                    txtDetalle.Text = If(cheque Is Nothing, "", ResumenCheque.Armar(cheque, _formato))
                    If cheque IsNot Nothing AndAlso seImprime Then
                        Dim trabajo = Await _impresion.ArmarAsync(banco, cuenta, numero, _local, _estacion, _formato)
                        If cmbSalida.SelectedIndex = SalidaPdf Then
                            _textosPdf.Add(trabajo.Texto)
                        Else
                            ImpresionMatricial.Enviar(trabajo.Texto, trabajo.Impresora)
                        End If
                    End If
                    _ultimo = numero
                Next
                Terminar()
            Catch ex As Exception When TypeOf ex Is MySqlException OrElse TypeOf ex Is InvalidOperationException OrElse TypeOf ex Is System.ComponentModel.Win32Exception
                ' _ultimo se queda en el último impreso, para poder seguir desde el que falló.
                _imprimiendo = False
                Mensajes.NoGuardo(ex.Message, "Imprimir cheques", Modulo.TituloModulo)
                btnImprimir.Enabled = True
                btnPausa.Enabled = False
                btnDetener.Enabled = True
                lblEstado.Text = "Detenido por un error"
            End Try
        End Function

        Private Sub BtnPausa_Click(sender As Object, e As EventArgs) Handles btnPausa.Click
            _imprimiendo = False
            btnPausa.Enabled = False
            btnDetener.Enabled = False
            btnImprimir.Enabled = True
            lblEstado.Text = "En pausa"
        End Sub

        Private Sub BtnDetener_Click(sender As Object, e As EventArgs) Handles btnDetener.Click
            Terminar()
        End Sub

        ''' <summary>CmdStop_Click, y guarda el PDF con lo que se alcanzó a imprimir.</summary>
        Private Sub Terminar()
            _imprimiendo = False
            If _textosPdf.Count > 0 Then
                Try
                    ImpresionMatricial.GuardarPdf(_textosPdf, _rutaPdf)
                Catch ex As InvalidOperationException
                    Mensajes.NoGuardo(ex.Message, "Guardar PDF", Modulo.TituloModulo)
                End Try
                _textosPdf.Clear()
            End If
            Bloquear(False)
            btnPausa.Enabled = False
            btnDetener.Enabled = False
            txtDesde.Text = ""
            txtHasta.Text = ""
            txtDetalle.Text = ""
            barra.Value = 0
            lblEstado.Text = "Esperando..."
            _ultimo = -1
            If txtDesde.CanFocus Then txtDesde.Focus()
        End Sub

        Private Sub BtnSalir_Click(sender As Object, e As EventArgs) Handles btnSalir.Click
            Close()
        End Sub

        Private Sub FrmPrintChequeM_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
            If _imprimiendo Then
                e.Cancel = True
                Aviso("Detenga la impresión antes de salir.")
            End If
        End Sub

    End Class

End Namespace
