Imports System.Threading
Imports Dapper
Imports SDACA.Core.Datos

Namespace Bancos

    ''' <summary>Fila de <c>ccbtiposmov</c>.</summary>
    Public NotInheritable Class TipoMovimiento
        Public Const Resta As Integer = 0
        Public Const Suma As Integer = 1

        Public Property Codigo As String = ""
        Public Property Descripcion As String = ""
        Public Property Abreviatura As String = ""
        ''' <summary>Columna <c>tipot</c>, como texto para no depender del tipo de la columna.</summary>
        Public Property Tipo As String = "0"

        Public ReadOnly Property Efecto As Integer
            Get
                Return If(Tipo = "1", Suma, Resta)
            End Get
        End Property

        Public Overrides Function ToString() As String
            Return $"{Descripcion} ({Abreviatura})"
        End Function
    End Class

    ''' <summary>Acceso a <c>ccbtiposmov</c>. Consultas de SD1401\FrmMantTiposMov.frm, con parámetros.</summary>
    Public NotInheritable Class TiposMovimientoRepositorio

        Private Const Columnas As String = "SELECT CAST(tipo AS CHAR) AS Codigo, IFNULL(detalle,'') AS Descripcion, IFNULL(abr,'') AS Abreviatura, CAST(tipot AS CHAR) AS Tipo FROM ccbtiposmov"

        Private ReadOnly _fabrica As IFabricaConexiones

        Public Sub New(fabrica As IFabricaConexiones)
            _fabrica = fabrica
        End Sub

        Public Function PrimeroAsync(Optional ct As CancellationToken = Nothing) As Task(Of TipoMovimiento)
            Return UnoAsync(Columnas & " ORDER BY tipo LIMIT 0,1", Nothing, ct)
        End Function

        Public Function UltimoAsync(Optional ct As CancellationToken = Nothing) As Task(Of TipoMovimiento)
            Return UnoAsync(Columnas & " ORDER BY tipo DESC LIMIT 0,1", Nothing, ct)
        End Function

        Public Function AnteriorAsync(codigo As Long, Optional ct As CancellationToken = Nothing) As Task(Of TipoMovimiento)
            Return UnoAsync(Columnas & " WHERE tipo < @codigo ORDER BY tipo DESC LIMIT 0,1", New With {codigo}, ct)
        End Function

        Public Function SiguienteAsync(codigo As Long, Optional ct As CancellationToken = Nothing) As Task(Of TipoMovimiento)
            Return UnoAsync(Columnas & " WHERE tipo > @codigo ORDER BY tipo LIMIT 0,1", New With {codigo}, ct)
        End Function

        Public Function ObtenerAsync(codigo As Long, Optional ct As CancellationToken = Nothing) As Task(Of TipoMovimiento)
            Return UnoAsync(Columnas & " WHERE tipo = @codigo LIMIT 0,1", New With {codigo}, ct)
        End Function

        Private Async Function UnoAsync(sql As String, parametros As Object, ct As CancellationToken) As Task(Of TipoMovimiento)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim comando As New CommandDefinition(sql, parametros, cancellationToken:=ct)
                Return Await cn.QueryFirstOrDefaultAsync(Of TipoMovimiento)(comando).ConfigureAwait(False)
            End Using
        End Function

        Public Async Function ListarAsync(Optional ct As CancellationToken = Nothing) As Task(Of List(Of TipoMovimiento))
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim filas = Await cn.QueryAsync(Of TipoMovimiento)(New CommandDefinition(Columnas & " ORDER BY detalle", cancellationToken:=ct)).ConfigureAwait(False)
                Return filas.ToList()
            End Using
        End Function

        ''' <summary>Con código vacío crea uno nuevo. Devuelve el código guardado.</summary>
        Public Async Function GuardarAsync(codigo As String, descripcion As String, abreviatura As String, efecto As Integer, local As Long,
                                           Optional ct As CancellationToken = Nothing) As Task(Of String)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                If codigo.Length = 0 Then codigo = Await Consecutivo.SiguienteAsync(cn, "ccbtiposmov", "tipo", local, ct:=ct).ConfigureAwait(False)
                Const sql As String = "REPLACE INTO ccbtiposmov (tipo, detalle, abr, tipot) VALUES (@codigo, @descripcion, @abreviatura, @tipo)"
                Dim tipo = efecto.ToString()
                Await cn.ExecuteAsync(New CommandDefinition(sql, New With {codigo, descripcion, abreviatura, tipo}, cancellationToken:=ct)).ConfigureAwait(False)
                Return codigo
            End Using
        End Function

    End Class

End Namespace
