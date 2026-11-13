Imports System.Globalization
Imports MySqlConnector
Imports CoopeNet.Core
Imports CoopeNet.Core.Pos
Imports CoopeNet.Ui

Namespace SD0502

    ''' <summary>
    ''' Cajas: la caja de cada estación y los usuarios que facturan en ella. Migración de
    ''' SD0502\frmMantCajas.frm (Mantenimiento, opción 013, privilegio 5018).
    ''' </summary>
    ''' <remarks>
    ''' No se migra el panel de factura electrónica (notas de crédito, reenvíos y descarga de imágenes) que VB6
    ''' esconde fuera de la ventana y solo muestra con un clic en "Modificada" en DEBUG o con el UID 0, ni la
    ''' casilla "Usar consola", que también está oculta: su valor (PCom1) se conserva al guardar.
    ''' </remarks>
    Public Class FrmCajas
        Inherits Form

        Private Const Titulo As String = "Caja"

        Private ReadOnly _repo As CajasRepositorio
        Private ReadOnly _local As Long
        Private ReadOnly _uid As Long
        Private ReadOnly _estacionSesion As String
        Private ReadOnly _genero As String
        Private _actual As Caja
        ''' <summary>La caja que se veía antes de Nuevo o Editar, para volver a ella con Cancelar.</summary>
        Private _antes As Caja
        ''' <summary>SDTNuCaja.Tag: el número con que se cargó la caja; si no cambia, no se revisa si está ocupado.</summary>
        Private _cajaCargada As String = "0"
        Private _consecutivo As Double
        Private _editando As Boolean

        Private ReadOnly gbDatos As New GroupBox With {.Name = "gbDatos", .Location = New Point(12, 8), .Size = New Size(560, 306)}
        Private ReadOnly cmbEstacion As New ComboBox With {.Name = "cmbEstacion", .AccessibleName = "Estación", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(90, 24), .Width = 296, .DropDownWidth = 360}
        Private ReadOnly cmbUsuario As New ComboBox With {.Name = "cmbUsuario", .AccessibleName = "Usuario", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(90, 60), .Width = 296}
        Private ReadOnly txtCaja As New TextBox With {.Name = "txtCaja", .AccessibleName = "Número de caja", .MaxLength = 3, .TextAlign = HorizontalAlignment.Right, .Location = New Point(458, 24), .Width = 90}
        Private ReadOnly txtConsecutivo As New TextBox With {.Name = "txtConsecutivo", .AccessibleName = "Consecutivo", .ReadOnly = True, .TabStop = False, .TextAlign = HorizontalAlignment.Right, .Location = New Point(458, 60), .Width = 90}
        Private ReadOnly btnModificar As New Button With {.Name = "btnModificar", .Text = "&Modificar", .Location = New Point(458, 94), .Size = New Size(90, 28)}
        Private ReadOnly gbTipo As New GroupBox With {.Name = "gbTipo", .Text = "Tipo de facturación", .Location = New Point(12, 132), .Size = New Size(536, 110)}
        Private ReadOnly optPos As New RadioButton With {.Name = "optPos", .Text = "Modo POS", .AutoSize = True, .Location = New Point(16, 26)}
        Private ReadOnly optFacturacion As New RadioButton With {.Name = "optFacturacion", .Text = "Modo Facturación", .AutoSize = True, .Location = New Point(16, 52)}
        Private ReadOnly optTactil As New RadioButton With {.Name = "optTactil", .Text = "Modo Táctil", .AutoSize = True, .Location = New Point(16, 78)}
        Private ReadOnly chkLineaRapida As New CheckBox With {.Name = "chkLineaRapida", .Text = "Línea rápida", .AutoSize = True, .Location = New Point(260, 26)}
        Private ReadOnly chkActiva As New CheckBox With {.Name = "chkActiva", .Text = "Activa", .AutoSize = True, .Location = New Point(260, 52)}
        Private ReadOnly lblCreada As New Label With {.Name = "lblCreada", .AutoSize = True, .Location = New Point(110, 254)}
        Private ReadOnly lblModificada As New Label With {.Name = "lblModificada", .AutoSize = True, .Location = New Point(110, 278)}
        Private ReadOnly barra As New BarraComandos With {.Name = "barra", .Location = New Point(12, 324), .Size = New Size(560, 32)}

        Public Sub New(repo As CajasRepositorio, local As Long, uid As Long, estacionSesion As String, genero As String)
            _repo = repo
            _local = local
            _uid = uid
            _estacionSesion = estacionSesion
            _genero = genero
            Text = Titulo
            ClientSize = New Size(584, 366)
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent

            gbTipo.Controls.AddRange(New Control() {optPos, optFacturacion, optTactil, chkLineaRapida, chkActiva})
            gbDatos.Controls.AddRange(New Control() {
                Rotulo("Estación", 12, 27), cmbEstacion, Rotulo("# Caja", 400, 27), txtCaja,
                Rotulo("Usuario", 12, 63), cmbUsuario, Rotulo("Consec.", 400, 63), txtConsecutivo, btnModificar,
                gbTipo,
                Rotulo("Creada", 12, 254), lblCreada, Rotulo("Modificada", 12, 278), lblModificada
            })
            Controls.AddRange(New Control() {gbDatos, barra})
            barra.AlHacerClic = AddressOf Barra_Clic
            AddHandler cmbEstacion.SelectionChangeCommitted, Async Sub() Await EstacionElegidaAsync()
            AddHandler txtCaja.Leave, Async Sub() Await CajaEscritaAsync()
            AddHandler txtCaja.KeyPress, AddressOf SoloDigitos
            AddHandler btnModificar.Click, AddressOf BtnModificar_Click
            Limpiar()
            Habilitar(False)
        End Sub

        Private Shared Function Rotulo(texto As String, x As Integer, y As Integer) As Label
            Return New Label With {.Text = texto, .AutoSize = True, .Location = New Point(x, y)}
        End Function

        Private Shared Sub SoloDigitos(sender As Object, e As KeyPressEventArgs)
            If Not Char.IsControl(e.KeyChar) AndAlso Not Char.IsDigit(e.KeyChar) Then e.Handled = True
        End Sub

        Protected Overrides Async Sub OnLoad(e As EventArgs)
            MyBase.OnLoad(e)
            Try
                Dim catalogos = Await _repo.CatalogosAsync()
                cmbEstacion.Items.AddRange(catalogos.Estaciones.ToArray())
                cmbUsuario.Items.AddRange(catalogos.Usuarios.ToArray())
                If catalogos.Estaciones.Count = 0 OrElse catalogos.Usuarios.Count = 0 Then
                    MessageBox.Show(Me, "NO SE ENCONTRARON ESTACIONES O USUARIOS PARA ASIGNAR", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Error)
                    barra.Enabled = False
                End If
            Catch ex As MySqlException
                Mensajes.NoConecta()
                barra.Enabled = False
            End Try
        End Sub

        ' ---- Datos en pantalla ----

        ''' <summary>SD_Limpiar.</summary>
        Private Sub Limpiar()
            _actual = Nothing
            cmbEstacion.SelectedIndex = -1
            cmbUsuario.SelectedIndex = -1
            lblCreada.Text = ""
            lblModificada.Text = ""
            chkActiva.Checked = False
            chkLineaRapida.Checked = False
            Consecutivo = 0
            txtCaja.Text = "0"
            _cajaCargada = "0"
            optPos.Checked = True
            Text = Titulo
        End Sub

        Private Property Consecutivo As Double
            Get
                Return _consecutivo
            End Get
            Set(value As Double)
                _consecutivo = value
                txtConsecutivo.Text = value.ToString("0", CultureInfo.InvariantCulture)
                btnModificar.Enabled = _editando AndAlso CajasRepositorio.PartirConsecutivo(value).HasValue
            End Set
        End Property

        Private Sub Mostrar(c As Caja)
            Limpiar()
            If c Is Nothing Then Return
            _actual = c
            Elegir(cmbEstacion, c.Estacion)
            Elegir(cmbUsuario, c.Uid.ToString(CultureInfo.InvariantCulture))
            txtCaja.Text = c.Numero
            _cajaCargada = c.Numero
            Consecutivo = c.Consecutivo
            chkActiva.Checked = c.Activa
            chkLineaRapida.Checked = c.LineaRapida
            optPos.Checked = c.Facturacion = ModoFacturacion.Pos
            optFacturacion.Checked = c.Facturacion = ModoFacturacion.Facturacion
            optTactil.Checked = c.Facturacion = ModoFacturacion.Tactil
            lblCreada.Text = If(c.FC.HasValue, c.FC.Value.ToString("G"), "")
            lblModificada.Text = If(c.FM.HasValue, c.FM.Value.ToString("G"), "")
        End Sub

        Private Shared Sub Elegir(combo As ComboBox, codigo As String)
            combo.SelectedIndex = -1
            For i = 0 To combo.Items.Count - 1
                If String.Equals(CType(combo.Items(i), OpcionCaja).Codigo, codigo, StringComparison.OrdinalIgnoreCase) Then
                    combo.SelectedIndex = i
                    Return
                End If
            Next
        End Sub

        Private Shared Function Codigo(combo As ComboBox) As String
            Return If(TryCast(combo.SelectedItem, OpcionCaja)?.Codigo, "")
        End Function

        ''' <summary>Habilitar de VB6 sobre frmDatos. El consecutivo nunca se escribe: se cambia con Modificar.</summary>
        Private Sub Habilitar(editando As Boolean)
            _editando = editando
            For Each control In New Control() {cmbEstacion, cmbUsuario, txtCaja, gbTipo}
                control.Enabled = editando
            Next
            btnModificar.Enabled = editando AndAlso CajasRepositorio.PartirConsecutivo(_consecutivo).HasValue
        End Sub

        Private Function ModoElegido() As ModoFacturacion
            If optFacturacion.Checked Then Return ModoFacturacion.Facturacion
            If optTactil.Checked Then Return ModoFacturacion.Tactil
            Return ModoFacturacion.Pos
        End Function

        ' ---- Estación y número de caja ----

        ''' <summary>
        ''' cmbEstacion_Click: una estación con caja muestra su número y su consecutivo, y el número no se
        ''' cambia; una sin caja propone la siguiente del local.
        ''' </summary>
        Private Async Function EstacionElegidaAsync() As Task
            Consecutivo = 0
            Dim estacion = Codigo(cmbEstacion)
            If estacion.Length = 0 Then Return
            Try
                Dim numero = Await _repo.CajaDeEstacionAsync(estacion, _local)
                If numero.Length = 0 Then
                    txtCaja.Text = Await _repo.SiguienteCajaAsync(_local)
                    txtCaja.Enabled = True
                    Await ValidarConsecutivoAsync()
                Else
                    txtCaja.Text = numero
                    Consecutivo = Await _repo.ConsecutivoDeEstacionAsync(estacion)
                    txtCaja.Enabled = False
                End If
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Function

        ''' <summary>SDTNuCaja_LostFocus.</summary>
        Private Async Function CajaEscritaAsync() As Task
            If Not _editando OrElse Not txtCaja.Enabled Then Return
            Consecutivo = 0
            Try
                Await ValidarConsecutivoAsync()
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Function

        ''' <summary>
        ''' ValidaConsec: el consecutivo empieza en la caja por un millón o sigue a la última factura de esa caja.
        ''' VB6 pregunta si se quiere reasignar un número ocupado, pero Guardar lo rechaza de todos modos, así que
        ''' aquí solo se avisa.
        ''' </summary>
        Private Async Function ValidarConsecutivoAsync() As Task
            Dim numero = txtCaja.Text.Trim()
            If Vb6.Val(numero) = 0 Then Return
            If Await CajaOcupadaAsync(numero) Then Return
            Consecutivo = CajasRepositorio.ConsecutivoInicial(numero)
            Dim ultima = Await _repo.UltimaFacturaAsync(numero)
            If ultima > 0 Then Consecutivo = ultima
        End Function

        ''' <summary>ExisteCaja: avisa si otra estación ya tiene ese número.</summary>
        Private Async Function CajaOcupadaAsync(numero As String) As Task(Of Boolean)
            If numero = _cajaCargada Then Return False
            If Not Await _repo.CajaOcupadaAsync(Codigo(cmbEstacion), numero) Then Return False
            MessageBox.Show(Me, $"Ya existe la caja número {numero}." & vbCrLf & "Nota: La duplicación de números de caja interfiere con los consecutivos de factura.",
                            Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return True
        End Function

        Private Sub BtnModificar_Click(sender As Object, e As EventArgs)
            Dim estacion = Codigo(cmbEstacion)
            If estacion.Length = 0 OrElse Not CajasRepositorio.PartirConsecutivo(_consecutivo).HasValue Then Return
            Using ventana As New FrmConsecutivoCaja(_repo, estacion, _consecutivo, _local, _uid, _estacionSesion)
                Tema.Aplicar(ventana, Modulo.TituloModulo)
                If ventana.ShowDialog(Me) = DialogResult.OK Then Consecutivo = ventana.Nuevo
            End Using
        End Sub

        ' ---- Barra ----

        Private Async Function Barra_Clic(boton As BotonBarra) As Task(Of Boolean)
            Try
                Select Case boton
                    Case BotonBarra.Nuevo
                        _antes = _actual
                        Limpiar()
                        Habilitar(True)
                        txtCaja.Enabled = True
                        Elegir(cmbEstacion, _estacionSesion)
                        Await EstacionElegidaAsync()
                        cmbEstacion.Focus()
                    Case BotonBarra.Editar
                        If _actual Is Nothing Then Return True
                        _antes = _actual
                        Habilitar(True)
                        txtCaja.Enabled = False
                        cmbUsuario.Focus()
                    Case BotonBarra.Guardar
                        If Not Await GuardarAsync() Then Return True
                        Habilitar(False)
                    Case BotonBarra.Cancelar
                        Mostrar(_antes)
                        Habilitar(False)
                    Case BotonBarra.Borrar
                        Await BorrarAsync()
                    Case BotonBarra.Buscar
                        Await BuscarAsync()
                    Case BotonBarra.Salir
                        Close()
                End Select
            Catch ex As MySqlException
                Mensajes.NoGuardo(ex.Message, "CAJAS", Modulo.TituloModulo)
                Return True
            End Try
            Return False
        End Function

        ''' <summary>SD_Guardar.</summary>
        Private Async Function GuardarAsync() As Task(Of Boolean)
            Dim numero = txtCaja.Text.Trim()
            If Codigo(cmbEstacion).Length = 0 OrElse Codigo(cmbUsuario).Length = 0 OrElse Vb6.Val(numero) = 0 Then
                MessageBox.Show(Me, "Faltan datos requeridos para el correcto funcionamiento de la caja", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return False
            End If
            If Await CajaOcupadaAsync(numero) Then Return False
            Dim c As New Caja With {
                .Estacion = Codigo(cmbEstacion), .Numero = numero, .Consecutivo = _consecutivo, .Facturacion = ModoElegido(),
                .Activa = chkActiva.Checked, .Uid = CLng(Vb6.Val(Codigo(cmbUsuario))), .FC = _actual?.FC,
                .Consola = If(_actual?.Consola, False), .LineaRapida = chkLineaRapida.Checked
            }
            Await _repo.GuardarAsync(c, _local)
            Mostrar(If(Await _repo.CargarAsync(c.Estacion, c.Uid, c.Numero, _local), c))
            Text = $"CAJA {c.Numero}"
            Return True
        End Function

        ''' <summary>SD_Borrar: quita al usuario de la caja; la caja queda.</summary>
        Private Async Function BorrarAsync() As Task
            If _actual Is Nothing Then Return
            Dim pregunta = $"Está segur{If(_genero = "0", "o", "a")} que desea eliminar el registro"
            If MessageBox.Show(Me, pregunta, Modulo.TituloModulo, MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Return
            Await _repo.QuitarUsuarioAsync(_actual.Estacion, _actual.Uid)
            Limpiar()
            Habilitar(False)
        End Function

        Private Async Function BuscarAsync() As Task
            Using datos = Await _repo.BuscarAsync(_local)
                Using buscador As New FrmBuscador("Búsqueda de cajas", datos)
                    If buscador.ShowDialog(Me) <> DialogResult.OK OrElse buscador.Resultado(1).Length = 0 Then Return
                    Dim c = Await _repo.CargarAsync(buscador.Resultado(4), CLng(Vb6.Val(buscador.Resultado(3))), buscador.Resultado(1), _local)
                    Mostrar(c)
                    If c IsNot Nothing Then Text = $"CAJA {c.Numero} - Usuario {buscador.Resultado(0)}"
                End Using
            End Using
            Habilitar(False)
        End Function

    End Class

End Namespace
