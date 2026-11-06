Imports System.Threading
Imports Dapper
Imports CoopeNet.Core.Datos

Namespace Bodegas

    ''' <summary>Fila de <c>sbhtipher</c>.</summary>
    Public NotInheritable Class TipoHerramienta
        Public Property Codigo As Integer
        Public Property Descripcion As String
    End Class

    ''' <summary>Hacia dónde se mueve la consulta, igual que el Enum Consultar del formulario VB6.</summary>
    Public Enum DireccionConsulta
        Exacta
        Siguiente
        Anterior
    End Enum

    ''' <summary>
    ''' Acceso a <c>sbhtipher</c>. Las consultas son las de SD1101\frmTiposHerramientas.frm, con
    ''' parámetros en lugar de texto concatenado: en VB6 un apóstrofo en la descripción rompía el SQL.
    ''' </summary>
    Public NotInheritable Class TiposHerramientaRepositorio

        Private ReadOnly _fabrica As IFabricaConexiones

        Public Sub New(fabrica As IFabricaConexiones)
            _fabrica = fabrica
        End Sub

        Public Async Function SiguienteCodigoAsync(Optional ct As CancellationToken = Nothing) As Task(Of Integer)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Const sql As String = "SELECT ifnull(max(tipher)+1,1) FROM sbhtipher"
                Return Await cn.ExecuteScalarAsync(Of Integer)(New CommandDefinition(sql, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>Devuelve el tipo pedido, o Nothing si no hay más en esa dirección.</summary>
        Public Async Function ObtenerAsync(codigo As Integer, direccion As DireccionConsulta, Optional ct As CancellationToken = Nothing) As Task(Of TipoHerramienta)
            Dim sql = "SELECT tipher AS Codigo, des AS Descripcion FROM sbhtipher"
            Select Case direccion
                Case DireccionConsulta.Siguiente
                    sql &= " WHERE tipher > @codigo ORDER BY tipher LIMIT 0,1"
                Case DireccionConsulta.Anterior
                    sql &= " WHERE tipher < @codigo ORDER BY tipher DESC LIMIT 0,1"
                Case Else
                    sql &= " WHERE tipher = @codigo LIMIT 0,1"
            End Select
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim comando As New CommandDefinition(sql, New With {codigo}, cancellationToken:=ct)
                Return Await cn.QueryFirstOrDefaultAsync(Of TipoHerramienta)(comando).ConfigureAwait(False)
            End Using
        End Function

        Public Async Function ListarAsync(Optional ct As CancellationToken = Nothing) As Task(Of List(Of TipoHerramienta))
            Const sql As String = "SELECT tipher AS Codigo, des AS Descripcion FROM sbhtipher ORDER BY tipher ASC"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim filas = Await cn.QueryAsync(Of TipoHerramienta)(New CommandDefinition(sql, cancellationToken:=ct)).ConfigureAwait(False)
                Return filas.ToList()
            End Using
        End Function

        Public Async Function GuardarAsync(codigo As Integer, descripcion As String, uid As Integer, Optional ct As CancellationToken = Nothing) As Task
            Const sql As String = "REPLACE INTO sbhtipher (tipher, des, uidc, fc) VALUES (@codigo, @descripcion, @uid, NOW())"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition(sql, New With {codigo, descripcion, uid}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        Public Async Function BorrarAsync(codigo As Integer, Optional ct As CancellationToken = Nothing) As Task
            Const sql As String = "DELETE FROM sbhtipher WHERE tipher = @codigo"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition(sql, New With {codigo}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

    End Class

End Namespace
