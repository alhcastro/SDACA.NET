Imports CoopeNet.Core.Pos

Public Class ProveedoresTests

    Private Shared Function Ejemplo() As CuentaProveedor
        Return New CuentaProveedor With {
            .Codigo = "1408", .Descripcion = "Compras O'Neil", .TipoPago = 1, .Cuenta = "2723", .Banco = 0, .Confirmacion = "TELEFONO:",
            .DiaPago = 34, .DiaVisita = 7, .Credito = 150000.5, .EncargadoCompras = "111", .Agente = "222", .Vendedor = "",
            .Moneda = 0, .DiasPlazo = 30, .Observaciones = "Nota", .DiasEntrega = 3, .CC = "213000301", .Categ = "A",
            .Devoluciones = True, .MontoPromocional = 2500}
    End Function

    <Fact>
    Public Sub Insertar_lleva_Local_para_la_replica_y_escapa_comillas()
        Assert.Equal("INSERT INTO cxpcuenprov (CTAPROV, LOCAL, ID, DESCRIPCION, TIPOPAGO, cuenta, banco, CONFIRMACION, DIAPAGO, DIAVISITA, CREDITO, " &
                     "ENCARGADOCOMPRAS, AGENTE, VENDEDOR, MONEDA, DIASPLAZO, FULTPAGO, Observaciones, ACTIVO, DiasEntrega, Categ, MontoPromocional, Devol, UIDC, FC, FE, CC) VALUES (" &
                     "'1408', $Local, '3101058022', 'Compras O\'Neil', 1, '2723', 0, 'TELEFONO:', 34, 7, 150000.5, '111', '222', '', 0, 30, '1900-01-01', 'Nota', '1', 3, 'A', 2500, '1', 565, NOW(), NULL, '213000301')",
                     ProveedoresRepositorio.SentenciaInsertar(Ejemplo(), " 3101058022 ", 565))
    End Sub

    <Fact>
    Public Sub Actualizar_busca_el_codigo_original_en_el_local()
        Dim c = Ejemplo()
        c.CodigoOriginal = "1400"
        Dim sql = ProveedoresRepositorio.SentenciaActualizar(c)
        Assert.StartsWith("UPDATE cxpcuenprov SET CTAPROV='1408', DESCRIPCION='Compras O\'Neil', TIPOPAGO=1,", sql)
        Assert.EndsWith("Devol='1' WHERE CTAPROV='1400' AND LOCAL=$Local", sql)
        Assert.Equal("UPDATE cxp SET ctaprov='1408' WHERE ctaprov='1400'", ProveedoresRepositorio.SentenciaCambiarCxp("1400", "1408"))
    End Sub

    <Fact>
    Public Sub Proveedor_y_borrado()
        Assert.Equal("INSERT INTO cxpproveedores (LOCAL, ID, ESTADO, FC, FE) VALUES ($Local, '3101058022', '1', NOW(), NULL)", ProveedoresRepositorio.SentenciaProveedor("3101058022 "))
        Assert.Equal("DELETE FROM cxpcuenprov WHERE ctaprov='1408'", ProveedoresRepositorio.SentenciaBorrar("1408"))
    End Sub

    <Theory>
    <InlineData("", 1L)>
    <InlineData("1,2,3", 4L)>
    <InlineData("1,2,4,5", 3L)>
    <InlineData("2,3", 1L)>
    <InlineData("1,1,2,2,3,0,-4", 4L)>
    Public Sub Primer_codigo_libre(usados As String, esperado As Long)
        Dim lista = If(usados.Length = 0, New List(Of Long)(), usados.Split(","c).Select(Function(x) CLng(x)).ToList())
        Assert.Equal(esperado, ProveedoresRepositorio.PrimerLibre(lista))
    End Sub

    <Fact>
    Public Sub Una_cuenta_sin_codigo_original_es_nueva()
        Dim c = Ejemplo()
        Assert.True(c.Nueva)
        c.CodigoOriginal = "1408"
        Assert.False(c.Nueva)
    End Sub

End Class
