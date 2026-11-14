Imports System.IO
Imports CoopeNet.Core.Pos

Public Class PropiedadesFacturacionTests

    Private Shared Function L(pos As Integer, linea As String, Optional multiple As Integer = -1) As LineaFormato
        Return New LineaFormato With {.Pos = pos, .Linea = linea, .Multiple = multiple}
    End Function

    <Fact>
    Public Sub Exportar_e_importar_conservan_las_lineas_aunque_traigan_punto_y_coma()
        Dim lineas = {L(1, "AC$|TL$14|C000ET$COOPERATIVA040"), L(8, "I000CAN#005|D000DES;#021", 0), L(9, "I000CANLIN040", -2)}
        Dim texto = PropiedadesFacturacionRepositorio.Exportar(lineas)
        Assert.StartsWith("1;AC$|TL$14|C000ET$COOPERATIVA040;-1" & vbCrLf, texto)
        Dim leidas = PropiedadesFacturacionRepositorio.Importar(texto)
        Assert.Equal(3, leidas.Count)
        Assert.Equal("I000CAN#005|D000DES;#021", leidas(1).Linea)
        Assert.Equal(0, leidas(1).Multiple)
        Assert.Equal(-2, leidas(2).Multiple)
    End Sub

    <Fact>
    Public Sub Importar_salta_filas_sin_posicion_y_acepta_fin_de_linea_unix()
        Dim leidas = PropiedadesFacturacionRepositorio.Importar("Pos;Linea;Multiple" & vbLf & "3;I000LSEP040;-1" & vbLf & vbLf & "4;sin multiple")
        Assert.Equal(2, leidas.Count)
        Assert.Equal(3, leidas(0).Pos)
        Assert.Equal("sin multiple", leidas(1).Linea)
        Assert.Equal(-1, leidas(1).Multiple)
    End Sub

    <Fact>
    Public Sub Exportar_no_incluye_lineas_vacias()
        Assert.Equal("2;X;-1" & vbCrLf, PropiedadesFacturacionRepositorio.Exportar({L(1, ""), L(2, "X")}))
    End Sub

    <Theory>
    <InlineData("", "Escriba el nombre del formato.")>
    <InlineData("FacturaNueva", "El nombre del formato debe empezar con POS.")>
    <InlineData("POSNombreDemasiadoLargo", "El nombre del formato admite hasta 16 caracteres.")>
    Public Sub Valida_el_nombre(nombre As String, esperado As String)
        Assert.Equal(esperado, PropiedadesFacturacionRepositorio.Validar(nombre, {L(1, "X")}))
    End Sub

    <Fact>
    Public Sub Valida_posiciones_repetidas_o_fuera_de_rango_e_ignora_las_vacias()
        Assert.Equal("La posición 2 está repetida.", PropiedadesFacturacionRepositorio.Validar("POSFacCon", {L(2, "A"), L(2, "B")}))
        Assert.Equal("La posición 300 no es válida: va de 0 a 255.", PropiedadesFacturacionRepositorio.Validar("POSFacCon", {L(300, "A")}))
        Assert.Equal("", PropiedadesFacturacionRepositorio.Validar("posFacCon", {L(2, "A"), L(2, "")}))
    End Sub

    <Fact>
    Public Sub Valores_por_defecto_como_cmdRestaurar()
        Dim p As New ParametrosFacturacion With {.TextoProforma = "PROFORMA", .CopiasCredito = 3, .RedondeoTotal = 5}
        PropiedadesFacturacionRepositorio.ValoresPorDefecto(p)
        Assert.Equal("GRACIAS POR PREFERIRNOS", p.Leyendas(3))
        Assert.Equal(0.0, p.RedondeoTotal)
        Assert.Equal(DireccionRedondeo.Ambas, p.DireccionArticulo)
        Assert.Equal("PROFORMA", p.TextoProforma)
        Assert.Equal(3, p.CopiasCredito)
    End Sub

    <Fact>
    Public Sub Llaves_del_CONFIG_INI_como_SD_ImprimeFAC()
        Assert.Equal("POSCREDFORMAT", ImpresorasPos.LlaveFormato(DocumentoImpresion.Credito))
        Assert.Equal("PRINTER_POSBODE", ImpresorasPos.LlaveImpresora(DocumentoImpresion.OrdenEntrega))
        Assert.Equal("TIPOPRINTERCRED", ImpresorasPos.LlaveTipo(DocumentoImpresion.Credito))
        Assert.Null(ImpresorasPos.LlaveTipo(DocumentoImpresion.Cupon))
        Assert.Equal("PosFacCon", ImpresorasPos.FormatoPredeterminado(DocumentoImpresion.Contado))
    End Sub

    <Theory>
    <InlineData("EPSON TM-T20II Receipt", "EPSON")>
    <InlineData("Samsung Universal Print Driver", "SAMSUNG")>
    <InlineData("Microsoft Print To PDF", "OTRA")>
    <InlineData(Nothing, "OTRA")>
    Public Sub Tipo_de_impresora_por_el_controlador(controlador As String, esperado As String)
        Assert.Equal(esperado, ImpresorasPos.TipoPorControlador(controlador))
    End Sub

    <Fact>
    Public Sub Guarda_y_lee_impresora_y_formato_en_el_CONFIG_INI()
        Dim ruta = Path.Combine(Path.GetTempPath(), $"config-{Guid.NewGuid():N}.ini")
        Try
            File.WriteAllText(ruta, "[POS]" & vbCrLf & "MESES=6" & vbCrLf)
            Assert.Equal("Predeterminada", ImpresorasPos.Impresora(ruta, DocumentoImpresion.Credito, "Predeterminada"))
            Assert.Equal("PosFacCre", ImpresorasPos.Formato(ruta, DocumentoImpresion.Credito))
            ImpresorasPos.GuardarImpresora(ruta, DocumentoImpresion.Credito, "EPSON TM", "EPSON")
            ImpresorasPos.GuardarFormato(ruta, DocumentoImpresion.Credito, "POSFacCre2")
            Assert.Equal("EPSON TM", ImpresorasPos.Impresora(ruta, DocumentoImpresion.Credito, "Predeterminada"))
            Assert.Equal("POSFacCre2", ImpresorasPos.Formato(ruta, DocumentoImpresion.Credito))
            Dim texto = File.ReadAllText(ruta)
            Assert.Contains("TIPOPRINTERCRED=EPSON", texto)
            Assert.Contains("MESES=6", texto)
        Finally
            File.Delete(ruta)
        End Try
    End Sub

    <Fact>
    Public Sub Variables_en_mayusculas_y_recortadas()
        Assert.Equal("FACT", VariablesImpresionRepositorio.Normalizar(" fact ", 10))
        Assert.Equal(10, VariablesImpresionRepositorio.Normalizar("parametro largo", 10).Length)
    End Sub

End Class
