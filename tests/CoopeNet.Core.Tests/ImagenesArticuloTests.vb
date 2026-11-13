Imports CoopeNet.Core.Pos

Public Class ImagenesArticuloTests

    <Fact>
    Public Sub La_descripcion_se_recorta_al_largo_de_la_columna()
        Assert.Equal("", ImagenesArticuloRepositorio.Recortar(Nothing))
        Assert.Equal("foto.jpg", ImagenesArticuloRepositorio.Recortar("  foto.jpg "))
        Assert.Equal(128, ImagenesArticuloRepositorio.Recortar(New String("x"c, 300)).Length)
    End Sub

End Class
