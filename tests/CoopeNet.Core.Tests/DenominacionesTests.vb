Imports CoopeNet.Core.Pos

Public Class DenominacionesTests

    <Theory>
    <InlineData(2.0, "2")>
    <InlineData(1000.0, "1000")>
    <InlineData(0.5, "0.5")>
    <InlineData(20000.0, "20000")>
    Public Sub El_valor_como_texto_es_el_id_de_la_imagen(valor As Double, esperado As String)
        Assert.Equal(esperado, DenominacionesRepositorio.Texto(valor))
    End Sub

    <Theory>
    <InlineData("500", 500.0)>
    <InlineData(" 0,25 ", 0.25)>
    <InlineData("0.5", 0.5)>
    Public Sub Lee_el_valor_con_punto_o_coma(texto As String, esperado As Double)
        Assert.Equal(esperado, DenominacionesRepositorio.Leer(texto).Value)
    End Sub

    <Theory>
    <InlineData("")>
    <InlineData("0")>
    <InlineData("-5")>
    <InlineData("1,000.00")>
    <InlineData("abc")>
    Public Sub Rechaza_valores_que_no_sirven(texto As String)
        Assert.Null(DenominacionesRepositorio.Leer(texto))
    End Sub

End Class
