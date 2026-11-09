Imports CoopeNet.Core.Pos

Public Class MovimientosArticuloTests

    Private Shared ReadOnly FechaInicial As New Date(2025, 9, 23, 20, 6, 35)

    <Fact>
    Public Sub Con_la_casilla_siempre_es_el_inventario_inicial()
        Assert.Equal(MovimientosArticuloRepositorio.ReglaInicial.Inicial, MovimientosArticuloRepositorio.DecidirInicial(New Date(2020, 1, 1), FechaInicial, True))
    End Sub

    <Fact>
    Public Sub Sin_la_casilla_compara_al_segundo_con_la_fecha_del_inicial()
        Assert.Equal(MovimientosArticuloRepositorio.ReglaInicial.Cero, MovimientosArticuloRepositorio.DecidirInicial(New Date(2025, 9, 23), FechaInicial, False))
        Assert.Equal(MovimientosArticuloRepositorio.ReglaInicial.Inicial, MovimientosArticuloRepositorio.DecidirInicial(FechaInicial.AddMilliseconds(400), FechaInicial, False))
        Assert.Equal(MovimientosArticuloRepositorio.ReglaInicial.Calcular, MovimientosArticuloRepositorio.DecidirInicial(New Date(2025, 10, 1), FechaInicial, False))
    End Sub

    <Fact>
    Public Sub El_resumido_agrupa_por_dia_o_por_mes_en_todas_las_partes()
        Dim diario = MovimientosArticuloRepositorio.SqlResumen(True)
        Assert.DoesNotContain("'%c'", diario)
        Assert.Contains("FROM posdevoluciones WHERE fc BETWEEN @desde AND @hasta AND articulo = @codigo AND Local = @local GROUP BY DATE_FORMAT(fc, '%d/%b/%Y')", diario)
        Assert.Contains("GROUP BY DATE_FORMAT(d.fc, '%d/%b/%Y')", diario)
        Dim mensual = MovimientosArticuloRepositorio.SqlResumen(False)
        Assert.DoesNotContain("%d/%b/%Y", mensual)
        Assert.EndsWith("GROUP BY F1 ORDER BY S.fc", mensual)
    End Sub

    <Fact>
    Public Sub Las_consultas_solo_leen()
        For Each sql In {MovimientosArticuloRepositorio.SqlResumen(True), MovimientosArticuloRepositorio.SqlDetalle, MovimientosArticuloRepositorio.SqlInventarioAl}
            Assert.DoesNotContain("CREATE", sql.ToUpperInvariant())
            Assert.DoesNotContain("posinventariotmp", sql)
        Next
    End Sub

End Class
