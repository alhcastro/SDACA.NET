Imports SDACA.Core.Cheques
Imports SDACA.Core.Impresion

Public Class ImpresionChequesTests

    Private Const Esc As String = ChrW(27)

    Private Shared Function Plantilla(ParamArray filas As (Linea As String, Multiple As Integer)()) As List(Of LineaPlantilla)
        Return filas.Select(Function(f) New LineaPlantilla With {.Linea = f.Linea, .Multiple = f.Multiple}).ToList()
    End Function

    Private Shared Function Valores(ParamArray pares As (Clave As String, Valor As String)()) As Dictionary(Of String, String)
        Dim v As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
        For Each p In pares
            v(p.Clave) = p.Valor
        Next
        Return v
    End Function

    <Theory>
    <InlineData("abc", 5, "I"c, "abc  ")>
    <InlineData("abc", 5, "D"c, "  abc")>
    <InlineData("abc", 6, "C"c, "  abc ")>
    <InlineData("abc", 7, "C"c, "  abc  ")>
    <InlineData("abcdef", 3, "I"c, "abc")>
    <InlineData("abcdef", 3, "D"c, "def")>
    <InlineData("abcdef", 3, "C"c, "def")>
    Public Sub Ajusta_como_VB6(texto As String, ancho As Integer, alineacion As Char, esperado As String)
        Assert.Equal(esperado, PlantillaImpresion.Ajusta(texto, ancho, alineacion))
    End Sub

    <Fact>
    Public Sub Linea_con_comandos_datos_y_texto_fijo()
        Dim resultado = PlantillaImpresion.Armar(
            Plantilla(("TL$0|NL$|I009NombreF2010|TL$1|I006ET$***003|D000MontoF1010|TL$4", -1)),
            Valores(("NombreF2", "JUAN"), ("MontoF1", "1,500.00")))
        Dim esperado = Esc & "!" & ChrW(0) & vbCrLf & New String(" "c, 9) & "JUAN      " & Esc & "!" & ChrW(1) & "      ***" & "  1,500.00" & Esc & "!" & ChrW(4) & vbCrLf
        Assert.Equal(esperado, resultado)
    End Sub

    <Fact>
    Public Sub Repeticion_rellena_con_lineas_en_blanco_hasta_el_limite()
        Dim resultado = PlantillaImpresion.Armar(Plantilla(("I002Detalle#010", 2), ("I000Fin003", -1)), Valores(("Detalle0", "uno"), ("Fin", "fin")))
        Assert.Equal("  uno       " & vbCrLf & vbCrLf & vbCrLf & "fin" & vbCrLf, resultado)
    End Sub

    <Fact>
    Public Sub Repeticion_sigue_mientras_haya_datos_aunque_pase_el_limite()
        Dim resultado = PlantillaImpresion.Armar(Plantilla(("I000D#001", 0)), Valores(("D0", "a"), ("D1", "b")))
        Assert.Equal("a" & vbCrLf & "b" & vbCrLf, resultado)
    End Sub

    <Fact>
    Public Sub Multiple_menos_dos_omite_la_linea_sin_datos()
        Dim resultado = PlantillaImpresion.Armar(Plantilla(("I000Nada005", -2), ("I000Algo004", -2)), Valores(("Algo", "dato")))
        Assert.Equal("dato" & vbCrLf, resultado)
    End Sub

    <Fact>
    Public Sub Nombres_de_datos_sin_distinguir_mayusculas()
        Dim v = DatosImpresionCheque.Valores(New ChequeParaImprimir With {.Cheque = 7, .Fecha = New Date(2026, 11, 5), .Nombre = "juan pérez", .Monto = 1500.5}, FormatoCuenta.Predeterminado)
        Assert.Equal("JUAN PÉREZ", v("nombref2"))
        Assert.Equal("Juan Pérez", v("NombreF1"))
        Assert.Equal("7", v("Cheque"))
        Assert.Equal("Mil Quinientos Con 50/100", v("MontoLF1"))
    End Sub

    <Fact>
    Public Sub Lineas_del_asiento_dejan_vacio_el_lado_en_cero()
        Dim c As New ChequeParaImprimir With {.Lineas = {New LineaAsiento With {.Cuenta = "1110002010101", .Detalle = "BANCO", .Debe = 0, .Haber = 2500}}}
        Dim v = DatosImpresionCheque.Valores(c, New FormatoCuenta("1132222", False))
        Assert.Equal("10002010101", v("CC0"))
        Assert.Equal("", v("MDFA0"))
        Assert.Equal((2500.0).ToString("#,##0.00"), v("MHFA0"))
    End Sub

    <Fact>
    Public Sub Detalle_se_parte_en_lineas_numeradas()
        Dim c As New ChequeParaImprimir With {.Observaciones = "uno" & vbCrLf & "dos"}
        Dim v = DatosImpresionCheque.Valores(c, FormatoCuenta.Predeterminado)
        Assert.Equal("uno", v("Detalle0"))
        Assert.Equal("dos", v("Detalle1"))
        Assert.False(v.ContainsKey("Detalle2"))
    End Sub

    <Fact>
    Public Sub Detalle_largo_corta_cada_110_caracteres()
        Dim resultado = DatosImpresionCheque.AcomodaDetalle(New String("a"c, 115))
        Assert.Equal(New String("a"c, 110) & vbCrLf & New String("a"c, 5), resultado)
    End Sub

    <Fact>
    Public Sub Resumen_trae_asiento_en_columnas_de_83()
        Dim c As New ChequeParaImprimir With {.Cheque = 7, .Nombre = "JUAN", .Fecha = New Date(2026, 11, 5), .Monto = 100, .Observaciones = "PAGO",
            .Lineas = {New LineaAsiento With {.Cuenta = "111000201", .Detalle = "BANCO", .Haber = 100}}}
        Dim lineas = ResumenCheque.Armar(c, New FormatoCuenta("1132222", True)).Split(vbCrLf)
        Assert.Equal("IMPRIMIENDO CHEQUE # 7", lineas(0))
        Assert.Equal("A Nombre de:JUAN", lineas(1))
        Assert.Equal("DETALLE CONTABLE", lineas(8))
        Assert.Equal(New String("="c, 83), lineas(10))
        Assert.Equal(83, lineas(11).Length)
        Assert.StartsWith("111000201        BANCO", lineas(11))
    End Sub

    <Fact>
    Public Sub Resumen_sin_asiento_termina_en_el_detalle()
        Dim c As New ChequeParaImprimir With {.Cheque = 7, .Observaciones = "PAGO"}
        Assert.DoesNotContain("DETALLE CONTABLE", ResumenCheque.Armar(c, FormatoCuenta.Predeterminado))
    End Sub

    <Fact>
    Public Sub Banco_recibe_la_diferencia_y_es_el_monto()
        Dim lineas As New List(Of LineaCaptura) From {
            New LineaCaptura With {.Debe = 1000},
            New LineaCaptura With {.Haber = 200},
            New LineaCaptura With {.Sistema = LineaCaptura.SistemaBanco, .Debe = 5}
        }
        Dim t = AsientoCaptura.Recalcular(lineas)
        Assert.Equal(800, t.Monto)
        Assert.Equal(800, lineas(2).Haber)
        Assert.Equal(0, lineas(2).Debe)
        Assert.Equal(t.Debe, t.Haber)
    End Sub

    <Fact>
    Public Sub Diferencia_conserva_el_signo_para_comparar_con_el_cheque()
        Dim lineas As New List(Of LineaCaptura) From {New LineaCaptura With {.Haber = 200}}
        Assert.Equal(-200, AsientoCaptura.Recalcular(lineas).Diferencia)
    End Sub

    <Fact>
    Public Sub Monto_negativo_queda_en_cero()
        Dim lineas As New List(Of LineaCaptura) From {
            New LineaCaptura With {.Haber = 200},
            New LineaCaptura With {.Sistema = LineaCaptura.SistemaBanco}
        }
        Assert.Equal(0, AsientoCaptura.Recalcular(lineas).Monto)
    End Sub

    <Theory>
    <InlineData("1", "3-004-045003", 4)>
    <InlineData("3", "3-004-045003", 4)>
    <InlineData("0", "3-004-045003", 2)>
    <InlineData("1", "9-999-999999", 2)>
    Public Sub Locales_contables_agrupan_1_2_3_en_Coopepalmares(local As String, cedula As String, cantidad As Integer)
        Assert.Equal(cantidad, EmisionCheques.LocalesContables(local, cedula).Length)
    End Sub

    <Theory>
    <InlineData("\\PC1\EPSON FX", "pc1", "EPSON FX")>
    <InlineData("\\130.0.0.11\EPSON FX", "pc1", "\\130.0.0.11\EPSON FX")>
    <InlineData("EPSON FX", "pc1", "EPSON FX")>
    Public Sub Impresora_compartida_en_esta_PC_usa_el_nombre_local(impresora As String, equipo As String, esperado As String)
        Assert.Equal(esperado, ImpresionCheques.ImpresoraLocal(impresora, equipo))
    End Sub

End Class
