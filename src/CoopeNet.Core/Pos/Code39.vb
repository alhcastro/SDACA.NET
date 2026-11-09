Namespace Pos

    ''' <summary>
    ''' Código de barras Code 39, el que VB6 imprimía con la fuente "Código de Barras 3/9" escribiendo *código*.
    ''' Aquí se calculan las barras para dibujarlas sin depender de que la fuente esté instalada.
    ''' </summary>
    Public NotInheritable Class Code39

        Private Sub New()
        End Sub

        Private Const Caracteres As String = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ-. $/+%*"

        ' Nueve elementos por carácter (barra, espacio, barra...): 1 = ancho, 0 = angosto.
        Private Shared ReadOnly Patrones As String() = {
            "000110100", "100100001", "001100001", "101100000", "000110001", "100110000", "001110000", "000100101", "100100100", "001100100",
            "100001001", "001001001", "101001000", "000011001", "100011000", "001011000", "000001101", "100001100", "001001100", "000011100",
            "100000011", "001000011", "101000010", "000010011", "100010010", "001010010", "000000111", "100000110", "001000110", "000010110",
            "110000001", "011000001", "111000000", "010010001", "110010000", "011010000", "010000101", "110000100", "011000100",
            "010101000", "010100010", "010001010", "000101010", "010010100"}

        ''' <summary>True si el texto se puede codificar (mayúsculas, dígitos y - . espacio $ / + %).</summary>
        Public Shared Function Admite(texto As String) As Boolean
            Return Not String.IsNullOrEmpty(texto) AndAlso texto.All(Function(c) c <> "*"c AndAlso Caracteres.IndexOf(c) >= 0)
        End Function

        ''' <summary>
        ''' Los anchos de los elementos, alternando barra y espacio y empezando por barra, en módulos angostos (1 o
        ''' <paramref name="ancho"/>), con los asteriscos de inicio y fin y un espacio angosto entre caracteres.
        ''' </summary>
        Public Shared Function Elementos(texto As String, Optional ancho As Integer = 3) As List(Of Integer)
            If Not Admite(texto) Then Throw New ArgumentException("El texto tiene caracteres que Code 39 no admite.", NameOf(texto))
            Dim lista As New List(Of Integer)
            Dim completo = "*" & texto & "*"
            For i = 0 To completo.Length - 1
                If i > 0 Then lista.Add(1)
                For Each e In Patrones(Caracteres.IndexOf(completo(i)))
                    lista.Add(If(e = "1"c, ancho, 1))
                Next
            Next
            Return lista
        End Function

    End Class

End Namespace
