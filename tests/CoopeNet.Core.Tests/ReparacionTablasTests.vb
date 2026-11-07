Imports CoopeNet.Core.Datos

Public Class ReparacionTablasTests

    <Theory>
    <InlineData("<<Todas>>", "%")>
    <InlineData("  ", "%")>
    <InlineData("gac", "gac%")>
    <InlineData("gat_x%", "gat\_x\%%")>
    Public Sub El_filtro_busca_por_el_inicio(filtro As String, esperado As String)
        Assert.Equal(esperado, ReparacionTablas.Patron(filtro))
    End Sub

    <Fact>
    Public Sub La_sentencia_es_la_de_VB6_y_escapa_el_nombre()
        Assert.Equal("REPAIR NO_WRITE_TO_BINLOG TABLE `gacdp` QUICK EXTENDED USE_FRM", ReparacionTablas.SentenciaReparar("gacdp"))
        Assert.Equal("REPAIR NO_WRITE_TO_BINLOG TABLE `a``b` QUICK EXTENDED USE_FRM", ReparacionTablas.SentenciaReparar("a`b"))
    End Sub

    <Theory>
    <InlineData(False, False, "myisamchk ""C:\MySQL\Data\sdprincipal\gacdp.MYI""")>
    <InlineData(True, False, "myisamchk -r ""C:\MySQL\Data\sdprincipal\gacdp.MYI""")>
    <InlineData(True, True, "myisamchk -r -q ""C:\MySQL\Data\sdprincipal\gacdp.MYI""")>
    Public Sub Las_lineas_de_myisamchk(recuperar As Boolean, rapido As Boolean, esperado As String)
        Dim lineas = ReparacionTablas.ComandosMyIsamChk("C:\MySQL\Data\sdprincipal\", {"gacdp"}, recuperar, rapido)
        Assert.Equal(esperado, Assert.Single(lineas))
    End Sub

    <Theory>
    <InlineData("", "gacdp (vista)")>
    <InlineData("MyISAM", "gacdp")>
    <InlineData("InnoDB", "gacdp (InnoDB)")>
    Public Sub La_lista_muestra_el_motor_si_no_es_MyISAM(motor As String, esperado As String)
        Assert.Equal(esperado, New TablaBase With {.Nombre = "gacdp", .Motor = motor}.ToString())
    End Sub

End Class
