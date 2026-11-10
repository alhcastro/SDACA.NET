Imports System.Globalization
Imports CoopeNet.Core.Pos

Public Class PresentacionArticuloTests

    Private Shared ReadOnly Cultura As CultureInfo = CultureInfo.GetCultureInfo("en-US")

    Private Shared Function Aceite() As PresentacionArticulo
        ' 41001005 del 3315: ACEITE IDEAL 900ML, IVA 1 %.
        Return New PresentacionArticulo With {.CodigoArt = "41001005", .DescCorta = "ACEITE IDEAL", .Descripcion = "ACEITE IDEAL 900ML C 12", .UnidadMedida = "7",
                                              .Grupo = 900, .Exento = 1, .Precio1 = 1039.60396039604, .Abrevia = "ML"}
    End Function

    <Fact>
    Public Sub El_precio_lleva_IVA_y_el_redondeo_del_local()
        Assert.Equal(1050.0, PresentacionesRepositorio.PrecioVenta(Aceite(), 1, DireccionRedondeo.Ascendente), 6)
        Dim sinIva = Aceite()
        sinIva.Exento = 0
        sinIva.Precio1 = 2166
        Assert.Equal(2166.0, PresentacionesRepositorio.PrecioVenta(sinIva, 0, DireccionRedondeo.Ascendente), 6)
    End Sub

    <Fact>
    Public Sub Precio_por_unidad_como_lblGramos()
        Assert.Equal("PRECIO POR ML: 1.17", PresentacionesRepositorio.TextoPrecioPorUnidad(Aceite(), 1050, Cultura))
        Dim sinPresentacion = Aceite()
        sinPresentacion.Grupo = 0
        Assert.Equal("", PresentacionesRepositorio.TextoPrecioPorUnidad(sinPresentacion, 1050, Cultura))
    End Sub

    <Fact>
    Public Sub Guardar_es_el_UPDATE_de_SD_GuardarArticulo()
        Dim a = Aceite()
        a.Grupo = 900.5
        a.DescCorta = "D'ACEITE "
        Assert.Equal("update posarticulos set DescCorta = 'D\'ACEITE',descripcion = 'ACEITE IDEAL 900ML C 12',unidadmedida = '7',grupo = '900.5' where codigoart='41001005';",
                     PresentacionesRepositorio.SentenciaGuardar(a))
    End Sub

End Class
