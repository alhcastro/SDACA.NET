Imports System.Drawing.Printing
Imports System.Globalization
Imports MySqlConnector
Imports CoopeNet.Core
Imports CoopeNet.Core.Bancos
Imports CoopeNet.Ui

Namespace SD1401

    ''' <summary>
    ''' Opción Movimientos Bancarios, privilegio 14003. Migración de Fuentes\SD1401\FrmLibro.frm. Borrar pide
    ''' 14004. No se migran el Registro Contable (ver <see cref="MovimientosLibro"/>), los conceptos, que no
    ''' tienen tablas. Saldo Actual muestra el saldo real: allá arranca en cero
    ''' porque la llamada a CCBLoadMovimientos está comentada.
    ''' </summary>
    Partial Public Class FrmLibro
        Inherits Form

        Private Const MesesDeBusqueda As Integer = 3
        Private Const ClaveDuplicada As Integer = 1062

        Private ReadOnly _movimientos As MovimientosLibro
        Private ReadOnly _saldos As SaldosCuenta
        Private ReadOnly _cierre As CierreMes
        Private ReadOnly _tipos As TiposMovimientoRepositorio
        Private ReadOnly _uid As Long
        Private ReadOnly _local As Long
        Private ReadOnly _todasLasCuentas As Boolean
        Private ReadOnly _cedula As String
        Private ReadOnly _empresa As String
        Private _actual As MovimientoBanco
        Private _consultaSaldo As Integer

        Public Sub New(movimientos As MovimientosLibro, saldos As SaldosCuenta, cierre As CierreMes, tipos As TiposMovimientoRepositorio,
                       uid As Long, local As Long, puedeBorrar As Boolean, todasLasCuentas As Boolean, cedula As String, empresa As String)
            _movimientos = movimientos
            _saldos = saldos
            _cierre = cierre
            _tipos = tipos
            _uid = uid
            _local = local
            _todasLasCuentas = todasLasCuentas
            _cedula = cedula
            _empresa = empresa
            InitializeComponent()
            dtpFecha.Value = Date.Today
            dtpBuscar.Value = Date.Today.AddMonths(-MesesDeBusqueda)
            barra.AlHacerClic = AddressOf Barra_Clic
            barra.Estado(BotonBarra.Borrar, puedeBorrar)
            Limpiar()
            Editable(False)
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

        Private ReadOnly Property Tipo As TipoMovimiento
            Get
                Return TryCast(cmbTipo.SelectedItem, TipoMovimiento)
            End Get
        End Property

        Private Async Sub FrmLibro_Load(sender As Object, e As EventArgs) Handles MyBase.Load
            Try
                Dim tipos = Await _tipos.ListarAsync()
                cmbTipo.Items.AddRange(tipos.OrderBy(Function(t) Vb6.Val(t.Codigo)).ToArray())
                If cmbTipo.Items.Count > 0 Then cmbTipo.SelectedIndex = 0
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
            txtSaldo.Clear()
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
            Limpiar()
            txtMoneda.Text = If(Cuenta Is Nothing, "", Cuenta.Moneda)
            Await RefrescarSaldo()
        End Sub

        Private Async Function RefrescarSaldo() As Task
            _consultaSaldo += 1
            Dim consulta = _consultaSaldo
            txtSaldo.Clear()
            If Banco Is Nothing OrElse Cuenta Is Nothing Then Return
            Try
                Dim saldo = Await _saldos.SaldoActualAsync(Banco.Codigo, Cuenta.Cuenta, Date.Today)
                If consulta = _consultaSaldo Then txtSaldo.Text = saldo.ToString("#,##0.00")
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Function

        Private Sub Limpiar()
            _actual = Nothing
            txtDocumento.Clear()
            txtMonto.Text = "0.00"
            txtDetalle.Clear()
            chkConciliado.Checked = False
            dtpBanco.Value = Date.Today
            txtDetalleBanco.Clear()
            If cmbTipo.Items.Count > 0 Then cmbTipo.SelectedIndex = 0
        End Sub

        Private Sub Editable(activo As Boolean)
            gbCuenta.Enabled = Not activo
            dtpBuscar.Enabled = Not activo
            gbMovimiento.Enabled = activo
            gbConciliacion.Enabled = activo
            If Not activo Then
                barra.Estado(BotonBarra.Guardar, True)
                lblInfo.Text = ""
            End If
        End Sub

        Private Sub ChkConciliado_CheckedChanged(sender As Object, e As EventArgs) Handles chkConciliado.CheckedChanged
            dtpBanco.Enabled = chkConciliado.Checked
            txtDetalleBanco.Enabled = chkConciliado.Checked
        End Sub

        Private Async Sub DtpFecha_ValueChanged(sender As Object, e As EventArgs) Handles dtpFecha.ValueChanged
            Await RevisarMes()
        End Sub

        ''' <summary>Como en VB6, en un mes cerrado no se puede guardar.</summary>
        Private Async Function RevisarMes() As Task
            If Not barra.EnEdicion OrElse Banco Is Nothing OrElse Cuenta Is Nothing Then Return
            Dim fecha = dtpFecha.Value.Date
            Try
                Dim cerrado = Await _cierre.EstaCerradoAsync(Banco.Codigo, Cuenta.Cuenta, fecha)
                If fecha <> dtpFecha.Value.Date OrElse Not barra.EnEdicion Then Return
                barra.Estado(BotonBarra.Guardar, Not cerrado)
                lblInfo.Text = If(cerrado, "El mes está cerrado.", "")
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Function

        ''' <summary>Devuelve True para que la barra no cambie de estado.</summary>
        Private Async Function Barra_Clic(boton As BotonBarra) As Task(Of Boolean)
            Select Case boton
                Case BotonBarra.Nuevo
                    If Cuenta Is Nothing Then
                        cmbCuentas.Focus()
                        Return True
                    End If
                    Limpiar()
                    Editable(True)
                    txtDocumento.Focus()
                    ' La barra entra en edición al terminar este clic: RevisarMes va después.
                    BeginInvoke(New Action(Async Sub() Await RevisarMes()))
                Case BotonBarra.Editar
                    If _actual Is Nothing Then Return True
                    Editable(True)
                    txtDocumento.Focus()
                    BeginInvoke(New Action(Async Sub() Await RevisarMes()))
                Case BotonBarra.Guardar
                    Return Await Guardar()
                Case BotonBarra.Cancelar
                    Limpiar()
                    Editable(False)
                Case BotonBarra.Buscar
                    Await Buscar()
                Case BotonBarra.Borrar
                    Await Borrar()
                Case BotonBarra.Imprimir
                    If _actual Is Nothing Then
                        Aviso("Para imprimir, seleccione un movimiento primero.")
                    Else
                        Await Imprimir(_actual)
                    End If
                Case BotonBarra.Salir
                    Close()
            End Select
            Return False
        End Function

        Private Function Aviso(texto As String) As DialogResult
            Return MessageBox.Show(Me, texto, Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
        End Function

        Private Function Pregunta(texto As String) As Boolean
            Return MessageBox.Show(Me, texto, Modulo.TituloModulo, MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes
        End Function

        Private Function LeerMonto() As Decimal
            Dim monto As Decimal
            Return If(Decimal.TryParse(txtMonto.Text, NumberStyles.Number, CultureInfo.CurrentCulture, monto), monto, 0D)
        End Function

        Private Function Armar() As MovimientoBanco
            Return New MovimientoBanco With {
                .Ids = If(_actual Is Nothing, 0, _actual.Ids), .Local = If(_actual Is Nothing, _local, _actual.Local),
                .Banco = Banco.Codigo, .Cuenta = Cuenta.Cuenta, .Fecha = dtpFecha.Value.Date, .Documento = txtDocumento.Text.Trim(),
                .TipoMov = Vb6.Val(Tipo.Codigo), .Tipo = Tipo.Efecto, .Monto = LeerMonto(), .Detalle = txtDetalle.Text.Trim(),
                .Conciliado = chkConciliado.Checked, .BancoFecha = dtpBanco.Value.Date, .BancoDetalle = txtDetalleBanco.Text.Trim()
            }
        End Function

        Private Async Function Guardar() As Task(Of Boolean)
            pestanas.SelectedIndex = 0
            If Tipo Is Nothing Then Return True
            If dtpFecha.Value.Date > Date.Today AndAlso
               Not Pregunta($"Está intentando agregar un movimiento con fecha ({dtpFecha.Value:dd/MMM/yyyy}) mayor a hoy ({Date.Today:dd/MMM/yyyy}). ¿Está seguro?") Then Return True
            If txtDocumento.Text.Trim().Length = 0 Then
                Aviso("Debe especificar el número de documento.")
                txtDocumento.Focus()
                Return True
            End If
            If txtDetalle.Text.Trim().Length = 0 OrElse LeerMonto() <= 0 Then
                Aviso("Faltan datos: el detalle es requerido y el monto debe ser mayor a cero.")
                Return True
            End If

            Dim movimiento = Armar()
            Try
                If movimiento.Ids = 0 AndAlso Await _movimientos.ExisteAsync(movimiento) AndAlso
                   Not Pregunta("El documento ya se encuentra registrado." & Environment.NewLine & "¿Desea agregarlo?" & Environment.NewLine & "Si responde sí, esto podría duplicar el documento.") Then Return True
                Dim fechaAnterior = If(_actual Is Nothing, CType(Nothing, Date?), _actual.Fecha)
                Await _movimientos.GuardarAsync(movimiento, _local, _uid, Date.Today, fechaAnterior)
            Catch ex As MySqlException When ex.Number = ClaveDuplicada
                Aviso("Número de documento duplicado.")
                txtDocumento.Focus()
                Return True
            Catch ex As MySqlException
                Mensajes.NoGuardo(ex.Message, "GUARDANDO MOVIMIENTO", Modulo.TituloModulo)
                Return True
            End Try
            If Pregunta("¿Imprimir comprobante?") Then Await Imprimir(movimiento)
            Limpiar()
            Editable(False)
            Await RefrescarSaldo()
            Return False
        End Function

        Private Async Function Imprimir(movimiento As MovimientoBanco) As Task
            Try
                Dim comprobante = Await _movimientos.ObtenerComprobanteAsync(movimiento.Local, movimiento.Ids, movimiento.Banco, movimiento.Cuenta, _cedula)
                If comprobante Is Nothing Then Return
                Using documento As New ComprobanteImpreso(comprobante, _empresa)
                    Using vista As New PrintPreviewDialog With {.Document = documento, .UseAntiAlias = True, .WindowState = FormWindowState.Maximized}
                        vista.ShowDialog(Me)
                    End Using
                End Using
            Catch ex As MySqlException
                Mensajes.NoConecta()
            Catch ex As InvalidPrinterException
                Aviso("No hay una impresora disponible para mostrar el comprobante.")
            End Try
        End Function

        Private Async Function Buscar() As Task
            If Banco Is Nothing OrElse Cuenta Is Nothing Then
                cmbCuentas.Focus()
                Return
            End If
            Try
                Using datos = Await _movimientos.BuscarAsync(Banco.Codigo, Cuenta.Cuenta, dtpBuscar.Value.Date)
                    Using buscador As New FrmBuscador($"Buscar movimiento de la cuenta {Cuenta} desde {dtpBuscar.Value:dd/MM/yyyy}", datos)
                        If buscador.ShowDialog(Me) <> DialogResult.OK OrElse buscador.Resultado(0).Length = 0 Then Return
                        Dim movimiento = Await _movimientos.ObtenerAsync(Vb6.Val(buscador.Resultado(1)), Vb6.Val(buscador.Resultado(0)), Banco.Codigo, Cuenta.Cuenta)
                        If movimiento IsNot Nothing Then Mostrar(movimiento)
                    End Using
                End Using
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Function

        Private Sub Mostrar(movimiento As MovimientoBanco)
            _actual = movimiento
            dtpFecha.Value = movimiento.Fecha
            txtDocumento.Text = movimiento.Documento
            cmbTipo.SelectedItem = cmbTipo.Items.Cast(Of TipoMovimiento)().FirstOrDefault(Function(t) Vb6.Val(t.Codigo) = movimiento.TipoMov)
            txtMonto.Text = movimiento.Monto.ToString("#,##0.00")
            txtDetalle.Text = movimiento.Detalle
            chkConciliado.Checked = movimiento.Conciliado
            dtpBanco.Value = If(movimiento.Conciliado, movimiento.BancoFecha, Date.Today)
            txtDetalleBanco.Text = movimiento.BancoDetalle
        End Sub

        Private Async Function Borrar() As Task
            If _actual Is Nothing Then
                Aviso("Para poder borrar, seleccione un movimiento primero.")
                Return
            End If
            If Not Pregunta($"¿Está seguro de que desea eliminar el movimiento?{Environment.NewLine}Documento #: {_actual.Documento}{Environment.NewLine}Monto: {_actual.Monto:#,##0.00}") Then Return
            If _actual.Conciliado Then
                Aviso("El movimiento está conciliado. Para poder borrarlo debe desconciliarlo primero.")
                Return
            End If
            Try
                Await _movimientos.BorrarAsync(_actual, _local, _uid, Date.Today)
            Catch ex As MySqlException
                Mensajes.NoGuardo(ex.Message, "BORRANDO MOVIMIENTO", Modulo.TituloModulo)
                Return
            End Try
            Limpiar()
            Await RefrescarSaldo()
        End Function

    End Class

End Namespace
