Imports MySqlConnector
Imports CoopeNet.Core.Pos
Imports CoopeNet.Ui

Namespace SD0502

    ''' <summary>
    ''' Catálogo de monedas: los billetes y monedas de cada tipo de moneda, con su imagen. Migración de
    ''' SD0502\frmMantMonedas.frm (Mantenimiento, opción 067, privilegio 5012).
    ''' </summary>
    ''' <remarks>
    ''' Diferencias con VB6: navega solo las del local; el valor se escribe al crear y no se cambia al editar (en VB6
    ''' editarlo creaba otra y dejaba la anterior); no se guarda una nueva sobre otra que ya tenga ese valor; cambiar
    ''' el tipo limpia la pantalla; la imagen se importa con la moneda guardada y no se toca si el valor es el código
    ''' de un artículo (comparten posimagartic).
    ''' </remarks>
    Public Class FrmDenominaciones
        Inherits Form

        Private ReadOnly _repo As DenominacionesRepositorio
        Private ReadOnly _uid As Long
        Private ReadOnly _local As Long
        Private ReadOnly _genero As String
        Private _actual As Denominacion
        Private _antes As Denominacion
        Private _editando As Boolean
        Private _cargando As Boolean

        Private ReadOnly gbDatos As New GroupBox With {.Name = "gbDatos", .Location = New Point(12, 8), .Size = New Size(600, 176)}
        Private ReadOnly txtMoneda As New TextBox With {.Name = "txtMoneda", .AccessibleName = "Moneda", .MaxLength = 16, .Location = New Point(130, 28), .Width = 120}
        Private ReadOnly cmbTipo As New ComboBox With {.Name = "cmbTipo", .AccessibleName = "Tipo de moneda", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(130, 66), .Width = 260}
        Private ReadOnly txtDescripcion As New TextBox With {
            .Name = "txtDescripcion", .AccessibleName = "Descripción", .CharacterCasing = CharacterCasing.Upper,
            .MaxLength = DenominacionesRepositorio.LargoDescripcion, .Location = New Point(130, 104), .Width = 260}
        Private ReadOnly picImagen As New PictureBox With {
            .Name = "picImagen", .AccessibleName = "Imagen de la moneda", .BorderStyle = BorderStyle.FixedSingle, .SizeMode = PictureBoxSizeMode.Zoom,
            .Location = New Point(410, 24), .Size = New Size(172, 104)}
        Private ReadOnly btnImagen As New Button With {.Name = "btnImagen", .Text = "&Imagen...", .Location = New Point(410, 134), .Size = New Size(172, 30)}
        Private ReadOnly btnAnterior As New Button With {.Name = "btnAnterior", .Text = "◀", .AccessibleName = "Moneda anterior", .Location = New Point(12, 194), .Size = New Size(56, 32)}
        Private ReadOnly btnSiguiente As New Button With {.Name = "btnSiguiente", .Text = "▶", .AccessibleName = "Moneda siguiente", .Location = New Point(72, 194), .Size = New Size(56, 32)}
        Private ReadOnly barra As New BarraComandos With {.Name = "barra", .Location = New Point(136, 194), .Size = New Size(476, 32)}

        Public Sub New(repo As DenominacionesRepositorio, uid As Long, local As Long, genero As String)
            _repo = repo
            _uid = uid
            _local = local
            _genero = genero
            Text = "Catálogo de monedas"
            ClientSize = New Size(624, 238)
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent

            gbDatos.Controls.AddRange(New Control() {
                Rotulo("Moneda", 12, 31), txtMoneda, Rotulo("Tipo de moneda", 12, 69), cmbTipo,
                Rotulo("Descripción", 12, 107), txtDescripcion, picImagen, btnImagen})
            Controls.AddRange(New Control() {gbDatos, btnAnterior, btnSiguiente, barra})

            barra.AlHacerClic = AddressOf Barra_Clic
            AddHandler btnAnterior.Click, Async Sub() Await MoverAsync(False)
            AddHandler btnSiguiente.Click, Async Sub() Await MoverAsync(True)
            AddHandler btnImagen.Click, Async Sub() Await ImportarAsync()
            AddHandler cmbTipo.SelectedIndexChanged, AddressOf Tipo_Cambio
            AddHandler FormClosed, Sub() Pintar(Nothing)
            Limpiar()
            Habilitar(False)
        End Sub

        Private Shared Function Rotulo(texto As String, x As Integer, y As Integer) As Label
            Return New Label With {.Text = texto, .AutoSize = True, .Location = New Point(x, y)}
        End Function

        ''' <summary>Form_Load: SD_ComboTipos, con el primer tipo elegido.</summary>
        Protected Overrides Async Sub OnLoad(e As EventArgs)
            MyBase.OnLoad(e)
            Try
                _cargando = True
                cmbTipo.Items.AddRange((Await _repo.TiposAsync()).ToArray())
                If cmbTipo.Items.Count > 0 Then cmbTipo.SelectedIndex = 0
            Catch ex As MySqlException
                Mensajes.NoConecta()
            Finally
                _cargando = False
            End Try
            Habilitar(False)
        End Sub

        Private ReadOnly Property TipoElegido As Integer?
            Get
                Return TryCast(cmbTipo.SelectedItem, TipoDenominacion)?.Tipo
            End Get
        End Property

        Private Sub Tipo_Cambio(sender As Object, e As EventArgs)
            If _cargando OrElse _editando Then Return
            Limpiar()
            Habilitar(False)
        End Sub

        Private Sub Pintar(imagen As Image)
            Dim anterior = picImagen.Image
            picImagen.Image = imagen
            anterior?.Dispose()
        End Sub

        Private Sub Limpiar()
            _actual = Nothing
            txtMoneda.Text = ""
            txtDescripcion.Text = ""
            Pintar(Nothing)
        End Sub

        Private Sub ElegirTipo(tipo As Integer)
            _cargando = True
            For i = 0 To cmbTipo.Items.Count - 1
                If DirectCast(cmbTipo.Items(i), TipoDenominacion).Tipo = tipo Then cmbTipo.SelectedIndex = i
            Next
            _cargando = False
        End Sub

        ''' <summary>Los datos y la imagen (POSLoadPictArticF).</summary>
        Private Async Function MostrarAsync(d As Denominacion) As Task
            Limpiar()
            If d Is Nothing Then Return
            _actual = d
            txtMoneda.Text = d.Codigo
            txtDescripcion.Text = d.Descripcion
            ElegirTipo(d.Tipo)
            Pintar(Imagenes.Crear(Await _repo.ImagenAsync(d.Moneda)))
        End Function

        Private Sub Habilitar(editando As Boolean)
            _editando = editando
            txtMoneda.Enabled = editando AndAlso _actual Is Nothing
            txtDescripcion.Enabled = editando
            btnAnterior.Enabled = Not editando AndAlso TipoElegido.HasValue
            btnSiguiente.Enabled = Not editando AndAlso TipoElegido.HasValue
            btnImagen.Enabled = Not editando AndAlso _actual IsNot Nothing
            barra.Estado(BotonBarra.Borrar, _actual IsNot Nothing)
        End Sub

        ''' <summary>SDCargando 1 y 2: en el extremo se apaga el botón de ese lado.</summary>
        Private Async Function MoverAsync(siguiente As Boolean) As Task
            If Not TipoElegido.HasValue Then Return
            Dim boton = If(siguiente, btnSiguiente, btnAnterior)
            Dim otro = If(siguiente, btnAnterior, btnSiguiente)
            otro.Enabled = True
            Try
                Dim desde = If(_actual IsNot Nothing, _actual.Moneda, If(siguiente, 0, Double.MaxValue))
                Dim d = Await _repo.VecinaAsync(TipoElegido.Value, desde, _local, siguiente)
                If d Is Nothing Then
                    boton.Enabled = False
                    Return
                End If
                Await MostrarAsync(d)
                btnImagen.Enabled = True
                barra.Estado(BotonBarra.Borrar, True)
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
        End Function

        Private Async Function Barra_Clic(boton As BotonBarra) As Task(Of Boolean)
            Try
                Select Case boton
                    Case BotonBarra.Nuevo
                        If Not TipoElegido.HasValue Then Return True
                        _antes = _actual
                        Limpiar()
                        Habilitar(True)
                        txtMoneda.Focus()
                    Case BotonBarra.Editar
                        If _actual Is Nothing Then Return True
                        _antes = _actual
                        Habilitar(True)
                        txtDescripcion.Focus()
                    Case BotonBarra.Guardar
                        If Not Await GuardarAsync() Then Return True
                        Habilitar(False)
                    Case BotonBarra.Cancelar
                        Await MostrarAsync(_antes)
                        Habilitar(False)
                    Case BotonBarra.Borrar
                        Await BorrarAsync()
                    Case BotonBarra.Buscar
                        Await BuscarAsync()
                    Case BotonBarra.Salir
                        Close()
                End Select
            Catch ex As MySqlException
                Mensajes.NoGuardo(ex.Message, "Datos de la moneda", Modulo.TituloModulo)
                Return True
            End Try
            Return False
        End Function

        ''' <summary>SDBoton_Guardar: el valor y la descripción son obligatorios.</summary>
        Private Async Function GuardarAsync() As Task(Of Boolean)
            Dim valor = If(_actual IsNot Nothing, CType(_actual.Moneda, Double?), DenominacionesRepositorio.Leer(txtMoneda.Text))
            If Not valor.HasValue Then
                txtMoneda.Focus()
                Return False
            End If
            If txtDescripcion.Text.Trim().Length = 0 OrElse Not TipoElegido.HasValue Then
                txtDescripcion.Focus()
                Return False
            End If
            If _actual Is Nothing Then
                Dim existe = Await _repo.ExisteAsync(valor.Value, _local)
                If existe IsNot Nothing Then
                    MessageBox.Show(Me, $"Ya existe la moneda {existe.Codigo} ({existe.Descripcion}) en el tipo {existe.Tipo}.", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
                    txtMoneda.Focus()
                    Return False
                End If
            End If
            Dim d As New Denominacion With {.Moneda = valor.Value, .Descripcion = txtDescripcion.Text, .Tipo = TipoElegido.Value}
            Await _repo.GuardarAsync(d, _uid, _local)
            Await MostrarAsync(If(Await _repo.CargarAsync(d.Tipo, d.Moneda, _local), d))
            Return True
        End Function

        Private Async Function BorrarAsync() As Task
            If _actual Is Nothing Then Return
            Dim pregunta = $"Está segur{If(_genero = "0", "o", "a")} de eliminar los datos actuales?"
            If MessageBox.Show(Me, pregunta, Modulo.TituloModulo, MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Return
            Await _repo.BorrarAsync(_actual.Moneda, _local)
            Limpiar()
            Habilitar(False)
        End Function

        Private Async Function BuscarAsync() As Task
            If Not TipoElegido.HasValue Then Return
            Limpiar()
            Using datos = Await _repo.BuscarAsync(TipoElegido.Value, _local)
                Using buscador As New FrmBuscador("Búsqueda de monedas", datos)
                    If buscador.ShowDialog(Me) = DialogResult.OK Then
                        Dim valor = DenominacionesRepositorio.Leer(buscador.Resultado(0))
                        If valor.HasValue Then Await MostrarAsync(Await _repo.CargarAsync(TipoElegido.Value, valor.Value, _local))
                    End If
                End Using
            End Using
            Habilitar(False)
        End Function

        ''' <summary>cmdBuscar: un jpg de menos de 256 KB, que se guarda enseguida.</summary>
        Private Async Function ImportarAsync() As Task
            If _actual Is Nothing Then Return
            Using dialogo As New OpenFileDialog With {.Title = "Seleccione la imagen a importar", .Filter = "Archivos de imágenes (jpg)|*.jpg"}
                If dialogo.ShowDialog(Me) <> DialogResult.OK Then Return
                Dim contenido = IO.File.ReadAllBytes(dialogo.FileName)
                If contenido.Length >= DenominacionesRepositorio.TamanoMaximoImagen Then
                    MessageBox.Show(Me, "El tamaño del archivo excede los 256 kb permitidos.", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Return
                End If
                Dim imagen = Imagenes.Crear(contenido)
                If imagen Is Nothing Then
                    MessageBox.Show(Me, "El archivo no es una imagen válida.", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Return
                End If
                Try
                    If Await _repo.GuardarImagenAsync(_actual.Moneda, _actual.Descripcion, contenido, _uid) Then
                        Pintar(imagen)
                    Else
                        imagen.Dispose()
                        MessageBox.Show(Me, $"{_actual.Codigo} es también el código de un artículo: la imagen reemplazaría la del artículo.", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
                    End If
                Catch ex As MySqlException
                    imagen.Dispose()
                    Mensajes.NoGuardo(ex.Message, "IMAGENES", Modulo.TituloModulo)
                End Try
            End Using
        End Function

    End Class

End Namespace
