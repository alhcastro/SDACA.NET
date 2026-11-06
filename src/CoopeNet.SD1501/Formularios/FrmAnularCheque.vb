Imports MySqlConnector
Imports CoopeNet.Core.Cheques
Imports CoopeNet.Core.Datos
Imports CoopeNet.Ui

Namespace SD1501

    ''' <summary>
    ''' Opción 007 (Anular Cheques), privilegio 15007. Migración de Fuentes\SD1501\FrmAnularCheque.frm.
    ''' El original actúa sobre el número escrito en la casilla, aunque no sea el cheque que se ve.
    ''' Aquí se actúa siempre sobre el cheque cargado, y cambiar la cuenta o el número lo descarga.
    ''' </summary>
    Partial Public Class FrmAnularCheque
        Inherits Form

        ''' <summary>Para que <see cref="FrmMoveCheque"/> comparta la pantalla.</summary>
        Protected Sub Rotular(titulo As String, etiquetaNumero As String, textoBoton As String)
            Text = titulo
            lblNumero.Text = etiquetaNumero
            txtNumCheque.AccessibleName = etiquetaNumero
            btnAnular.Text = textoBoton
        End Sub

        Protected ReadOnly Property ChequeCargado As ChequeConsulta
            Get
                Return _cheque
            End Get
        End Property

        Protected ReadOnly Property BancoCargado As Integer
            Get
                Return _banco
            End Get
        End Property

        Protected ReadOnly Property CuentaCargada As String
            Get
                Return _cuenta
            End Get
        End Property

        Protected ReadOnly Property Motivo As String
            Get
                Return txtMotivo.Text
            End Get
        End Property

        ''' <summary>Avisa que no se puede usar el cheque. Lo limpia quien llama.</summary>
        Protected Overridable Sub ChequeNoDisponible(cheque As ChequeConsulta)
            Dim aviso = If(cheque Is Nothing, "El Cheque no Existe...", "El Cheque ya esta anulado...")
            MessageBox.Show(Me, aviso, Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
        End Sub

        Protected Overridable Function PermiteBorrar(cheque As ChequeConsulta, dias As Long) As Boolean
            Return AnulacionCheques.SePuedeBorrar(cheque, dias, Date.Today)
        End Function

        Protected Overridable Function AccionPrincipalAsync() As Task
            Return _anulacion.AnularAsync(_banco, _cuenta, _cheque.Numero, txtMotivo.Text, _local, _uid)
        End Function

        Private Const TituloCheque As String = "Información del Cheque # "

        Private ReadOnly _repositorio As ChequesRepositorio
        Private ReadOnly _anulacion As AnulacionCheques
        Private ReadOnly _parametros As Parametros
        Private ReadOnly _local As Long
        Private ReadOnly _uid As Long
        Private ReadOnly _todasLasCuentas As Boolean
        Private ReadOnly _cedula As String
        Private _formato As FormatoCuenta = FormatoCuenta.Predeterminado
        Private _cheque As ChequeConsulta
        Private _banco As Integer
        Private _cuenta As String = ""

        Public Sub New(repositorio As ChequesRepositorio, anulacion As AnulacionCheques, parametros As Parametros,
                       local As Long, uid As Long, todasLasCuentas As Boolean, cedula As String)
            _repositorio = repositorio
            _anulacion = anulacion
            _parametros = parametros
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

        Private Async Sub FrmAnularCheque_Load(sender As Object, e As EventArgs) Handles MyBase.Load
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
            Limpiar()
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

        Private Sub CmbCuentas_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbCuentas.SelectedIndexChanged
            Limpiar()
        End Sub

        Private Sub CmbCuentas_SelectionChangeCommitted(sender As Object, e As EventArgs) Handles cmbCuentas.SelectionChangeCommitted
            txtNumCheque.Focus()
        End Sub

        Private Sub TxtNumCheque_TextChanged(sender As Object, e As EventArgs) Handles txtNumCheque.TextChanged
            If _cheque IsNot Nothing AndAlso txtNumCheque.Text.Trim() <> _cheque.Numero.ToString() Then Limpiar()
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
                Using datos = Await _repositorio.ListarChequesAsync(Banco.Codigo, Cuenta.Cuenta, soloVigentes:=True)
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
            Dim dias As Long
            Try
                If Integer.TryParse(txtNumCheque.Text.Trim(), numero) Then
                    cheque = Await _repositorio.ObtenerAsync(Banco.Codigo, Cuenta.Cuenta, numero)
                    dias = Await _parametros.ObtenerNumeroAsync(Parametros.LocalGlobal, AnulacionCheques.ParametroDiasBorrar)
                End If
            Catch ex As MySqlException
                Mensajes.NoConecta()
                Return
            End Try
            If cheque Is Nothing OrElse cheque.EstaAnulado Then
                ChequeNoDisponible(cheque)
                txtNumCheque.Text = ""
                Limpiar()
                txtNumCheque.Focus()
                Return
            End If
            Mostrar(cheque, PermiteBorrar(cheque, dias))
        End Function

        Private Sub Limpiar()
            _cheque = Nothing
            gbCheque.Text = TituloCheque
            txtFecha.Text = ""
            txtNombre.Text = ""
            txtMonto.Text = "0.00"
            lblLetras.Text = ""
            txtCodigoSeguridad.Text = ""
            txtUsuario.Text = ""
            lblDetalle.Text = ""
            lblConciliado.Visible = False
            btnAnular.Enabled = False
            btnBorrar.Visible = False
            rejilla.Rows.Clear()
            tabs.SelectedIndex = 0
        End Sub

        Private Sub Mostrar(cheque As ChequeConsulta, sePuedeBorrar As Boolean)
            _cheque = cheque
            _banco = Banco.Codigo
            _cuenta = Cuenta.Cuenta
            gbCheque.Text = TituloCheque & cheque.Numero
            txtFecha.Text = If(cheque.Fecha = Date.MinValue, "", cheque.Fecha.ToString("dd/MM/yyyy"))
            txtNombre.Text = cheque.Nombre
            txtMonto.Text = cheque.Monto.ToString("N2")
            lblLetras.Text = MontoEnLetras.Convertir(cheque.Monto, True, True)
            txtCodigoSeguridad.Text = cheque.CodigoSeguridad
            txtUsuario.Text = cheque.Usuario
            lblDetalle.Text = cheque.Observaciones
            lblConciliado.Visible = cheque.Conciliado
            btnAnular.Enabled = Not cheque.Conciliado
            btnBorrar.Visible = sePuedeBorrar
            rejilla.Rows.Clear()
            For Each linea In cheque.Lineas
                rejilla.Rows.Add(_formato.Mostrar(linea.Cuenta), linea.Detalle, linea.Debe, linea.Haber)
            Next
        End Sub

        Private Async Sub BtnAnular_Click(sender As Object, e As EventArgs) Handles btnAnular.Click
            If _cheque Is Nothing Then Return
            Await Ejecutar(AddressOf AccionPrincipalAsync, btnAnular.Text.Replace("&", ""))
        End Sub

        Private Async Sub BtnBorrar_Click(sender As Object, e As EventArgs) Handles btnBorrar.Click
            If _cheque Is Nothing Then Return
            Dim descripcion = $"cheque # {_cheque.Numero} - {_cheque.Nombre}"
            If Pregunta("Esta completamente seguro de querer borrar por completo el " & descripcion) = DialogResult.No Then Return
            If Pregunta($"Si usted borra este {descripcion}, No podra recuperarlo.{vbCrLf}Esta seguro?") = DialogResult.No Then Return
            Await Ejecutar(Function() _anulacion.BorrarAsync(_banco, _cuenta, _cheque.Numero, txtMotivo.Text, _local, _uid), "Borrar")
        End Sub

        Private Async Function Ejecutar(accion As Func(Of Task), nombre As String) As Task
            btnAnular.Enabled = False
            btnBorrar.Enabled = False
            Try
                Await accion()
                txtMotivo.Text = ""
                txtNumCheque.Text = ""
                Limpiar()
                txtNumCheque.Focus()
            Catch ex As MySqlException
                ' Sin transacciones lo anterior al error quedó hecho: hay que revisar el cheque.
                Mensajes.NoGuardo(ex.Message, nombre, Modulo.TituloModulo)
                btnAnular.Enabled = _cheque IsNot Nothing AndAlso Not _cheque.Conciliado
            Finally
                btnBorrar.Enabled = True
            End Try
        End Function

        Private Function Pregunta(texto As String) As DialogResult
            Return MessageBox.Show(Me, texto, Modulo.TituloModulo, MessageBoxButtons.YesNo, MessageBoxIcon.Question)
        End Function

        Private Sub BtnSalir_Click(sender As Object, e As EventArgs) Handles btnSalir.Click
            Close()
        End Sub

    End Class

End Namespace
