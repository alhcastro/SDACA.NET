Imports System.Threading
Imports Dapper
Imports CoopeNet.Core.Datos

Namespace Pos

    ''' <summary>Una fila de posimagartic: la imagen número Consec de un artículo.</summary>
    Public NotInheritable Class ImagenArticulo
        Public Property Consec As Integer
        Public Property Descr As String = ""
        Public Property Imag As Byte()
    End Class

    ''' <summary>
    ''' Imágenes del artículo (pestaña Imagenes de SD0502\frmMantArticulos.frm): POSLoadPictArtic,
    ''' POSSavePictArtic, cmdIModificar y cmdIEliminar. Como en VB6, se graban al pulsar cada botón, no al
    ''' guardar el artículo, no se replican y Local queda en 0 (todas las filas del local 1 lo tienen así).
    ''' </summary>
    Public NotInheritable Class ImagenesArticuloRepositorio

        ''' <summary>cmdINueva: VB6 rechaza archivos de 256 KB o más.</summary>
        Public Const TamanoMaximo As Integer = 262144
        Public Const LargoDescripcion As Integer = 128

        Private ReadOnly _fabrica As IFabricaConexiones

        Public Sub New(fabrica As IFabricaConexiones)
            _fabrica = fabrica
        End Sub

        ''' <summary>
        ''' La primera imagen (desde = 0), o la siguiente o anterior a la imagen desde. Nothing si no hay.
        ''' VB6 tomaba la primera fila sin ORDER BY; aquí es la de menor Consec.
        ''' </summary>
        Public Async Function ObtenerAsync(articulo As String, desde As Integer, siguiente As Boolean, Optional ct As CancellationToken = Nothing) As Task(Of ImagenArticulo)
            Dim sql = "SELECT Consec, Descr, imag AS Imag FROM posimagartic WHERE id = @articulo"
            If desde = 0 Then
                sql &= " ORDER BY Consec LIMIT 1"
            ElseIf siguiente Then
                sql &= " AND Consec > @desde ORDER BY Consec LIMIT 1"
            Else
                sql &= " AND Consec < @desde ORDER BY Consec DESC LIMIT 1"
            End If
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Await cn.QueryFirstOrDefaultAsync(Of ImagenArticulo)(New CommandDefinition(sql, New With {articulo, desde}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>
        ''' POSSavePictArtic: con consec 0 toma el siguiente número del artículo; reemplaza la fila de ese número.
        ''' Devuelve el número con que quedó.
        ''' </summary>
        Public Async Function GuardarAsync(articulo As String, consec As Integer, descripcion As String, imagen As Byte(), uid As Long, Optional ct As CancellationToken = Nothing) As Task(Of Integer)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                If consec = 0 Then
                    consec = Await cn.ExecuteScalarAsync(Of Integer)(New CommandDefinition(
                        "SELECT IFNULL(MAX(Consec) + 1, 1) FROM posimagartic WHERE id = @articulo", New With {articulo}, cancellationToken:=ct)).ConfigureAwait(False)
                End If
                Await cn.ExecuteAsync(New CommandDefinition("DELETE FROM posimagartic WHERE id = @articulo AND Consec = @consec", New With {articulo, consec}, cancellationToken:=ct)).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition(
                    "INSERT INTO posimagartic (id, Consec, Descr, imag, fact, uid) VALUES (@articulo, @consec, @descr, @imagen, NOW(), @uid)",
                    New With {articulo, consec, .descr = Recortar(descripcion), imagen, uid}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
            Return consec
        End Function

        ''' <summary>cmdIModificar: cambia solo la descripción.</summary>
        Public Async Function CambiarDescripcionAsync(articulo As String, consec As Integer, descripcion As String, Optional ct As CancellationToken = Nothing) As Task
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition("UPDATE posimagartic SET Descr = @descr WHERE id = @articulo AND Consec = @consec",
                                                            New With {articulo, consec, .descr = Recortar(descripcion)}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>cmdIEliminar.</summary>
        Public Async Function BorrarAsync(articulo As String, consec As Integer, Optional ct As CancellationToken = Nothing) As Task
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition("DELETE FROM posimagartic WHERE id = @articulo AND Consec = @consec", New With {articulo, consec}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        Public Shared Function Recortar(descripcion As String) As String
            Dim d = If(descripcion, "").Trim()
            Return If(d.Length > LargoDescripcion, d.Substring(0, LargoDescripcion), d)
        End Function

    End Class

End Namespace
