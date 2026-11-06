Imports System.Threading
Imports Dapper
Imports MySqlConnector

Namespace Datos

    ''' <summary>Fila de gatreplicadef: a qué local se replica y si la sentencia cambia de local.</summary>
    Public NotInheritable Class DestinoReplica
        Public Property ALocal As String = ""
        Public Property CambiaLoc As String = ""
    End Class

    ''' <summary>
    ''' SD_Replicar2 de Comun\SD_MOD_UTILES02.bas. Encola la sentencia en gatreplica para cada destino
    ''' de gatreplicadef. Sin destinos no hace nada. Mientras convivan VB6 y .NET, todo lo que VB6
    ''' replica debe seguir pasando por aquí.
    ''' </summary>
    Public NotInheritable Class Replicador

        Private Sub New()
        End Sub

        ''' <summary>Texto que recibe cada destino, en el orden de <paramref name="destinos"/>.</summary>
        Public Shared Function ArmarSentencias(sql As String, local As Long, destinos As IEnumerable(Of DestinoReplica)) As List(Of String)
            Dim sentencias As New List(Of String)
            Dim actual = sql
            For Each d In destinos
                ' Un destino sin prefijo conocido reusa el texto del anterior, igual que el original.
                Select Case d.ALocal
                    Case "1" : actual = sql.Replace("$Prefijolocal", "")
                    Case "2" : actual = sql.Replace("$Prefijolocal", "R")
                    Case "3" : actual = sql.Replace("$Prefijolocal", "E")
                    Case "9" : actual = sql.Replace("$Prefijolocal", "Z")
                End Select
                sentencias.Add(actual.Replace("$Local", If(d.CambiaLoc = "1", d.ALocal, local.ToString())))
            Next
            Return sentencias
        End Function

        Public Shared Async Function ReplicarAsync(cn As MySqlConnection, identificador As String, sql As String, local As Long, Optional ct As CancellationToken = Nothing) As Task
            Const consulta As String = "SELECT ALocal, CambiaLoc FROM gatreplicadef WHERE Ident = @identificador AND LOCAL = @local"
            Const insercion As String = "INSERT INTO gatreplica (Ident, Fecha, iSQL, Estado, ALocal) VALUES (@identificador, NOW(), @sentencia, '0', @destino)"
            Dim destinos = (Await cn.QueryAsync(Of DestinoReplica)(New CommandDefinition(consulta, New With {identificador, local}, cancellationToken:=ct)).ConfigureAwait(False)).ToList()
            Dim sentencias = ArmarSentencias(sql, local, destinos)
            For i = 0 To destinos.Count - 1
                Dim fila = New With {identificador, .sentencia = sentencias(i), .destino = destinos(i).ALocal}
                Await cn.ExecuteAsync(New CommandDefinition(insercion, fila, cancellationToken:=ct)).ConfigureAwait(False)
            Next
        End Function

    End Class

End Namespace
