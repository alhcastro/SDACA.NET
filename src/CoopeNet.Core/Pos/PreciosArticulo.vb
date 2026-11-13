Namespace Pos

    Public Enum DireccionRedondeo
        Ascendente
        Descendente
        Ambas
    End Enum

    ''' <summary>
    ''' Cálculo de precios del catálogo de artículos. Migra SD_ConsPrecImp de SD0502\ModPrin.bas (sin las
    ''' fórmulas G_ORDCOMPVAR, que solo llenan Compras y Órdenes), SD_DevUtilidad y SD_Rnd de
    ''' Comun\SD_MOD_UTILES02.bas. Los precios se guardan sin IVA; la pantalla los muestra con IVA cuando la
    ''' casilla I.V.A. está marcada, que es como arranca.
    ''' </summary>
    Public NotInheritable Class PreciosArticulo

        Private Sub New()
        End Sub

        ''' <summary>
        ''' SD_Rnd con punto decimal, que es como corre en Coopepalmares: las utilidades guardadas son múltiplos
        ''' de 0.005 (82,169 de 82,174 en el 3315). Con coma decimal VB6 no redondeaba.
        ''' </summary>
        Public Shared Function Redondear(monto As Double, factor As Double, direccion As DireccionRedondeo) As Double
            If factor = 0 Then Return monto
            factor = Math.Abs(factor)
            Dim decimales = DecimalesDe(factor)
            If monto = 0 Then Return 0
            If direccion = DireccionRedondeo.Ambas Then
                Dim arriba = (Math.Truncate(monto / factor) + 1) * factor
                Dim abajo = Math.Truncate(monto / factor) * factor
                direccion = If(arriba - monto < monto - abajo, DireccionRedondeo.Ascendente, DireccionRedondeo.Descendente)
            End If
            Dim cantidad = monto / factor
            ' VB6 busca el punto en CStr(iCant), que muestra 15 dígitos: 5001.9999999999 se ve como 5002 y no se toca.
            If cantidad.ToString("G15", Globalization.CultureInfo.InvariantCulture).Contains("."c) Then
                ' FormatNumber(x, 0) redondea la mitad alejándose de cero.
                cantidad = Math.Round(If(direccion = DireccionRedondeo.Ascendente, cantidad, cantidad - 1), MidpointRounding.AwayFromZero)
            End If
            Dim resultado = cantidad * factor
            If direccion = DireccionRedondeo.Ascendente AndAlso resultado = 0 Then resultado = factor
            If direccion = DireccionRedondeo.Ascendente AndAlso resultado < monto Then
                resultado = (cantidad + 1) * factor
            ElseIf direccion = DireccionRedondeo.Descendente AndAlso resultado < monto - factor Then
                resultado = (cantidad + 1) * factor
            End If
            Return Math.Round(resultado, decimales, MidpointRounding.AwayFromZero)
        End Function

        ''' <summary>Val(Str_GetLateral(factor, ".", False)): los dígitos después del punto leídos como número.</summary>
        Private Shared Function DecimalesDe(factor As Double) As Integer
            Dim texto = factor.ToString(Globalization.CultureInfo.InvariantCulture)
            Dim punto = texto.IndexOf("."c)
            If punto < 0 Then Return 0
            Dim valor As Integer
            Integer.TryParse(texto.Substring(punto + 1), valor)
            Return Math.Min(If(valor = 1, 2, valor), 15)
        End Function

        ''' <summary>
        ''' SD_ConsPrecImp(...).PreciMasImp: el costo con el descuento aplicado y los otros impuestos, más la
        ''' utilidad, más el IVA, más el extra y el flete. <paramref name="iva"/> es 0 si el precio va sin IVA.
        ''' </summary>
        Public Shared Function PrecioDesdeUtilidad(costo As Double, descuento As Double, porcentajeOtros As Double, montoOtros As Double,
                                                   extraYFlete As Double, utilidad As Double, iva As Double) As Double
            Dim neto = costo - costo * (descuento / 100)
            ' VB6 solo divide entre 100 un porcentaje positivo.
            Dim impuestos = neto * If(porcentajeOtros > 0, porcentajeOtros / 100, porcentajeOtros) + montoOtros
            Dim conImpuestos = neto + impuestos
            Dim sinIva = conImpuestos + conImpuestos * (utilidad / 100)
            Dim impuestoVentas = If(iva > 0, sinIva * (iva / 100), 0)
            Return sinIva + impuestoVentas + extraYFlete
        End Function

        ''' <summary>
        ''' Moneda1_LostFocus con SD_DevUtilidad: la utilidad sobre el costo con descuento (sin otros impuestos),
        ''' quitando el extra, el flete y el IVA. Redondea hacia abajo a 0.005; entre -0.01 y 0.01 vale 0.
        ''' Un costo de 0 deja la utilidad en 0, donde VB6 mostraba un error.
        ''' </summary>
        Public Shared Function UtilidadDesdePrecio(costo As Double, descuento As Double, precio As Double, extraYFlete As Double, iva As Double) As Double
            Dim neto = costo - costo * (descuento / 100)
            Dim venta = precio - extraYFlete
            If venta = 0 OrElse neto = 0 Then Return 0
            Dim sinIva = If(iva > 0, venta / (iva / 100 + 1), venta)
            Dim utilidad = Redondear((sinIva - neto) / neto * 100, 0.005, DireccionRedondeo.Descendente)
            Return If(utilidad < 0.01 AndAlso utilidad > -0.01, 0, utilidad)
        End Function

        ''' <summary>SD_GuardarArticulo: con la casilla I.V.A. marcada, los precios se guardan sin el impuesto.</summary>
        Public Shared Function PrecioParaGuardar(mostrado As Double, iva As Double, conIva As Boolean) As Double
            Return If(conIva AndAlso iva > 0, mostrado / (iva / 100 + 1), mostrado)
        End Function

        ''' <summary>SD_CargarArt: el precio guardado, con IVA si la casilla está marcada.</summary>
        Public Shared Function PrecioParaMostrar(guardado As Double, iva As Double, conIva As Boolean) As Double
            Return If(conIva AndAlso iva <> 0, guardado * (iva / 100 + 1), guardado)
        End Function

        ''' <summary>pos_fun_costoart de la base: costo con descuento, otros impuestos, flete y monto de otros.</summary>
        Public Shared Function CostoNeto(costo As Double, descuento As Double, porcentajeOtros As Double, flete As Double, montoOtros As Double) As Double
            Return Math.Round((costo - costo * (descuento / 100)) * (porcentajeOtros / 100 + 1) + flete + montoOtros, 2, MidpointRounding.AwayFromZero)
        End Function

    End Class

End Namespace
