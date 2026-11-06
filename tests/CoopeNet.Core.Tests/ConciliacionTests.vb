Imports CoopeNet.Core.Bancos

Public Class ConciliacionTests

    Private Const Cuenta As String = "100010190012533"

    ''' <summary>Arma un registro de 120 caracteres con las posiciones de ReadTECFileBNCR.</summary>
    Private Shared Function Registro1(tipo As Integer, documento As String, ddmmyyyy As String, monto As String, detalle As String, Optional detalle2 As String = "") As String
        Return ("1" & tipo & documento.PadLeft(8, "0"c) & ddmmyyyy & monto.PadLeft(16, "0"c) & detalle.PadRight(30) & New String(" "c, 8) & detalle2.PadRight(48)).PadRight(120)
    End Function

    Private Shared Function Registro0(cuentaBanco As String) As String
        Return ("0" & cuentaBanco).PadRight(120)
    End Function

    Private Shared Function Registro2() As String
        Return "2".PadRight(120)
    End Function

    <Fact>
    Public Sub Tec_lee_tipo_fecha_monto_y_prefiere_el_segundo_detalle()
        Dim lineas = {Registro0(Cuenta), Registro1(1, "00012345", "29102026", "123456", "CORTO", "DETALLE LARGO"), Registro1(4, "77", "01112026", "1000", "DEPOSITO"), Registro2()}

        Dim movimientos = ArchivoTec.Leer(lineas, Cuenta)

        Assert.Equal(2, movimientos.Count)
        Assert.Equal(#2026-10-29#, movimientos(0).Fecha)
        Assert.Equal("12345", movimientos(0).Documento)
        Assert.Equal(1234.56, movimientos(0).Monto)
        Assert.Equal("DETALLE LARGO", movimientos(0).Detalle)
        Assert.Equal(0, movimientos(0).Tipo)
        Assert.Equal(0, movimientos(0).TipoMov)
        Assert.Equal(1, movimientos(1).Tipo)
        Assert.Equal(3, movimientos(1).TipoMov)
        Assert.Equal("DEPOSITO", movimientos(1).Detalle)
    End Sub

    <Theory>
    <InlineData(1, 0, 0)>
    <InlineData(2, 1, 1)>
    <InlineData(3, 0, 2)>
    <InlineData(4, 1, 3)>
    Public Sub Tec_clasifica_debito_credito_cheque_y_deposito(tipoTec As Integer, tipo As Integer, tipoMov As Integer)
        Dim m = ArchivoTec.Leer({Registro0(Cuenta), Registro1(tipoTec, "1", "01012026", "100", "X")}, Cuenta).Single()
        Assert.Equal(tipo, m.Tipo)
        Assert.Equal(tipoMov, m.TipoMov)
    End Sub

    <Fact>
    Public Sub Tec_solo_toma_los_movimientos_de_la_cuenta_pedida()
        Dim lineas = {Registro0("999999999999999"), Registro1(1, "1", "01012026", "100", "OTRA"), Registro2(), Registro0(Cuenta), Registro1(1, "2", "01012026", "200", "MIA"), Registro2()}
        Assert.Equal("MIA", ArchivoTec.Leer(lineas, Cuenta).Single().Detalle)
    End Sub

    <Fact>
    Public Sub Tec_rechaza_largo_incorrecto_y_avisa_si_no_trae_la_cuenta()
        Assert.Equal(EstadoArchivoTec.NoValido, ArchivoTec.Verificar({"0" & Cuenta}, Cuenta))
        Assert.Equal(EstadoArchivoTec.NoValido, ArchivoTec.Verificar({"X".PadRight(120)}, Cuenta))
        Assert.Equal(EstadoArchivoTec.SinCuenta, ArchivoTec.Verificar({Registro0("111111111111111")}, Cuenta))
        Assert.Equal(EstadoArchivoTec.Valido, ArchivoTec.Verificar({Registro0(Cuenta), Registro2()}, Cuenta))
    End Sub

    <Theory>
    <InlineData("RECIBE DE JUAN", "ccblibrobancort")>
    <InlineData("  TRANSF. A PEDRO", "ccblibrobancort")>
    <InlineData("DEPOSITO", "ccblibrobanco")>
    Public Sub Transferencias_entre_cuentas_van_aparte(detalle As String, tabla As String)
        Assert.Equal(tabla, ConciliacionBancaria.Tabla(#2026-10-15#, #2026-10-01#, #2026-10-31#, detalle))
    End Sub

    <Fact>
    Public Sub Fecha_fuera_de_rango_va_a_errores_aunque_la_transferencia_no()
        Assert.Equal("ccblibrobancomal", ConciliacionBancaria.Tabla(#2026-09-30#, #2026-10-01#, #2026-10-31#, "DEPOSITO"))
        Assert.Equal("ccblibrobancort", ConciliacionBancaria.Tabla(#2026-09-30#, #2026-10-01#, #2026-10-31#, "RECIBE DE X"))
    End Sub

    <Fact>
    Public Sub Csv_toma_debitos_y_creditos_y_conserva_el_tipomov_de_la_base()
        Dim columnas = ColumnasCsv.DesdeFinport("01234")
        Dim filas = {
            New String() {"15/10/2026", "123", "CHEQUE", "1000.50", "0"},
            New String() {"16/10/2026", "124", "DEPOSITO", "0", "250"},
            New String() {"Fecha", "Doc", "Detalle", "Deb", "Cre"},
            New String() {"17/10/2026", "ABC", "SIN NUMERO", "5", "0"},
            New String() {"18/10/2026", "125", "EN CERO", "0", "0"}
        }

        Dim leidos = ArchivoCsv.Leer(filas, columnas, #2026-10-01#, #2026-10-31#)

        Assert.Equal(2, leidos.Count)
        Assert.Equal(0, leidos(0).Tipo)
        Assert.Equal(1, leidos(0).TipoMov)
        Assert.Equal(1000.5, leidos(0).Monto)
        Assert.Equal(1, leidos(1).Tipo)
        Assert.Equal(0, leidos(1).TipoMov)
    End Sub

    <Fact>
    Public Sub Csv_prueba_con_mes_y_dia_invertidos_si_la_fecha_cae_fuera_del_rango()
        Assert.Equal(#2026-10-03#, ArchivoCsv.LeerFecha("03/10/2026", #2026-10-01#, #2026-10-31#))
        Assert.Equal(#2026-10-03#, ArchivoCsv.LeerFecha("10/03/2026", #2026-10-01#, #2026-10-31#))
        Assert.Equal(#2026-09-30#, ArchivoCsv.LeerFecha("30/09/2026", #2026-10-01#, #2026-10-31#))
        Assert.Null(ArchivoCsv.LeerFecha("nada", #2026-10-01#, #2026-10-31#))
    End Sub

    <Fact>
    Public Sub Csv_respeta_comillas_y_el_separador()
        Assert.Equal({"a", "1,000.50", "b"}, ArchivoCsv.Separar("a,""1,000.50"",b", ","c))
    End Sub

    <Fact>
    Public Sub Columnas_se_guardan_un_digito_por_columna_y_detectan_repetidas()
        Dim c = ColumnasCsv.DesdeFinport("01423")
        Assert.Equal(4, c.Detalle)
        Assert.Equal(3, c.Creditos)
        Assert.Equal("01423", c.AFinport())
        Assert.False(c.Repetidas)
        Assert.True(ColumnasCsv.DesdeFinport("00000").Repetidas)
    End Sub

    Private Shared Function Fila(tipo As Integer, monto As Double, fecha As Date, conciliado As Boolean) As FilaConciliacion
        Return New FilaConciliacion With {.Tipo = tipo, .Monto = monto, .Fecha = fecha, .Conciliado = conciliado}
    End Function

    <Fact>
    Public Sub Informe_descuenta_del_mes_lo_aplicado_de_meses_anteriores()
        Dim d As New DatosInforme With {
            .Inicio = #2026-10-01#, .Fin = #2026-10-31#, .MontoInicial = 1000, .ChequesAnteriores = 300, .ChequesAplicados = -300, .ChequesActuales = 50, .DepositosActuales = 20
        }
        d.Filas.Add(Fila(0, 300, #2026-09-20#, True))
        d.Filas.Add(Fila(0, 100, #2026-10-05#, True))
        d.Filas.Add(Fila(0, 50, #2026-10-06#, False))
        d.Filas.Add(Fila(1, 500, #2026-10-07#, True))
        d.Filas.Add(Fila(1, 20, #2026-10-08#, False))

        Dim i = InformeConciliacion.Calcular(d)

        Assert.Equal(450, i.TotalDebitos)
        Assert.Equal(520, i.TotalCreditos)
        Assert.Equal(1370, i.SaldoLibros)
        Assert.Equal(50, i.ChequesTotal)
        Assert.Equal(20, i.DepositosTotal)
        Assert.Equal(1400, i.SaldoBanco)
        Assert.Equal(400, i.DebitosConciliados)
        Assert.Equal(500, i.CreditosConciliados)
    End Sub

End Class
