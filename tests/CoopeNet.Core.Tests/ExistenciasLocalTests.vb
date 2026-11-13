Imports CoopeNet.Core.Datos
Imports CoopeNet.Core.Pos

Public Class ExistenciasLocalTests

    Private Shared Function Ejemplo() As ExistenciaLocal
        Return New ExistenciaLocal With {
            .CArticulo = "000101", .Stock = 61.5, .Min = 203, .Max = 0, .Recom = 270.25, .InvInicial = 12,
            .InvInicialF = New Date(2025, 9, 23, 20, 6, 35), .FC = New Date(2006, 11, 23, 15, 58, 46), .LinCredito = 1, .Localiz = "1", .CosInicial = 850.75}
    End Function

    <Fact>
    Public Sub Nuevo_es_un_REPLACE_con_el_local_escrito_y_conserva_CosInicial()
        Dim sql = ExistenciasLocalRepositorio.SentenciaGuardar(Ejemplo(), True, 1, 565)
        Assert.Equal("REPLACE INTO posarticuloslocal (CLocal, CArticulo, Stock, Min, Max, Recom, FM, FC, Usuario, InvInicial, InvInicialF, LinCredito, Localiz, CosInicial) VALUES (" &
                     "'1', '000101', '61.5', '203', '0', '270.25', NOW(), '2006-11-23 15:58:46', '565', '12', '2025-09-23 20:06:35', '1', '1', '850.75')", sql)
        Assert.DoesNotContain("$Local", sql)
    End Sub

    <Fact>
    Public Sub Nuevo_sin_fecha_de_creacion_usa_la_de_hoy()
        Dim e = Ejemplo()
        e.FC = Date.MinValue
        Dim sql = ExistenciasLocalRepositorio.SentenciaGuardar(e, True, 1, 565)
        Assert.Contains($"'{Date.Now:yyyy-MM-dd}", sql)
    End Sub

    <Fact>
    Public Sub Editar_es_un_UPDATE_con_Local_para_la_replica_y_sin_Stock()
        Dim sql = ExistenciasLocalRepositorio.SentenciaGuardar(Ejemplo(), False, 1, 565)
        Assert.Equal("UPDATE posarticuloslocal SET Min = 203, Max = 0, Recom = 270.25, FM = NOW(), Usuario = 565, InvInicial = 12, InvInicialF = '2025-09-23 20:06:35', " &
                     "LinCredito = 1, Localiz = '1' WHERE CLocal = $Local AND CArticulo = '000101'", sql)
        Assert.DoesNotContain("Stock", sql)
        ' Cada destino de la réplica pone su propio local.
        Dim destinos = {New DestinoReplica With {.ALocal = "2", .CambiaLoc = "1"}, New DestinoReplica With {.ALocal = "1", .CambiaLoc = "0"}}
        Dim replicas = Replicador.ArmarSentencias(sql, 1, destinos)
        Assert.EndsWith("WHERE CLocal = 2 AND CArticulo = '000101'", replicas(0))
        Assert.EndsWith("WHERE CLocal = 1 AND CArticulo = '000101'", replicas(1))
    End Sub

    <Fact>
    Public Sub Escapa_comillas_en_el_codigo()
        Dim e = Ejemplo()
        e.CArticulo = "A'1"
        Assert.Contains("CArticulo = 'A\'1'", ExistenciasLocalRepositorio.SentenciaGuardar(e, False, 1, 1))
    End Sub

    <Fact>
    Public Sub La_consulta_respeta_el_filtro_y_navega_por_codigo_como_texto()
        Dim consulta = ExistenciasLocalRepositorio.SqlCargar(BusquedaExistencia.Consulta, "")
        Assert.EndsWith("WHERE A.CLocal = @local AND A.CArticulo = @codigo LIMIT 1", consulta)
        Assert.Contains("INNER JOIN posinventario I ON I.CArticulo = A.CArticulo AND I.CLocal = @local", consulta)
        Dim siguiente = ExistenciasLocalRepositorio.SqlCargar(BusquedaExistencia.Siguiente, "I.Categ = 3")
        Assert.EndsWith("WHERE A.CLocal = @local AND I.Categ = 3 AND A.CArticulo > @codigo ORDER BY A.CArticulo LIMIT 1", siguiente)
        Assert.EndsWith("A.CArticulo < @codigo ORDER BY A.CArticulo DESC LIMIT 1", ExistenciasLocalRepositorio.SqlCargar(BusquedaExistencia.Anterior, ""))
    End Sub

End Class
