Imports CoopeNet.Core.Bancos

Public Class LibroMovimientosTests

    Private Shared Function Dia(fecha As Date, Optional salidas As Double = 0, Optional entradas As Double = 0, Optional salidasBanco As Double = 0, Optional entradasBanco As Double = 0) As MovimientoDia
        Return New MovimientoDia With {.Fecha = fecha, .Salidas = salidas, .Entradas = entradas, .SalidasBanco = salidasBanco, .EntradasBanco = entradasBanco}
    End Function

    <Fact>
    Public Sub Cierre_acumula_entradas_y_salidas_en_orden_de_fecha()
        Dim inicial As New SaldoCierre With {.Monto = 1000}
        Dim dias = {Dia(#2026-11-03#, salidas:=50), Dia(#2026-11-01#, salidas:=200, entradas:=500)}

        Dim cierres = CierresDiarios.Calcular(inicial, dias)

        Assert.Equal({#2026-11-01#, #2026-11-03#}, cierres.Select(Function(c) c.Fecha))
        Assert.Equal({1300.0, 1250.0}, cierres.Select(Function(c) c.Monto))
    End Sub

    <Fact>
    Public Sub Cierre_en_transito_sube_con_el_libro_y_baja_cuando_el_banco_lo_registra()
        Dim dias = {Dia(#2026-11-01#, salidas:=200, entradas:=70), Dia(#2026-11-02#, salidasBanco:=200, entradasBanco:=70)}

        Dim cierres = CierresDiarios.Calcular(New SaldoCierre(), dias)

        Assert.Equal({200.0, 0.0}, cierres.Select(Function(c) c.Transito))
        Assert.Equal({70.0, 0.0}, cierres.Select(Function(c) c.DepTransito))
        Assert.Equal({-130.0, -130.0}, cierres.Select(Function(c) c.Monto))
    End Sub

    <Fact>
    Public Sub Cierre_omite_los_dias_sin_cambios()
        Dim dias = {Dia(#2026-11-01#, salidas:=100, salidasBanco:=100, entradas:=100, entradasBanco:=100)}
        Assert.Empty(CierresDiarios.Calcular(New SaldoCierre With {.Monto = 5}, dias))
    End Sub

    <Fact>
    Public Sub Cierre_escribe_numeros_con_punto_y_parte_las_sentencias_grandes()
        Dim cierres = Enumerable.Range(0, 201).Select(Function(i) New CierreDia With {.Fecha = #2026-01-01#.AddDays(i), .Monto = 1.5}).ToList()

        Dim sentencias = CierresDiarios.Sentencias(3, 1, "O'1", cierres)

        Assert.Equal(2, sentencias.Count)
        Assert.Contains("(3,1,'O\'1','2026-01-01',1.5,0,0)", sentencias(0))
        Assert.StartsWith("REPLACE INTO ccbcierrediario", sentencias(1))
    End Sub

    <Fact>
    Public Sub Insertar_sin_conciliar_deja_la_fecha_del_banco_vacia_y_escapa_el_texto()
        Dim movimiento As New MovimientoBanco With {
            .Ids = 7, .Local = 2, .Banco = 1, .Cuenta = "1253", .Fecha = #2026-11-06#, .Documento = "D'1", .TipoMov = 3, .Tipo = 1, .Monto = 1234.5D, .Detalle = "Pago\'x"
        }
        Assert.Equal("INSERT INTO ccblibro VALUES (7,2,1,'1253','2026-11-06','D\'1',3,1,1234.5,'Pago\\\'x','0','1900-01-01','',9,NOW())",
                     MovimientosLibro.SentenciaInsertar(movimiento, 9))
    End Sub

    <Fact>
    Public Sub Actualizar_conciliado_guarda_los_datos_del_banco_y_usa_el_local_original()
        Dim movimiento As New MovimientoBanco With {
            .Ids = 7, .Local = 2, .Banco = 1, .Cuenta = "1253", .Fecha = #2026-11-06#, .Documento = "D1", .Monto = 10D, .Detalle = "x",
            .Conciliado = True, .BancoFecha = #2026-11-07#, .BancoDetalle = "Banco"
        }
        Dim sql = MovimientosLibro.SentenciaActualizar(movimiento)
        Assert.Contains("conciliado = 1, bancofecha = '2026-11-07', bancodetalle = 'Banco'", sql)
        Assert.EndsWith("WHERE local = 2 AND ids = 7 AND banco = 1 AND cuenta = '1253'", sql)
    End Sub

    <Fact>
    Public Sub Actualizar_sin_conciliar_borra_los_datos_del_banco()
        Dim movimiento As New MovimientoBanco With {.Ids = 7, .Fecha = #2026-11-06#, .BancoFecha = #2026-11-07#, .BancoDetalle = "Banco"}
        Assert.Contains("conciliado = 0, bancofecha = '1900-01-01', bancodetalle = ''", MovimientosLibro.SentenciaActualizar(movimiento))
    End Sub

End Class
