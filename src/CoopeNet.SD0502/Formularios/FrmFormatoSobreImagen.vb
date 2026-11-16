Imports System.Globalization
Imports CoopeNet.Core.Configuracion
Imports CoopeNet.Core.Pos
Imports CoopeNet.Ui

Namespace SD0502

    ''' <summary>
    ''' El texto de la factura sobre la imagen de la factura preimpresa, para cuadrar el formato. Migración de
    ''' SD0502\FACPOS\frmConfFact.frm: la imagen se elige con el botón "-" (V_FORMAT_F de CONFIG.INI) y el tamaño de la
    ''' ventana se recuerda (V_FORMAT_W y V_FORMAT_H, en twips como VB6); el título muestra el tamaño en centímetros.
    ''' </summary>
    Public Class FrmFormatoSobreImagen
        Inherits Form

        Private Const TwipsPorPixel As Integer = 15

        Private ReadOnly _configIni As String
        Private ReadOnly picFondo As New PictureBox With {.Name = "picFondo", .Dock = DockStyle.Fill, .SizeMode = PictureBoxSizeMode.StretchImage, .BackColor = Color.White}
        Private ReadOnly lblTexto As New Label With {.Name = "lblTexto", .AutoSize = True, .BackColor = Color.Transparent, .ForeColor = Color.Black, .Font = New Font("Courier New", 9.75F), .Location = New Point(7, 25)}
        Private ReadOnly btnImagen As New Button With {.Name = "btnImagen", .Text = "-", .AccessibleName = "Elegir la imagen de la factura", .Location = New Point(0, 0), .Size = New Size(18, 16), .FlatStyle = FlatStyle.System}

        Public Sub New(texto As String, configIni As String)
            _configIni = configIni
            ShowInTaskbar = False
            StartPosition = FormStartPosition.CenterParent
            lblTexto.Text = ImpresionFormato.ParaPantalla(texto) & vbCrLf & "____"
            picFondo.Controls.Add(lblTexto)
            picFondo.Controls.Add(btnImagen)
            Controls.Add(picFondo)
            Dim ancho = CInt(Core.Vb6.Val(ConexionesIni.LeerValor(configIni, "POS", "V_FORMAT_W")))
            Dim alto = CInt(Core.Vb6.Val(ConexionesIni.LeerValor(configIni, "POS", "V_FORMAT_H")))
            Size = New Size(If(ancho > 0, ancho, 10000) \ TwipsPorPixel, If(alto > 0, alto, 10000) \ TwipsPorPixel)
            CargarImagen(ConexionesIni.LeerValor(configIni, "POS", "V_FORMAT_F"))
            AddHandler btnImagen.Click, AddressOf Imagen_Clic
            AddHandler Resize, Sub() Titulo()
            AddHandler FormClosed, AddressOf Recordar
            Titulo()
        End Sub

        ''' <summary>sd_Tam: VB6 medía el ancho con 567 twips por centímetro y el alto con 460.</summary>
        Private Sub Titulo()
            Dim twipsAncho = Width * TwipsPorPixel
            Dim twipsAlto = Height * TwipsPorPixel
            Text = $"ANCHO: {(twipsAncho / 567.0).ToString("0.00", CultureInfo.InvariantCulture)}      ALTO: {((twipsAlto - 500) / 460.0).ToString("0.00", CultureInfo.InvariantCulture)}       CENTIMETROS"
        End Sub

        Private Sub CargarImagen(ruta As String)
            If String.IsNullOrEmpty(ruta) OrElse Not IO.File.Exists(ruta) Then Return
            Try
                Dim anterior = picFondo.Image
                picFondo.Image = Imagenes.Crear(IO.File.ReadAllBytes(ruta))
                anterior?.Dispose()
            Catch ex As Exception When TypeOf ex Is IO.IOException OrElse TypeOf ex Is UnauthorizedAccessException
                ' Sin la imagen se ve solo el texto.
            End Try
        End Sub

        Private Sub Imagen_Clic(sender As Object, e As EventArgs)
            Using dialogo As New OpenFileDialog With {.Title = "Seleccione la imagen de la factura...", .Filter = "BMP|*.bmp|JPG|*.jpg", .InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}
                If dialogo.ShowDialog(Me) <> DialogResult.OK Then Return
                Guardar("V_FORMAT_F", dialogo.FileName)
                CargarImagen(dialogo.FileName)
            End Using
        End Sub

        Private Sub Recordar(sender As Object, e As FormClosedEventArgs)
            Guardar("V_FORMAT_W", (Width * TwipsPorPixel).ToString(CultureInfo.InvariantCulture))
            Guardar("V_FORMAT_H", (Height * TwipsPorPixel).ToString(CultureInfo.InvariantCulture))
            picFondo.Image?.Dispose()
        End Sub

        Private Sub Guardar(llave As String, valor As String)
            Try
                ConexionesIni.FijarValor(_configIni, "POS", llave, valor)
            Catch ex As Exception When TypeOf ex Is IO.IOException OrElse TypeOf ex Is UnauthorizedAccessException
                ' Sin permiso sobre CONFIG.INI solo se pierde el recuerdo.
            End Try
        End Sub

    End Class

End Namespace
