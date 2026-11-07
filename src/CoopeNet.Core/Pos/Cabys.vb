Imports System.Data
Imports System.Globalization
Imports System.Threading
Imports Dapper
Imports CoopeNet.Core.Datos

Namespace Pos

    ''' <summary>Una fila de pos_catalogo: el código CABYS de 13 dígitos y los 8 niveles que lo agrupan.</summary>
    Public NotInheritable Class FilaCabys
        Public Property Cat1 As String = ""
        Public Property Cat1d As String = ""
        Public Property Cat2 As String = ""
        Public Property Cat2d As String = ""
        Public Property Cat3 As String = ""
        Public Property Cat3d As String = ""
        Public Property Cat4 As String = ""
        Public Property Cat4d As String = ""
        Public Property Cat5 As String = ""
        Public Property Cat5d As String = ""
        Public Property Cat6 As String = ""
        Public Property Cat6d As String = ""
        Public Property Cat7 As String = ""
        Public Property Cat7d As String = ""
        Public Property Cat8 As String = ""
        Public Property Cat8d As String = ""
        Public Property Codigo As String = ""
        Public Property Descripcion As String = ""
        ''' <summary>Porcentaje de IVA que Hacienda asigna al código.</summary>
        Public Property Impuesto As Integer?
    End Class

    ''' <summary>Un nodo del árbol CABYS: un nivel de agrupación o, en la hoja, un código de 13 dígitos.</summary>
    Public NotInheritable Class NodoCabys
        Public Property Codigo As String = ""
        Public Property Descripcion As String = ""
        Public Property Impuesto As Integer?
        Public Property Padre As NodoCabys
        Public ReadOnly Property Hijos As New List(Of NodoCabys)
        ''' <summary>La descripción en mayúsculas y sin tildes, para buscar.</summary>
        Friend Property Normalizada As String = ""

        Public ReadOnly Property EsCodigo As Boolean
            Get
                Return Codigo.Length = CatalogoCabys.LargoCodigo
            End Get
        End Property

        Public Overrides Function ToString() As String
            Return $"{Codigo} - {Descripcion}"
        End Function
    End Class

    ''' <summary>
    ''' El catálogo CABYS en memoria, armado como el árbol de SD_CargaCateg (frmCatCatal.frm): ocho niveles y los
    ''' códigos. VB6 cargaba los 50 mil nodos en el TreeView al abrir; aquí la pantalla crea cada rama al expandirla.
    ''' </summary>
    Public NotInheritable Class CatalogoCabys

        Public Const LargoCodigo As Integer = 13

        Private ReadOnly _porCodigo As New Dictionary(Of String, NodoCabys)(StringComparer.Ordinal)
        Private ReadOnly _codigos As New List(Of NodoCabys)

        Public ReadOnly Property Raices As New List(Of NodoCabys)

        Public Sub New(filas As IEnumerable(Of FilaCabys))
            For Each f In filas.OrderBy(Function(x) x.Codigo, StringComparer.Ordinal)
                Dim padre As NodoCabys = Nothing
                Dim niveles = {(f.Cat1, f.Cat1d), (f.Cat2, f.Cat2d), (f.Cat3, f.Cat3d), (f.Cat4, f.Cat4d), (f.Cat5, f.Cat5d), (f.Cat6, f.Cat6d), (f.Cat7, f.Cat7d), (f.Cat8, f.Cat8d)}
                For Each n In niveles
                    Dim codigo = If(n.Item1, "").Trim()
                    If codigo.Length = 0 Then Continue For
                    padre = Agregar(codigo, n.Item2, Nothing, padre)
                Next
                _codigos.Add(Agregar(f.Codigo.Trim(), f.Descripcion, f.Impuesto, padre))
            Next
        End Sub

        Private Function Agregar(codigo As String, descripcion As String, impuesto As Integer?, padre As NodoCabys) As NodoCabys
            Dim nodo As NodoCabys = Nothing
            If _porCodigo.TryGetValue(codigo, nodo) Then Return nodo
            nodo = New NodoCabys With {.Codigo = codigo, .Descripcion = If(descripcion, "").Trim(), .Impuesto = impuesto, .Padre = padre}
            nodo.Normalizada = Normalizar(nodo.Descripcion)
            _porCodigo(codigo) = nodo
            If padre Is Nothing Then Raices.Add(nodo) Else padre.Hijos.Add(nodo)
            Return nodo
        End Function

        Public ReadOnly Property Cantidad As Integer
            Get
                Return _codigos.Count
            End Get
        End Property

        Public Function Buscar(codigo As String) As NodoCabys
            Dim nodo As NodoCabys = Nothing
            _porCodigo.TryGetValue(If(codigo, "").Trim(), nodo)
            Return nodo
        End Function

        ''' <summary>
        ''' SD_Carga_Cat: los códigos cuya descripción tiene las palabras en ese orden (el LIKE '%a%b%' de VB6), sin
        ''' distinguir mayúsculas ni tildes, o cuyo código empieza con el texto (VB6 pedía el código exacto).
        ''' </summary>
        Public Function Coincidencias(texto As String, Optional maximo As Integer = 500) As List(Of NodoCabys)
            Dim t = If(texto, "").Trim()
            If t.Length = 0 Then Return New List(Of NodoCabys)
            Dim palabras = Normalizar(t).Split(" "c, StringSplitOptions.RemoveEmptyEntries)
            Return _codigos.Where(Function(n) n.Codigo.StartsWith(t, StringComparison.Ordinal) OrElse EnOrden(n.Normalizada, palabras)).Take(maximo).ToList()
        End Function

        Private Shared Function EnOrden(descripcion As String, palabras As String()) As Boolean
            Dim desde = 0
            For Each p In palabras
                Dim i = descripcion.IndexOf(p, desde, StringComparison.Ordinal)
                If i < 0 Then Return False
                desde = i + p.Length
            Next
            Return True
        End Function

        Friend Shared Function Normalizar(texto As String) As String
            Dim d = texto.ToUpperInvariant().Normalize(Text.NormalizationForm.FormD)
            Return New String(d.Where(Function(c) CharUnicodeInfo.GetUnicodeCategory(c) <> UnicodeCategory.NonSpacingMark).ToArray())
        End Function

    End Class

    ''' <summary>Consultas del buscador CABYS (frmCatCatal en modo Buscar).</summary>
    Public NotInheritable Class CabysRepositorio

        Private ReadOnly _fabrica As IFabricaConexiones
        Private _catalogo As CatalogoCabys

        Public Sub New(fabrica As IFabricaConexiones)
            _fabrica = fabrica
        End Sub

        ''' <summary>El catálogo completo; se lee una vez por repositorio.</summary>
        Public Async Function CatalogoAsync(Optional ct As CancellationToken = Nothing) As Task(Of CatalogoCabys)
            If _catalogo IsNot Nothing Then Return _catalogo
            Const sql As String = "SELECT cat1, cat1d, cat2, cat2d, cat3, cat3d, cat4, cat4d, cat5, cat5d, cat6, cat6d, cat7, cat7d, cat8, cat8d, codigo, descripcion, impuesto FROM pos_catalogo"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim filas = Await cn.QueryAsync(Of FilaCabys)(New CommandDefinition(sql, commandTimeout:=120, cancellationToken:=ct)).ConfigureAwait(False)
                _catalogo = New CatalogoCabys(filas)
            End Using
            Return _catalogo
        End Function

        ''' <summary>
        ''' SD_CargaArticulos sin filtros: los artículos del local cuyo CABYS empieza con el código del nodo, hasta 1000,
        ''' ordenados por código. Columnas Código, Descripción, CABYS y Categoría.
        ''' </summary>
        Public Async Function ArticulosAsync(prefijo As String, local As Long, Optional ct As CancellationToken = Nothing) As Task(Of DataTable)
            Const sql As String = "SELECT a.CodigoArt AS 'Código', a.Descripcion AS 'Descripción', a.CodAlterno AS 'CABYS', " &
                "CONCAT_WS('-', CAST(I.Categ AS CHAR), CAST(I.SCateg AS CHAR), CAST(I.SSCateg AS CHAR)) AS 'Categoría' FROM posarticulos a " &
                "INNER JOIN posinventario I ON I.CLocal = @local AND I.CArticulo = a.CodigoArt " &
                "INNER JOIN posarticuloslocal l ON l.CLocal = @local AND l.CArticulo = a.CodigoArt " &
                "WHERE a.CodAlterno LIKE @patron ORDER BY a.CodigoArt LIMIT 1000"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Using lector = Await cn.ExecuteReaderAsync(New CommandDefinition(sql, New With {local, .patron = Escapar(prefijo) & "%"}, commandTimeout:=120, cancellationToken:=ct)).ConfigureAwait(False)
                    Dim tabla As New DataTable()
                    tabla.Load(lector)
                    Return tabla
                End Using
            End Using
        End Function

        Private Shared Function Escapar(texto As String) As String
            Return If(texto, "").Replace("\", "\\").Replace("%", "\%").Replace("_", "\_")
        End Function

    End Class

End Namespace
