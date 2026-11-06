Imports SDACA.Core.Bancos
Imports SDACA.Core.Datos

Public Class ConsecutivoTests

    <Theory>
    <InlineData(0, 8, "8")>
    <InlineData(1, 8, "10000008")>
    <InlineData(12, 345, "120000345")>
    Public Sub Local_cero_numera_de_corrido_y_los_demas_anteponen_su_numero(local As Long, contador As Long, esperado As String)
        Assert.Equal(esperado, Consecutivo.Componer(local, contador, Consecutivo.TamanoPredeterminado))
    End Sub

    <Fact>
    Public Sub Local_cero_ignora_los_codigos_de_otros_locales()
        Assert.Equal("SELECT IFNULL(MAX(tipo),0) + 1 FROM ccbtiposmov WHERE tipo < 9999999", Consecutivo.Consulta("ccbtiposmov", "tipo", 0, 7))
    End Sub

    <Fact>
    Public Sub Otro_local_cuenta_solo_lo_suyo_con_los_ultimos_siete_digitos()
        Dim sql = Consecutivo.Consulta("ccbtiposmov", "tipo", 3, 7)
        Assert.Contains("MID(tipo,LENGTH(tipo)-6)", sql)
        Assert.Contains("MID(tipo,1,LENGTH(tipo)-7) = @local", sql)
    End Sub

    <Fact>
    Public Sub Saldo_inicial_resta_cheques_y_suma_depositos()
        Assert.Equal(850, CuentaBancaria.SaldoInicial(1000, 200, 50))
    End Sub

    <Fact>
    Public Sub Guardar_cuenta_nombra_las_veinte_columnas_y_escapa_el_texto()
        Dim c As New CuentaBancaria With {.Local = 1, .Banco = 2, .Cuenta = "123", .Descripcion = "O'Brien", .MontoInicial = 1234.5}
        Dim sql = CuentasBancariasRepositorio.SentenciaGuardar(c, 7, New Date(2026, 11, 6, 8, 30, 0))
        Assert.Equal(20, sql.Substring(sql.IndexOf("("c), sql.IndexOf(")"c) - sql.IndexOf("("c)).Split(","c).Length)
        Assert.Contains("'O\'Brien'", sql)
        Assert.Contains(",1234.5,", sql)
        Assert.EndsWith(",7,'2026-11-06 08:30:00')", sql)
    End Sub

    <Fact>
    Public Sub Mes_de_cierre_es_MMYYYY()
        Assert.Equal("012026", LibroBancos.MesDeCierre(New Date(2026, 1, 31)))
    End Sub

    <Theory>
    <InlineData("0", TipoMovimiento.Resta)>
    <InlineData("1", TipoMovimiento.Suma)>
    <InlineData("", TipoMovimiento.Resta)>
    Public Sub Efecto_del_tipo_de_movimiento(tipot As String, esperado As Integer)
        Assert.Equal(esperado, New TipoMovimiento With {.Tipo = tipot}.Efecto)
    End Sub

End Class
