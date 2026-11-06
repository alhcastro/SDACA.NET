Imports CoopeNet.Core.Configuracion

Public Class ConfiguracionTests

    Private Const Ini As String =
        "[CP.LOCAL]" & vbCrLf & "CATALOGO=sdprincipal" & vbCrLf & "PUERTO= 3306" & vbCrLf & "SERVIDOR=130.0.0.2" & vbCrLf &
        "USUARIO=Administrador" & vbCrLf & "USUARIO0=root" & vbCrLf & "USUARIO1=mrodriguez" & vbCrLf & "USUARIO2=Administrador" & vbCrLf & "USER::ROOT= 0" & vbCrLf &
        "[GENERAL]" & vbCrLf & "DEFAULT=CP DB SERVER PRUEBAS" & vbCrLf & "CNN=Provider=MSDASQL.1;Password=secreto" & vbCrLf &
        "[CP DB SERVER PRUEBAS]" & vbCrLf & "CATALOGO=pruebas" & vbCrLf & "PUERTO=" & vbCrLf & "SERVIDOR=130.0.0.2" & vbCrLf

    <Fact>
    Public Sub Lee_perfiles_y_omite_GENERAL()
        Dim p = ConexionesIni.Parsear(Ini)
        Assert.Equal({"CP.LOCAL", "CP DB SERVER PRUEBAS"}, p.Select(Function(x) x.Nombre).ToArray())
        Assert.Equal("sdprincipal", p(0).Catalogo)
        Assert.Equal(3306UI, p(0).Puerto)
        Assert.Equal("Administrador", p(0).UltimoUsuario)
        Assert.Equal({"root", "mrodriguez", "Administrador"}, p(0).Usuarios.ToArray())
        Assert.Equal(3306UI, p(1).Puerto)
    End Sub

    <Fact>
    <System.Runtime.Versioning.SupportedOSPlatform("windows")>
    Public Sub Secretos_ida_y_vuelta()
        Dim cifrado = Secretos.Proteger("Café-2024")
        Assert.StartsWith("dpapi:", cifrado)
        Assert.DoesNotContain("Café", cifrado)
        Assert.Equal("Café-2024", Secretos.Desproteger(cifrado))
    End Sub

    <Theory>
    <InlineData("12abc", 12L)>
    <InlineData("abc", 0L)>
    <InlineData(" 3 4", 34L)>
    <InlineData("-7", -7L)>
    <InlineData("12.6", 13L)>
    <InlineData("&H1F", 31L)>
    <InlineData("", 0L)>
    <InlineData(Nothing, 0L)>
    Public Sub Val_como_CLng_Val_de_vb6(texto As String, esperado As Long)
        Assert.Equal(esperado, Vb6.Val(texto))
    End Sub

End Class
