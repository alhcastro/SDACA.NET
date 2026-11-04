Imports System.Threading
Imports Dapper

Namespace Datos

    ''' <summary>
    ''' Parámetros por local en <c>gatparametros</c>. Reemplaza a
    ''' <c>SD_CLS_CNN.Get_Parametro / Get_Parametro_Lng / Set_Parametro</c>.
    ''' </summary>
    ''' <remarks>
    ''' Si el local no tiene el parámetro, se busca en el local 1000, que guarda los valores globales.
    ''' La búsqueda no distingue mayúsculas (<c>ucase(parametro)</c>). Las consultas ahora van
    ''' parametrizadas; en el original se concatenaba el texto.
    ''' </remarks>
    Public NotInheritable Class Parametros

        Public Const LocalGlobal As Long = 1000
        Private ReadOnly _fabrica As IFabricaConexiones

        Public Sub New(fabrica As IFabricaConexiones)
            _fabrica = fabrica
        End Sub

        Public Async Function ObtenerAsync(local As Long, parametro As String, Optional ct As CancellationToken = Nothing) As Task(Of String)
            Const sql As String = "SELECT valor FROM gatparametros WHERE local = @local AND UPPER(parametro) = UPPER(@parametro) LIMIT 1;"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim valor = Await cn.ExecuteScalarAsync(Of String)(New CommandDefinition(sql, New With {.local = local, .parametro = parametro.Trim()}, cancellationToken:=ct)).ConfigureAwait(False)
                If String.IsNullOrEmpty(valor) AndAlso local <> LocalGlobal Then
                    valor = Await cn.ExecuteScalarAsync(Of String)(New CommandDefinition(sql, New With {.local = LocalGlobal, .parametro = parametro.Trim()}, cancellationToken:=ct)).ConfigureAwait(False)
                End If
                Return If(valor, "")
            End Using
        End Function

        ''' <summary>Igual que Get_Parametro_Lng: <c>CLng(Val(Get_Parametro(...)))</c>.</summary>
        Public Async Function ObtenerNumeroAsync(local As Long, parametro As String, Optional ct As CancellationToken = Nothing) As Task(Of Long)
            Dim texto = Await ObtenerAsync(local, parametro, ct).ConfigureAwait(False)
            Return Vb6.Val(texto)
        End Function

        Public Async Function GuardarAsync(local As Long, parametro As String, valor As String, uidc As Long, Optional ct As CancellationToken = Nothing) As Task(Of Boolean)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim p = New With {.local = local, .parametro = parametro.Trim(), .valor = valor, .uidc = uidc}
                Dim existe = Await cn.ExecuteScalarAsync(Of Long)(New CommandDefinition(
                    "SELECT COUNT(valor) FROM gatparametros WHERE local = @local AND UPPER(parametro) = UPPER(@parametro);", p, cancellationToken:=ct)).ConfigureAwait(False)
                Dim sql = If(existe = 0,
                    "INSERT INTO gatparametros (local, Parametro, Valor, uidc, fc) VALUES (@local, @parametro, @valor, @uidc, NOW());",
                    "UPDATE gatparametros SET valor = @valor WHERE local = @local AND UPPER(parametro) = UPPER(@parametro);")
                Await cn.ExecuteAsync(New CommandDefinition(sql, p, cancellationToken:=ct)).ConfigureAwait(False)
                Return True
            End Using
        End Function

    End Class

End Namespace
