Imports System.IO
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

    <Fact>
    Public Sub ConValor_cambia_la_llave_y_respeta_el_resto()
        Dim texto = ConexionesIni.ConValor(Ini, "cp.local", "Puerto", "3312")
        Assert.Contains("PUERTO=3312" & vbCrLf & "SERVIDOR=130.0.0.2", texto)
        Assert.Contains("USER::ROOT= 0", texto)
        Assert.Contains("CNN=Provider=MSDASQL.1;Password=secreto", texto)
        Assert.Equal(Ini.Replace("PUERTO= 3306", "PUERTO=3312"), texto)
    End Sub

    <Fact>
    Public Sub ConValor_agrega_al_final_de_la_seccion_o_del_archivo()
        Dim texto = ConexionesIni.ConValor(Ini, "GENERAL", "USUARIO", "alvaro")
        Assert.Contains("CNN=Provider=MSDASQL.1;Password=secreto" & vbCrLf & "USUARIO=alvaro" & vbCrLf & "[CP DB SERVER PRUEBAS]", texto)
        texto = ConexionesIni.ConValor(texto, "NUEVA", "SERVIDOR", "10.0.0.1")
        Assert.EndsWith("SERVIDOR=130.0.0.2" & vbCrLf & "[NUEVA]" & vbCrLf & "SERVIDOR=10.0.0.1" & vbCrLf, texto)
        Assert.Equal("[A]" & vbCrLf & "X=1" & vbCrLf, ConexionesIni.ConValor("", "A", "X", "1"))
    End Sub

    <Fact>
    Public Sub SinSeccion_quita_solo_esa_seccion()
        Dim texto = ConexionesIni.SinSeccion(Ini, "CP.LOCAL")
        Assert.Equal({"GENERAL", "CP DB SERVER PRUEBAS"}, ConexionesIni.Secciones(texto).ToArray())
        Assert.StartsWith("[GENERAL]", texto)
        Assert.Equal(Ini, ConexionesIni.SinSeccion(Ini, "NO EXISTE"))
    End Sub

    <Fact>
    Public Sub Guardar_y_eliminar_perfil_en_archivo()
        Dim ruta = Path.Combine(Path.GetTempPath(), $"sdcnn-{Guid.NewGuid():N}.ini")
        Try
            File.WriteAllText(ruta, Ini.Replace("DEFAULT=CP DB SERVER PRUEBAS", "DEFAULT=Peñas"), ConexionesIni.CodificacionWindows1252())
            ConexionesIni.GuardarPerfil(ruta, "Peñas", "10.0.0.9", 3315UI, "sdprincipal")
            Dim perfil = ConexionesIni.Leer(ruta).Single(Function(p) p.Nombre = "Peñas")
            Assert.Equal("10.0.0.9", perfil.Servidor)
            Assert.Equal(3315UI, perfil.Puerto)
            Assert.Equal("sdprincipal", perfil.Catalogo)
            Assert.DoesNotContain("Ã", File.ReadAllText(ruta, ConexionesIni.CodificacionWindows1252()))

            ConexionesIni.EliminarPerfil(ruta, "PEÑAS")
            Assert.DoesNotContain(ConexionesIni.LeerSecciones(ruta), Function(s) s = "Peñas")
            Assert.Equal("CP.LOCAL", ConexionesIni.LeerValor(ruta, "GENERAL", "DEFAULT"))
            Assert.Equal("secreto", ConexionesIni.LeerValor(ruta, "GENERAL", "CNN").Split("="c).Last())
        Finally
            File.Delete(ruta)
        End Try
    End Sub

    <Fact>
    Public Sub Guardar_crea_el_archivo_y_la_carpeta_si_no_existen()
        Dim carpeta = Path.Combine(Path.GetTempPath(), $"sdaca-{Guid.NewGuid():N}")
        Dim ruta = Path.Combine(carpeta, "Bin", "sdcnn007.ini")
        Try
            ConexionesIni.GuardarPerfil(ruta, "NUEVA", "10.0.0.1", 3306UI, "sdprincipal")
            ConexionesIni.FijarValor(ruta, "GENERAL", "DEFAULT", "NUEVA")
            Assert.Equal({"NUEVA", "GENERAL"}, ConexionesIni.LeerSecciones(ruta).ToArray())
            Assert.Equal("10.0.0.1", ConexionesIni.Leer(ruta).Single().Servidor)
        Finally
            If Directory.Exists(carpeta) Then Directory.Delete(carpeta, True)
        End Try
    End Sub

    <Theory>
    <InlineData("", False)>
    <InlineData("General", False)>
    <InlineData("(Predeterminado)", False)>
    <InlineData("cp.local", False)>
    <InlineData("A[1]", False)>
    <InlineData("CENTRO 5", True)>
    Public Sub Valida_nombres_de_perfil(nombre As String, valido As Boolean)
        Dim motivo = ConexionesIni.ValidarNombre(nombre, ConexionesIni.Secciones(Ini))
        Assert.Equal(valido, motivo.Length = 0)
    End Sub

End Class
