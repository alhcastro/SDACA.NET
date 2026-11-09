Imports System.Drawing
Imports CoopeNet.Core.Pos

''' <summary>Dibuja códigos de barras en impresiones y vistas previas.</summary>
Public Module DibujoBarras

    ''' <summary>
    ''' Code 39 con sus asteriscos, estirado al ancho del área y centrado. Devuelve False sin dibujar si el texto
    ''' tiene caracteres que Code 39 no admite.
    ''' </summary>
    Public Function Dibujar39(g As Graphics, area As RectangleF, texto As String) As Boolean
        If Not Code39.Admite(texto) Then Return False
        Dim elementos = Code39.Elementos(texto)
        Dim modulos = elementos.Sum()
        Dim modulo = area.Width / modulos
        Dim x = area.Left
        For i = 0 To elementos.Count - 1
            Dim ancho = elementos(i) * modulo
            If i Mod 2 = 0 Then g.FillRectangle(Brushes.Black, x, area.Top, ancho, area.Height)
            x += ancho
        Next
        Return True
    End Function

End Module
