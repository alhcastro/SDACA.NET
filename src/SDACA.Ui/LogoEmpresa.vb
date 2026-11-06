Imports System.Drawing.Drawing2D
Imports System.Reflection

''' <summary>
''' Logo de la empresa sobre fondo blanco, porque logo.jpg no tiene transparencia. Se dibuja aquí y no
''' en un PictureBox para reducirlo con buena calidad a cualquier DPI, sin los márgenes del archivo.
''' </summary>
Public NotInheritable Class LogoEmpresa
    Inherits Control

    Private Const Recurso As String = "SDACA.Ui.logo.jpg"
    Private Shared ReadOnly Contenido As New Rectangle(24, 194, 2112, 332)
    Private Shared ReadOnly Imagen As New Lazy(Of Image)(AddressOf Cargar)

    Public Sub New()
        SetStyle(ControlStyles.OptimizedDoubleBuffer Or ControlStyles.AllPaintingInWmPaint Or ControlStyles.ResizeRedraw Or ControlStyles.UserPaint, True)
        SetStyle(ControlStyles.Selectable, False)
        TabStop = False
        BackColor = Color.White
        AccessibleName = "Logo de la empresa"
        AccessibleRole = AccessibleRole.Graphic
    End Sub

    Private Shared Function Cargar() As Image
        Using flujo = Assembly.GetExecutingAssembly().GetManifestResourceStream(Recurso)
            Return New Bitmap(flujo)
        End Using
    End Function

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        MyBase.OnPaint(e)
        Dim area = New Rectangle(Padding.Left, Padding.Top, ClientSize.Width - Padding.Horizontal, ClientSize.Height - Padding.Vertical)
        If area.Width <= 0 OrElse area.Height <= 0 Then Return
        Dibujar(e.Graphics, area)
    End Sub

    ''' <summary>Dibuja el logo centrado en el área, sin deformarlo. También lo usan los reportes.</summary>
    Public Shared Sub Dibujar(grafico As Graphics, area As Rectangle)
        Dim escala = Math.Min(area.Width / Contenido.Width, area.Height / Contenido.Height)
        Dim ancho = CInt(Contenido.Width * escala)
        Dim alto = CInt(Contenido.Height * escala)
        Dim destino As New Rectangle(area.X + (area.Width - ancho) \ 2, area.Y + (area.Height - alto) \ 2, ancho, alto)
        grafico.InterpolationMode = InterpolationMode.HighQualityBicubic
        grafico.PixelOffsetMode = PixelOffsetMode.HighQuality
        grafico.DrawImage(Imagen.Value, destino, Contenido, GraphicsUnit.Pixel)
    End Sub

End Class
