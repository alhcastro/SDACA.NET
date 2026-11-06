''' <summary>Funciones de VB6 que el código de negocio usa a menudo y que hay que reproducir igual.</summary>
''' <remarks>
''' Es una clase y no un Module a propósito: así sus funciones no tapan a las de
''' Microsoft.VisualBasic (Val, Mid...) en el resto del proyecto.
''' </remarks>
Public NotInheritable Class Vb6

    Private Sub New()
    End Sub

    ''' <summary>
    ''' Igual que <c>CLng(Val(texto))</c> de VB6, como en <c>SD_CLS_CNN.Get_Parametro_Lng</c>.
    ''' "12abc" → 12, "abc" → 0, " 3 4" → 34, "12.6" → 13, "&amp;H1F" → 31.
    ''' Devuelve 0 si el número no cabe en un Long, en lugar del error 6 (desbordamiento) de VB6.
    ''' </summary>
    Public Shared Function Val(texto As String) As Long
        If String.IsNullOrEmpty(texto) Then Return 0
        Dim numero = Conversion.Val(texto)
        If numero > Long.MaxValue OrElse numero < Long.MinValue Then Return 0
        Return CLng(numero)
    End Function

End Class
