Imports System.Threading
Imports Dapper
Imports CoopeNet.Core.Datos

Namespace Bancos

    ''' <summary>Año contable de 12 meses que empieza en <see cref="MesInicio"/>.</summary>
    Public NotInheritable Class PeriodoFiscal

        ''' <summary>Valor de SD_CargarEmpresa si InitPer es 0 o la empresa no existe.</summary>
        Public Const MesInicioPredeterminado As Integer = 10

        Public ReadOnly Property Anio As Integer
        Public ReadOnly Property MesInicio As Integer

        Public Sub New(anio As Integer, mesInicio As Integer)
            Me.Anio = anio
            Me.MesInicio = mesInicio
        End Sub

        Public ReadOnly Property Inicio As Date
            Get
                Return New Date(Anio, MesInicio, 1)
            End Get
        End Property

        ''' <summary>Primer día del periodo siguiente, que ya queda fuera.</summary>
        Public ReadOnly Property Fin As Date
            Get
                Return Inicio.AddYears(1)
            End Get
        End Property

        Public Overrides Function ToString() As String
            Return $"{Anio}-{If(MesInicio = 1, Anio, Anio + 1)}"
        End Function

    End Class

    ''' <summary>
    ''' Fila de Saldos por Cuenta. Débito son las entradas (tipo 1) y Crédito las salidas (tipo 0),
    ''' igual que en FrmLibro: los cheques van en Crédito.
    ''' </summary>
    Public NotInheritable Class SaldoMes
        Public Property Mes As Date
        Public Property SaldoInicial As Decimal
        Public Property Debito As Decimal
        Public Property Credito As Decimal

        Public ReadOnly Property SaldoFinal As Decimal
            Get
                Return SaldoInicial + Debito - Credito
            End Get
        End Property
    End Class

    ''' <summary>Fila de la consulta mensual, antes de ubicarla en el periodo.</summary>
    Public NotInheritable Class MovimientoMes
        Public Property Anio As Integer
        Public Property Mes As Integer
        Public Property SaldoInicial As Decimal
        Public Property Debito As Decimal
        Public Property Credito As Decimal
    End Class

    ''' <summary>
    ''' Saldos mensuales de una cuenta. Migración de SD1401\FrmSaldosBanco.frm. Difiere de VB6 en dos
    ''' cosas. El periodo termina el último día del mes 12: allá se arma con MesInicio + 11 y día 30, que
    ''' omite el día 31 y con MesInicio 10 da una fecha inválida. Y Saldo Actual parte del último cierre
    ''' aunque el mes no tenga movimientos, como CCBLoadMovimientos de FrmLibro: allá vuelve al monto inicial.
    ''' Como en VB6, un mes sin movimientos sale en cero, sin arrastrar el saldo.
    ''' </summary>
    Public NotInheritable Class SaldosCuenta

        Private Const SaldoAnterior As String =
            "IFNULL((SELECT ccd.monto FROM ccbcierrediario ccd WHERE ccd.banco = c.banco AND ccd.cuenta = c.cuenta AND ccd.fecha < {0} ORDER BY ccd.fecha DESC LIMIT 1), c.montoinicial)"

        Private ReadOnly _fabrica As IFabricaConexiones

        Public Sub New(fabrica As IFabricaConexiones)
            _fabrica = fabrica
        End Sub

        ''' <summary>InitPer de la empresa de la licencia, como ObtenerFormatoCuentaAsync.</summary>
        Public Async Function ObtenerMesInicioAsync(cedula As String, Optional ct As CancellationToken = Nothing) As Task(Of Integer)
            Const sql As String = "SELECT InitPer FROM sgcempresas WHERE Cedula = @cedula LIMIT 1"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim valor = Await cn.ExecuteScalarAsync(Of Integer?)(New CommandDefinition(sql, New With {cedula}, cancellationToken:=ct)).ConfigureAwait(False)
                Return MesInicioValido(valor.GetValueOrDefault())
            End Using
        End Function

        Public Shared Function MesInicioValido(initPer As Integer) As Integer
            Return If(initPer = 0, PeriodoFiscal.MesInicioPredeterminado, initPer)
        End Function

        ''' <summary>Cuentas del local y las que tienen cuenta contable en él (ccbcuentasconta).</summary>
        Public Async Function ListarCuentasAsync(banco As Integer, local As Long, uid As Long, todasLasCuentas As Boolean, Optional ct As CancellationToken = Nothing) As Task(Of List(Of CuentaCierre))
            Dim permiso = If(todasLasCuentas, "", " INNER JOIN ccbusuarioscuentas uc ON (uc.banco = c.banco AND uc.cuenta = c.cuenta AND uc.usuario = @uid) ")
            Const columnas As String = "SELECT c.cuenta AS Cuenta, c.descripcion AS Descripcion, IFNULL(m.descripcion,'') AS Moneda "
            Const moneda As String = " LEFT JOIN gatmonedas m ON (m.moneda = c.moneda) "
            Dim sql = columnas & "FROM ccbcuentas c" & moneda & permiso & "WHERE c.banco = @banco AND c.local = @local " &
                      "UNION " &
                      columnas & "FROM ccbcuentasconta a INNER JOIN ccbcuentas c ON (c.banco = a.banco AND c.cuenta = a.cuenta)" & moneda & permiso &
                      "WHERE a.banco = @banco AND a.local = @local"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim filas = Await cn.QueryAsync(Of CuentaCierre)(New CommandDefinition(sql, New With {banco, local, uid}, cancellationToken:=ct)).ConfigureAwait(False)
                Return filas.ToList()
            End Using
        End Function

        ''' <summary>Periodos con movimientos en cualquier cuenta, del más reciente al más viejo.</summary>
        Public Async Function ListarPeriodosAsync(mesInicio As Integer, Optional ct As CancellationToken = Nothing) As Task(Of List(Of PeriodoFiscal))
            Const sql As String = "SELECT DISTINCT IF(MONTH(fecha) < @mesInicio, YEAR(fecha) - 1, YEAR(fecha)) AS anio FROM ccblibro ORDER BY anio DESC"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim anios = Await cn.QueryAsync(Of Integer)(New CommandDefinition(sql, New With {mesInicio}, cancellationToken:=ct)).ConfigureAwait(False)
                Return anios.Select(Function(a) New PeriodoFiscal(a, mesInicio)).ToList()
            End Using
        End Function

        Public Async Function ListarMesesAsync(banco As Integer, cuenta As String, periodo As PeriodoFiscal, Optional ct As CancellationToken = Nothing) As Task(Of List(Of SaldoMes))
            Dim sql = "SELECT YEAR(l.fecha) AS Anio, MONTH(l.fecha) AS Mes, " &
                      String.Format(SaldoAnterior, "CAST(CONCAT(YEAR(l.fecha),'-',MONTH(l.fecha),'-01') AS DATE)") & " AS SaldoInicial, " &
                      "SUM(IF(l.tipo = '1', l.monto, 0)) AS Debito, SUM(IF(l.tipo = '0', l.monto, 0)) AS Credito " &
                      "FROM ccbcuentas c INNER JOIN ccblibro l ON (c.banco = l.banco AND c.cuenta = l.cuenta AND l.fecha >= @desde AND l.fecha < @hasta) " &
                      "WHERE c.banco = @banco AND c.cuenta = @cuenta GROUP BY YEAR(l.fecha), MONTH(l.fecha)"
            Dim parametros = New With {banco, cuenta, .desde = periodo.Inicio, .hasta = periodo.Fin}
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim filas = Await cn.QueryAsync(Of MovimientoMes)(New CommandDefinition(sql, parametros, cancellationToken:=ct)).ConfigureAwait(False)
                Return ArmarMeses(periodo, filas)
            End Using
        End Function

        ''' <summary>Los 12 meses del periodo en orden; los que no tienen movimientos, en cero.</summary>
        Public Shared Function ArmarMeses(periodo As PeriodoFiscal, movimientos As IEnumerable(Of MovimientoMes)) As List(Of SaldoMes)
            Dim porMes = movimientos.ToDictionary(Function(m) New Date(m.Anio, m.Mes, 1))
            Return Enumerable.Range(0, 12).Select(
                Function(i)
                    Dim mes = periodo.Inicio.AddMonths(i)
                    Dim fila As MovimientoMes = Nothing
                    If Not porMes.TryGetValue(mes, fila) Then Return New SaldoMes With {.Mes = mes}
                    Return New SaldoMes With {.Mes = mes, .SaldoInicial = fila.SaldoInicial, .Debito = fila.Debito, .Credito = fila.Credito}
                End Function).ToList()
        End Function

        Public Async Function SaldoActualAsync(banco As Integer, cuenta As String, hoy As Date, Optional ct As CancellationToken = Nothing) As Task(Of Decimal)
            Dim sql = "SELECT " & String.Format(SaldoAnterior, "@desde") &
                      " + IFNULL(SUM(IF(l.tipo = '1', l.monto, 0)), 0) - IFNULL(SUM(IF(l.tipo = '0', l.monto, 0)), 0) " &
                      "FROM ccbcuentas c LEFT JOIN ccblibro l ON (c.banco = l.banco AND c.cuenta = l.cuenta AND l.fecha >= @desde AND l.fecha < @hasta) " &
                      "WHERE c.banco = @banco AND c.cuenta = @cuenta GROUP BY c.banco, c.cuenta"
            Dim desde = New Date(hoy.Year, hoy.Month, 1)
            Dim parametros = New With {banco, cuenta, desde, .hasta = desde.AddMonths(1)}
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Await cn.ExecuteScalarAsync(Of Decimal)(New CommandDefinition(sql, parametros, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

    End Class

End Namespace
