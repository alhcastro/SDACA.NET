Imports MySqlConnector
Imports CoopeNet.Core.Pos
Imports CoopeNet.Ui

Namespace SD0502

    ''' <summary>
    ''' La imagen de un mensaje para clientes. Migración de SD0502\FrmImgsMens.frm: Importar la muestra y la deja
    ''' pendiente hasta Guardar o Cancelar. El combo de VB6 no tiene elementos y no se migra.
    ''' </summary>
    ''' <remarks>
    ''' VB6 abría con Borrar apagado aunque hubiera imagen; aquí se activa cuando la hay. Importar y Borrar piden el
    ''' 5000, como en VB6.
    ''' </remarks>
    Public Class FrmImagenMensaje
        Inherits Form

        Private ReadOnly _repo As MensajesClientesRepositorio
        Private ReadOnly _codigo As Long
        Private ReadOnly _local As Long
        Private ReadOnly _genero As String
        Private ReadOnly _puede As Boolean
        Private _pendiente As Byte()
        Private _hayImagen As Boolean

        Private ReadOnly gbMensaje As New GroupBox With {.Name = "gbMensaje", .Text = "Datos del mensaje", .Location = New Point(8, 4), .Size = New Size(744, 48), .Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right}
        Private ReadOnly lblMensaje As New Label With {.Name = "lblMensaje", .AutoSize = False, .AutoEllipsis = True, .Location = New Point(12, 20), .Size = New Size(720, 20), .Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right}
        Private ReadOnly btnBorrar As New Button With {.Name = "btnBorrar", .Text = "&Borrar", .Location = New Point(8, 60), .Size = New Size(96, 28)}
        Private ReadOnly btnImportar As New Button With {.Name = "btnImportar", .Text = "&Importar", .Location = New Point(352, 60), .Size = New Size(96, 28), .Anchor = AnchorStyles.Top Or AnchorStyles.Right}
        Private ReadOnly btnGuardar As New Button With {.Name = "btnGuardar", .Text = "&Guardar", .Location = New Point(452, 60), .Size = New Size(96, 28), .Anchor = AnchorStyles.Top Or AnchorStyles.Right}
        Private ReadOnly btnCancelar As New Button With {.Name = "btnCancelar", .Text = "&Cancelar", .Location = New Point(552, 60), .Size = New Size(96, 28), .Anchor = AnchorStyles.Top Or AnchorStyles.Right}
        Private ReadOnly btnSalir As New Button With {.Name = "btnSalir", .Text = "&Salir", .Location = New Point(656, 60), .Size = New Size(96, 28), .Anchor = AnchorStyles.Top Or AnchorStyles.Right}
        Private ReadOnly picImagen As New PictureBox With {
            .Name = "picImagen", .AccessibleName = "Imagen del mensaje", .BorderStyle = BorderStyle.FixedSingle, .SizeMode = PictureBoxSizeMode.Zoom,
            .Location = New Point(8, 96), .Size = New Size(744, 456), .Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right}

        Public Sub New(repo As MensajesClientesRepositorio, codigo As Long, titulo As String, local As Long, genero As String, puede As Boolean)
            _repo = repo
            _codigo = codigo
            _local = local
            _genero = genero
            _puede = puede
            Text = "Imágenes"
            ClientSize = New Size(760, 560)
            ShowInTaskbar = False
            MinimizeBox = False
            StartPosition = FormStartPosition.CenterParent
            CancelButton = btnSalir
            lblMensaje.Text = $"{codigo}  -  {titulo}"

            gbMensaje.Controls.Add(lblMensaje)
            Controls.AddRange(New Control() {gbMensaje, btnBorrar, btnImportar, btnGuardar, btnCancelar, btnSalir, picImagen})
            Ventanas.Recordar(Me)

            AddHandler btnImportar.Click, AddressOf Importar_Clic
            AddHandler btnGuardar.Click, Async Sub() Await GuardarAsync()
            AddHandler btnCancelar.Click, Async Sub() Await CancelarAsync()
            AddHandler btnBorrar.Click, Async Sub() Await BorrarAsync()
            AddHandler btnSalir.Click, Sub() Close()
            AddHandler FormClosed, Sub() Pintar(Nothing)
            Botones()
        End Sub

        Protected Overrides Async Sub OnLoad(e As EventArgs)
            MyBase.OnLoad(e)
            Await CargarAsync()
        End Sub

        ''' <summary>Con una imagen pendiente no se sale sin decidir: Salir se apaga, como en VB6.</summary>
        Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
            If _pendiente IsNot Nothing AndAlso e.CloseReason = CloseReason.UserClosing Then e.Cancel = True
            MyBase.OnFormClosing(e)
        End Sub

        Private Sub Pintar(imagen As Image)
            Dim anterior = picImagen.Image
            picImagen.Image = imagen
            anterior?.Dispose()
        End Sub

        Private Sub Botones()
            Dim pendiente = _pendiente IsNot Nothing
            btnImportar.Enabled = _puede AndAlso Not pendiente
            btnGuardar.Enabled = pendiente
            btnCancelar.Enabled = pendiente
            btnSalir.Enabled = Not pendiente
            btnBorrar.Enabled = _puede AndAlso Not pendiente AndAlso _hayImagen
        End Sub

        Private Async Function CargarAsync() As Task
            Try
                Dim imagen = Imagenes.Crear(Await _repo.ImagenAsync(_codigo, _local))
                _hayImagen = imagen IsNot Nothing
                Pintar(imagen)
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
            Botones()
        End Function

        ''' <summary>cmdAbrir: solo jpg de menos de 256 KB.</summary>
        Private Sub Importar_Clic(sender As Object, e As EventArgs)
            Using dialogo As New OpenFileDialog With {.Title = "Seleccione la imagen a importar", .Filter = "Archivos de imágenes (jpg)|*.jpg"}
                If dialogo.ShowDialog(Me) <> DialogResult.OK Then Return
                Dim contenido = IO.File.ReadAllBytes(dialogo.FileName)
                If contenido.Length >= MensajesClientesRepositorio.TamanoMaximoImagen Then
                    MessageBox.Show(Me, "El tamaño del archivo excede los 256 kb permitidos.", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Return
                End If
                Dim imagen = Imagenes.Crear(contenido)
                If imagen Is Nothing Then
                    MessageBox.Show(Me, "El archivo no es una imagen válida.", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Return
                End If
                Pintar(imagen)
                _pendiente = contenido
                Botones()
            End Using
        End Sub

        Private Async Function GuardarAsync() As Task
            If _pendiente Is Nothing Then Return
            Try
                Await _repo.GuardarImagenAsync(_codigo, _local, _pendiente)
                _pendiente = Nothing
                _hayImagen = True
            Catch ex As MySqlException
                Mensajes.NoGuardo(ex.Message, "IMAGENES", Modulo.TituloModulo)
            End Try
            Botones()
        End Function

        Private Async Function CancelarAsync() As Task
            _pendiente = Nothing
            Await CargarAsync()
        End Function

        Private Async Function BorrarAsync() As Task
            Dim pregunta = $"Está segur{If(_genero = "0", "o", "a")} que desea eliminar la imagen?"
            If MessageBox.Show(Me, pregunta, Modulo.TituloModulo, MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Return
            Try
                Await _repo.BorrarImagenAsync(_codigo, _local)
                _hayImagen = False
                Pintar(Nothing)
            Catch ex As MySqlException
                Mensajes.NoGuardo(ex.Message, "IMAGENES", Modulo.TituloModulo)
            End Try
            Botones()
        End Function

    End Class

End Namespace
