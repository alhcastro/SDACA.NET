Imports System.Globalization
Imports MySqlConnector
Imports CoopeNet.Core
Imports CoopeNet.Core.Bancos
Imports CoopeNet.Core.Cheques
Imports CoopeNet.Ui

Namespace SD1401

    ''' <summary>
    ''' Opción 004 (Cuentas Bancarias), privilegio 14002. Migración de Fuentes\SD1401\FrmCuentasBancarias.frm.
    ''' Difiere del original en tres casos que perdían datos: CCBSearchInCombo buscaba la moneda en la
    ''' lista de bancos y al guardar podía cambiarla; una cuenta contable ausente del catálogo se
    ''' limpiaba al cargar y se borraba al guardar; una cuenta nueva con el número de otro departamento
    ''' lo sobrescribía, porque la llave es banco y cuenta.
    ''' </summary>
    Partial Public Class FrmCuentasBancarias
        Inherits Form

        Private Const SinMoneda As Long = 0

        Private ReadOnly _repositorio As CuentasBancariasRepositorio
        Private ReadOnly _cheques As ChequesRepositorio
        Private ReadOnly _uid As Long
        Private ReadOnly _localSesion As Long
        Private ReadOnly _cedula As String
        Private _formato As FormatoCuenta = FormatoCuenta.Predeterminado
        Private _cuenta As CuentaBancaria
        Private _contable As String = ""
        Private _conversion As String = ""
        Private _editando As Boolean
        Private _nueva As Boolean

        Public Sub New(repositorio As CuentasBancariasRepositorio, cheques As ChequesRepositorio, uid As Long, localSesion As Long, cedula As String)
            _repositorio = repositorio
            _cheques = cheques
            _uid = uid
            _localSesion = localSesion
            _cedula = cedula
            InitializeComponent()
            barra.AlHacerClic = AddressOf Barra_Clic
            barra.Estado(BotonBarra.Borrar, False)
            cmbTipoCuenta.SelectedIndex = 0
            Limpiar()
            Estado(False)
        End Sub

        Private ReadOnly Property Local As Long
            Get
                Return Codigo(cmbLocal)
            End Get
        End Property

        Private Shared Function Codigo(lista As ComboBox) As Long
            Dim opcion = TryCast(lista.SelectedItem, OpcionCodigo)
            Return If(opcion Is Nothing, 0, opcion.Codigo)
        End Function

        Private Shared Sub Seleccionar(lista As ComboBox, valor As Long)
            lista.SelectedIndex = -1
            For i = 0 To lista.Items.Count - 1
                If CType(lista.Items(i), OpcionCodigo).Codigo = valor Then
                    lista.SelectedIndex = i
                    Return
                End If
            Next
        End Sub

        Private Sub Estado(editando As Boolean)
            _editando = editando
            cmbLocal.Enabled = Not editando
            cmbBancos.Enabled = _nueva OrElse Not editando
            txtCodigo.Enabled = _nueva OrElse Not editando
            For Each control In New Control() {cmbMonedas, txtDescripcion, cmbTipoCuenta, txtCuentaBanco, txtNumCliente, txtMontoInicial, txtChequesTransito,
                                            txtDepositosTransito, chkCheques, txtNumCheque, chkConta, txtContable, btnBuscarContable}
                control.Enabled = editando
            Next
            btnDepartamentos.Enabled = editando AndAlso Not _nueva
            btnUsuarios.Enabled = editando AndAlso Not _nueva
            ActualizarConversion()
        End Sub

        ''' <summary>La conversión solo aplica a cuentas de cheques en otra moneda.</summary>
        Private Sub ActualizarConversion()
            Dim activa = _editando AndAlso chkCheques.Checked AndAlso Codigo(cmbMonedas) <> SinMoneda
            txtConversion.Enabled = activa
            btnBuscarConversion.Enabled = activa
        End Sub

        Private Sub Limpiar()
            txtCodigo.Clear()
            txtDescripcion.Clear()
            txtCuentaBanco.Clear()
            txtNumCliente.Clear()
            txtMontoInicial.Text = "0.00"
            txtChequesTransito.Text = "0.00"
            txtDepositosTransito.Text = "0.00"
            chkCheques.Checked = False
            chkConta.Checked = False
            txtNumCheque.Clear()
            txtContable.Clear()
            txtDetalleContable.Clear()
            txtConversion.Clear()
            txtDetalleConversion.Clear()
            _contable = ""
            _conversion = ""
            If cmbBancos.Items.Count > 0 Then cmbBancos.SelectedIndex = 0
            If cmbMonedas.Items.Count > 0 Then cmbMonedas.SelectedIndex = 0
            cmbTipoCuenta.SelectedIndex = 0
            tabs.SelectedIndex = 0
        End Sub

        Private Async Sub FrmCuentasBancarias_Load(sender As Object, e As EventArgs) Handles MyBase.Load
            Try
                cmbLocal.Items.AddRange((Await _repositorio.ListarLocalesAsync()).ToArray())
                cmbBancos.Items.AddRange((Await _repositorio.ListarBancosAsync()).ToArray())
                cmbMonedas.Items.AddRange((Await _repositorio.ListarMonedasAsync()).ToArray())
                _formato = Await _cheques.ObtenerFormatoCuentaAsync(_cedula)
                If cmbLocal.Items.Count > 0 Then cmbLocal.SelectedIndex = 0
                If cmbBancos.Items.Count > 0 Then cmbBancos.SelectedIndex = 0
                If cmbMonedas.Items.Count > 0 Then cmbMonedas.SelectedIndex = 0
                btnDepartamentos.Visible = cmbLocal.Items.Count > 1
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        Private Async Function Cargar(banco As Long, cuenta As String) As Task(Of Boolean)
            Try
                Dim c = Await _repositorio.ObtenerAsync(Local, banco, cuenta)
                If c Is Nothing Then Return False
                _cuenta = c
                _nueva = False
                Seleccionar(cmbBancos, c.Banco)
                Seleccionar(cmbMonedas, c.Moneda)
                txtCodigo.Text = c.Cuenta
                txtDescripcion.Text = c.Descripcion
                txtCuentaBanco.Text = c.CuentaBanco
                txtNumCliente.Text = c.NumeroCliente
                txtMontoInicial.Text = c.MontoInicial.ToString("#,##0.00")
                txtChequesTransito.Text = c.ChequesTransito.ToString("#,##0.00")
                txtDepositosTransito.Text = c.DepositosTransito.ToString("#,##0.00")
                cmbTipoCuenta.SelectedIndex = If(c.Tipo = "1", 1, If(c.Tipo = "2", 2, 0))
                chkCheques.Checked = c.Cheques = "1"
                txtNumCheque.Text = c.NumCheque.ToString()
                chkConta.Checked = c.Conta = "1"
                _contable = c.Contable
                _conversion = c.Conversion
                txtContable.Text = _formato.Mostrar(c.Contable)
                txtConversion.Text = _formato.Mostrar(c.Conversion)
                txtDetalleContable.Text = Await Detalle(txtContable.Text, c.DetalleContable)
                txtDetalleConversion.Text = Await Detalle(txtConversion.Text, c.DetalleConversion)
                Return True
            Catch ex As MySqlException
                Mensajes.NoConecta()
                Return False
            End Try
        End Function

        ''' <summary>Si la cuenta ya no está en el catálogo se muestra la descripción guardada.</summary>
        Private Async Function Detalle(cuenta As String, guardado As String) As Task(Of String)
            If cuenta.Length = 0 Then Return guardado
            Dim fila = Await _repositorio.ObtenerCuentaCatalogoAsync(cuenta, _formato, _cedula)
            Return If(fila Is Nothing, guardado, fila.Descripcion)
        End Function

        Private Async Sub TxtCodigo_Leave(sender As Object, e As EventArgs) Handles txtCodigo.Leave
            txtCodigo.Text = txtCodigo.Text.Trim()
            If txtCodigo.TextLength > 0 Then Await Cargar(Codigo(cmbBancos), txtCodigo.Text)
        End Sub

        Private Sub CmbLocal_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbLocal.SelectedIndexChanged
            If _editando Then Return
            _cuenta = Nothing
            Limpiar()
        End Sub

        Private Sub ChkCheques_CheckedChanged(sender As Object, e As EventArgs) Handles chkCheques.CheckedChanged
            ActualizarConversion()
        End Sub

        Private Function Barra_Clic(boton As BotonBarra) As Task(Of Boolean)
            Select Case boton
                Case BotonBarra.Nuevo
                    _nueva = True
                    Limpiar()
                    Estado(True)
                    cmbBancos.Focus()
                Case BotonBarra.Editar
                    If txtCodigo.TextLength = 0 Then Return Task.FromResult(True)
                    _nueva = False
                    Estado(True)
                    txtDescripcion.Focus()
                Case BotonBarra.Guardar
                    Return Guardar()
                Case BotonBarra.Cancelar
                    Return Cancelar()
                Case BotonBarra.Buscar
                    Return Buscar()
                Case BotonBarra.Salir
                    Close()
            End Select
            Return Task.FromResult(False)
        End Function

        Private Async Function Cancelar() As Task(Of Boolean)
            _nueva = False
            Estado(False)
            Dim anterior = _cuenta
            If anterior Is Nothing Then
                Limpiar()
            Else
                Await Cargar(anterior.Banco, anterior.Cuenta)
            End If
            tabs.SelectedIndex = 0
            Return False
        End Function

        Private Async Function Buscar() As Task(Of Boolean)
            Try
                Using datos = Await _repositorio.BuscarAsync(Local)
                    Using buscador As New FrmBuscador("Búsqueda de cuentas", datos)
                        If buscador.ShowDialog(Me) = DialogResult.OK AndAlso buscador.Resultado(0).Length > 0 Then
                            Await Cargar(Vb6.Val(buscador.Resultado(3)), buscador.Resultado(0))
                        End If
                    End Using
                End Using
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
            Return False
        End Function

        Private Shared Function Numero(caja As TextBox, ByRef valor As Double) As Boolean
            If caja.Text.Trim().Length = 0 Then
                valor = 0
                Return True
            End If
            Return Double.TryParse(caja.Text, NumberStyles.Number, CultureInfo.CurrentCulture, valor)
        End Function

        Private Sub MontoCambio(sender As Object, e As EventArgs) Handles txtMontoInicial.TextChanged, txtChequesTransito.TextChanged, txtDepositosTransito.TextChanged
            Dim monto, cheques, depositos As Double
            If Numero(txtMontoInicial, monto) AndAlso Numero(txtChequesTransito, cheques) AndAlso Numero(txtDepositosTransito, depositos) Then
                txtSaldo.Text = CuentaBancaria.SaldoInicial(monto, cheques, depositos).ToString("#,##0.00")
            End If
        End Sub

        ''' <summary>Devuelve True para que la barra no cambie de estado.</summary>
        Private Async Function Guardar() As Task(Of Boolean)
            Dim monto, cheques, depositos As Double
            If txtCodigo.Text.Trim().Length = 0 OrElse txtDescripcion.Text.Trim().Length = 0 OrElse cmbBancos.SelectedIndex < 0 Then
                Aviso("Faltan Datos...")
                Return True
            End If
            If Not (Numero(txtMontoInicial, monto) AndAlso Numero(txtChequesTransito, cheques) AndAlso Numero(txtDepositosTransito, depositos)) Then
                Aviso("Los montos no son válidos.")
                Return True
            End If
            Try
                If Not Await ResolverContables() Then Return True
                Dim banco = Codigo(cmbBancos)
                Dim cuenta = txtCodigo.Text.Trim()
                Dim duena = Await _repositorio.LocalDeCuentaAsync(banco, cuenta)
                If duena.HasValue AndAlso duena.Value <> Local Then
                    Aviso("Esa cuenta ya existe en otro departamento.")
                    Return True
                End If
                Dim c As New CuentaBancaria With {
                    .Local = Local, .Banco = banco, .Cuenta = cuenta, .Descripcion = txtDescripcion.Text, .Moneda = Codigo(cmbMonedas),
                    .MontoInicial = monto, .ChequesTransito = cheques, .DepositosTransito = depositos, .Tipo = cmbTipoCuenta.SelectedIndex.ToString(),
                    .Cheques = If(chkCheques.Checked, "1", "0"), .NumCheque = Vb6.Val(txtNumCheque.Text), .Conta = If(chkConta.Checked, "1", "0"),
                    .Contable = _contable, .DetalleContable = txtDetalleContable.Text, .Conversion = _conversion, .DetalleConversion = txtDetalleConversion.Text,
                    .CuentaBanco = txtCuentaBanco.Text, .NumeroCliente = txtNumCliente.Text
                }
                Await _repositorio.GuardarAsync(c, _uid, _localSesion)
                _cuenta = c
                _nueva = False
                Estado(False)
                tabs.SelectedIndex = 0
                Return False
            Catch ex As MySqlException
                Mensajes.NoGuardo(ex.Message, "GUARDANDO CUENTA BANCARIA", Modulo.TituloModulo)
                Return True
            End Try
        End Function

        ''' <summary>La cuenta escrita pudo cambiar sin salir de la casilla.</summary>
        Private Async Function ResolverContables() As Task(Of Boolean)
            If txtContable.Text <> _formato.Mostrar(_contable) AndAlso Not Await Validar(txtContable, txtDetalleContable, Sub(v) _contable = v) Then Return False
            If txtConversion.Text <> _formato.Mostrar(_conversion) AndAlso Not Await Validar(txtConversion, txtDetalleConversion, Sub(v) _conversion = v) Then Return False
            Return True
        End Function

        Private Async Function Validar(caja As TextBox, detalle As TextBox, asignar As Action(Of String)) As Task(Of Boolean)
            If caja.Text.Trim().Length = 0 Then
                asignar("")
                detalle.Clear()
                Return True
            End If
            Dim fila = Await _repositorio.ObtenerCuentaCatalogoAsync(caja.Text.Trim(), _formato, _cedula)
            If fila Is Nothing Then
                Aviso("La cuenta contable no existe.")
            ElseIf fila.Auxiliar <> "1" Then
                Aviso("La cuenta es un padre...")
            Else
                asignar(fila.Codigo)
                detalle.Text = fila.Descripcion
                Return True
            End If
            caja.Clear()
            detalle.Clear()
            asignar("")
            Return False
        End Function

        Private Async Sub TxtContable_Leave(sender As Object, e As EventArgs) Handles txtContable.Leave
            If Not _editando Then Return
            Try
                Await Validar(txtContable, txtDetalleContable, Sub(v) _contable = v)
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        Private Async Sub TxtConversion_Leave(sender As Object, e As EventArgs) Handles txtConversion.Leave
            If Not _editando Then Return
            Try
                Await Validar(txtConversion, txtDetalleConversion, Sub(v) _conversion = v)
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        Private Sub BtnBuscarContable_Click(sender As Object, e As EventArgs) Handles btnBuscarContable.Click
            BuscarCatalogo(txtContable, txtDetalleContable, Sub(v) _contable = v)
        End Sub

        Private Sub BtnBuscarConversion_Click(sender As Object, e As EventArgs) Handles btnBuscarConversion.Click
            BuscarCatalogo(txtConversion, txtDetalleConversion, Sub(v) _conversion = v)
        End Sub

        Private Sub TxtContable_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtContable.KeyPress
            If e.KeyChar = ChrW(Keys.Enter) AndAlso txtContable.TextLength = 0 Then BuscarCatalogo(txtContable, txtDetalleContable, Sub(v) _contable = v)
        End Sub

        Private Sub TxtConversion_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtConversion.KeyPress
            If e.KeyChar = ChrW(Keys.Enter) AndAlso txtConversion.TextLength = 0 Then BuscarCatalogo(txtConversion, txtDetalleConversion, Sub(v) _conversion = v)
        End Sub

        Private Async Sub BuscarCatalogo(caja As TextBox, detalle As TextBox, asignar As Action(Of String))
            Try
                Using datos = Await _repositorio.BuscarCatalogoAsync(_formato, _cedula)
                    Using buscador As New FrmBuscador("Catálogo contable", datos)
                        If buscador.ShowDialog(Me) = DialogResult.OK AndAlso buscador.Resultado(0).Length > 0 Then
                            detalle.Text = buscador.Resultado(0)
                            caja.Text = buscador.Resultado(1)
                            asignar(buscador.Resultado(2))
                        End If
                    End Using
                End Using
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        Private Sub BtnDepartamentos_Click(sender As Object, e As EventArgs) Handles btnDepartamentos.Click
            Dim conversion = chkCheques.Checked AndAlso Codigo(cmbMonedas) <> SinMoneda
            Using ventana As New FrmCuentaDepartamentos(_repositorio, _formato, _cedula, Local, Codigo(cmbBancos), cmbBancos.Text, txtCodigo.Text, txtDescripcion.Text, conversion, _uid, _localSesion)
                Tema.Aplicar(ventana, Modulo.TituloModulo)
                ventana.ShowDialog(Me)
            End Using
        End Sub

        Private Sub BtnUsuarios_Click(sender As Object, e As EventArgs) Handles btnUsuarios.Click
            Using ventana As New FrmCuentaUsuarios(_repositorio, Codigo(cmbBancos), cmbBancos.Text, txtCodigo.Text, txtDescripcion.Text, _localSesion)
                Tema.Aplicar(ventana, Modulo.TituloModulo)
                ventana.ShowDialog(Me)
            End Using
        End Sub

        Private Sub Aviso(texto As String)
            MessageBox.Show(Me, texto, Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
        End Sub

    End Class

End Namespace
