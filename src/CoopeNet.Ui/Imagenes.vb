Imports System.Drawing
Imports System.IO

''' <summary>Imágenes guardadas como blob en la base (gatimagenes, posimagartic).</summary>
Public Module Imagenes

    ''' <summary>La imagen guardada, o Nothing si no hay o no se puede leer. Quien la recibe la libera.</summary>
    Public Function Crear(contenido As Byte()) As Image
        If contenido Is Nothing OrElse contenido.Length = 0 Then Return Nothing
        Try
            Using flujo As New MemoryStream(contenido), original = Image.FromStream(flujo)
                Return New Bitmap(original)
            End Using
        Catch ex As ArgumentException
            Return Nothing
        End Try
    End Function

End Module
