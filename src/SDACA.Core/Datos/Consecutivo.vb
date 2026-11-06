Imports System.Data
Imports System.Threading
Imports Dapper

Namespace Datos

    ''' <summary>
    ''' GetConsecutivo de Comun\SD_MOD_UTILES01.bas. El local 0 numera de corrido y los demás anteponen
    ''' su número a un contador de siete dígitos. Como en VB6, es MAX + 1 sin
    ''' bloqueo: dos usuarios a la vez pueden obtener el mismo número.
    ''' </summary>
    Public NotInheritable Class Consecutivo

        Public Const TamanoPredeterminado As Integer = 7

        Private Sub New()
        End Sub

        ''' <param name="tabla">Nombre fijo del código, nunca un dato del usuario.</param>
        ''' <param name="campo">Nombre fijo del código, nunca un dato del usuario.</param>
        Public Shared Async Function SiguienteAsync(cn As IDbConnection, tabla As String, campo As String, local As Long,
                                                    Optional tamano As Integer = TamanoPredeterminado, Optional ct As CancellationToken = Nothing) As Task(Of String)
            Dim sql = Consulta(tabla, campo, local, tamano)
            Dim comando As New CommandDefinition(sql, New With {.local = local.ToString()}, cancellationToken:=ct)
            Dim valor = Await cn.ExecuteScalarAsync(Of Object)(comando).ConfigureAwait(False)
            Return Componer(local, Convert.ToInt64(valor), tamano)
        End Function

        Friend Shared Function Consulta(tabla As String, campo As String, local As Long, tamano As Integer) As String
            If local = 0 Then
                Return $"SELECT IFNULL(MAX({campo}),0) + 1 FROM {tabla} WHERE {campo} < {New String("9"c, tamano)}"
            End If
            Return $"SELECT IFNULL(MAX(MID({campo},LENGTH({campo})-{tamano - 1})),0) + 1 FROM {tabla} WHERE MID({campo},1,LENGTH({campo})-{tamano}) = @local"
        End Function

        Friend Shared Function Componer(local As Long, contador As Long, tamano As Integer) As String
            If local = 0 Then Return contador.ToString()
            Return local.ToString() & contador.ToString().PadLeft(tamano, "0"c)
        End Function

    End Class

End Namespace
