Imports System.Threading
Imports Dapper

Namespace Datos

    ''' <summary>Tipo de registro de bitácora. Mismos valores que SDACA_BITACORA_TIPO de ModBitacora.bas.</summary>
    Public Enum TipoBitacora
        [Error] = 0
        Suceso = 1
    End Enum

    ''' <summary>Bitácora en <c>gatbitacora</c>. Reemplaza a <c>SD_CLS_CNN.BITACORA_AGREGAR</c>.</summary>
    Public NotInheritable Class Bitacora

        Private ReadOnly _fabrica As IFabricaConexiones

        Public Sub New(fabrica As IFabricaConexiones)
            _fabrica = fabrica
        End Sub

        ''' <summary>Agrega un registro y devuelve su id (LAST_INSERT_ID).</summary>
        Public Async Function AgregarAsync(local As Long, modulo As Long, uid As Long, tipo As TipoBitacora, estacionId As String,
                                           detalle As String, detalleSql As String, Optional ct As CancellationToken = Nothing) As Task(Of Long)
            Const sql As String =
                "INSERT INTO gatbitacora (tid, local, modulo, estacion, tipo, det, detsql, uid, fe, fc) " &
                "VALUES (NULL, @local, @modulo, @estacionId, @tipo, @detalle, @detalleSql, @uid, NOW(), NOW()); " &
                "SELECT LAST_INSERT_ID();"
            Dim p = New With {.local = local, .modulo = modulo, .estacionId = estacionId, .tipo = CInt(tipo),
                              .detalle = detalle, .detalleSql = detalleSql, .uid = uid}
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Await cn.ExecuteScalarAsync(Of Long)(New CommandDefinition(sql, p, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

    End Class

End Namespace
