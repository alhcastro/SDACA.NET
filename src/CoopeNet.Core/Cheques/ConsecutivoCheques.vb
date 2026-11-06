Imports System.Threading
Imports Dapper
Imports MySqlConnector

Namespace Cheques

    ''' <summary>
    ''' ECCNextCheque de SD1501\ModCheque.bas y LoadImpresoraConsecutivo de Comun. Busca el consecutivo
    ''' de la cuenta en gatimpresion: primero la estación en el local, luego la estación en cualquier
    ''' local, luego el local en cualquier estación y al final cualquiera de los dos. Ahí el número es
    ''' el último usado. Si no hay ninguno, usa ccbcuentas.numcheque, que es el siguiente por usar.
    ''' </summary>
    Public NotInheritable Class ConsecutivoCheques

        Private Sub New()
        End Sub

        Public Shared Async Function SiguienteAsync(cn As MySqlConnection, local As Long, estacion As String, banco As Long, cuenta As String, actualizar As Boolean, Optional ct As CancellationToken = Nothing) As Task(Of Long)
            Const consulta As String = "SELECT consecutivo FROM gatimpresion WHERE local = @l AND estacion = @e AND documento = @documento AND llevaconsecutivo = '1' LIMIT 1"
            Const avance As String = "UPDATE gatimpresion SET consecutivo = @siguiente WHERE local = @l AND estacion = @e AND documento = @documento AND llevaconsecutivo = '1'"
            Const cuentaConsulta As String = "SELECT numcheque FROM ccbcuentas WHERE banco = @banco AND cuenta = @cuenta LIMIT 1"
            Const cuentaAvance As String = "UPDATE ccbcuentas SET numcheque = @siguiente WHERE banco = @banco AND cuenta = @cuenta"

            Dim documento = "CHK" & cuenta.Trim()
            For Each origen In {(l:=local, e:=estacion), (l:=-1L, e:=estacion), (l:=local, e:="-1"), (l:=-1L, e:="-1")}
                Dim actual = Await cn.ExecuteScalarAsync(Of Double?)(New CommandDefinition(consulta, New With {origen.l, origen.e, documento}, cancellationToken:=ct)).ConfigureAwait(False)
                If actual.HasValue Then
                    Dim siguiente = CLng(actual.Value) + 1
                    If actualizar Then Await cn.ExecuteAsync(New CommandDefinition(avance, New With {siguiente, origen.l, origen.e, documento}, cancellationToken:=ct)).ConfigureAwait(False)
                    Return siguiente
                End If
            Next

            Dim numero = If(Await cn.ExecuteScalarAsync(Of Long?)(New CommandDefinition(cuentaConsulta, New With {banco, cuenta}, cancellationToken:=ct)).ConfigureAwait(False), 0L)
            If actualizar Then Await cn.ExecuteAsync(New CommandDefinition(cuentaAvance, New With {.siguiente = numero + 1, banco, cuenta}, cancellationToken:=ct)).ConfigureAwait(False)
            Return numero
        End Function

    End Class

End Namespace
