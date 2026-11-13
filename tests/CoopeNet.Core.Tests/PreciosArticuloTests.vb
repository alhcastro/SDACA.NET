Imports CoopeNet.Core.Pos

Public Class PreciosArticuloTests

    <Theory>
    <InlineData(25.0, 25.0)>
    <InlineData(34.997, 34.995)>
    <InlineData(34.995, 34.995)>
    <InlineData(10.5, 10.5)>
    <InlineData(-3.2571, -3.26)>
    <InlineData(25.01, 25.01)>
    Public Sub Redondea_hacia_abajo_a_0005_como_SD_Rnd(monto As Double, esperado As Double)
        Assert.Equal(esperado, PreciosArticulo.Redondear(monto, 0.005, DireccionRedondeo.Descendente), 9)
    End Sub

    <Theory>
    <InlineData(159.0, 25.0, DireccionRedondeo.Ascendente, 175.0)>
    <InlineData(159.0, 25.0, DireccionRedondeo.Descendente, 150.0)>
    <InlineData(159.0, 25.0, DireccionRedondeo.Ambas, 150.0)>
    <InlineData(170.0, 25.0, DireccionRedondeo.Ambas, 175.0)>
    <InlineData(5.0, 25.0, DireccionRedondeo.Ascendente, 25.0)>
    <InlineData(7.0, 0.0, DireccionRedondeo.Ascendente, 7.0)>
    Public Sub Redondea_con_el_factor_y_la_direccion(monto As Double, factor As Double, direccion As DireccionRedondeo, esperado As Double)
        Assert.Equal(esperado, PreciosArticulo.Redondear(monto, factor, direccion), 9)
    End Sub

    <Fact>
    Public Sub El_precio_suma_utilidad_iva_y_extra()
        ' 1000 con 10% de descuento = 900; 2% otros = 18 + 5 de monto = 923; 25% = 1153.75; 13% IVA = 1303.7375; extra 50.
        Assert.Equal(1353.7375, PreciosArticulo.PrecioDesdeUtilidad(1000, 10, 2, 5, 50, 25, 13), 6)
    End Sub

    <Fact>
    Public Sub Sin_iva_el_precio_no_lleva_impuesto_de_ventas()
        Assert.Equal(1250.0, PreciosArticulo.PrecioDesdeUtilidad(1000, 0, 0, 0, 0, 25, 0), 9)
    End Sub

    <Fact>
    Public Sub La_utilidad_desde_el_precio_es_la_inversa()
        Assert.Equal(25.0, PreciosArticulo.UtilidadDesdePrecio(1000, 0, 1412.5, 0, 13), 9)
        Assert.Equal(25.0, PreciosArticulo.UtilidadDesdePrecio(1000, 0, 1462.5, 50, 13), 9)
    End Sub

    <Fact>
    Public Sub La_utilidad_ignora_los_otros_impuestos_como_VB6()
        ' Moneda1_LostFocus calcula sobre el costo con descuento, sin otros impuestos.
        Assert.Equal(20.0, PreciosArticulo.UtilidadDesdePrecio(1000, 10, 1080, 0, 0), 9)
    End Sub

    <Theory>
    <InlineData(0.0, 1000.0)>
    <InlineData(1000.0, 0.0)>
    Public Sub Sin_venta_o_sin_costo_la_utilidad_es_cero(precio As Double, costo As Double)
        Assert.Equal(0.0, PreciosArticulo.UtilidadDesdePrecio(costo, 0, precio, 0, 13))
    End Sub

    <Fact>
    Public Sub Una_utilidad_casi_cero_queda_en_cero()
        Assert.Equal(0.0, PreciosArticulo.UtilidadDesdePrecio(1000, 0, 1000.05, 0, 0))
    End Sub

    <Fact>
    Public Sub Con_iva_el_precio_se_guarda_sin_el_impuesto()
        Assert.Equal(1000.0, PreciosArticulo.PrecioParaGuardar(1130, 13, True), 9)
        Assert.Equal(1130.0, PreciosArticulo.PrecioParaGuardar(1130, 13, False), 9)
        Assert.Equal(1130.0, PreciosArticulo.PrecioParaMostrar(1000, 13, True), 9)
        Assert.Equal(1000.0, PreciosArticulo.PrecioParaMostrar(1000, 0, True), 9)
    End Sub

    <Fact>
    Public Sub El_costo_neto_es_el_de_pos_fun_costoart()
        ' (1000 - 10%) * 1.02 + 30 de flete + 5 de otros.
        Assert.Equal(953.0, PreciosArticulo.CostoNeto(1000, 10, 2, 30, 5), 9)
    End Sub

End Class
