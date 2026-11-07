Imports CoopeNet.Core.Impresion

Public Class ConfiguracionImpresionTests

    Private Shared Function Existente() As FilaImpresion
        Dim f As New FilaImpresion With {.Local = 4, .Estacion = "12", .Impresora = "\\PC\EPSON", .LlevaConsecutivo = True, .Consecutivo = 100, .Tipo = 0}
        f.Original = f.Copia()
        Return f
    End Function

    <Fact>
    Public Sub Los_documentos_fijos_se_agregan_sin_repetir_y_ordenados()
        Dim lista = ConfiguracionImpresion.UnirDocumentos({"CHEQUES", "gar - factura", ""})
        Assert.Equal(7, lista.Count)
        Assert.Equal("CHEQUES", lista(0))
        Assert.Single(lista, Function(d) d.Equals("GAR - FACTURA", StringComparison.OrdinalIgnoreCase))
    End Sub

    <Fact>
    Public Sub Sin_cambios_no_hay_sentencia()
        Assert.False(ConfiguracionImpresion.Sentencia("CHEQUES", Existente()).HasValue)
    End Sub

    <Fact>
    Public Sub Un_cambio_actualiza_la_fila_completa_con_la_llave_original()
        Dim f = Existente()
        f.Local = -1
        f.Impresora = "OTRA"
        f.Consecutivo = 101
        Dim c = ConfiguracionImpresion.Sentencia("CHEQUES", f).Value
        Assert.StartsWith("UPDATE gatimpresion SET local = @Local, estacion = @Estacion, impresora = @Impresora, llevaconsecutivo = @Lleva, consecutivo = @Consecutivo, Type = @Tipo WHERE local = @localOrg AND estacion = @estacionOrg AND documento = @documento", c.CommandText)
        Dim p = CType(c.Parameters, Dapper.DynamicParameters)
        Assert.Equal(-1L, p.Get(Of Long)("Local"))
        Assert.Equal(4L, p.Get(Of Long)("localOrg"))
        Assert.Equal("1", p.Get(Of String)("Lleva"))
    End Sub

    <Fact>
    Public Sub Borrar_usa_la_llave_original()
        Dim f = Existente()
        f.Estacion = "99"
        f.Borrar = True
        Dim c = ConfiguracionImpresion.Sentencia("CHEQUES", f).Value
        Assert.Equal("DELETE FROM gatimpresion WHERE local = @localOrg AND estacion = @estacionOrg AND documento = @documento", c.CommandText)
        Assert.Equal("12", CType(c.Parameters, Dapper.DynamicParameters).Get(Of String)("estacionOrg"))
    End Sub

    <Fact>
    Public Sub Una_fila_nueva_se_inserta_con_el_documento_en_mayusculas()
        Dim c = ConfiguracionImpresion.Sentencia("gar - recibo", New FilaImpresion With {.Impresora = " X "}).Value
        Assert.StartsWith("INSERT INTO gatimpresion", c.CommandText)
        Dim p = CType(c.Parameters, Dapper.DynamicParameters)
        Assert.Equal("GAR - RECIBO", p.Get(Of String)("documentoNuevo"))
        Assert.Equal("X", p.Get(Of String)("Impresora"))
        Assert.Equal("0", p.Get(Of String)("Lleva"))
    End Sub

    <Fact>
    Public Sub Una_fila_nueva_sin_estacion_o_borrada_no_hace_nada()
        Assert.False(ConfiguracionImpresion.Sentencia("X", New FilaImpresion With {.Estacion = " "}).HasValue)
        Assert.False(ConfiguracionImpresion.Sentencia("X", New FilaImpresion With {.Borrar = True}).HasValue)
    End Sub

End Class
