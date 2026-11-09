Imports CoopeNet.Core.Pos

Public Class Code39Tests

    <Theory>
    <InlineData("7441003590489", True)>
    <InlineData("ABC-12. $/+%", True)>
    <InlineData("abc", False)>
    <InlineData("12*3", False)>
    <InlineData("Ñ1", False)>
    <InlineData("", False)>
    <InlineData(Nothing, False)>
    Public Sub Admite_solo_el_juego_de_Code39(texto As String, esperado As Boolean)
        Assert.Equal(esperado, Code39.Admite(texto))
    End Sub

    <Fact>
    Public Sub Cada_caracter_tiene_tres_elementos_anchos_de_nueve()
        ' *A*: tres caracteres de 9 elementos y dos espacios angostos entre ellos.
        Dim e = Code39.Elementos("A")
        Assert.Equal(3 * 9 + 2, e.Count)
        Assert.Equal(3 * 15 + 2, e.Sum())
        ' El asterisco es n w n n w n w n n (barra, espacio, ...), igual al inicio y al final.
        Assert.Equal({1, 3, 1, 1, 3, 1, 3, 1, 1}, e.Take(9).ToArray())
        Assert.Equal({1, 3, 1, 1, 3, 1, 3, 1, 1}, e.Skip(e.Count - 9).ToArray())
        ' La A es w n n n n w n n w.
        Assert.Equal({3, 1, 1, 1, 1, 3, 1, 1, 3}, e.Skip(10).Take(9).ToArray())
    End Sub

    <Fact>
    Public Sub Rechaza_lo_que_no_admite()
        Assert.Throws(Of ArgumentException)(Function() Code39.Elementos("abc"))
    End Sub

End Class
