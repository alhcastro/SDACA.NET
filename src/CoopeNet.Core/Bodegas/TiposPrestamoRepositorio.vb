Imports System.Threading
Imports Dapper
Imports CoopeNet.Core.Datos

Namespace Bodegas

    ''' <summary>Fila de <c>sbhtippre</c>.</summary>
    Public NotInheritable Class TipoPrestamo
        Public Property Codigo As Integer
        Public Property Descripcion As String
        ''' <summary>Columna <c>est</c>: "1" si el préstamo requiere aprobación.</summary>
        Public Property Estado As String

        Public ReadOnly Property RequiereAprobacion As Boolean
            Get
                Return Estado = "1"
            End Get
        End Property
    End Class

    ''' <summary>
    ''' Acceso a <c>sbhtippre</c>. Las consultas son las de SD1101\frmTiposPrestamos.frm, con
    ''' parámetros en lugar de texto concatenado. <c>est</c> se lee y se escribe como texto, igual
    ''' que en VB6, para no depender del tipo de la columna.
    ''' </summary>
    Public NotInheritable Class TiposPrestamoRepositorio

        Private Const Columnas As String = "SELECT tippre AS Codigo, des AS Descripcion, CAST(est AS CHAR) AS Estado FROM sbhtippre"

        Private ReadOnly _fabrica As IFabricaConexiones

        Public Sub New(fabrica As IFabricaConexiones)
            _fabrica = fabrica
        End Sub

        Public Async Function SiguienteCodigoAsync(Optional ct As CancellationToken = Nothing) As Task(Of Integer)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Const sql As String = "SELECT ifnull(max(tippre)+1,1) FROM sbhtippre"
                Return Await cn.ExecuteScalarAsync(Of Integer)(New CommandDefinition(sql, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>Devuelve el tipo pedido, o Nothing si no hay más en esa dirección.</summary>
        Public Async Function ObtenerAsync(codigo As Integer, direccion As DireccionConsulta, Optional ct As CancellationToken = Nothing) As Task(Of TipoPrestamo)
            Dim sql = Columnas
            Select Case direccion
                Case DireccionConsulta.Siguiente
                    sql &= " WHERE tippre > @codigo ORDER BY tippre LIMIT 0,1"
                Case DireccionConsulta.Anterior
                    sql &= " WHERE tippre < @codigo ORDER BY tippre DESC LIMIT 0,1"
                Case Else
                    sql &= " WHERE tippre = @codigo LIMIT 0,1"
            End Select
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim comando As New CommandDefinition(sql, New With {codigo}, cancellationToken:=ct)
                Return Await cn.QueryFirstOrDefaultAsync(Of TipoPrestamo)(comando).ConfigureAwait(False)
            End Using
        End Function

        Public Async Function ListarAsync(Optional ct As CancellationToken = Nothing) As Task(Of List(Of TipoPrestamo))
            Const sql As String = Columnas & " ORDER BY tippre ASC"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim filas = Await cn.QueryAsync(Of TipoPrestamo)(New CommandDefinition(sql, cancellationToken:=ct)).ConfigureAwait(False)
                Return filas.ToList()
            End Using
        End Function

        Public Async Function GuardarAsync(codigo As Integer, descripcion As String, requiereAprobacion As Boolean, Optional ct As CancellationToken = Nothing) As Task
            Const sql As String = "REPLACE INTO sbhtippre (tippre, des, est) VALUES (@codigo, @descripcion, @estado)"
            Dim estado = If(requiereAprobacion, "1", "0")
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition(sql, New With {codigo, descripcion, estado}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        Public Async Function BorrarAsync(codigo As Integer, Optional ct As CancellationToken = Nothing) As Task
            Const sql As String = "DELETE FROM sbhtippre WHERE tippre = @codigo"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition(sql, New With {codigo}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

    End Class

End Namespace
