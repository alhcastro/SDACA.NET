Imports CoopeNet.Core.Bancos

''' <summary>
''' Los valores esperados salen de leer SD_Obtener_Elemento y Ajusta de VB6 con los formatos reales de
''' ccbtransferenciasconfig. No vienen de un .ENV real.
''' </summary>
Public Class ArchivoTransferenciaTests

    Private Const Encabezado As String = "ENCTR1,1,I,48,0,0|ENCCLI,6,I,48,0,5|ENCFEC,8,I,48,0,1|ENCINT,6,I,48,0,5|ENCREA,6,I,48,0,5|ENCTIP,1,I,48,0,5|ENCERR,4,I,48,0,5|ENCTOT,12,I,48,2,9|ENCTCC,7,I,48,2,9|ENCTCV,7,I,48,2,9|ENCFIL,10,I,48,0,5"
    Private Const Cuerpo As String = "CUETR2,1,I,48,0,0|CUECUE,15,I,48,0,0|CUENUM,8,I,48,0,5|CUETOT,12,I,48,2,9|CUECON,30,I,32,0,0|CUEEST,2,I,48,0,5"

    Private Shared Function Valores(ParamArray pares As String()) As IReadOnlyDictionary(Of String, String)
        Dim resultado As New Dictionary(Of String, String)
        For i = 0 To pares.Length - 1 Step 2
            resultado(pares(i)) = pares(i + 1)
        Next
        Return resultado
    End Function

    <Fact>
    Public Sub Encabezado_real_con_numero_interno_largo_conserva_los_ultimos_seis_digitos()
        Dim linea = ArchivoTransferencia.Linea(Encabezado, Valores(
            "ENCTR1", "1", "ENCCLI", "053650", "ENCFEC", "2026-10-29 11:37:03", "ENCINT", "29102026", "ENCREA", "0", "ENCTIP", "1",
            "ENCERR", "0", "ENCTOT", "1234567.5", "ENCTCC", "0", "ENCTCV", "0", "ENCFIL", ""))
        Assert.Equal("1" & "053650" & "29102026" & "102026" & "000000" & "1" & "0000" & "000123456750" & "0000000" & "0000000" & "0000000000", linea)
    End Sub

    <Fact>
    Public Sub Cuerpo_rellena_el_concepto_a_la_derecha_y_recorta_lo_que_sobra()
        Dim corto = ArchivoTransferencia.Linea(Cuerpo, Valores("CUETR2", "3", "CUECUE", "100010190", "CUENUM", "123", "CUETOT", "10.5", "CUECON", "PAGO", "CUEEST", "0"))
        Assert.Equal("3" & "100010190000000" & "00000123" & "000000001050" & "PAGO".PadRight(30) & "00", corto)
        Dim largo = ArchivoTransferencia.Linea(Cuerpo, Valores("CUETR2", "3", "CUECUE", "1234567890123456789", "CUENUM", "1", "CUETOT", "1", "CUECON", New String("X"c, 40), "CUEEST", "0"))
        Assert.Equal(1 + 15 + 8 + 12 + 30 + 2, largo.Length)
        Assert.Contains("123456789012345" & "00000001", largo)
    End Sub

    <Theory>
    <InlineData("1.005", "000000000101")>
    <InlineData("0.125", "000000000013")>
    <InlineData("999999.999", "000100000000")>
    Public Sub Monto_redondea_hacia_arriba_en_la_mitad_y_quita_el_punto(valor As String, esperado As String)
        Assert.Equal(esperado, ArchivoTransferencia.Linea("M,12,I,48,2,9", Valores("M", valor)))
    End Sub

    <Fact>
    Public Sub Numero_con_letras_toma_lo_que_Val_entiende()
        Assert.Equal("00012", ArchivoTransferencia.Linea("N,5,I,48,0,5", Valores("N", "12-AB")))
        Assert.Equal("00000", ArchivoTransferencia.Linea("N,5,I,48,0,5", Valores("N", "AB")))
    End Sub

    <Fact>
    Public Sub Variable_que_no_viene_en_la_consulta_queda_en_relleno()
        Assert.Equal("000", ArchivoTransferencia.Linea("Z,3,I,48,0,5", Valores("A", "1")))
    End Sub

    <Fact>
    Public Sub Tipos_sin_relleno_usan_espacios()
        Assert.Equal("   12", ArchivoTransferencia.Linea("N,5,I,48,0,4", Valores("N", "12")))
        Assert.Equal(" 1250", ArchivoTransferencia.Linea("N,5,I,48,2,8", Valores("N", "12.5")))
    End Sub

    <Fact>
    Public Sub Justificacion_derecha_rellena_y_recorta_por_la_derecha()
        Assert.Equal("AB...", ArchivoTransferencia.Ajusta("AB", 5, "D", "."c))
        Assert.Equal("ABC", ArchivoTransferencia.Ajusta("ABCDE", 3, "D", "."c))
        Assert.Equal("CDE", ArchivoTransferencia.Ajusta("ABCDE", 3, "I", "."c))
    End Sub

    <Fact>
    Public Sub Double_pasa_a_quince_cifras_como_CStr()
        Assert.Equal("0.3", ArchivoTransferencia.Texto(0.1 + 0.2))
        Assert.Equal("", ArchivoTransferencia.Texto(DBNull.Value))
    End Sub

    <Fact>
    Public Sub Archivo_va_en_ansi_con_crlf()
        Assert.Equal(New Byte() {&HF1, 13, 10, 65, 13, 10}, ArchivoTransferencia.Bytes({"ñ", "A"}))
    End Sub

    <Fact>
    Public Sub Nombre_del_archivo_y_documento_del_libro()
        Assert.Equal("20261106-0-16-3961", ArchivoTransferencia.NombreArchivo(#2026-11-06#, 0, 16, " 3961 "))
        Assert.Equal("16-3961", TransferenciasBancarias.Documento(16, 3961))
    End Sub

End Class
