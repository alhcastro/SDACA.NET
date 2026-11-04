Imports System.Threading
Imports MySqlConnector

Namespace Datos

    ''' <summary>Abre conexiones a MySQL. Reemplaza a SD_CLS_CNN.Conectar / DB_Conectar.</summary>
    Public Interface IFabricaConexiones
        Function AbrirAsync(Optional ct As CancellationToken = Nothing) As Task(Of MySqlConnection)
    End Interface

    Public NotInheritable Class FabricaConexiones
        Implements IFabricaConexiones

        Private ReadOnly _cadena As String

        Public Sub New(cadenaConexion As String)
            _cadena = cadenaConexion
        End Sub

        Public Async Function AbrirAsync(Optional ct As CancellationToken = Nothing) As Task(Of MySqlConnection) Implements IFabricaConexiones.AbrirAsync
            Dim cn As New MySqlConnection(_cadena)
            Await cn.OpenAsync(ct).ConfigureAwait(False)
            Return cn
        End Function

    End Class

End Namespace
