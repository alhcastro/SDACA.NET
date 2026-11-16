Imports CoopeNet.Core.Pos

Public Class MarcasTests

    <Fact>
    Public Sub Guardar_como_VB6_en_mayusculas_y_escapado()
        Dim m As New MarcaArticulo With {.Codigo = 82, .Descripcion = "  d'cocina "}
        Assert.Equal("REPLACE INTO posmarcas (Codigo, Descripcion, Usuario, local, FM) VALUES ('82', 'D\'COCINA', '515', 7, NOW())",
                     MarcasRepositorio.SentenciaGuardar(m, 515, 7))
    End Sub

    <Fact>
    Public Sub La_descripcion_se_recorta_al_largo_de_la_columna()
        Assert.Equal(MarcasRepositorio.LargoDescripcion, MarcasRepositorio.Normalizar(New String("a"c, 80)).Length)
    End Sub

End Class
