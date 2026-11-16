Imports System.Data
Imports System.Globalization
Imports System.Threading
Imports Dapper
Imports CoopeNet.Core.Bancos
Imports CoopeNet.Core.Datos

Namespace Pos

    ''' <summary>Una marca de artículos (posmarcas). El artículo la guarda en posarticulos.CodArtProv.</summary>
    Public NotInheritable Class MarcaArticulo
        ''' <summary>0 mientras no se ha guardado.</summary>
        Public Property Codigo As Integer
        Public Property Descripcion As String = ""
    End Class

    ''' <summary>
    ''' Catálogo de marcas de SD0502. Migración de SD0502\frmMarcas.frm (SD_Guardar, SD_Cargar, Borrar y Buscar).
    ''' Cada local ve y mantiene las suyas.
    ''' </summary>
    ''' <remarks>
    ''' Guardar se replica a los locales de gatreplicadef; Borrar no, igual que en VB6. La llave de posmarcas es solo
    ''' Codigo, pero VB6 calculaba el siguiente con el máximo del local: la primera marca de un local que no fuera el 1
    ''' tomaba el código 1 y el REPLACE reemplazaba la marca 1 del local 1. Aquí el siguiente es el máximo de la tabla.
    ''' </remarks>
    Public NotInheritable Class MarcasRepositorio

        Public Const Replica As String = "posmarcas"
        Public Const LargoDescripcion As Integer = 64

        Private Shared ReadOnly Invariante As CultureInfo = CultureInfo.InvariantCulture
        Private ReadOnly _fabrica As IFabricaConexiones

        Public Sub New(fabrica As IFabricaConexiones)
            _fabrica = fabrica
        End Sub

        ''' <summary>SDTDescri_LostFocus la pasa a mayúsculas; además se recorta al largo de la columna.</summary>
        Public Shared Function Normalizar(descripcion As String) As String
            Dim d = If(descripcion, "").Trim().ToUpper(CultureInfo.CurrentCulture)
            Return If(d.Length > LargoDescripcion, d.Substring(0, LargoDescripcion), d)
        End Function

        ''' <summary>El REPLACE de SD_Guardar, para ejecutarlo y replicarlo igual. FM es la fecha del guardado.</summary>
        Friend Shared Function SentenciaGuardar(m As MarcaArticulo, uid As Long, local As Long) As String
            Return "REPLACE INTO posmarcas (Codigo, Descripcion, Usuario, local, FM) VALUES (" &
                   $"'{m.Codigo.ToString(Invariante)}', '{LibroBancos.Literal(Normalizar(m.Descripcion))}', '{uid.ToString(Invariante)}', {local.ToString(Invariante)}, NOW())"
        End Function

        Private Const Columnas As String = "SELECT Codigo, Descripcion FROM posmarcas WHERE local = @local"

        Public Async Function CargarAsync(codigo As Integer, local As Long, Optional ct As CancellationToken = Nothing) As Task(Of MarcaArticulo)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Await cn.QueryFirstOrDefaultAsync(Of MarcaArticulo)(New CommandDefinition(
                    Columnas & " AND Codigo = @codigo LIMIT 1", New With {codigo, local}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>cmdAnterior y cmdProximo: la vecina del local, o Nothing en el extremo.</summary>
        Public Async Function VecinaAsync(codigo As Integer, local As Long, siguiente As Boolean, Optional ct As CancellationToken = Nothing) As Task(Of MarcaArticulo)
            Dim sql = Columnas & If(siguiente, " AND Codigo > @codigo ORDER BY Codigo LIMIT 1", " AND Codigo < @codigo ORDER BY Codigo DESC LIMIT 1")
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Await cn.QueryFirstOrDefaultAsync(Of MarcaArticulo)(New CommandDefinition(sql, New With {codigo, local}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>Buscar de la barra: las del local. Columnas: Código y Descripción.</summary>
        Public Async Function BuscarAsync(local As Long, Optional ct As CancellationToken = Nothing) As Task(Of DataTable)
            Const sql As String = "SELECT Codigo AS `Código`, Descripcion AS `Descripción` FROM posmarcas WHERE local = @local ORDER BY Codigo"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Using lector = Await cn.ExecuteReaderAsync(New CommandDefinition(sql, New With {local}, cancellationToken:=ct)).ConfigureAwait(False)
                    Dim tabla As New DataTable()
                    tabla.Load(lector)
                    Return tabla
                End Using
            End Using
        End Function

        ''' <summary>SD_Guardar: una nueva toma el siguiente código de la tabla. Devuelve el código con que quedó.</summary>
        Public Async Function GuardarAsync(m As MarcaArticulo, uid As Long, local As Long, Optional ct As CancellationToken = Nothing) As Task(Of Integer)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim codigo = m.Codigo
                If codigo = 0 Then
                    codigo = Await cn.ExecuteScalarAsync(Of Integer)(New CommandDefinition(
                        "SELECT IFNULL(MAX(Codigo) + 1, 1) FROM posmarcas", cancellationToken:=ct)).ConfigureAwait(False)
                End If
                Dim sql = SentenciaGuardar(New MarcaArticulo With {.Codigo = codigo, .Descripcion = m.Descripcion}, uid, local)
                Await cn.ExecuteAsync(New CommandDefinition(sql, cancellationToken:=ct)).ConfigureAwait(False)
                Await Replicador.ReplicarAsync(cn, Replica, sql, local, ct).ConfigureAwait(False)
                Return codigo
            End Using
        End Function

        ''' <summary>Borrar de la barra. Como en VB6, no se replica.</summary>
        Public Async Function BorrarAsync(codigo As Integer, local As Long, Optional ct As CancellationToken = Nothing) As Task
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition("DELETE FROM posmarcas WHERE Codigo = @codigo AND local = @local", New With {codigo, local}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

    End Class

End Namespace
