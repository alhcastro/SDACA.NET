Imports CoopeNet.Core.Bancos

Public Class SaldosCuentaTests

    <Fact>
    Public Sub Periodo_de_octubre_termina_en_setiembre_del_ano_siguiente()
        Dim periodo As New PeriodoFiscal(2024, 10)
        Assert.Equal(New Date(2024, 10, 1), periodo.Inicio)
        Assert.Equal(New Date(2025, 10, 1), periodo.Fin)
        Assert.Equal("2024-2025", periodo.ToString())
    End Sub

    <Fact>
    Public Sub Periodo_de_enero_incluye_el_31_de_diciembre()
        Dim periodo As New PeriodoFiscal(2024, 1)
        Assert.Equal(New Date(2025, 1, 1), periodo.Fin)
        Assert.Equal("2024-2024", periodo.ToString())
    End Sub

    <Theory>
    <InlineData(0, 10)>
    <InlineData(1, 1)>
    <InlineData(10, 10)>
    Public Sub InitPer_en_cero_usa_octubre(initPer As Integer, esperado As Integer)
        Assert.Equal(esperado, SaldosCuenta.MesInicioValido(initPer))
    End Sub

    <Fact>
    Public Sub Los_doce_meses_salen_en_orden_y_sin_movimientos_quedan_en_cero()
        Dim movimientos = {
            New MovimientoMes With {.Anio = 2025, .Mes = 2, .SaldoInicial = 1000, .Debito = 300, .Credito = 50},
            New MovimientoMes With {.Anio = 2024, .Mes = 10, .SaldoInicial = 500, .Debito = 0, .Credito = 100}
        }
        Dim meses = SaldosCuenta.ArmarMeses(New PeriodoFiscal(2024, 10), movimientos)

        Assert.Equal(12, meses.Count)
        Assert.Equal(New Date(2024, 10, 1), meses(0).Mes)
        Assert.Equal(New Date(2025, 9, 1), meses(11).Mes)
        Assert.Equal(400D, meses(0).SaldoFinal)
        Assert.Equal(1250D, meses(4).SaldoFinal)
        Assert.Equal(0D, meses(1).SaldoInicial)
        Assert.Equal(0D, meses(1).SaldoFinal)
    End Sub

End Class
