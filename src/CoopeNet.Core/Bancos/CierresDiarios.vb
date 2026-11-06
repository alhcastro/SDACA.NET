Imports System.Globalization
Imports System.Text
Imports System.Threading
Imports Dapper
Imports MySqlConnector

Namespace Bancos

    ''' <summary>Saldos de <c>ccbcierrediario</c>, o los iniciales de la cuenta.</summary>
    Public NotInheritable Class SaldoCierre
        Public Property Monto As Double
        Public Property Transito As Double
        Public Property DepTransito As Double
    End Class

    ''' <summary>Totales de un día: por fecha del libro y por fecha en que el banco los registró.</summary>
    Public NotInheritable Class MovimientoDia
        Public Property Fecha As Date
        Public Property Salidas As Double
        Public Property Entradas As Double
        Public Property SalidasBanco As Double
        Public Property EntradasBanco As Double
    End Class

    Public NotInheritable Class CierreDia
        Public Property Fecha As Date
        Public Property Monto As Double
        Public Property Transito As Double
        Public Property DepTransito As Double
    End Class

    ''' <summary>
    ''' CCB_Cierre de SD1401\ModPrin.bas. Allá son cinco consultas por día; aquí son dos en total. También
    ''' calcula antes de borrar, para que un error de lectura no deje la cuenta sin cierres. Como en VB6,
    ''' no se replica y un día sin cambios no lleva fila.
    ''' </summary>
    Public NotInheritable Class CierresDiarios

        Private Const FilasPorSentencia As Integer = 200

        Private Sub New()
        End Sub

        ''' <summary>Acumula día a día desde <paramref name="inicial"/>.</summary>
        Public Shared Function Calcular(inicial As SaldoCierre, dias As IEnumerable(Of MovimientoDia)) As List(Of CierreDia)
            Dim monto = inicial.Monto
            Dim transito = inicial.Transito
            Dim depTransito = inicial.DepTransito
            Dim cierres As New List(Of CierreDia)
            For Each dia In dias.OrderBy(Function(d) d.Fecha)
                Dim cambioMonto = dia.Entradas - dia.Salidas
                Dim cambioTransito = dia.Salidas - dia.SalidasBanco
                Dim cambioDepTransito = dia.Entradas - dia.EntradasBanco
                If cambioMonto = 0 AndAlso cambioTransito = 0 AndAlso cambioDepTransito = 0 Then Continue For
                monto += cambioMonto
                transito += cambioTransito
                depTransito += cambioDepTransito
                cierres.Add(New CierreDia With {.Fecha = dia.Fecha, .Monto = monto, .Transito = transito, .DepTransito = depTransito})
            Next
            Return cierres
        End Function

        ''' <summary>Rehace los cierres de la cuenta desde <paramref name="desde"/> hasta <paramref name="hoy"/>.</summary>
        Public Shared Async Function RecalcularAsync(cn As MySqlConnection, local As Long, banco As Long, cuenta As String, desde As Date, hoy As Date, Optional ct As CancellationToken = Nothing) As Task
            Dim inicial = Await SaldoInicialAsync(cn, banco, cuenta, desde, ct).ConfigureAwait(False)
            Dim dias = Await LeerDiasAsync(cn, banco, cuenta, desde, hoy, ct).ConfigureAwait(False)
            Dim cierres = Calcular(inicial, dias)

            Const borrar As String = "DELETE FROM ccbcierrediario WHERE banco = @banco AND cuenta = @cuenta AND fecha >= @desde"
            Await cn.ExecuteAsync(New CommandDefinition(borrar, New With {banco, cuenta, desde}, cancellationToken:=ct)).ConfigureAwait(False)
            For Each sentencia In Sentencias(local, banco, cuenta, cierres)
                Await cn.ExecuteAsync(New CommandDefinition(sentencia, cancellationToken:=ct)).ConfigureAwait(False)
            Next
        End Function

        Private Shared Async Function SaldoInicialAsync(cn As MySqlConnection, banco As Long, cuenta As String, desde As Date, ct As CancellationToken) As Task(Of SaldoCierre)
            Const cierre As String = "SELECT monto AS Monto, transito AS Transito, deptransito AS DepTransito FROM ccbcierrediario " &
                                     "WHERE banco = @banco AND cuenta = @cuenta AND fecha < @desde ORDER BY fecha DESC LIMIT 1"
            Const inicial As String = "SELECT montoinicial AS Monto, montoinicialTransito AS Transito, montoinicialdeptransito AS DepTransito " &
                                      "FROM ccbcuentas WHERE banco = @banco AND cuenta = @cuenta"
            Dim fila = Await cn.QueryFirstOrDefaultAsync(Of SaldoCierre)(New CommandDefinition(cierre, New With {banco, cuenta, desde}, cancellationToken:=ct)).ConfigureAwait(False)
            If fila Is Nothing Then fila = Await cn.QueryFirstOrDefaultAsync(Of SaldoCierre)(New CommandDefinition(inicial, New With {banco, cuenta}, cancellationToken:=ct)).ConfigureAwait(False)
            Return If(fila, New SaldoCierre())
        End Function

        Private Shared Async Function LeerDiasAsync(cn As MySqlConnection, banco As Long, cuenta As String, desde As Date, hoy As Date, ct As CancellationToken) As Task(Of List(Of MovimientoDia))
            Const libro As String = "SELECT fecha AS Fecha, SUM(IF(tipo = '0', monto, 0)) AS Salidas, SUM(IF(tipo = '1', monto, 0)) AS Entradas FROM ccblibro " &
                                    "WHERE banco = @banco AND cuenta = @cuenta AND fecha >= @desde AND fecha <= @hoy GROUP BY fecha"
            Const registrados As String = "SELECT bancofecha AS Fecha, SUM(IF(tipo = '0', monto, 0)) AS SalidasBanco, SUM(IF(tipo = '1', monto, 0)) AS EntradasBanco FROM ccblibro " &
                                          "WHERE banco = @banco AND cuenta = @cuenta AND bancofecha >= @desde AND bancofecha <= @hoy GROUP BY bancofecha"
            Dim parametros = New With {banco, cuenta, desde, hoy}
            Dim porFecha = (Await cn.QueryAsync(Of MovimientoDia)(New CommandDefinition(libro, parametros, cancellationToken:=ct)).ConfigureAwait(False)).ToDictionary(Function(d) d.Fecha)
            For Each dia In Await cn.QueryAsync(Of MovimientoDia)(New CommandDefinition(registrados, parametros, cancellationToken:=ct)).ConfigureAwait(False)
                Dim existente As MovimientoDia = Nothing
                If porFecha.TryGetValue(dia.Fecha, existente) Then
                    existente.SalidasBanco = dia.SalidasBanco
                    existente.EntradasBanco = dia.EntradasBanco
                Else
                    porFecha.Add(dia.Fecha, dia)
                End If
            Next
            Return porFecha.Values.ToList()
        End Function

        Friend Shared Function Sentencias(local As Long, banco As Long, cuenta As String, cierres As IReadOnlyList(Of CierreDia)) As List(Of String)
            Dim resultado As New List(Of String)
            For inicio = 0 To cierres.Count - 1 Step FilasPorSentencia
                Dim texto As New StringBuilder("REPLACE INTO ccbcierrediario (local, banco, cuenta, fecha, monto, transito, deptransito) VALUES ")
                Dim fin = Math.Min(inicio + FilasPorSentencia, cierres.Count) - 1
                For i = inicio To fin
                    Dim c = cierres(i)
                    If i > inicio Then texto.Append(","c)
                    Dim fecha = c.Fecha.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                    texto.Append($"({local},{banco},'{LibroBancos.Literal(cuenta)}','{fecha}',{Numero(c.Monto)},{Numero(c.Transito)},{Numero(c.DepTransito)})")
                Next
                resultado.Add(texto.ToString())
            Next
            Return resultado
        End Function

        Private Shared Function Numero(valor As Double) As String
            Return valor.ToString("R", CultureInfo.InvariantCulture)
        End Function

    End Class

End Namespace
