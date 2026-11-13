Imports CoopeNet.Core.Pos

Public Class MonedasPosTests

    <Fact>
    Public Sub El_cambio_es_el_REPLACE_de_cmdAceptar_con_la_fecha_sin_hora()
        Assert.Equal("REPLACE INTO postipocambio VALUES ('2026-11-13 00:00:00',532.45,1,51,NULL)",
                     MonedasPosRepositorio.SentenciaCambio(New Date(2026, 11, 13, 16, 40, 5), 532.45, 1, 51))
    End Sub

    <Theory>
    <InlineData("532.45", 532.45)>
    <InlineData("532,45", 532.45)>
    <InlineData(" 530 ", 530.0)>
    <InlineData("0", 0.0)>
    Public Sub Lee_el_monto_con_punto_o_coma(texto As String, esperado As Double)
        Assert.Equal(esperado, MonedasPosRepositorio.LeerMonto(texto))
    End Sub

    <Theory>
    <InlineData("")>
    <InlineData("1.000,50")>
    <InlineData("abc")>
    <InlineData("-5")>
    Public Sub Rechaza_montos_invalidos(texto As String)
        Assert.Null(MonedasPosRepositorio.LeerMonto(texto))
    End Sub

    <Fact>
    Public Sub La_descripcion_va_en_mayusculas_y_hasta_24()
        Assert.Equal("DOLAR", MonedasPosRepositorio.Normalizar(" dolar "))
        Assert.Equal(24, MonedasPosRepositorio.Normalizar(New String("e"c, 40)).Length)
    End Sub

End Class
