Imports CoopeNet.Core.Pos

Public Class ConexionesRemotasTests

    <Fact>
    Public Sub Un_catalogo_de_SDACA_tiene_al_menos_22_tablas_conocidas()
        Dim todas = ConexionesRemotasRepositorio.TablasDeSdaca.ToList()
        Assert.True(ConexionesRemotasRepositorio.EsCatalogoDeSdaca(todas.Take(22).Select(Function(t) t.ToUpperInvariant()).Concat({"posarticulos"})))
        Assert.False(ConexionesRemotasRepositorio.EsCatalogoDeSdaca(todas.Take(21).Concat(todas.Take(21)).Concat({"otra"})))
    End Sub

    <Fact>
    Public Sub Recorta_al_largo_de_la_columna()
        Assert.Equal("201.204.201.107", ConexionesRemotasRepositorio.Recortar(" 201.204.201.107 ", ConexionesRemotasRepositorio.LargoServidor))
        Assert.Equal(16, ConexionesRemotasRepositorio.Recortar(New String("x"c, 40), ConexionesRemotasRepositorio.LargoServidor).Length)
    End Sub

End Class
