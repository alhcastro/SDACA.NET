Imports CoopeNet.Core.Pos

Public Class CatalogoCabysTests

    Private Shared Function Fila(codigo As String, descripcion As String, Optional impuesto As Integer = 13) As FilaCabys
        Return New FilaCabys With {
            .Cat1 = codigo.Substring(0, 1), .Cat1d = "Agricultura", .Cat2 = codigo.Substring(0, 2), .Cat2d = "Horticultura",
            .Cat3 = codigo.Substring(0, 3), .Cat3d = "Cereales", .Cat4 = codigo.Substring(0, 4), .Cat4d = "Nivel 4",
            .Cat5 = codigo.Substring(0, 5), .Cat5d = "Nivel 5", .Cat6 = codigo.Substring(0, 7), .Cat6d = "Nivel 6",
            .Cat7 = codigo.Substring(0, 9), .Cat7d = "Nivel 7", .Cat8 = codigo.Substring(0, 11), .Cat8d = "Nivel 8",
            .Codigo = codigo, .Descripcion = descripcion, .Impuesto = impuesto}
    End Function

    Private Shared Function Catalogo() As CatalogoCabys
        Return New CatalogoCabys({
            Fila("0111100009900", "Trigo, para siembra (semillas), excepto duro", 1),
            Fila("0111100000100", "Trigo duro, para siembra (semillas)", 1),
            Fila("2165200009900", "Aceite de girasol refinado"),
            Fila("2111100030100", "Carne de res molida, fresca o refrigerada")})
    End Function

    <Fact>
    Public Sub Arma_los_niveles_sin_repetir_ramas()
        Dim c = Catalogo()
        Assert.Equal(4, c.Cantidad)
        Assert.Equal({"0", "2"}, c.Raices.Select(Function(n) n.Codigo))
        Dim hoja = c.Buscar("0111100000100")
        Assert.True(hoja.EsCodigo)
        Assert.Equal("01111000001", hoja.Padre.Codigo)
        Assert.Equal(2, c.Buscar("011110000").Hijos.Count)
        Assert.False(c.Buscar("011110000").EsCodigo)
        Assert.Equal(1, hoja.Impuesto)
        Dim profundidad = 0
        Dim actual = hoja
        While actual IsNot Nothing
            profundidad += 1
            actual = actual.Padre
        End While
        Assert.Equal(9, profundidad)
    End Sub

    <Theory>
    <InlineData("trigo semillas", 2)>
    <InlineData("semillas trigo", 0)>
    <InlineData("REFINADO", 1)>
    <InlineData("aceite girasol", 1)>
    <InlineData("21", 2)>
    <InlineData("0111100000100", 1)>
    <InlineData("", 0)>
    Public Sub Busca_palabras_en_orden_o_el_comienzo_del_codigo(texto As String, esperados As Integer)
        Assert.Equal(esperados, Catalogo().Coincidencias(texto).Count)
    End Sub

    <Fact>
    Public Sub No_distingue_tildes()
        Dim c = New CatalogoCabys({Fila("1234567890123", "Jamón cocido")})
        Assert.Single(c.Coincidencias("jamon"))
        Assert.Single(c.Coincidencias("JAMÓN"))
    End Sub

End Class
