Imports MySqlConnector
Imports CoopeNet.Core.Cheques
Imports CoopeNet.Ui

Namespace SD1501

    ''' <summary>
    ''' Opción 004 (Configuración Cheque), privilegio 15005. Migración de Fuentes\SD1501\FrmConfigCheque.frm.
    ''' Las medidas se aplican al salir de cada casilla, no con cada tecla: el original avisaba de
    ''' los límites mientras se escribía. Las reglas están en <see cref="CampoCheque"/>.
    ''' </summary>
    Partial Public Class FrmConfigCheque
        Inherits Form

        Private Const MontoEjemplo As Double = 999999999.99

        Private ReadOnly _repositorio As ChequesRepositorio
        Private ReadOnly _local As Long
        Private _config As New ConfigCheque()
        Private _indice As Integer
        Private _primero As Integer
        Private _ultimo As Integer = 4
        Private _mostrando As Boolean

        Public Sub New(repositorio As ChequesRepositorio, local As Long)
            _repositorio = repositorio
            _local = local
            InitializeComponent()
            cmbForma.SelectedIndex = 0
            cmbPosicion.SelectedIndex = 0
            MostrarPaso(1)
        End Sub

        Private ReadOnly Property Campo As CampoCheque
            Get
                Return _config.Campos(_indice)
            End Get
        End Property

        Private Async Sub FrmConfigCheque_Load(sender As Object, e As EventArgs) Handles MyBase.Load
            Try
                cmbBancos.DisplayMember = NameOf(BancoCheque.Etiqueta)
                cmbBancos.DataSource = Await _repositorio.ListarBancosConfigAsync(_local)
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Sub

        Private Async Sub CmbBancos_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbBancos.SelectedIndexChanged
            Dim banco = TryCast(cmbBancos.SelectedItem, BancoCheque)
            If banco Is Nothing Then Return
            Try
                Dim leida = Await _repositorio.CargarConfigAsync(_local, banco.Codigo)
                If cmbBancos.SelectedItem IsNot banco Then Return
                _config = leida
            Catch ex As MySqlException
                Mensajes.NoConecta()
                Return
            End Try
            If _config.SinRegistro Then Aviso("Registro no Encontrado. Cargando Valores Predeterminados.")
            MostrarGeneral()
            rbCheque.Checked = True
            ElegirGrupo(0, 4)
        End Sub

        Private Sub Aviso(texto As String)
            MessageBox.Show(Me, texto, Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
        End Sub

        Private Shared Function Numero(casilla As TextBox) As Double
            Dim valor As Double
            Double.TryParse(casilla.Text, valor)
            Return valor
        End Function

        ' ---- Paso 1 ----

        Private Sub MostrarGeneral()
            txtAlto.Text = _config.AltoCm.ToString("0.00")
            txtAncho.Text = _config.AnchoCm.ToString("0.00")
            cmbForma.SelectedIndex = _config.FormaImpresion
            cmbPosicion.SelectedIndex = _config.PosicionHoja
            chkDetalle.Checked = _config.ImprimeDetalle
            chkContabilidad.Checked = _config.ImprimeContabilidad
            txtAltoDetalle.Text = _config.AltoDetalleCm.ToString("0.00")
            txtAltoContabilidad.Text = _config.AltoContabilidadCm.ToString("0.00")
        End Sub

        Private Sub LeerGeneral()
            _config.AltoCm = Numero(txtAlto)
            _config.AnchoCm = Numero(txtAncho)
            _config.FormaImpresion = cmbForma.SelectedIndex
            _config.PosicionHoja = cmbPosicion.SelectedIndex
            _config.ImprimeDetalle = chkDetalle.Checked
            _config.ImprimeContabilidad = chkContabilidad.Checked
            _config.AltoDetalleCm = Numero(txtAltoDetalle)
            _config.AltoContabilidadCm = Numero(txtAltoContabilidad)
        End Sub

        Private Sub TxtMedida_TextChanged(sender As Object, e As EventArgs) Handles txtAlto.TextChanged, txtAncho.TextChanged
            btnSiguiente.Enabled = panelPaso1.Visible AndAlso Numero(txtAlto) <> 0 AndAlso Numero(txtAncho) <> 0
        End Sub

        Private Sub MostrarPaso(paso As Integer)
            panelPaso1.Visible = paso = 1
            panelPaso2.Visible = paso = 2
            btnAnterior.Enabled = paso = 2
            btnSiguiente.Enabled = paso = 1 AndAlso Numero(txtAlto) <> 0 AndAlso Numero(txtAncho) <> 0
        End Sub

        Private Sub BtnSiguiente_Click(sender As Object, e As EventArgs) Handles btnSiguiente.Click
            MostrarPaso(2)
        End Sub

        Private Sub BtnAnterior_Click(sender As Object, e As EventArgs) Handles btnAnterior.Click
            MostrarPaso(1)
        End Sub

        ' ---- Paso 2 ----

        Private Sub RbTipo_CheckedChanged(sender As Object, e As EventArgs) Handles rbCheque.CheckedChanged, rbDetalle.CheckedChanged, rbContabilidad.CheckedChanged
            If Not CType(sender, RadioButton).Checked Then Return
            If rbCheque.Checked Then
                ElegirGrupo(0, 4)
            ElseIf rbDetalle.Checked Then
                ElegirGrupo(5, 7)
            Else
                ElegirGrupo(8, 11)
            End If
        End Sub

        Private Sub ElegirGrupo(primero As Integer, ultimo As Integer)
            _primero = primero
            _ultimo = ultimo
            For i = 0 To 11
                opciones(i).Visible = i >= primero AndAlso i <= ultimo
            Next
            txtArriba.Enabled = primero < 8
            ElegirCampo(primero)
            If opciones(primero).Checked Then Return
            opciones(primero).Checked = True
        End Sub

        Private Sub Opcion_CheckedChanged(sender As Object, e As EventArgs)
            Dim opcion = CType(sender, RadioButton)
            If opcion.Checked Then ElegirCampo(CInt(opcion.Tag))
        End Sub

        Private Sub Etiqueta_Click(sender As Object, e As EventArgs)
            opciones(CInt(CType(sender, Label).Tag)).Checked = True
        End Sub

        Private Sub ElegirCampo(indice As Integer)
            _indice = indice
            MostrarCampo()
            Dibujar()
        End Sub

        Private Sub MostrarCampo()
            _mostrando = True
            Dim c = Campo
            gbImprime.Text = $"Imprimir el Campo ""{opciones(c.Indice).Text}"" en el Cheque"
            chkImprime.Checked = c.Imprime
            txtArriba.Text = c.ArribaCm.ToString("N2")
            txtIzquierda.Text = (c.Izquierda / CampoCheque.TwipsPorCm).ToString("N2")
            txtAltoCampo.Text = (c.Alto / CampoCheque.TwipsPorCm).ToString("N2")
            txtAnchoCampo.Text = (c.Ancho / CampoCheque.TwipsPorCm).ToString("N2")
            txtFuente.Text = c.Fuente
            txtTamano.Text = c.Tamano.ToString()
            chkNegrita.Checked = c.Negrita
            chkCursiva.Checked = c.Cursiva
            cmbAlineacion.SelectedIndex = c.Alineacion
            cmbFormato.Items.Clear()
            For Each o In c.Opciones
                cmbFormato.Items.Add(o)
            Next
            cmbFormato.SelectedIndex = c.IndiceFormato()
            _mostrando = False
        End Sub

        ''' <summary>Redibuja la vista previa. Un twip de la tabla se escala al ancho del panel.</summary>
        Private Sub Dibujar()
            Dim escala = vista.ClientSize.Width / CampoCheque.VistaAncho
            For i = 0 To 11
                Dim c = _config.Campos(i)
                Dim etiqueta = etiquetas(i)
                etiqueta.Visible = i >= _primero AndAlso i <= _ultimo AndAlso c.Imprime
                etiqueta.SetBounds(CInt(c.Izquierda * escala), CInt(c.Arriba * escala), Math.Max(1, CInt(c.Ancho * escala)), Math.Max(1, CInt(c.Alto * escala)))
                etiqueta.TextAlign = {ContentAlignment.TopLeft, ContentAlignment.TopRight, ContentAlignment.TopCenter}(Math.Min(c.Alineacion, 2))
                Dim estilo = If(c.Negrita, FontStyle.Bold, FontStyle.Regular) Or If(c.Cursiva, FontStyle.Italic, FontStyle.Regular)
                etiqueta.Font = New Font(c.Fuente, CSng(Math.Max(c.Tamano, 1)), estilo)
                etiqueta.Text = TextoEjemplo(c)
                etiqueta.BackColor = If(i = _indice, SystemColors.Info, Color.White)
            Next
        End Sub

        Private Shared Function TextoEjemplo(c As CampoCheque) As String
            Select Case c.TipoFormato
                Case TipoFormatoCampo.Fecha
                    Return FormatoCheque.Fecha(Date.Today, c.Formato)
                Case TipoFormatoCampo.Monto
                    ' Debe y Haber son columnas angostas: el original los muestra en cero.
                    Return FormatoCheque.Monto(If(c.Indice = 2, MontoEjemplo, 0), c.Formato)
            End Select
            Dim texto = {"", Tema.Desarrollador, "", MontoEnLetras.Convertir(MontoEjemplo, True, True),
                         "Número De Control", "", "# Cheque", "Detalle", "C.Contable", "Descripción"}(c.Indice)
            Return FormatoCheque.Texto(texto, c.IndiceFormato())
        End Function

        Private Sub Aplicar(limite As Double?, nombre As String)
            If limite.HasValue Then Aviso($"La posición ""{nombre}"", no puede ser mayor a {limite.Value:N2}")
            MostrarCampo()
            Dibujar()
        End Sub

        Private Sub TxtArriba_Leave(sender As Object, e As EventArgs) Handles txtArriba.Leave
            Aplicar(Campo.PonerArribaCm(Numero(txtArriba)), "TOP")
        End Sub

        Private Sub TxtIzquierda_Leave(sender As Object, e As EventArgs) Handles txtIzquierda.Leave
            Aplicar(Campo.PonerIzquierdaCm(Numero(txtIzquierda)), "IZQUIERDA")
        End Sub

        Private Sub TxtAltoCampo_Leave(sender As Object, e As EventArgs) Handles txtAltoCampo.Leave
            Aplicar(Campo.PonerAltoCm(Numero(txtAltoCampo)), "ALTO")
        End Sub

        Private Sub TxtAnchoCampo_Leave(sender As Object, e As EventArgs) Handles txtAnchoCampo.Leave
            Aplicar(Campo.PonerAnchoCm(Numero(txtAnchoCampo)), "ANCHO")
        End Sub

        Private Sub ChkImprime_CheckedChanged(sender As Object, e As EventArgs) Handles chkImprime.CheckedChanged
            If _mostrando Then Return
            Campo.Imprime = chkImprime.Checked
            Dibujar()
        End Sub

        Private Sub CmbAlineacion_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbAlineacion.SelectedIndexChanged
            If _mostrando Then Return
            Campo.Alineacion = cmbAlineacion.SelectedIndex
            Dibujar()
        End Sub

        Private Sub CmbFormato_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbFormato.SelectedIndexChanged
            If _mostrando OrElse cmbFormato.SelectedIndex < 0 Then Return
            Campo.ElegirFormato(cmbFormato.SelectedIndex)
            Dibujar()
        End Sub

        Private Sub BtnPredeterminados_Click(sender As Object, e As EventArgs) Handles btnPredeterminados.Click
            _config.Predeterminar(_indice)
            MostrarCampo()
            Dibujar()
        End Sub

        Private Sub BtnFuente_Click(sender As Object, e As EventArgs) Handles btnFuente.Click
            Dim c = Campo
            Using dialogo As New FontDialog With {.ShowEffects = False, .Font = etiquetas(c.Indice).Font}
                Try
                    If dialogo.ShowDialog(Me) <> DialogResult.OK Then Return
                Catch ex As ArgumentException
                    ' FontDialog rechaza las fuentes que no son TrueType.
                    Aviso("Esa fuente no se puede usar.")
                    Return
                End Try
                c.Fuente = dialogo.Font.Name
                c.Tamano = dialogo.Font.SizeInPoints
                c.Negrita = dialogo.Font.Bold
                c.Cursiva = dialogo.Font.Italic
            End Using
            MostrarCampo()
            Dibujar()
        End Sub

        ' ---- Grabar ----

        Private Async Sub BtnAplicar_Click(sender As Object, e As EventArgs) Handles btnAplicar.Click
            Dim banco = TryCast(cmbBancos.SelectedItem, BancoCheque)
            If banco Is Nothing Then Return
            If Pregunta("Realmente quiere guardar esta configuración?") = DialogResult.No Then Return
            If _config.Local = -1 AndAlso Pregunta("Guardar Configuración personalizada para el Departamento?") = DialogResult.Yes Then
                _config.Local = _local
            End If
            LeerGeneral()
            btnAplicar.Enabled = False
            Try
                Await _repositorio.GuardarConfigAsync(_config.AFilas(banco.Codigo))
                Aviso("La información a sido guardada.")
            Catch ex As MySqlException
                Aviso("Error al guardar la información.")
            Finally
                btnAplicar.Enabled = True
            End Try
        End Sub

        Private Function Pregunta(texto As String) As DialogResult
            Return MessageBox.Show(Me, texto, Modulo.TituloModulo, MessageBoxButtons.YesNo, MessageBoxIcon.Question)
        End Function

        Private Sub BtnSalir_Click(sender As Object, e As EventArgs) Handles btnSalir.Click
            Close()
        End Sub

    End Class

End Namespace
