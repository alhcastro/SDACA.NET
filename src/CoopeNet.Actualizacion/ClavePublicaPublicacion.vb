''' <summary>
''' La clave pública con que el lanzador verifica cada versión. La privada está cifrada en el perfil de quien
''' publica (herramientas\Publicar, comando claves). Si se cambia, los equipos rechazan las versiones firmadas
''' con la nueva hasta que tengan un lanzador con esta constante actualizada: se reparte con el instalador.
''' </summary>
Public NotInheritable Class ClavePublicaPublicacion

    Private Sub New()
    End Sub

    Public Const Pem As String =
        "-----BEGIN PUBLIC KEY-----" & vbLf &
        "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEkbBdgXuYKchQoywTUmpQpeXWaMng" & vbLf &
        "zOdFyxfqXNHCmDRzb+EaQzEMCNoPNUAX0dxrs3LaBr5yxOgsJeLkBjkKQQ==" & vbLf &
        "-----END PUBLIC KEY-----"

End Class
