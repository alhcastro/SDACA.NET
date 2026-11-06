Imports CoopeNet.Core.Cheques
Imports CoopeNet.Core.Datos

Public Class ChequesTests

    <Theory>
    <InlineData(0.0, "Cero")>
    <InlineData(0.5, "Cero Con 50/100")>
    <InlineData(1.0, "Uno Con 00/100")>
    <InlineData(21.0, "Veintiuno Con 00/100")>
    <InlineData(100.0, "Cien Con 00/100")>
    <InlineData(101.0, "Ciento uno Con 00/100")>
    <InlineData(1000.0, "Mil Con 00/100")>
    <InlineData(21000.0, "Veintiuno mil Con 00/100")>
    <InlineData(1000000.0, "Un millón Con 00/100")>
    <InlineData(164000.0, "Ciento sesenta y cuatro mil Con 00/100")>
    <InlineData(329444.79, "Trescientos veintinueve mil cuatrocientos cuarenta y cuatro Con 79/100")>
    <InlineData(238000000.0, "Doscientos treinta y ocho millones Con 00/100")>
    <InlineData(-9663343.77, "Nueve millones seiscientos sesenta y tres mil trescientos cuarenta y tres Con 77/100")>
    <InlineData(1.995, "Uno Con 00/100")>
    Public Sub Monto_en_letras_como_Numeros2CaracteresC(monto As Double, esperado As String)
        Assert.Equal(esperado, MontoEnLetras.Convertir(monto, True, True))
    End Sub

    <Fact>
    Public Sub Sin_centimos_y_en_femenino()
        Assert.Equal("Doscientas", MontoEnLetras.Convertir(200, False))
    End Sub

    <Theory>
    <InlineData("1132222", False, "1110002010101", "10002010101")>
    <InlineData("113222", False, "1110002010101", "10002010101")>
    <InlineData("1132222", True, "1110002010101", "1110002010101")>
    <InlineData("1132222", False, "1", "")>
    Public Sub Cuenta_oculta_los_dos_primeros_niveles_sin_showgands(codificacion As String, grupos As Boolean, cuenta As String, esperado As String)
        Assert.Equal(esperado, New FormatoCuenta(codificacion, grupos).Mostrar(cuenta))
    End Sub

    <Theory>
    <InlineData("E", 4, 4)>
    <InlineData("D", 300, 5)>
    <InlineData("D", 302, 7)>
    <InlineData("C", 201, 8)>
    <InlineData("C", 204, 11)>
    <InlineData("A", 100, -1)>
    <InlineData("C", 999, -1)>
    Public Sub Filas_de_configuracion_van_a_su_campo(tipo As String, campo As Integer, indice As Integer)
        Assert.Equal(indice, ConfigCheque.IndiceDe(tipo, campo))
    End Sub

    <Fact>
    Public Sub Configuracion_vuelve_igual_al_grabarla()
        Dim original = New ConfigCheque With {.Local = 7, .AltoCm = 7, .AnchoCm = 17, .FormaImpresion = 3, .PosicionHoja = 1,
                                              .ImprimeDetalle = True, .AltoDetalleCm = 3.5, .AltoContabilidadCm = 8}
        Dim filas = original.AFilas(2)
        Assert.Equal(14, filas.Count)
        Assert.Equal(14, filas.Select(Function(f) f.Campo).Distinct().Count())
        Dim leida = ConfigCheque.DesdeFilas(filas, 7)
        Assert.Equal(3, leida.FormaImpresion)
        Assert.Equal(1, leida.PosicionHoja)
        Assert.True(leida.ImprimeDetalle)
        Assert.False(leida.ImprimeContabilidad)
        Assert.Equal(8, leida.AltoContabilidadCm)
        Assert.Equal(1000, leida.Campos(8).Arriba)
        Assert.Equal(original.Campos(3).Ancho, leida.Campos(3).Ancho)
    End Sub

    <Theory>
    <InlineData(0, "0", 0, "Long Date")>
    <InlineData(5, "", 0, "Long Date")>
    <InlineData(10, "0", 0, "#,##0.00")>
    <InlineData(0, "dddd, dd/MMM/yyy", 4, "dddd, dd/MMM/yyyy")>
    Public Sub Formato_de_fecha_y_monto_se_normaliza_al_leer(indice As Integer, guardado As String, opcion As Integer, esperado As String)
        Dim tipo = If(indice <= 4, "E", If(indice <= 7, "D", "C"))
        Dim campo = If(indice <= 4, indice, If(indice <= 7, indice + 295, indice + 193))
        Dim fila As New FilaConfigCheque With {.Tipo = tipo, .Campo = campo, .Formato = guardado}
        Dim c = ConfigCheque.DesdeFilas({fila}, 0).Campos(indice)
        Assert.Equal(opcion, c.IndiceFormato())
        Assert.Equal(esperado, c.Formato)
    End Sub

    <Fact>
    Public Sub Campo_no_se_sale_de_la_vista()
        Dim c As New CampoCheque(1) With {.Izquierda = 9000, .Ancho = 2000, .Alto = 300}
        Assert.Equal((CampoCheque.VistaAncho - 2000) / CampoCheque.TwipsPorCm, c.PonerIzquierdaCm(16))
        Assert.Equal(CampoCheque.VistaAncho - 2000, c.Izquierda)
        Assert.Null(c.PonerIzquierdaCm(1))
        c.PonerArribaCm(0.1)
        Assert.Equal(0, c.Arriba)
        Assert.NotNull(c.PonerArribaCm(50))
    End Sub

    <Fact>
    Public Sub Cambiar_el_alto_conserva_el_borde_de_abajo()
        Dim c As New CampoCheque(1) With {.Arriba = 1000, .Alto = 300}
        c.PonerAltoCm(1)
        Assert.Equal(572, c.Alto)
        Assert.Equal(1300 - 572, c.Arriba)
    End Sub

    <Theory>
    <InlineData("SISTEMAS  digitales s. a.", 0, "Sistemas  Digitales S. A.")>
    <InlineData("Ñandú uno", 1, "ÑANDÚ UNO")>
    <InlineData("ÑANDÚ", 2, "ñandú")>
    Public Sub Texto_del_campo_segun_opcion(valor As String, opcion As Integer, esperado As String)
        Assert.Equal(esperado, FormatoCheque.Texto(valor, opcion))
    End Sub

    <Theory>
    <InlineData("2026-11-05", False, 0, True)>
    <InlineData("2026-11-04", False, 0, False)>
    <InlineData("2026-11-01", False, 4, True)>
    <InlineData("2026-11-05", True, 0, False)>
    Public Sub Borrar_exige_plazo_y_sin_conciliar(fecha As String, conciliado As Boolean, dias As Long, esperado As Boolean)
        Dim cheque As New ChequeConsulta With {.Fecha = Date.Parse(fecha, Globalization.CultureInfo.InvariantCulture), .Conciliado = conciliado}
        Assert.Equal(esperado, AnulacionCheques.SePuedeBorrar(cheque, dias, New Date(2026, 11, 5)))
    End Sub

    <Fact>
    Public Sub Replica_cambia_local_y_prefijo_por_destino()
        Dim destinos = {
            New DestinoReplica With {.ALocal = "1", .CambiaLoc = "1"},
            New DestinoReplica With {.ALocal = "2", .CambiaLoc = "0"},
            New DestinoReplica With {.ALocal = "7", .CambiaLoc = "1"}
        }
        Dim sentencias = Replicador.ArmarSentencias("UPDATE $Prefijolocalx SET l = $Local", 5, destinos)
        Assert.Equal({"UPDATE x SET l = 1", "UPDATE Rx SET l = 5", "UPDATE Rx SET l = 7"}, sentencias)
    End Sub

    <Theory>
    <InlineData("", "%%")>
    <InlineData(Nothing, "%%")>
    <InlineData("juan perez", "%juan%perez%")>
    Public Sub Filtro_de_entrega_usa_los_espacios_como_comodines(texto As String, esperado As String)
        Assert.Equal(esperado, ChequesRepositorio.PatronFiltro(texto))
    End Sub

End Class
