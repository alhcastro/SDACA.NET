Imports MySqlConnector
Imports SDACA.Core.Cheques
Imports SDACA.Ui

Namespace SD1501

    ''' <summary>
    ''' Opción 009 (Editar Asiento del Cheque), privilegio 15008. Migración de Fuentes\SD1501\FrmEditarCheque.frm.
    ''' Si el asiento no coincide con el cheque avisa y deja guardar, igual que el original. Tres
    ''' diferencias, para no perder el asiento: el tipo solo cambia si lo cambia el usuario (allá, al
    ''' cargar, el evento del combo reemplazaba las líneas); si el tipo no está en la lista se conserva
    ''' el del cheque (allá el INSERT fallaba en silencio después del DELETE); y se guarda el cheque
    ''' cargado, no el número escrito.
    ''' </summary>
    Partial Public Class FrmEditarCheque
        Inherits Form

        Private Const TituloCheque As String = "Información del Cheque # "

        Private ReadOnly _emision As EmisionCheques
        Private ReadOnly _repositorio As ChequesRepositorio
        Private ReadOnly _local As Long
        Private ReadOnly _uid As Long
        Private ReadOnly _todasLasCuentas As Boolean
        Private ReadOnly _cedula As String
        Private ReadOnly _cedulaConGuiones As String
        Private ReadOnly _lineas As New List(Of LineaCaptura)
        Private _formato As FormatoCuenta = FormatoCuenta.Predeterminado
        Private _cheque As ChequeConsulta
        Private _banco As Integer
        Private _cuenta As String = ""
        Private _tipoOriginal As Integer
        Private _contable As String = ""
        Private _mostrando As Boolean

        Public Sub New(emision As EmisionCheques, repositorio As ChequesRepositorio, local As Long, uid As Long,
                       todasLasCuentas As Boolean, cedula As String, cedulaConGuiones As String)
            _emision = emision
            _repositorio = repositorio
            _local = local
            _uid = uid
            _todasLasCuentas = todasLasCuentas
            _cedula = cedula
            _cedulaConGuiones = cedulaConGuiones
            InitializeComponent()
            cmbDebeHaber.SelectedIndex = 0
            Limpiar()
        End Sub

        Private ReadOnly Property Banco As BancoCheque
            Get
                Return TryCast(cmbBancos.SelectedItem, BancoCheque)
            End Get
        End Property

        Private ReadOnly Property Cuenta As CuentaEmision
            Get
                Return TryCast(cmbCuentas.SelectedItem, CuentaEmision)
            End Get
        End Property

        Private ReadOnly Property LocalesContables As String()
            Get
                Return EmisionCheques.LocalesContables(If(Cuenta?.LocalCuenta, _local.ToString()), _cedulaConGuiones)
            End Get
        End Property

        Private Sub Aviso(texto As String)
            MessageBox.Show(Me, texto, Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
        End Sub

        Private Shared Function Numero(texto As String) As Double
            Dim valor As Double
            Double.TryParse(texto, valor)
            Return valor
        End Function

        Private Async Sub FrmEditarCheque_Load(sender As Object, e As EventArgs) Handles MyBase.Load
            Try
                _formato = Await _repositorio.ObtenerFormatoCuentaAsync(_cedula)
                cmbTipo.DisplayMember = NameOf(TipoAsiento.Descripcion)
                cmbTipo.Items.AddRange((Await _emision.ListarTiposAsientoAsync(_local)).ToArray())
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
                Dim cuentas = Await _emision.ListarCuentasAsync(_local, elegido.Codigo, _uid, _todasLasCuentas)
                If Banco IsNot elegido Then Return
                cmbCuentas.DisplayMember = NameOf(CuentaEmision.Etiqueta)
                cmbCuentas.DataSource = cuentas
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        Private Sub CmbCuentas_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbCuentas.SelectedIndexChanged
            txtNumCheque.Text = ""
            Limpiar()
            If Cuenta Is Nothing Then Return
            If Cuenta.Moneda <> 0 Then Aviso("Las cuentas en otra moneda todavía no están migradas. Use el sistema anterior.")
            txtNumCheque.Enabled = Cuenta.Moneda = 0
            btnBuscar.Enabled = Cuenta.Moneda = 0
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
            Dim num As Integer
            Dim cheque As ChequeConsulta = Nothing
            Try
                If Integer.TryParse(txtNumCheque.Text.Trim(), num) Then cheque = Await _repositorio.ObtenerAsync(Banco.Codigo, Cuenta.Cuenta, num)
            Catch ex As MySqlException
                Mensajes.NoConecta()
                Return
            End Try
            If cheque Is Nothing OrElse cheque.EstaAnulado Then
                Aviso("El Cheque no Existe...")
                txtNumCheque.Text = ""
                Limpiar()
                txtNumCheque.Focus()
                Return
            End If
            Mostrar(cheque)
        End Function

        Private Sub Limpiar()
            _cheque = Nothing
            _lineas.Clear()
            _contable = ""
            gbCheque.Text = TituloCheque
            txtFecha.Text = ""
            txtNombre.Text = ""
            txtMonto.Text = "0.00"
            lblLetras.Text = ""
            txtCodigoSeguridad.Text = ""
            txtUsuario.Text = ""
            txtDetalle.Text = ""
            txtDetalle.Enabled = False
            lblConciliado.Visible = False
            gbLinea.Enabled = False
            cmbTipo.Enabled = False
            btnGuardar.Enabled = False
            _mostrando = True
            cmbTipo.SelectedIndex = -1
            _mostrando = False
            rejilla.Rows.Clear()
            txtDebe.Text = "0.00"
            txtHaber.Text = "0.00"
            lblFaltante.Text = "Diferencia entre el debe y el haber:   "
            tabs.SelectedIndex = 0
        End Sub

        Private Sub Mostrar(cheque As ChequeConsulta)
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
            txtDetalle.Text = cheque.Observaciones
            lblConciliado.Visible = cheque.Conciliado
            _lineas.Clear()
            For Each l In cheque.Lineas
                _lineas.Add(New LineaCaptura With {.Sistema = l.Sistema, .Cuenta = l.Cuenta, .Detalle = l.Detalle, .Debe = l.Debe, .Haber = l.Haber})
            Next
            _tipoOriginal = If(cheque.Lineas.Count = 0, 0, cheque.Lineas(cheque.Lineas.Count - 1).TipoAc)
            _mostrando = True
            cmbTipo.SelectedItem = cmbTipo.Items.Cast(Of TipoAsiento)().FirstOrDefault(Function(t) t.Tipo = _tipoOriginal)
            _mostrando = False
            MostrarLineas()
            txtDebe.Text = _lineas.Sum(Function(l) l.Debe).ToString("N2")
            txtHaber.Text = _lineas.Sum(Function(l) l.Haber).ToString("N2")
            txtDetalle.Enabled = True
            gbLinea.Enabled = True
            cmbTipo.Enabled = True
            btnGuardar.Enabled = True
        End Sub

        Private Sub MostrarLineas()
            _mostrando = True
            rejilla.Rows.Clear()
            For Each l In _lineas.OrderBy(Function(x) x.Sistema)
                Dim i = rejilla.Rows.Add(False, _formato.Mostrar(l.Cuenta), l.Detalle, l.Debe, l.Haber)
                rejilla.Rows(i).Tag = l
                If l.Sistema = LineaCaptura.SistemaBanco Then rejilla.Rows(i).ReadOnly = True
            Next
            _mostrando = False
        End Sub

        ''' <summary>ECCMontoCheque: el banco toma la diferencia y se avisa si ya no coincide con el cheque.</summary>
        Private Async Function Recalcular() As Task
            If Not _lineas.Any(Function(l) l.Sistema = LineaCaptura.SistemaBanco) AndAlso Cuenta IsNot Nothing Then
                Try
                    _lineas.Add(Await _emision.LineaBancoAsync(Cuenta, _cedula))
                Catch ex As MySqlException
                    Mensajes.NoConecta()
                End Try
            End If
            Dim totales = AsientoCaptura.Recalcular(_lineas)
            MostrarLineas()
            txtDebe.Text = totales.Debe.ToString("N2")
            txtHaber.Text = totales.Haber.ToString("N2")
            lblFaltante.Text = "Diferencia entre el debe y el haber:   " & (totales.Haber - totales.Debe).ToString("N2")
            If _cheque IsNot Nothing AndAlso Math.Round(totales.Diferencia, 2) <> Math.Round(_cheque.Monto, 2) Then
                Aviso($"El monto del asiento contable es diferente al del cheque. ({txtMonto.Text}){vbCrLf}Verifique por favor. Gracias!")
            End If
        End Function

        Private Async Sub CmbTipo_SelectionChangeCommitted(sender As Object, e As EventArgs) Handles cmbTipo.SelectionChangeCommitted
            Dim tipo = TryCast(cmbTipo.SelectedItem, TipoAsiento)
            If _mostrando OrElse tipo Is Nothing OrElse Cuenta Is Nothing Then Return
            Try
                Dim lineas = Await _emision.CargarAsientoAsync(_local, tipo.Tipo, Cuenta, _cedula)
                ' Un tipo sin cuentas fijas deja el asiento como está.
                If lineas.Count <= 1 Then Return
                _lineas.Clear()
                _lineas.AddRange(lineas)
                Await Recalcular()
            Catch ex As MySqlException
                Mensajes.NoConecta()
            Catch ex As NotSupportedException
                Aviso(ex.Message)
            End Try
        End Sub

        Private Async Sub Rejilla_CellEndEdit(sender As Object, e As DataGridViewCellEventArgs) Handles rejilla.CellEndEdit
            If _mostrando OrElse e.ColumnIndex < 3 Then Return
            Dim l = CType(rejilla.Rows(e.RowIndex).Tag, LineaCaptura)
            Dim valor = Numero(Convert.ToString(rejilla.Rows(e.RowIndex).Cells(e.ColumnIndex).Value))
            If e.ColumnIndex = 3 Then l.Debe = valor Else l.Haber = valor
            Await Task.Yield()
            Await Recalcular()
        End Sub

        Private Sub BtnBuscarCuenta_Click(sender As Object, e As EventArgs) Handles btnBuscarCuenta.Click
            BuscarCuenta()
        End Sub

        Private Async Sub BuscarCuenta()
            Try
                Using datos = Await _emision.BuscarCatalogoAsync(LocalesContables, _formato, _cedula)
                    Using buscador As New FrmBuscador("Catálogo contable", datos)
                        If buscador.ShowDialog(Me) = DialogResult.OK AndAlso buscador.Resultado(0).Length > 0 Then
                            txtDescripcion.Text = buscador.Resultado(0)
                            txtCuenta.Text = buscador.Resultado(1)
                            _contable = buscador.Resultado(2)
                        End If
                    End Using
                End Using
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        Private Async Sub TxtCuenta_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtCuenta.KeyPress
            If e.KeyChar <> ChrW(Keys.Return) Then Return
            e.Handled = True
            If txtCuenta.Text.Trim().Length = 0 Then
                BuscarCuenta()
            Else
                Await CargarCuenta()
                txtMontoLinea.Focus()
            End If
        End Sub

        Private Async Function CargarCuenta() As Task
            Dim hallada As CuentaCatalogo
            Try
                hallada = Await _emision.ObtenerCuentaCatalogoAsync(txtCuenta.Text.Trim(), LocalesContables, _formato, _cedula)
            Catch ex As MySqlException
                Mensajes.NoConecta()
                Return
            End Try
            Dim mensaje = ""
            If hallada IsNot Nothing Then
                If hallada.Auxiliar <> "1" Then
                    mensaje = "La cuenta es un padre..."
                ElseIf hallada.EnOtroLocal Then
                    mensaje = "La cuenta pertenece a otro departamento..."
                Else
                    txtDescripcion.Text = hallada.Descripcion
                    _contable = hallada.Codigo
                    Return
                End If
            End If
            txtCuenta.Text = ""
            txtDescripcion.Text = ""
            _contable = ""
            If mensaje.Length > 0 Then Aviso(mensaje)
        End Function

        Private Async Sub BtnIncluir_Click(sender As Object, e As EventArgs) Handles btnIncluir.Click
            If txtCuenta.Text.Trim().Length = 0 Then
                Aviso("Nesecita especificar una cuenta.")
                txtCuenta.Focus()
                Return
            End If
            If _contable.Length = 0 Then Await CargarCuenta()
            If _contable.Length = 0 Then Return
            Dim monto = Numero(txtMontoLinea.Text)
            If monto = 0 Then
                Aviso("El Monto es menor o igual a Cero.")
                txtMontoLinea.Focus()
                Return
            End If
            Dim alDebe = cmbDebeHaber.SelectedIndex = 0
            _lineas.Add(New LineaCaptura With {.Cuenta = _contable, .Detalle = txtDescripcion.Text, .Debe = If(alDebe, monto, 0), .Haber = If(alDebe, 0, monto)})
            txtCuenta.Text = ""
            txtDescripcion.Text = ""
            txtMontoLinea.Text = ""
            _contable = ""
            Await Recalcular()
            txtCuenta.Focus()
        End Sub

        Private Async Sub BtnRemover_Click(sender As Object, e As EventArgs) Handles btnRemover.Click
            rejilla.EndEdit()
            For Each fila In rejilla.Rows.Cast(Of DataGridViewRow)()
                Dim l = CType(fila.Tag, LineaCaptura)
                If CBool(fila.Cells(0).Value) AndAlso l.Sistema < 50 Then _lineas.Remove(l)
            Next
            Await Recalcular()
        End Sub

        Private Sub BtnMarcar_Click(sender As Object, e As EventArgs) Handles btnMarcarTodo.Click, btnMarcarNada.Click
            For Each fila In rejilla.Rows.Cast(Of DataGridViewRow)()
                If CType(fila.Tag, LineaCaptura).Sistema = 0 Then fila.Cells(0).Value = sender Is btnMarcarTodo
            Next
        End Sub

        Private Async Sub BtnGuardar_Click(sender As Object, e As EventArgs) Handles btnGuardar.Click
            If _cheque Is Nothing Then Return
            Dim tipo = If(TryCast(cmbTipo.SelectedItem, TipoAsiento)?.Tipo, _tipoOriginal)
            Dim lineas = _lineas.OrderBy(Function(l) l.Sistema).ToList()
            btnGuardar.Enabled = False
            Try
                Await _emision.GuardarAsientoAsync(_banco, _cuenta, _cheque.Numero, _cheque.Local, txtDetalle.Text, tipo, lineas, _uid)
                Aviso("Cheque Guardado Correctamente.")
                txtNumCheque.Text = ""
                Limpiar()
                txtNumCheque.Focus()
            Catch ex As MySqlException
                ' Las líneas siguen en pantalla para volver a guardar: el asiento pudo quedar incompleto.
                Mensajes.NoGuardo($"El asiento pudo quedar incompleto. Vuelva a guardar. {ex.Message}", "Guardar asiento", Modulo.TituloModulo)
                btnGuardar.Enabled = True
            End Try
        End Sub

        Private Sub BtnSalir_Click(sender As Object, e As EventArgs) Handles btnSalir.Click
            Close()
        End Sub

    End Class

End Namespace
