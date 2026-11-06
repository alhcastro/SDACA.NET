Imports System.Globalization
Imports System.Threading
Imports Dapper
Imports MySqlConnector
Imports SDACA.Core.Bancos
Imports SDACA.Core.Datos

Namespace Catalogos

    ''' <summary>Fila de <c>gatcambioid</c>: una columna que guarda una identificación.</summary>
    Public NotInheritable Class TablaCambioId
        Public Property Tabla As String = ""
        Public Property Campo As String = ""
        Public Property Condicion As String = ""
    End Class

    Public Enum EstadoCambioId
        Actualizada
        NoExiste
        Fallo
    End Enum

    ''' <summary>Resultado de una fila de <c>gatcambioid</c>.</summary>
    Public NotInheritable Class ResultadoCambioId
        Public Property Tabla As String = ""
        Public Property Estado As EstadoCambioId = EstadoCambioId.Actualizada

        Public ReadOnly Property Mensaje As String
            Get
                Select Case Estado
                    Case EstadoCambioId.Actualizada
                        Return $"Se actualizó correctamente {Tabla}..."
                    Case EstadoCambioId.NoExiste
                        Return $"No se encontró en {Tabla}..."
                    Case Else
                        Return $"Error en la tabla {Tabla}..."
                End Select
            End Get
        End Property
    End Class

    ''' <summary>
    ''' Cambio de Identificación. Migración de SD_CambiarID de SD0001\FrmCambioID.frm. Recorre gatcambioid
    ''' y cambia cada columna con UPDATE IGNORE, replicado como DP. No hay transacción: las tablas son
    ''' MyISAM. Se replica aunque falle aquí, igual que en VB6, porque cada local tiene sus propias tablas.
    ''' </summary>
    Public NotInheritable Class CambioIdentificacionRepositorio

        Private Const Replica As String = "DP"

        Private ReadOnly _fabrica As IFabricaConexiones

        Public Sub New(fabrica As IFabricaConexiones)
            _fabrica = fabrica
        End Sub

        Private Shared Function Q(texto As String) As String
            Return $"'{LibroBancos.Literal(texto)}'"
        End Function

        Friend Shared Function Sentencia(fila As TablaCambioId, anterior As String, nueva As String) As String
            Dim condicion = If(fila.Condicion, "").Trim()
            Dim sql = $"UPDATE IGNORE {fila.Tabla} SET {fila.Campo} = {Q(nueva)} WHERE {fila.Campo} = {Q(anterior)}"
            Return If(condicion.Length = 0, sql, $"{sql} AND {condicion}")
        End Function

        ''' <summary>En VB6 el mensaje decía los errores como si fueran los cambios exitosos.</summary>
        Public Shared Function Resumen(resultados As IReadOnlyCollection(Of ResultadoCambioId)) As String
            Dim exitos = resultados.Where(Function(r) r.Estado = EstadoCambioId.Actualizada).Count()
            Return $"{exitos} de {resultados.Count} fueron realizados exitosamente."
        End Function

        ''' <summary>Texto de la persona para confirmar el cambio.</summary>
        Public Shared Function Descripcion(p As Persona) As String
            Dim conocido = If(p.Alias.Trim().Length = 0, "", $", conocido como {p.Alias.Trim()}")
            Dim nacimiento = p.FNac.ToString("D", CultureInfo.GetCultureInfo("es-CR"))
            Return $"Pertenece a: {p.NombreCompleto}{conocido}{Environment.NewLine}Fecha de nacimiento: {nacimiento}"
        End Function

        Private Shared Async Function AplicarAsync(cn As MySqlConnection, fila As TablaCambioId, anterior As String, nueva As String, local As Long, ct As CancellationToken) As Task(Of ResultadoCambioId)
            Dim resultado As New ResultadoCambioId With {.Tabla = fila.Tabla}
            Dim sql = Sentencia(fila, anterior, nueva)
            Try
                Await cn.ExecuteAsync(New CommandDefinition(sql, cancellationToken:=ct)).ConfigureAwait(False)
            Catch ex As MySqlException When ex.ErrorCode = MySqlErrorCode.NoSuchTable
                resultado.Estado = EstadoCambioId.NoExiste
            Catch ex As MySqlException
                resultado.Estado = EstadoCambioId.Fallo
            End Try
            Try
                Await Replicador.ReplicarAsync(cn, Replica, sql, local, ct).ConfigureAwait(False)
            Catch ex As MySqlException
                resultado.Estado = EstadoCambioId.Fallo
            End Try
            Return resultado
        End Function

        ''' <summary>Vacío si gatcambioid no tiene filas: entonces no se cambió nada.</summary>
        Public Async Function CambiarAsync(anterior As String, nueva As String, local As Long, progreso As IProgress(Of ResultadoCambioId), Optional ct As CancellationToken = Nothing) As Task(Of List(Of ResultadoCambioId))
            Const consulta As String = "SELECT tabla AS Tabla, campo AS Campo, condicion AS Condicion FROM gatcambioid WHERE tipo = 'id'"
            Dim resultados As New List(Of ResultadoCambioId)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim filas = (Await cn.QueryAsync(Of TablaCambioId)(New CommandDefinition(consulta, cancellationToken:=ct)).ConfigureAwait(False)).ToList()
                For Each fila In filas
                    Dim resultado = Await AplicarAsync(cn, fila, anterior, nueva, local, ct).ConfigureAwait(False)
                    resultados.Add(resultado)
                    progreso?.Report(resultado)
                Next
            End Using
            Return resultados
        End Function

    End Class

End Namespace
