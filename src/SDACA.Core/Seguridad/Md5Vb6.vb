Imports System.Security.Cryptography
Imports System.Text

Namespace Seguridad

    ''' <summary>
    ''' MD5 idéntico al de VB6 (<c>Comun\SD_CLS_MD5.cls</c> → <c>NEWMD5</c>).
    ''' </summary>
    ''' <remarks>
    ''' El original toma el byte bajo de cada carácter UTF-16 (<c>AscB(Mid(s, i, 1))</c>)
    ''' y devuelve hexadecimal en minúsculas. Con eso se calculan las contraseñas de MySQL de
    ''' cada usuario, la clave que recibe <c>Init()</c> de cada módulo y los hashes de
    ''' <c>gatusuariospriv2</c>. Si el resultado cambia en un solo carácter, el usuario ya no
    ''' puede entrar, así que este método no debe "corregirse" a UTF-8.
    ''' </remarks>
    Public NotInheritable Class Md5Vb6

        Private Sub New()
        End Sub

        Public Shared Function Calcular(texto As String) As String
            If texto Is Nothing Then texto = String.Empty
            Dim bytes(texto.Length - 1) As Byte
            For i = 0 To texto.Length - 1
                bytes(i) = CByte(AscW(texto(i)) And &HFF)
            Next

            Using algoritmo = MD5.Create()
                Dim hash = algoritmo.ComputeHash(bytes)
                Dim sb As New StringBuilder(32)
                For Each b In hash
                    sb.Append(b.ToString("x2"))
                Next
                Return sb.ToString()
            End Using
        End Function

    End Class

End Namespace
