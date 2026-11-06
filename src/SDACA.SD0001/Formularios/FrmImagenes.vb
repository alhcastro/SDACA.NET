Imports System.IO
Imports MySqlConnector
Imports SDACA.Core.Catalogos
Imports SDACA.Ui

Namespace SD0001

    ''' <summary>
    ''' Imágenes de una persona. Migración de Fuentes\SD0001\FrmImgsCliente.frm: identificación,
    ''' fotografía y firma en gatimagenes, sin replicar. Abrir un archivo lo muestra y Guardar lo graba.
    ''' </summary>
    Public Class FrmImagenes
        Inherits Form

        Private Const TamanoMaximo As Integer = 262144

        Private ReadOnly _repo As DatosPersonalesRepositorio
        Private ReadOnly _id As String
        Private ReadOnly _puedeModificar As Boolean
        Private _pendiente As Byte()

        Private ReadOnly lblPersona As New Label With {.Name = "lblPersona", .AutoSize = False, .Location = New Point(12, 12), .Size = New Size(536, 20)}
        Private ReadOnly cmbImagen As New ComboBox With {.Name = "cmbImagen", .AccessibleName = "Tipo de imagen", .DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(12, 38), .Width = 220}
        Private ReadOnly picImagen As New PictureBox With {.Name = "picImagen", .AccessibleName = "Imagen", .BorderStyle = BorderStyle.FixedSingle, .SizeMode = PictureBoxSizeMode.Zoom, .Location = New Point(12, 72), .Size = New Size(536, 340)}
        Private ReadOnly btnAbrir As New Button With {.Name = "btnAbrir", .Text = "Abrir", .Location = New Point(12, 422), .Size = New Size(84, 30)}
        Private ReadOnly btnGuardar As New Button With {.Name = "btnGuardar", .Text = "Guardar", .Location = New Point(102, 422), .Size = New Size(84, 30)}
        Private ReadOnly btnCancelar As New Button With {.Name = "btnCancelar", .Text = "Cancelar", .Location = New Point(192, 422), .Size = New Size(84, 30)}
        Private ReadOnly btnBorrar As New Button With {.Name = "btnBorrar", .Text = "Borrar", .Location = New Point(282, 422), .Size = New Size(84, 30)}
        Private ReadOnly btnSalir As New Button With {.Name = "btnSalir", .Text = "Salir", .DialogResult = DialogResult.Cancel, .Location = New Point(464, 422), .Size = New Size(84, 30)}

        Public Sub New(repositorio As DatosPersonalesRepositorio, id As String, nombre As String, puedeModificar As Boolean)
            _repo = repositorio
            _id = id
            _puedeModificar = puedeModificar
            Text = "Imágenes"
            ClientSize = New Size(560, 464)
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent
            CancelButton = btnSalir
            lblPersona.Text = $"{id}  -  {nombre}"
            cmbImagen.Items.AddRange({"Identificación", "Fotografía", "Firma"})
            Controls.AddRange(New Control() {lblPersona, cmbImagen, picImagen, btnAbrir, btnGuardar, btnCancelar, btnBorrar, btnSalir})
            AddHandler Load, Sub() cmbImagen.SelectedIndex = CInt(TipoImagen.Identificacion)
            AddHandler FormClosed, Sub() Pintar(Nothing)
            AddHandler cmbImagen.SelectedIndexChanged, AddressOf Imagen_Cambio
            AddHandler btnAbrir.Click, AddressOf Abrir_Clic
            AddHandler btnGuardar.Click, AddressOf Guardar_Clic
            AddHandler btnCancelar.Click, AddressOf Imagen_Cambio
            AddHandler btnBorrar.Click, AddressOf Borrar_Clic
        End Sub

        ''' <summary>La imagen guardada, o Nothing si no hay o no se puede leer. Quien la recibe la libera.</summary>
        Friend Shared Function CrearImagen(contenido As Byte()) As Image
            If contenido Is Nothing OrElse contenido.Length = 0 Then Return Nothing
            Try
                Using flujo As New MemoryStream(contenido), original = Image.FromStream(flujo)
                    Return New Bitmap(original)
                End Using
            Catch ex As ArgumentException
                Return Nothing
            End Try
        End Function

        Private ReadOnly Property Tipo As TipoImagen
            Get
                Return CType(cmbImagen.SelectedIndex, TipoImagen)
            End Get
        End Property

        Private Sub Pintar(imagen As Image)
            Dim anterior = picImagen.Image
            picImagen.Image = imagen
            anterior?.Dispose()
        End Sub

        Private Sub Botones()
            Dim pendiente = _pendiente IsNot Nothing
            cmbImagen.Enabled = Not pendiente
            btnSalir.Enabled = Not pendiente
            btnAbrir.Enabled = _puedeModificar AndAlso Not pendiente
            btnGuardar.Enabled = pendiente
            btnCancelar.Enabled = pendiente
            btnBorrar.Enabled = _puedeModificar AndAlso Not pendiente AndAlso picImagen.Image IsNot Nothing
        End Sub

        Private Async Sub Imagen_Cambio(sender As Object, e As EventArgs)
            _pendiente = Nothing
            Try
                Pintar(CrearImagen(Await _repo.ObtenerImagenAsync(_id, Tipo)))
            Catch ex As MySqlException
                Mensajes.NoConecta()
            End Try
            Botones()
        End Sub

        Private Sub Abrir_Clic(sender As Object, e As EventArgs)
            Using dialogo As New OpenFileDialog With {.Title = "Seleccione la imagen a importar", .Filter = "Archivos de imágenes (.jpg)|*.jpg"}
                If dialogo.ShowDialog(Me) <> DialogResult.OK Then Return
                Dim contenido = File.ReadAllBytes(dialogo.FileName)
                If contenido.Length >= TamanoMaximo Then
                    MessageBox.Show(Me, "El tamaño del archivo excede los 256 kb permitidos.", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Return
                End If
                Dim imagen = CrearImagen(contenido)
                If imagen Is Nothing Then
                    MessageBox.Show(Me, "El archivo no es una imagen válida.", Modulo.TituloModulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Return
                End If
                _pendiente = contenido
                Pintar(imagen)
                Botones()
            End Using
        End Sub

        Private Async Sub Guardar_Clic(sender As Object, e As EventArgs)
            Try
                Await _repo.GuardarImagenAsync(_id, Tipo, _pendiente)
                _pendiente = Nothing
                Botones()
            Catch ex As MySqlException
                Mensajes.NoGuardo(ex.Message, "IMAGENES", Modulo.TituloModulo)
            End Try
        End Sub

        Private Async Sub Borrar_Clic(sender As Object, e As EventArgs)
            If MessageBox.Show(Me, "¿Está seguro de que desea eliminar la imagen?", Modulo.TituloModulo, MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Return
            Try
                Await _repo.BorrarImagenAsync(_id, Tipo)
                Pintar(Nothing)
                Botones()
            Catch ex As MySqlException
                Mensajes.NoGuardo(ex.Message, "IMAGENES", Modulo.TituloModulo)
            End Try
        End Sub

    End Class

End Namespace
