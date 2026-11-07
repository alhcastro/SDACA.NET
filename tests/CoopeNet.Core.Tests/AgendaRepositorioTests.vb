Imports CoopeNet.Core.Catalogos

Public Class AgendaRepositorioTests

    Private Shared Function Marco() As MarcoAgenda
        Return New MarcoAgenda With {.Empresa = "COOPE", .Cedula = "3004045002", .Telefono = "2222-2222", .Usuario = "ANA", .Fecha = #2026-11-07 15:04:05#}
    End Function

    <Theory>
    <InlineData("", "%A%")>
    <InlineData("   ", "%A%")>
    <InlineData(" mora  vargas ", "%mora%vargas%")>
    <InlineData("ANA   MARIA  X", "%ANA%MARIA%X%")>
    Public Sub El_patron_sigue_a_VB6(texto As String, esperado As String)
        Assert.Equal(esperado, AgendaRepositorio.Patron(texto))
    End Sub

    <Fact>
    Public Sub La_lista_solo_trae_personas_activas_y_vivas()
        Assert.Contains("WHERE D.fdef = '1900-01-01' AND D.activo = '1'", AgendaRepositorio.SqlLista)
        Assert.Contains("LIMIT 100", AgendaRepositorio.SqlLista)
    End Sub

    <Fact>
    Public Sub La_direccion_omite_lo_que_falta_y_escapa_las_senas()
        Dim f As New FichaAgenda With {.Provincia = "SAN JOSE", .Canton = "", .Distrito = "CARMEN", .Sector = Nothing, .Senas = "50 M <NORTE>"}
        Assert.Equal("SAN JOSE, CARMEN, <b><u>SEÑAS:</u></b> 50 M &lt;NORTE&gt;", AgendaRepositorio.Direccion(f))
        Assert.Equal("<b><u>SEÑAS:</u></b> ", AgendaRepositorio.Direccion(New FichaAgenda()))
    End Sub

    <Fact>
    Public Sub La_ficha_trae_datos_y_contactos()
        Dim f As New FichaAgenda With {.Id = "999999901", .Nombre = "UNO DOS ANA", .Tipo = "FISICO"}
        f.Contactos.Add(New ContactoAgenda With {.Tipo = "1 - TELEFONO", .Dato = "8888-8888"})
        Dim html = AgendaRepositorio.Html("999999901", f, Marco())
        Assert.Contains("COOPE<br>Cédula: 3004045002 Tel: 2222-2222", html)
        Assert.Contains("999999901, FISICO", html)
        Assert.Contains("UNO DOS ANA", html)
        Assert.Contains("<td class=""e"">1 - TELEFONO</td><td>8888-8888</td>", html)
        Assert.DoesNotContain("NO HAY DATOS", html)
        Assert.Contains("ANA, 07/11/2026", html)
    End Sub

    <Fact>
    Public Sub Sin_contactos_lo_avisa()
        Dim html = AgendaRepositorio.Html("1", New FichaAgenda With {.Id = "1"}, Marco())
        Assert.Contains("NO HAY DATOS DE LOCALIZACIÓN", html)
    End Sub

    <Fact>
    Public Sub Sin_registro_avisa_solo_si_hay_identificacion()
        Assert.Contains("NO HAY REGISTRO PARA 'O&#39;X'", AgendaRepositorio.Html("O'X", Nothing, Marco()))
        Assert.DoesNotContain("NO HAY REGISTRO", AgendaRepositorio.Html("", Nothing, Marco()))
    End Sub

End Class
