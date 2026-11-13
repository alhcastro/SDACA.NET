Imports System.Threading
Imports Dapper
Imports CoopeNet.Core.Datos

Namespace Pos

    ''' <summary>
    ''' Cómo está conectado el escáner de una estación (posscanestac). Migración de SD0502\frmScaner.frm. La columna
    ''' teclado guarda "0" para teclado y "1" para puerto serial (sí, al revés de lo que dice su nombre). No se replica.
    ''' </summary>
    Public NotInheritable Class ScanerEstacionRepositorio

        Private ReadOnly _fabrica As IFabricaConexiones

        Public Sub New(fabrica As IFabricaConexiones)
            _fabrica = fabrica
        End Sub

        ''' <summary>SD_Cargar: True si la estación tiene el escáner en puerto serial. Sin fila es teclado.</summary>
        Public Async Function PuertoSerialAsync(estacionId As String, Optional ct As CancellationToken = Nothing) As Task(Of Boolean)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim valor = Await cn.ExecuteScalarAsync(Of String)(New CommandDefinition(
                    "SELECT teclado FROM posscanestac WHERE estacion = @estacionId", New With {estacionId}, cancellationToken:=ct)).ConfigureAwait(False)
                Return valor = "1"
            End Using
        End Function

        ''' <summary>cmdGuardar.</summary>
        Public Async Function GuardarAsync(estacionId As String, puertoSerial As Boolean, local As Long, Optional ct As CancellationToken = Nothing) As Task
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition(
                    "REPLACE INTO posscanestac (estacion, teclado, local) VALUES (@estacionId, @teclado, @local)",
                    New With {estacionId, .teclado = If(puertoSerial, "1", "0"), local}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

    End Class

End Namespace
