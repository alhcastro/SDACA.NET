Imports System.Drawing

''' <summary>
''' Reemplaza a SDSizer.ocx (ResizeControls): al agrandar la ventana, cada control crece y se mueve en
''' proporción, y su letra crece con el menor de los dos factores para no deformarse. Las medidas se toman
''' una vez, con el tamaño de diseño ya escalado por <see cref="Tema.Aplicar"/>; después siempre se calcula
''' desde ellas, así que no se acumula redondeo.
''' </summary>
''' <remarks>
''' Para pantallas sin listas, como el verificador de presentación, donde lo que importa es ver más grande.
''' Las pantallas con rejillas usan anclajes. No entra en los controles de usuario (la barra de comandos
''' acomoda sus botones sola).
''' </remarks>
Public NotInheritable Class Proporcional

    Private NotInheritable Class Medida
        Public Limites As Rectangle
        Public Letra As Font
    End Class

    Private ReadOnly _contenedor As Control
    Private ReadOnly _base As Size
    Private ReadOnly _medidas As New Dictionary(Of Control, Medida)
    Private _escala As Single = 1.0F

    Private Sub New(contenedor As Control)
        _contenedor = contenedor
        _base = contenedor.ClientSize
        Registrar(contenedor)
        AddHandler contenedor.Resize, Sub() Escalar()
    End Sub

    ''' <summary>
    ''' Empieza a escalar lo que hay dentro de <paramref name="contenedor"/> desde su tamaño actual. Llamarlo
    ''' al cargar la ventana, antes de que se restaure su último tamaño.
    ''' </summary>
    Public Shared Sub Aplicar(contenedor As Control)
        Dim p As New Proporcional(contenedor)
    End Sub

    Private Sub Registrar(padre As Control)
        For Each c In padre.Controls.Cast(Of Control)()
            _medidas(c) = New Medida With {.Limites = c.Bounds, .Letra = c.Font}
            If Not TypeOf c Is UserControl Then Registrar(c)
        Next
    End Sub

    Private Sub Escalar()
        Dim actual = _contenedor.ClientSize
        If actual.Width = 0 OrElse actual.Height = 0 OrElse _base.Width = 0 OrElse _base.Height = 0 Then Return
        Dim fx = actual.Width / CSng(_base.Width)
        Dim fy = actual.Height / CSng(_base.Height)
        Dim escala = Math.Min(fx, fy)
        Dim cambiaLetra = Math.Abs(escala - _escala) > 0.01F
        _contenedor.SuspendLayout()
        Try
            For Each par In _medidas
                Dim c = par.Key
                Dim m = par.Value
                If cambiaLetra Then
                    c.Font = If(Math.Abs(escala - 1.0F) < 0.01F, m.Letra, New Font(m.Letra.FontFamily, m.Letra.Size * escala, m.Letra.Style, m.Letra.Unit))
                End If
                c.Bounds = New Rectangle(CInt(m.Limites.X * fx), CInt(m.Limites.Y * fy), CInt(m.Limites.Width * fx), CInt(m.Limites.Height * fy))
            Next
            If cambiaLetra Then _escala = escala
        Finally
            _contenedor.ResumeLayout(True)
        End Try
    End Sub

End Class
