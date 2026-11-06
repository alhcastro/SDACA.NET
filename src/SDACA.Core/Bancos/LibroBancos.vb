Imports System.Globalization
Imports System.Threading
Imports Dapper
Imports MySqlConnector
Imports SDACA.Core.Datos

Namespace Bancos

    ''' <summary>Movimientos de ccblibro. Migración de lo que SD1501 usa de SD1401\ModBancos.bas.</summary>
    Public NotInheritable Class LibroBancos

        Private Const IdentificadorReplica As String = "BANCOS"
        Private Const TablaNoExiste As Integer = 1146

        Private Sub New()
        End Sub

        ''' <summary>
        ''' CCBMesCerrado. Una base sin ccbcierre (ver sql\2026-11-06-ccbcierre.sql) cuenta como mes
        ''' abierto: en VB6 ese error se pierde en RS_Ejecutar y el resultado es 0. Cualquier otro
        ''' error detiene la operación.
        ''' </summary>
        Public Shared Async Function MesCerradoAsync(cn As MySqlConnection, banco As Long, cuenta As String, fecha As Date, Optional ct As CancellationToken = Nothing) As Task(Of Boolean)
            Const sql As String = "SELECT COUNT(*) FROM ccbcierre WHERE banco = @banco AND cuenta = @cuenta AND mes = @mes"
            Dim mes = MesDeCierre(fecha)
            Try
                Return Await cn.ExecuteScalarAsync(Of Long)(New CommandDefinition(sql, New With {banco, cuenta, mes}, cancellationToken:=ct)).ConfigureAwait(False) > 0
            Catch ex As MySqlException When ex.Number = TablaNoExiste
                Return False
            End Try
        End Function

        ''' <summary>Formato de <c>ccbcierre.mes</c>: Format(fecha, "MMYYYY").</summary>
        Public Shared Function MesDeCierre(fecha As Date) As String
            Return fecha.ToString("MMyyyy", CultureInfo.InvariantCulture)
        End Function

        ''' <summary>
        ''' iCCBAnularLibro. Si el mes está abierto borra el movimiento y su asiento. Si está cerrado
        ''' lo marca como anulado y crea el movimiento contrario. Las sentencias se arman como texto
        ''' porque el mismo texto se replica a los otros locales.
        ''' </summary>
        ''' <returns>False si el movimiento no existe.</returns>
        Public Shared Async Function AnularAsync(cn As MySqlConnection, banco As Long, cuenta As String, ids As Long, local As Long, uid As Long, Optional ct As CancellationToken = Nothing) As Task(Of Boolean)
            Const consulta As String = "SELECT fecha AS Fecha, conciliado AS Conciliado FROM ccblibro WHERE banco = @banco AND cuenta = @cuenta AND ids = @ids"
            Dim movimiento = Await cn.QueryFirstOrDefaultAsync(Of MovimientoLibro)(New CommandDefinition(consulta, New With {banco, cuenta, ids}, cancellationToken:=ct)).ConfigureAwait(False)
            If movimiento Is Nothing Then Return False

            Dim condicion = $"banco = {banco} AND cuenta = '{Literal(cuenta)}' AND ids = {ids}"
            If Not Await MesCerradoAsync(cn, banco, cuenta, movimiento.Fecha, ct).ConfigureAwait(False) Then
                Await EjecutarAsync(cn, "DELETE FROM ccblibro WHERE " & condicion, local, ct).ConfigureAwait(False)
                Await EjecutarAsync(cn, "DELETE FROM ccbconta WHERE " & condicion, local, ct).ConfigureAwait(False)
                Return True
            End If

            If movimiento.Conciliado = "0" Then
                Await EjecutarAsync(cn, "UPDATE ccblibro SET conciliado = '1', bancofecha = CURDATE(), bancodetalle = 'Movimiento Anulado' WHERE " & condicion, local, ct).ConfigureAwait(False)
            End If
            Dim reversion = "INSERT INTO ccblibro SELECT NULL, local, banco, cuenta, CURDATE(), documento, tipomov, IF(tipo = 0, 1, 0) AS tipo, monto, " &
                            "CONCAT('Reversión de ', detalle) AS detalle, '1', CURDATE(), CONCAT('Reversión de ', detalle) AS bancodetalle, " &
                            $"{uid}, NOW() FROM ccblibro WHERE " & condicion
            Await EjecutarAsync(cn, reversion, local, ct).ConfigureAwait(False)
            Return True
        End Function

        Friend Shared Async Function EjecutarAsync(cn As MySqlConnection, sql As String, local As Long, ct As CancellationToken) As Task
            Await cn.ExecuteAsync(New CommandDefinition(sql, cancellationToken:=ct)).ConfigureAwait(False)
            Await Replicador.ReplicarAsync(cn, IdentificadorReplica, sql, local, ct).ConfigureAwait(False)
        End Function

        Friend Shared Function Literal(texto As String) As String
            Return texto.Replace("\", "\\").Replace("'", "\'")
        End Function

        Private NotInheritable Class MovimientoLibro
            Public Property Fecha As Date
            Public Property Conciliado As String = ""
        End Class

    End Class

End Namespace
