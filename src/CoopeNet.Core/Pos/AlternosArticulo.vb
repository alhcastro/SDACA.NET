Imports System.Globalization
Imports System.Threading
Imports Dapper
Imports MySqlConnector
Imports CoopeNet.Core.Bancos
Imports CoopeNet.Core.Datos

Namespace Pos

    ''' <summary>Una fila de posarticulosalternos: otro artículo que sustituye al artículo, con un detalle.</summary>
    Public NotInheritable Class AlternoArticulo
        Public Property CodAlterno As String = ""
        Public Property Descripcion As String = ""
        Public Property Detalle As String = ""
        Public Property FC As Date
    End Class

    ''' <summary>
    ''' Artículos alternos (botón Asignar Alternos de SD0502\frmMantArticulos.frm, que abre frmArtAlternos.frm).
    ''' Guardar es el REPLACE de SD_Guardar y se replica como posarticulosalternos; borrar también se replica,
    ''' cosa que VB6 no hacía. En el 3315 la tabla está vacía y gatreplicadef no tiene destinos para ella.
    ''' </summary>
    Public NotInheritable Class AlternosArticuloRepositorio

        Public Const Replica As String = "posarticulosalternos"
        Public Const LargoCodigo As Integer = 16
        Public Const LargoDetalle As Integer = 255
        Private Shared ReadOnly Invariante As CultureInfo = CultureInfo.InvariantCulture

        Private ReadOnly _fabrica As IFabricaConexiones

        Public Sub New(fabrica As IFabricaConexiones)
            _fabrica = fabrica
        End Sub

        Private Shared Function Q(texto As String) As String
            Return $"'{LibroBancos.Literal(If(texto, ""))}'"
        End Function

        Public Async Function ListarAsync(articulo As String, local As Long, Optional ct As CancellationToken = Nothing) As Task(Of List(Of AlternoArticulo))
            Const sql As String = "SELECT B.CodAlterno, IFNULL(A.Descripcion, '') AS Descripcion, B.Detalle, B.FC " &
                "FROM posarticulosalternos B LEFT JOIN posarticulos A ON A.CodigoArt = B.CodAlterno " &
                "WHERE B.CodigoArt = @articulo AND B.Local = @local ORDER BY B.CodAlterno"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return (Await cn.QueryAsync(Of AlternoArticulo)(New CommandDefinition(sql, New With {articulo, local}, cancellationToken:=ct)).ConfigureAwait(False)).ToList()
            End Using
        End Function

        ''' <summary>El detalle va en mayúsculas, como txtDetalle_LostFocus, y recortado al largo de la columna.</summary>
        Public Shared Function Detalle(texto As String) As String
            Dim d = If(texto, "").Trim().ToUpper(CultureInfo.CurrentCulture)
            Return If(d.Length > LargoDetalle, d.Substring(0, LargoDetalle), d)
        End Function

        Friend Shared Function SentenciaGuardar(articulo As String, alterno As String, detalleTexto As String, uid As Long, fc As Date) As String
            Return "REPLACE INTO posarticulosalternos (CodigoArt, CodAlterno, Detalle, Local, Usuario, FC, FM) VALUES (" &
                $"{Q(articulo)}, {Q(alterno)}, {Q(Detalle(detalleTexto))}, $Local, '{uid.ToString(Invariante)}', '{fc.ToString("yyyy-MM-dd HH:mm:ss", Invariante)}', NOW())"
        End Function

        Friend Shared Function SentenciaBorrar(articulo As String, alterno As String) As String
            Return $"DELETE FROM posarticulosalternos WHERE Local = $Local AND CodigoArt = {Q(articulo)} AND CodAlterno = {Q(alterno)}"
        End Function

        Private Shared Async Function EjecutarYReplicarAsync(cn As MySqlConnection, plantilla As String, local As Long, ct As CancellationToken) As Task
            Await cn.ExecuteAsync(New CommandDefinition(plantilla.Replace("$Local", local.ToString(Invariante)), cancellationToken:=ct)).ConfigureAwait(False)
            Await Replicador.ReplicarAsync(cn, Replica, plantilla, local, ct).ConfigureAwait(False)
        End Function

        ''' <summary>SD_Guardar. Un alterno nuevo lleva la fecha de ahora; uno existente conserva su FC.</summary>
        Public Async Function GuardarAsync(articulo As String, alterno As String, detalleTexto As String, local As Long, uid As Long, fc As Date, Optional ct As CancellationToken = Nothing) As Task
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await EjecutarYReplicarAsync(cn, SentenciaGuardar(articulo, alterno, detalleTexto, uid, fc), local, ct).ConfigureAwait(False)
            End Using
        End Function

        Public Async Function BorrarAsync(articulo As String, alterno As String, local As Long, Optional ct As CancellationToken = Nothing) As Task
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await EjecutarYReplicarAsync(cn, SentenciaBorrar(articulo, alterno), local, ct).ConfigureAwait(False)
            End Using
        End Function

    End Class

End Namespace
