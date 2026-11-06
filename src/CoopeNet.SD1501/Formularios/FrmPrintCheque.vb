Imports MySqlConnector
Imports CoopeNet.Core.Cheques
Imports CoopeNet.Ui

Namespace SD1501

    ''' <summary>
    ''' Opción 006 (Impresión de Cheques), privilegio 15006. Migración de Fuentes\SD1501\FrmPrintCheque.frm
    ''' abierto desde el menú. Las reglas están en <see cref="EmisionCheques"/> y <see cref="AsientoCaptura"/>.
    ''' Una cuenta en otra moneda se rechaza: esa parte no se migró porque ninguna cuenta de cheques la usa.
    ''' </summary>
    Partial Public Class FrmPrintCheque
        Inherits Form

        Private Const SalidaImpresora As Integer = 0
        Private Const SalidaPdf As Integer = 1
        Private Const SalidaNinguna As Integer = 2

        Private ReadOnly _emision As EmisionCheques
        Private ReadOnly _impresion As ImpresionCheques
        Private ReadOnly _repositorio As ChequesRepositorio
        Private ReadOnly _local As Long
        Private ReadOnly _uid As Long
        Private ReadOnly _todasLasCuentas As Boolean
        Private ReadOnly _cedula As String
        Private ReadOnly _cedulaConGuiones As String
        Private ReadOnly _estacion As String
        Private ReadOnly _lineas As New List(Of LineaCaptura)
        Private _formato As FormatoCuenta = FormatoCuenta.Predeterminado
        Private _contable As String = ""
        Private _cargando As Boolean

        Public Sub New(emision As EmisionCheques, impresion As ImpresionCheques, repositorio As ChequesRepositorio,
                       local As Long, uid As Long, todasLasCuentas As Boolean, cedula As String, cedulaConGuiones As String, estacion As String)
            _emision = emision
            _impresion = impresion
            _repositorio = repositorio
            _local = local
            _uid = uid
            _todasLasCuentas = todasLasCuentas
            _cedula = cedula
            _cedulaConGuiones = cedulaConGuiones
            _estacion = estacion
            InitializeComponent()
            cmbSalida.SelectedIndex = SalidaImpresora
            cmbDebeHaber.SelectedIndex = 0
            dtpFecha.Value = Date.Today
            tabs.TabPages.Remove(pagConta)
            Verificar()
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

        Private ReadOnly Property TipoElegido As TipoAsiento
            Get
                Return TryCast(cmbTipo.SelectedItem, TipoAsiento)
            End Get
        End Property

        Private ReadOnly Property LlevaConta As Boolean
            Get
                Return Cuenta IsNot Nothing AndAlso Cuenta.LlevaConta
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

        Private Async Sub FrmPrintCheque_Load(sender As Object, e As EventArgs) Handles MyBase.Load
            Try
                If Not Await _impresion.UsaPlantillaAsync(_local) Then
                    Aviso("Este local imprime con el formato gráfico, que no está migrado. Use el sistema anterior.")
                    Close()
                    Return
                End If
                _formato = Await _repositorio.ObtenerFormatoCuentaAsync(_cedula)
                _cargando = True
                cmbTipo.DisplayMember = NameOf(TipoAsiento.Descripcion)
                ' Con Items y no DataSource: la pestaña está fuera del control y el enlace se haría tarde,
                ' dejando elegido el primer tipo sin avisar.
                cmbTipo.Items.AddRange((Await _emision.ListarTiposAsientoAsync(_local)).ToArray())
                _cargando = False
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
                Dim cuentas = Await _emision.ListarCuentasAsync(_local, elegido.Codigo, _uid, _todasLasCuentas)
                If Banco IsNot elegido Then Return
                cmbCuentas.DisplayMember = NameOf(CuentaEmision.Etiqueta)
                cmbCuentas.DataSource = cuentas
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        Private Async Sub CmbCuentas_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbCuentas.SelectedIndexChanged
            Dim elegida = Cuenta
            If elegida Is Nothing Then Return
            If elegida.Moneda <> 0 Then
                Aviso("Las cuentas en otra moneda todavía no están migradas. Use el sistema anterior.")
                gbCheque.Enabled = False
                Verificar()
                Return
            End If
            gbCheque.Enabled = True
            MostrarContabilidad(elegida.LlevaConta)
            Try
                txtNumero.Text = (Await _emision.SiguienteNumeroAsync(_local, _estacion, Banco.Codigo, elegida.Cuenta)).ToString()
                Dim existente = _lineas.FirstOrDefault(Function(l) l.Sistema = LineaCaptura.SistemaBanco)
                Dim nueva = Await _emision.LineaBancoAsync(elegida, _cedula)
                If Cuenta IsNot elegida Then Return
                If existente Is Nothing Then
                    _lineas.Add(nueva)
                Else
                    existente.Cuenta = nueva.Cuenta
                    existente.Detalle = nueva.Detalle
                End If
            Catch ex As MySqlException
                Mensajes.NoConecta()
                Return
            End Try
            Recalcular()
        End Sub

        Private Sub MostrarContabilidad(visible As Boolean)
            txtMonto.Enabled = Not visible
            If visible AndAlso Not tabs.TabPages.Contains(pagConta) Then tabs.TabPages.Add(pagConta)
            If Not visible AndAlso tabs.TabPages.Contains(pagConta) Then tabs.TabPages.Remove(pagConta)
        End Sub

        Private Async Sub CmbTipo_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbTipo.SelectedIndexChanged
            If _cargando OrElse TipoElegido Is Nothing OrElse Cuenta Is Nothing Then
                Verificar()
                Return
            End If
            Try
                Dim lineas = Await _emision.CargarAsientoAsync(_local, TipoElegido.Tipo, Cuenta, _cedula)
                _lineas.Clear()
                _lineas.AddRange(lineas)
            Catch ex As MySqlException
                Mensajes.NoConecta()
            Catch ex As NotSupportedException
                Aviso(ex.Message)
                cmbTipo.SelectedIndex = -1
            End Try
            Recalcular()
        End Sub

        ' ---- Asiento ----

        ''' <summary>Vuelve a calcular el asiento y lo muestra. El banco va al final, como al ordenar por System.</summary>
        Private Sub Recalcular()
            Dim totales = AsientoCaptura.Recalcular(_lineas)
            _cargando = True
            rejilla.Rows.Clear()
            For Each l In _lineas.OrderBy(Function(x) x.Sistema)
                Dim i = rejilla.Rows.Add(False, _formato.Mostrar(l.Cuenta), l.Detalle, l.Debe, l.Haber)
                rejilla.Rows(i).Tag = l
                If l.Sistema = LineaCaptura.SistemaBanco Then rejilla.Rows(i).ReadOnly = True
            Next
            _cargando = False
            txtDebe.Text = totales.Debe.ToString("N2")
            txtHaber.Text = totales.Haber.ToString("N2")
            lblFaltante.Text = "Diferencia entre el debe y el haber:   " & (totales.Haber - totales.Debe).ToString("N2")
            If LlevaConta Then txtMonto.Text = totales.Monto.ToString("N2")
            Verificar()
        End Sub

        Private Sub Rejilla_CellEndEdit(sender As Object, e As DataGridViewCellEventArgs) Handles rejilla.CellEndEdit
            If _cargando OrElse e.ColumnIndex < 3 Then Return
            Dim l = CType(rejilla.Rows(e.RowIndex).Tag, LineaCaptura)
            Dim valor = Numero(Convert.ToString(rejilla.Rows(e.RowIndex).Cells(e.ColumnIndex).Value))
            If e.ColumnIndex = 3 Then l.Debe = valor Else l.Haber = valor
            BeginInvoke(New Action(AddressOf Recalcular))
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

        ''' <summary>CargarCuenta: solo se aceptan cuentas auxiliares del departamento.</summary>
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
            Recalcular()
            txtCuenta.Focus()
        End Sub

        Private Sub BtnRemover_Click(sender As Object, e As EventArgs) Handles btnRemover.Click
            rejilla.EndEdit()
            For Each fila In rejilla.Rows.Cast(Of DataGridViewRow)()
                Dim l = CType(fila.Tag, LineaCaptura)
                If CBool(fila.Cells(0).Value) AndAlso l.Sistema < 50 Then _lineas.Remove(l)
            Next
            Recalcular()
        End Sub

        Private Sub BtnMarcar_Click(sender As Object, e As EventArgs) Handles btnMarcarTodo.Click, btnMarcarNada.Click
            For Each fila In rejilla.Rows.Cast(Of DataGridViewRow)()
                If CType(fila.Tag, LineaCaptura).Sistema = 0 Then fila.Cells(0).Value = sender Is btnMarcarTodo
            Next
        End Sub

        ' ---- Encabezado ----

        Private Sub Poner(persona As PersonaCheque)
            txtId.Text = persona.Id
            txtNombre.Text = persona.Nombre
            txtCodigo.Text = persona.Codigo
        End Sub

        Private Async Sub BuscarPersona()
            Try
                Using datos = Await _emision.BuscarPersonasAsync()
                    Using buscador As New FrmBuscador("Búsqueda de personas", datos)
                        If buscador.ShowDialog(Me) = DialogResult.OK AndAlso buscador.Resultado(0).Length > 0 Then
                            Poner(New PersonaCheque With {.Nombre = buscador.Resultado(0), .Id = buscador.Resultado(1), .Codigo = buscador.Resultado(2)})
                        End If
                    End Using
                End Using
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        Private Async Sub TxtId_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtId.KeyPress
            If e.KeyChar <> ChrW(Keys.Return) Then Return
            e.Handled = True
            If txtId.Text.Trim().Length = 0 Then
                BuscarPersona()
                Return
            End If
            Try
                Dim persona = Await _emision.ObtenerPersonaPorIdAsync(txtId.Text.Trim())
                If persona IsNot Nothing Then Poner(persona)
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        Private Async Sub TxtCodigo_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtCodigo.KeyPress
            If e.KeyChar <> ChrW(Keys.Return) Then Return
            e.Handled = True
            If txtCodigo.Text.Trim().Length = 0 Then
                BuscarPersona()
                Return
            End If
            Try
                Dim persona = Await _emision.ObtenerPersonaPorCodigoAsync(txtCodigo.Text.Trim())
                If persona IsNot Nothing Then Poner(persona)
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        Private Sub TxtNombre_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtNombre.KeyPress
            If e.KeyChar = ChrW(Keys.Return) AndAlso txtNombre.Text.Trim().Length = 0 Then
                e.Handled = True
                BuscarPersona()
            End If
        End Sub

        Private Sub TxtMonto_TextChanged(sender As Object, e As EventArgs) Handles txtMonto.TextChanged
            lblLetras.Text = If(txtMonto.Text.Length = 0, "", MontoEnLetras.Convertir(Numero(txtMonto.Text), True, True))
            Verificar()
        End Sub

        Private Sub Campo_TextChanged(sender As Object, e As EventArgs) Handles txtNombre.TextChanged, txtNumero.TextChanged
            Verificar()
        End Sub

        Private Async Sub TxtNumero_Validating(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles txtNumero.Validating
            Dim numeroCheque As Long
            If Banco Is Nothing OrElse Cuenta Is Nothing OrElse Not Long.TryParse(txtNumero.Text, numeroCheque) Then Return
            Try
                If Await _emision.ExisteAsync(_local, Banco.Codigo, Cuenta.Cuenta, numeroCheque) Then
                    Aviso($"El cheque # {numeroCheque}. Ya se encuentra registrado.")
                    txtNumero.Text = ""
                End If
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        Private Async Sub BtnRefrescar_Click(sender As Object, e As EventArgs) Handles btnRefrescar.Click
            If Banco Is Nothing OrElse Cuenta Is Nothing Then Return
            Try
                txtNumero.Text = (Await _emision.SiguienteNumeroAsync(_local, _estacion, Banco.Codigo, Cuenta.Cuenta)).ToString()
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        ''' <summary>EccVerifImpresion.</summary>
        Private Sub Verificar()
            Dim listo = gbCheque.Enabled AndAlso txtNumero.Text.Length > 0 AndAlso txtNombre.Text.Length > 0 AndAlso Numero(txtMonto.Text) <> 0
            If LlevaConta Then listo = listo AndAlso TipoElegido IsNot Nothing AndAlso Math.Round(Numero(txtHaber.Text) - Numero(txtDebe.Text), 2) = 0
            btnImprimir.Enabled = listo
        End Sub

        ' ---- Registrar e imprimir ----

        Private Async Sub BtnImprimir_Click(sender As Object, e As EventArgs) Handles btnImprimir.Click
            Dim numeroCheque As Long
            If Not Long.TryParse(txtNumero.Text, numeroCheque) Then Return
            Dim cheque As New ChequeEmision With {
                .Banco = Banco.Codigo, .Cuenta = Cuenta.Cuenta, .Numero = numeroCheque, .Fecha = dtpFecha.Value.Date,
                .Id = txtId.Text, .Codigo = txtCodigo.Text, .Nombre = txtNombre.Text, .Monto = Numero(txtMonto.Text),
                .Detalle = txtDetalle.Text, .LlevaConta = LlevaConta, .TipoAsiento = If(TipoElegido?.Tipo, 0),
                .Lineas = _lineas.OrderBy(Function(l) l.Sistema).ToList()
            }
            btnImprimir.Enabled = False
            Try
                Select Case Await _emision.RegistrarAsync(cheque, _local, _estacion, _uid)
                    Case ResultadoEmision.YaRegistrado
                        Aviso($"El cheque # {numeroCheque}. Ya se encuentra registrado.")
                        txtNumero.Focus()
                    Case ResultadoEmision.Repetido
                        Aviso("# Cheque Repetido.")
                    Case Else
                        Await Imprimir(cheque)
                        Limpiar()
                End Select
            Catch ex As MySqlException
                Mensajes.NoGuardo(ex.Message, "Registrar cheque", Modulo.TituloModulo)
            Finally
                Verificar()
            End Try
        End Sub

        ''' <summary>El cheque ya quedó registrado: un fallo aquí no lo deshace, y hay que avisarlo.</summary>
        Private Async Function Imprimir(cheque As ChequeEmision) As Task
            If cmbSalida.SelectedIndex = SalidaNinguna Then Return
            Try
                If Not Await _impresion.SeImprimeAsync(_local) Then Return
                Dim trabajo = Await _impresion.ArmarAsync(cheque.Banco, cheque.Cuenta, cheque.Numero, _local, _estacion, _formato)
                If cmbSalida.SelectedIndex = SalidaPdf Then
                    Using dialogo As New SaveFileDialog With {.Filter = "PDF|*.pdf", .FileName = $"Cheque {cheque.Cuenta} {cheque.Numero}.pdf"}
                        If dialogo.ShowDialog(Me) = DialogResult.OK Then ImpresionMatricial.GuardarPdf(trabajo.Texto, dialogo.FileName)
                    End Using
                Else
                    ImpresionMatricial.Enviar(trabajo.Texto, trabajo.Impresora)
                End If
            Catch ex As Exception When TypeOf ex Is MySqlException OrElse TypeOf ex Is InvalidOperationException OrElse TypeOf ex Is System.ComponentModel.Win32Exception
                Mensajes.NoGuardo($"El cheque # {cheque.Numero} quedó registrado, pero no se imprimió. {ex.Message}", "Imprimir cheque", Modulo.TituloModulo)
            End Try
        End Function

        Private Sub Limpiar()
            dtpFecha.Value = Date.Today
            txtNombre.Text = ""
            txtId.Text = ""
            txtCodigo.Text = ""
            txtMonto.Text = "0.00"
            txtDetalle.Text = ""
            cmbDebeHaber.SelectedIndex = 0
            _lineas.Clear()
            tabs.SelectedIndex = 0
            CmbCuentas_SelectedIndexChanged(Nothing, EventArgs.Empty)
            If dtpFecha.CanFocus Then dtpFecha.Focus()
        End Sub

        Private Sub BtnSalir_Click(sender As Object, e As EventArgs) Handles btnSalir.Click
            Close()
        End Sub

    End Class

End Namespace
