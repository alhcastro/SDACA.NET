Imports CoopeNet.Core.Pos

Public Class ImpresionFormatoTests

    Private Shared ReadOnly Esc As String = ChrW(27)

    Private Shared Function L(pos As Integer, linea As String, multiple As Integer) As LineaFormato
        Return New LineaFormato With {.Pos = pos, .Linea = linea, .Multiple = multiple}
    End Function

    <Theory>
    <InlineData("ABC", 6, AjusteTexto.RellenoDerecha, "ABC   ")>
    <InlineData("ABC", 6, AjusteTexto.RellenoIzquierda, "   ABC")>
    <InlineData("GRACIAS", 10, AjusteTexto.Centrado, "  GRACIAS ")>
    <InlineData("ABCD", 10, AjusteTexto.Centrado, "   ABCD   ")>
    <InlineData("ABCDEFG", 4, AjusteTexto.RellenoDerecha, "ABCD")>
    <InlineData("ABCDEFG", 4, AjusteTexto.RellenoIzquierda, "DEFG")>
    <InlineData("ABCDEFG", 4, AjusteTexto.Centrado, "DEFG")>
    <InlineData("X", 0, AjusteTexto.RellenoDerecha, "")>
    Public Sub Ajusta_como_VB6(cadena As String, tam As Integer, posicion As AjusteTexto, esperado As String)
        Assert.Equal(esperado, ImpresionFormato.Ajusta(cadena, tam, posicion))
    End Sub

    <Fact>
    Public Sub Ajusta_centrado_redondea_al_par_y_usa_el_relleno()
        ' (40 - 11) / 2 = 14.5 → 14: 14 + 11 + 14 = 39, se agrega uno a la izquierda.
        Assert.Equal("..............( 3 LINEAS )..............", ImpresionFormato.Ajusta("( 3 LINEAS )", 40, AjusteTexto.Centrado, ".").Replace("( 3 LINEAS )", "( 3 LINEAS )"))
        Assert.Equal(New String("-"c, 40), ImpresionFormato.Ajusta("-", 40, AjusteTexto.Centrado, "-"))
    End Sub

    <Fact>
    Public Sub Variables_se_emparejan_en_orden_y_el_ultimo_repetido_gana()
        Dim d = ImpresionFormato.Variables("A|B|C|a", "1||3|4")
        Assert.Equal("4", d("A"))
        Assert.Equal("", d("B"))
        Assert.Equal("3", d("C"))
        Dim corta = ImpresionFormato.Variables("X|Y|Z", "1")
        Assert.Equal("", corta("Z"))
    End Sub

    <Fact>
    Public Sub Interpreta_datos_textos_fijos_y_alineaciones()
        Dim lineas = {L(1, "I000ET$CLIENTE:009|I000NOM010", -1), L(2, "D000ET$TOTAL:010|D002TOT008", -1)}
        Dim r = ImpresionFormato.Interpretar(lineas, "NOM|TOT", "JUAN|1,500.00")
        Assert.Equal("CLIENTE: JUAN      " & vbCrLf & "    TOTAL:  1,500.00" & vbCrLf, r)
    End Sub

    <Fact>
    Public Sub Las_lineas_multiples_se_repiten_mientras_haya_datos()
        Dim lineas = {L(1, "I000CAN#005|I001DES#010", 0), L(2, "I000ET$FIN003", -1)}
        Dim r = ImpresionFormato.Interpretar(lineas, "CAN0|DES0|CAN1|DES1", "1.00|ARROZ|2.00|FRIJOL")
        Assert.Equal("1.00  ARROZ     " & vbCrLf & "2.00  FRIJOL    " & vbCrLf & "FIN" & vbCrLf, r)
    End Sub

    <Fact>
    Public Sub Con_multiple_n_imprime_al_menos_n_mas_uno_renglones()
        Dim r = ImpresionFormato.Interpretar({L(1, "I000CAN#005", 2)}, "CAN0", "1.00")
        Assert.Equal("1.00 " & vbCrLf & vbCrLf & vbCrLf, r)
    End Sub

    <Fact>
    Public Sub Desde_el_formato_global_las_repeticiones_empiezan_en_uno()
        Dim r = ImpresionFormato.Interpretar({L(1, "I000CAN#005", 0)}, "CAN0|CAN1", "1.00|2.00", inicioRepeticion:=1)
        Assert.Equal("2.00 " & vbCrLf, r)
    End Sub

    <Fact>
    Public Sub Multiple_menos_dos_solo_sale_si_hay_dato()
        Dim lineas = {L(1, "I000ET$DESC:005|I000DESM010", -2)}
        Assert.Equal("", ImpresionFormato.Interpretar(lineas, "DESM", " "))
        Assert.Equal("DESC:5.00      " & vbCrLf, ImpresionFormato.Interpretar(lineas, "DESM", "5.00"))
    End Sub

    <Fact>
    Public Sub Codigos_de_control_y_cajon()
        Dim r = ImpresionFormato.Interpretar({L(1, "TL$14|AC$|I000ET$X001|NL$|CT$", -1)}, "", "")
        Assert.Equal(Esc & "!" & ChrW(14) & "X" & vbCrLf & Esc & "i" & vbCrLf, r)
        Assert.Equal("X" & vbCrLf & vbCrLf, ImpresionFormato.ParaPantalla(r))
    End Sub

    <Fact>
    Public Sub El_QR_solo_va_al_imprimir()
        Dim lineas = {L(1, "QR$|I000ET$A001", -1)}
        Assert.Equal("A" & vbCrLf, ImpresionFormato.Interpretar(lineas, "", "", imprimir:=False, clave:="123"))
        Assert.Contains("1P0123", ImpresionFormato.Interpretar(lineas, "", "", imprimir:=True, clave:="123"))
    End Sub

    <Fact>
    Public Sub Numeros_y_fechas_como_las_cajas()
        Assert.Equal("1,234.50", FacturaImpresionRepositorio.Numero(1234.5))
        Assert.Equal("-12.00", FacturaImpresionRepositorio.Numero(-12))
        Assert.Equal("1,235", FacturaImpresionRepositorio.Numero(1234.5, 0))
        Assert.Equal("3.0", FacturaImpresionRepositorio.Corto(3, 1))
        Assert.Equal("05/nov/2026 18:08:56", FacturaImpresionRepositorio.FechaLarga(New Date(2026, 11, 5, 18, 8, 56)))
        Assert.Equal("noviembre", FacturaImpresionRepositorio.NombreMes(11))
    End Sub

End Class
