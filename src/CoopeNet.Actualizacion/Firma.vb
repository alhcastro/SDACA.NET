Imports System.Security.Cryptography
Imports System.Text

''' <summary>
''' Firma ECDSA P-256 del manifiesto. La clave privada solo la tiene quien publica (herramientas\Publicar); el
''' lanzador trae la pública en <see cref="ClavePublicaPublicacion"/>. Así, quien tenga el usuario sdaca puede
''' escribir en la base, pero no puede hacer que los equipos instalen una versión que no se firmó aquí.
''' </summary>
Public NotInheritable Class Firma

    Private Sub New()
    End Sub

    ''' <summary>Crea un par de claves. Devuelve la privada y la pública en PEM.</summary>
    Public Shared Function CrearClaves() As (Privada As String, Publica As String)
        Using ec = ECDsa.Create(ECCurve.NamedCurves.nistP256)
            Return (ec.ExportPkcs8PrivateKeyPem(), ec.ExportSubjectPublicKeyInfoPem())
        End Using
    End Function

    Public Shared Function PublicaDe(privadaPem As String) As String
        Using ec = ECDsa.Create()
            ec.ImportFromPem(privadaPem)
            Return ec.ExportSubjectPublicKeyInfoPem()
        End Using
    End Function

    Public Shared Function Firmar(texto As String, privadaPem As String) As String
        Using ec = ECDsa.Create()
            ec.ImportFromPem(privadaPem)
            Return Convert.ToBase64String(ec.SignData(Encoding.UTF8.GetBytes(texto), HashAlgorithmName.SHA256))
        End Using
    End Function

    Public Shared Function Verificar(texto As String, firmaBase64 As String, publicaPem As String) As Boolean
        If String.IsNullOrEmpty(texto) OrElse String.IsNullOrEmpty(firmaBase64) Then Return False
        Dim bytesFirma As Byte()
        Try
            bytesFirma = Convert.FromBase64String(firmaBase64)
        Catch ex As FormatException
            Return False
        End Try
        Using ec = ECDsa.Create()
            ec.ImportFromPem(publicaPem)
            Return ec.VerifyData(Encoding.UTF8.GetBytes(texto), bytesFirma, HashAlgorithmName.SHA256)
        End Using
    End Function

End Class
