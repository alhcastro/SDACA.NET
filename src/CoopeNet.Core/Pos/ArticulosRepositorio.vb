Imports System.Data
Imports System.Globalization
Imports System.Text
Imports System.Threading
Imports Dapper
Imports MySqlConnector
Imports CoopeNet.Core.Bancos
Imports CoopeNet.Core.Datos

Namespace Pos

    ''' <summary>Una diferencia entre la foto del artículo y la fila actual (SD_CompReg y SD_CompCambios).</summary>
    Public NotInheritable Class CambioArticulo
        Public Property Campo As String = ""
        Public Property Anterior As String = ""
        Public Property Actual As String = ""
    End Class

    ''' <summary>El artículo al que lleva un código de barras, y las opciones si hay más de uno.</summary>
    Public NotInheritable Class ResultadoCodigoBarras
        Public Property Articulo As String = ""
        ''' <summary>Con más de una fila, el usuario debe elegir (columnas Artículo, Descripción, Código, Unidades).</summary>
        Public Property Opciones As DataTable
    End Class

    ''' <summary>
    ''' Catálogo de artículos de SD0502. Migración de las consultas de SD0502\frmMantArticulos.frm: SD_CargarArt,
    ''' SD_Siguiente, SD_Anterior, Buscar, SD_GuardarArticulo, ValidarBorrar, SD_BorrarArticulo y SD_BuscaSug,
    ''' y de sd_ObtCodigoDisponible, SD_CodAlterno, SD_RespReg, SD_CompReg, SD_CompCambios y
    ''' RegistrarMovimiento de ModPrin.bas. Las sentencias que se replican se arman como texto, igual que en
    ''' VB6, porque gatreplica guarda ese texto: posarticulos se replica como ARTICULOS y
    ''' posarticuloslocal y posinventario como posarticuloslocal, con $Local para el local de destino.
    ''' </summary>
    Public NotInheritable Class ArticulosRepositorio

        Public Const ReplicaArticulos As String = "ARTICULOS"
        Public Const ReplicaLocal As String = "posarticuloslocal"
        Public Const Bitacora As String = "Mantenimiento de Artículos"

        Private Shared ReadOnly Invariante As CultureInfo = CultureInfo.InvariantCulture
        Private ReadOnly _fabrica As IFabricaConexiones

        Public Sub New(fabrica As IFabricaConexiones)
            _fabrica = fabrica
        End Sub

        Private Shared Function Q(texto As String) As String
            Return $"'{LibroBancos.Literal(If(texto, ""))}'"
        End Function

        Private Shared Function N(valor As Double) As String
            Return valor.ToString("R", Invariante)
        End Function

        ' ---- Catálogos ----

        Public Async Function CatalogosAsync(local As Long, Optional ct As CancellationToken = Nothing) As Task(Of CatalogosArticulo)
            Dim c As New CatalogosArticulo()
            Dim parametros As New Parametros(_fabrica)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                c.Unidades = (Await cn.QueryAsync(Of OpcionArticulo)(New CommandDefinition(
                    "SELECT CAST(UNIDAD AS CHAR) AS Codigo, CONCAT(UNIDAD, ' - ', DESCRIPCION, ' (', ABREVIA, ')') AS Descripcion FROM gatunidades WHERE LOCAL = @local ORDER BY UNIDAD",
                    New With {local}, cancellationToken:=ct)).ConfigureAwait(False)).ToList()
                c.Marcas = (Await cn.QueryAsync(Of OpcionArticulo)(New CommandDefinition(
                    "SELECT CAST(Codigo AS CHAR) AS Codigo, CONCAT(Codigo, ' - ', descripcion) AS Descripcion FROM posmarcas WHERE Local = @local ORDER BY Codigo",
                    New With {local}, cancellationToken:=ct)).ConfigureAwait(False)).ToList()
                c.Monedas = (Await cn.QueryAsync(Of String)(New CommandDefinition("SELECT simbolo FROM gatmonedas ORDER BY moneda", cancellationToken:=ct)).ConfigureAwait(False)).ToList()
                c.Ivas = (Await cn.QueryAsync(Of Double)(New CommandDefinition("SELECT porcentaje FROM posiva ORDER BY porcentaje", cancellationToken:=ct)).ConfigureAwait(False)).ToList()
                ' VB6 lee POSPrecioDia directo de gatparametros, sin el local 1000. Weekday: domingo es 1.
                Dim dia = Await cn.ExecuteScalarAsync(Of String)(New CommandDefinition(
                    "SELECT valor FROM gatparametros WHERE local = @local AND parametro = @parametro",
                    New With {local, .parametro = $"POSPrecioDia{CInt(Date.Today.DayOfWeek) + 1}"}, cancellationToken:=ct)).ConfigureAwait(False)
                Dim precio = CInt(Vb6.Val(If(dia, "")))
                c.PrecioDelDia = If(precio >= 1 AndAlso precio <= 6, precio, 1)
            End Using
            For i = 0 To 5
                c.EtiquetasPrecio.Add(Await parametros.ObtenerAsync(local, $"pos_etiquetaprecio{i}", ct).ConfigureAwait(False))
            Next
            c.FactorRedondeo = Conversion.Val(Await parametros.ObtenerAsync(local, "POSFacRedon", ct).ConfigureAwait(False))
            c.UsuariosUtilidadNegativa = Await parametros.ObtenerAsync(local, "PosUtilidadNeg", ct).ConfigureAwait(False)
            Return c
        End Function

        ''' <summary>
        ''' Las categorías del control SDCategorias: nivel 1 de poscategorias, 2 de poscategorias_s y 3 de
        ''' poscategorias_ss, siempre del local.
        ''' </summary>
        Public Async Function CategoriasAsync(local As Long, nivel As Integer, categ As Integer, scateg As Integer, Optional ct As CancellationToken = Nothing) As Task(Of List(Of OpcionArticulo))
            Dim sql As String
            Select Case nivel
                Case 1 : sql = "SELECT CAST(Categ AS CHAR) AS Codigo, CONCAT(Categ, ' - ', Descripcion) AS Descripcion FROM poscategorias WHERE Local = @local ORDER BY Categ"
                Case 2 : sql = "SELECT CAST(SCateg AS CHAR) AS Codigo, CONCAT(SCateg, ' - ', Descripcion) AS Descripcion FROM poscategorias_s WHERE Local = @local AND Categ = @categ ORDER BY SCateg"
                Case Else : sql = "SELECT CAST(SSCateg AS CHAR) AS Codigo, CONCAT(SSCateg, ' - ', Descripcion) AS Descripcion FROM poscategorias_ss WHERE Local = @local AND Categ = @categ AND SCateg = @scateg ORDER BY SSCateg"
            End Select
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return (Await cn.QueryAsync(Of OpcionArticulo)(New CommandDefinition(sql, New With {local, categ, scateg}, cancellationToken:=ct)).ConfigureAwait(False)).ToList()
            End Using
        End Function

        ' ---- Consulta ----

        Friend Const SqlArticulo As String =
            "SELECT A.*, IFNULL(C.CAlterno, '') AS CodigoCorto, L.CArticulo IS NOT NULL AS Registrado, IFNULL(L.Stock, 0) AS Stock, " &
            "IFNULL(I.Categ, 0) AS Categ, IFNULL(I.SCateg, 0) AS SCateg, IFNULL(I.SSCateg, 0) AS SSCateg FROM posarticulos A " &
            "LEFT JOIN poscodart_codbarra C ON C.articulo = A.CodigoArt AND LENGTH(C.CAlterno) < 7 AND C.Local = @local " &
            "LEFT JOIN posarticuloslocal L ON L.CArticulo = A.CodigoArt AND L.CLocal = @local " &
            "LEFT JOIN posinventario I ON I.CArticulo = A.CodigoArt AND I.CLocal = @local " &
            "WHERE A.CodigoArt = @codigo LIMIT 1"

        ''' <summary>SD_CargarArt. Nothing si no existe.</summary>
        Public Async Function ObtenerAsync(codigo As String, local As Long, Optional ct As CancellationToken = Nothing) As Task(Of Articulo)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Await cn.QueryFirstOrDefaultAsync(Of Articulo)(New CommandDefinition(SqlArticulo, New With {codigo, local}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>VB6 avisa y no lo muestra si no está en el local, salvo los servicios y los de precio libre.</summary>
        Public Shared Function SePuedeMostrar(a As Articulo) As Boolean
            Return a.Registrado OrElse Articulo.EsMarcado(a.Servicio) OrElse Articulo.EsMarcado(a.SinPrecio)
        End Function

        ''' <summary>El filtro de categorías de SD_Cat2.Where; 0 en un nivel lo deja fuera, igual que el control.</summary>
        Friend Shared Function FiltroCategorias(categ As Integer, scateg As Integer, sscateg As Integer) As String
            Dim partes As New List(Of String)
            If categ > 0 Then partes.Add($"I.Categ = {categ}")
            If scateg > 0 Then partes.Add($"I.SCateg = {scateg}")
            If sscateg > 0 Then partes.Add($"I.SSCateg = {sscateg}")
            Return String.Join(" AND ", partes)
        End Function

        Friend Shared Function SqlVecino(siguiente As Boolean, filtro As String) As String
            Return "SELECT A.CodigoArt FROM posarticulos A " &
                   "INNER JOIN posarticuloslocal L ON L.CArticulo = A.CodigoArt AND L.CLocal = @local " &
                   "INNER JOIN posinventario I ON I.CArticulo = A.CodigoArt AND I.CLocal = @local " &
                   $"WHERE A.CodigoArt {If(siguiente, ">", "<")} @codigo{If(filtro.Length = 0, "", " AND " & filtro)} " &
                   $"ORDER BY A.CodigoArt{If(siguiente, "", " DESC")} LIMIT 1"
        End Function

        ''' <summary>SD_Siguiente y SD_Anterior: comparan el código como texto. Vacío si no hay más.</summary>
        Public Async Function VecinoAsync(codigo As String, local As Long, siguiente As Boolean, categ As Integer, scateg As Integer, sscateg As Integer, Optional ct As CancellationToken = Nothing) As Task(Of String)
            Dim sql = SqlVecino(siguiente, FiltroCategorias(categ, scateg, sscateg))
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return If(Await cn.ExecuteScalarAsync(Of String)(New CommandDefinition(sql, New With {.codigo = If(codigo = "", "0", codigo), local}, cancellationToken:=ct)).ConfigureAwait(False), "")
            End Using
        End Function

        ''' <summary>
        ''' Buscar. En VB6 el filtro de categorías quedaba dentro del LEFT JOIN de los códigos de barras y no
        ''' filtraba; aquí va en el WHERE. La segunda columna es el código.
        ''' </summary>
        Friend Shared Function SqlBuscar(filtro As String) As String
            Return "SELECT CONCAT(A.Descripcion, IF(A.Consumo = '1', ' >>Bloqueado', '')) AS 'Descripción', A.CodigoArt AS 'Código', L.Stock AS 'Existencia', " &
                   "A.PrecioCosto AS 'Costo', ROUND(IF(A.Exento = 0, A.Precio1, A.Precio1 * ((A.Exento / 100) + 1)), 2) AS 'Precio de venta', IFNULL(F.CAlterno, '') AS 'Barras' " &
                   "FROM posarticulos A " &
                   "INNER JOIN posarticuloslocal L ON L.CArticulo = A.CodigoArt AND L.CLocal = @local " &
                   "INNER JOIN posinventario I ON I.CArticulo = A.CodigoArt AND I.CLocal = @local " &
                   "LEFT JOIN poscodart_codbarra_freq F ON F.Local = L.CLocal AND F.articulo = L.CArticulo" &
                   If(filtro.Length = 0, "", " WHERE " & filtro) & " ORDER BY A.Descripcion"
        End Function

        Public Async Function BuscarAsync(local As Long, categ As Integer, scateg As Integer, sscateg As Integer, Optional ct As CancellationToken = Nothing) As Task(Of DataTable)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Using lector = Await cn.ExecuteReaderAsync(New CommandDefinition(SqlBuscar(FiltroCategorias(categ, scateg, sscateg)), New With {local}, commandTimeout:=120, cancellationToken:=ct)).ConfigureAwait(False)
                    Dim tabla As New DataTable()
                    tabla.Load(lector)
                    Return tabla
                End Using
            End Using
        End Function

        ''' <summary>
        ''' SD_CodAlterno: las etiquetas de balanza que empiezan con 26 o 28 traen el código en las posiciones
        ''' 3 a 6. Devuelve el código tal cual para buscarlo directo.
        ''' </summary>
        Friend Shared Function CodigoDeEtiqueta(codigo As String) As String
            If codigo.Length > 8 AndAlso (codigo.StartsWith("26", StringComparison.Ordinal) OrElse codigo.StartsWith("28", StringComparison.Ordinal)) Then
                Return CLng(Vb6.Val(codigo.Substring(2, 4))).ToString(Invariante)
            End If
            Return codigo
        End Function

        ''' <summary>
        ''' SD_CodAlterno: busca primero en el local y después en los demás. Si en el local (o en -1) hay más de
        ''' un artículo con ese código, devuelve las opciones para elegir. Un código largo encontrado se anota en
        ''' poscodart_codbarra_freq, igual que en VB6. Vacío si no está; entonces se usa como código de artículo.
        ''' </summary>
        Public Async Function BuscarCodigoBarrasAsync(texto As String, local As Long, Optional ct As CancellationToken = Nothing) As Task(Of ResultadoCodigoBarras)
            Dim original = If(texto, "").Trim()
            Dim resultado As New ResultadoCodigoBarras()
            If original.Length = 0 Then Return resultado
            Dim codigo = CodigoDeEtiqueta(original)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim p = New With {codigo, local}
                Dim primero = Await cn.ExecuteScalarAsync(Of String)(New CommandDefinition(
                    "SELECT articulo FROM poscodart_codbarra WHERE CAlterno = @codigo ORDER BY Local <> @local LIMIT 1", p, cancellationToken:=ct)).ConfigureAwait(False)
                If String.IsNullOrEmpty(primero) Then Return resultado
                Dim enLocal = Await cn.ExecuteScalarAsync(Of Long)(New CommandDefinition(
                    "SELECT COUNT(articulo) FROM poscodart_codbarra WHERE CAlterno = @codigo AND Local IN (-1, @local)", p, cancellationToken:=ct)).ConfigureAwait(False)
                If enLocal > 1 Then
                    Using lector = Await cn.ExecuteReaderAsync(New CommandDefinition(
                        "SELECT C.articulo AS 'Artículo', IFNULL(A.Descripcion, '') AS 'Descripción', C.CAlterno AS 'Código', C.Unidades FROM poscodart_codbarra C " &
                        "LEFT JOIN posarticulos A ON A.CodigoArt = C.articulo WHERE C.CAlterno = @codigo", p, cancellationToken:=ct)).ConfigureAwait(False)
                        resultado.Opciones = New DataTable()
                        resultado.Opciones.Load(lector)
                    End Using
                    Return resultado
                End If
                resultado.Articulo = primero
                If original.Length > 7 Then
                    Dim anotado = Await cn.ExecuteScalarAsync(Of String)(New CommandDefinition("SELECT articulo FROM poscodart_codbarra_freq WHERE CAlterno = @original", New With {original}, cancellationToken:=ct)).ConfigureAwait(False)
                    If String.IsNullOrEmpty(anotado) Then
                        Await cn.ExecuteAsync(New CommandDefinition("REPLACE INTO poscodart_codbarra_freq (articulo, CAlterno, Local) VALUES (@primero, @original, @local)", New With {primero, original, local}, cancellationToken:=ct)).ConfigureAwait(False)
                    End If
                End If
            End Using
            Return resultado
        End Function

        ''' <summary>La descripción del artículo con ese código, o vacío si está libre (txtCodigoArt_Validate).</summary>
        Public Async Function DescripcionDeAsync(codigo As String, Optional ct As CancellationToken = Nothing) As Task(Of String)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return If(Await cn.ExecuteScalarAsync(Of String)(New CommandDefinition("SELECT Descripcion FROM posarticulos WHERE CodigoArt = @codigo", New With {codigo}, cancellationToken:=ct)).ConfigureAwait(False), "")
            End Using
        End Function

        ''' <summary>
        ''' pos_fun_obt_codigo2: el primer número libre desde 10000 entre los códigos de artículo y los códigos
        ''' cortos del local, o el siguiente al mayor. VB6 creaba la tabla postmpcodigos, con el mismo nombre para
        ''' todos los usuarios; aquí se calcula sin tablas.
        ''' </summary>
        Public Shared Function PrimerLibre(usados As IEnumerable(Of Long)) As Long
            Dim siguiente = 10000L
            For Each u In usados.Where(Function(x) x > 9999).Distinct().OrderBy(Function(x) x)
                If u <> siguiente Then Return siguiente
                siguiente += 1
            Next
            Return siguiente
        End Function

        Public Async Function CodigoNuevoAsync(local As Long, Optional ct As CancellationToken = Nothing) As Task(Of Long)
            Const sql As String = "SELECT CAST(CodigoArt AS UNSIGNED) FROM posarticulos UNION ALL SELECT CAST(CAlterno AS UNSIGNED) FROM poscodart_codbarra WHERE LENGTH(CAlterno) < 7 AND Local = @local"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return PrimerLibre(Await cn.QueryAsync(Of Long)(New CommandDefinition(sql, New With {local}, commandTimeout:=120, cancellationToken:=ct)).ConfigureAwait(False))
            End Using
        End Function

        ''' <summary>SD_BuscaSug: el código corto más alto del local más uno. En el local 4 los cortos tienen menos de 6.</summary>
        Public Async Function ProximoSugeridoAsync(local As Long, Optional ct As CancellationToken = Nothing) As Task(Of Long)
            Dim largo = If(local = 4, 6, 7)
            Dim sql = $"SELECT MAX(CONVERT(CAlterno, UNSIGNED)) + 1 FROM poscodart_codbarra WHERE Local = @local AND LENGTH(CAlterno) < {largo}"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return If(Await cn.ExecuteScalarAsync(Of Long?)(New CommandDefinition(sql, New With {local}, cancellationToken:=ct)).ConfigureAwait(False), 0L)
            End Using
        End Function

        ' ---- Foto para detectar cambios de otros usuarios y anotar lo que cambió ----

        ''' <summary>SD_RespReg: la fila de posarticulos sin las fechas (ADO las saltaba por ser adDBTimeStamp).</summary>
        Public Async Function FotoAsync(codigo As String, Optional ct As CancellationToken = Nothing) As Task(Of Dictionary(Of String, String))
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Await FotoAsync(cn, codigo, ct).ConfigureAwait(False)
            End Using
        End Function

        Private Shared Async Function FotoAsync(cn As MySqlConnection, codigo As String, ct As CancellationToken) As Task(Of Dictionary(Of String, String))
            Dim foto As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
            Using lector = Await cn.ExecuteReaderAsync(New CommandDefinition("SELECT * FROM posarticulos WHERE CodigoArt = @codigo", New With {codigo}, cancellationToken:=ct)).ConfigureAwait(False)
                If Not Await lector.ReadAsync(ct).ConfigureAwait(False) Then Return foto
                For i = 0 To lector.FieldCount - 1
                    If lector.GetFieldType(i) = GetType(Date) Then Continue For
                    foto(lector.GetName(i)) = If(lector.IsDBNull(i), "", Convert.ToString(lector.GetValue(i), Invariante).Replace(vbCrLf, ""))
                Next
            End Using
            Return foto
        End Function

        Public Shared Function Comparar(anterior As Dictionary(Of String, String), actual As Dictionary(Of String, String)) As List(Of CambioArticulo)
            Dim cambios As New List(Of CambioArticulo)
            If anterior Is Nothing OrElse anterior.Count = 0 Then Return cambios
            For Each campo In actual.Keys
                Dim antes As String = Nothing
                If Not anterior.TryGetValue(campo, antes) Then Continue For
                If antes.Trim() <> actual(campo).Trim() Then cambios.Add(New CambioArticulo With {.Campo = campo, .Anterior = antes.Trim(), .Actual = actual(campo).Trim()})
            Next
            Return cambios
        End Function

        ''' <summary>SD_CompReg: lo que otro usuario cambió desde la foto. Vacío si nadie lo tocó.</summary>
        Public Async Function CambiosDeOtrosAsync(codigo As String, foto As Dictionary(Of String, String), Optional ct As CancellationToken = Nothing) As Task(Of List(Of CambioArticulo))
            Return Comparar(foto, Await FotoAsync(codigo, ct).ConfigureAwait(False))
        End Function

        Public Shared Function TextoCambios(titulo As String, cambios As IEnumerable(Of CambioArticulo)) As String
            Dim sb As New StringBuilder()
            sb.AppendLine(titulo).AppendLine()
            sb.AppendLine($"{"IDENTIFICADOR",-20} {"ANTERIOR",-35} ACTUAL")
            sb.AppendLine(New String("-"c, 90))
            For Each c In cambios
                sb.AppendLine($"{c.Campo,-20} {c.Anterior,-35} {c.Actual}")
            Next
            Return sb.ToString()
        End Function

        ''' <summary>RegistrarMovimiento: una línea en posvitmovimientos. Proced admite 32 caracteres.</summary>
        Public Shared Async Function RegistrarAsync(cn As MySqlConnection, local As Long, uid As Long, proceso As String, descripcion As String, estacion As String, Optional ct As CancellationToken = Nothing) As Task
            Const sql As String = "INSERT INTO posvitmovimientos (local, uid, Proced, descrip, estacion, fc) VALUES (@local, @uid, @proceso, @descripcion, @estacion, NOW())"
            Dim p = New With {local, uid, .proceso = Recortar(proceso, 32), descripcion, .estacion = Recortar(estacion, 32)}
            Await cn.ExecuteAsync(New CommandDefinition(sql, p, cancellationToken:=ct)).ConfigureAwait(False)
        End Function

        Private Shared Function Recortar(texto As String, largo As Integer) As String
            texto = If(texto, "")
            Return If(texto.Length > largo, texto.Substring(0, largo), texto)
        End Function

        ' ---- Guardar ----

        ''' <summary>El REPLACE de SD_GuardarArticulo. FM queda en NOW() y Promedio en 0, igual que en VB6.</summary>
        Friend Shared Function SentenciaGuardar(a As Articulo) As String
            Dim fc = If(a.FC, Date.Now)
            Return "REPLACE INTO posarticulos (CodigoArt, CodAlterno, DescCorta, Descripcion, Ubicacion, Grupo, SubGrupo, SinCantidad, UnidadMedida, ValorEmpaque, Moneda, PrecioCosto, " &
                   "CostoPonderado, CostoPromedio, Utilidad1, Utilidad2, Utilidad3, Utilidad4, Utilidad5, Utilidad6, Ex1, Ex2, Ex3, Ex4, Ex5, Ex6, " &
                   "Precio1, Precio2, Precio3, Precio4, Precio5, Precio6, Exento, Porcentaje, SinPeso, PorcentajeOtros, Consignacion, SinPrecio, Gasto, Servicio, Descuento, Flete, Recomendada, " &
                   "Promedio, DescEspecial, NumDesc, Patrocinador, NumPatroc, DescuentoA, CodArtProv, Especificacion, Consumo, FC, FM, Usuario, MXMayor, UsuarioCrea) VALUES (" &
                   String.Join(", ", {
                       Q(a.CodigoArt), Q(a.CodAlterno), Q(a.DescCorta), Q(a.Descripcion), Q(a.Ubicacion), N(a.Grupo), a.SubGrupo.ToString(Invariante), Q(a.SinCantidad),
                       Q(a.UnidadMedida), Q(a.ValorEmpaque), Q(a.Moneda), N(a.PrecioCosto), N(a.CostoPonderado), N(If(a.CostoPromedio, 0)),
                       N(a.Utilidad1), N(a.Utilidad2), N(a.Utilidad3), N(a.Utilidad4), N(a.Utilidad5), N(a.Utilidad6),
                       N(a.Ex1), N(a.Ex2), N(a.Ex3), N(a.Ex4), N(a.Ex5), N(a.Ex6),
                       N(a.Precio1), N(a.Precio2), N(a.Precio3), N(a.Precio4), N(a.Precio5), N(a.Precio6),
                       N(a.Exento), N(a.Porcentaje), Q(a.SinPeso), N(a.PorcentajeOtros), Q(a.Consignacion), Q(a.SinPrecio), Q(a.Gasto), Q(a.Servicio),
                       N(a.Descuento), N(a.Flete), N(a.Recomendada), "0", Q(a.DescEspecial), a.NumDesc.ToString(Invariante), Q(a.Patrocinador), a.NumPatroc.ToString(Invariante),
                       N(a.DescuentoA), a.CodArtProv.ToString(Invariante), Q(a.Especificacion), Q(a.Consumo), Q(fc.ToString("yyyy-MM-dd HH:mm:ss", Invariante)), "NOW()",
                       Q(a.Usuario), a.MXMayor.ToString(Invariante), Q(a.UsuarioCrea)}) & ")"
        End Function

        Friend Shared Function SentenciaAltaLocal(codigo As String, uid As Long) As String
            Return "REPLACE INTO posarticuloslocal (CLocal, CArticulo, Stock, Min, Max, Recom, FM, FC, Usuario, InvInicial, InvInicialF, LinCredito, Localiz, CosInicial) " &
                   $"VALUES ($Local, {Q(codigo)}, 0, 0, 0, 0, NOW(), NOW(), '{uid}', 0, NOW(), 0, 1, 0)"
        End Function

        Friend Shared Function SentenciaAltaInventario(codigo As String, categ As Integer, scateg As Integer, sscateg As Integer, uid As Long) As String
            Return "INSERT INTO posinventario (CLocal, CArticulo, Stock, Min, Max, Recom, Categ, SCateg, SSCateg, FM, FC, Usuario, InvInicial, InvInicialF, LinCredito, Localiz) " &
                   $"VALUES ($Local, {Q(codigo)}, 0, 0, 0, 0, {categ}, {scateg}, {sscateg}, NOW(), NOW(), '{uid}', 0, NOW(), 1, 1)"
        End Function

        Friend Shared Function SentenciaCategorias(codigo As String, categ As Integer, scateg As Integer, sscateg As Integer) As String
            Return $"UPDATE posinventario SET Categ = {categ}, SCateg = {scateg}, SSCateg = {sscateg}, FM = NOW() WHERE CLocal = $Local AND CArticulo = {Q(codigo)}"
        End Function

        Private Shared Async Function EjecutarYReplicarAsync(cn As MySqlConnection, plantilla As String, identificador As String, local As Long, ct As CancellationToken) As Task
            Await cn.ExecuteAsync(New CommandDefinition(plantilla.Replace("$Local", local.ToString(Invariante)), cancellationToken:=ct)).ConfigureAwait(False)
            Await Replicador.ReplicarAsync(cn, identificador, plantilla, local, ct).ConfigureAwait(False)
        End Function

        ''' <summary>
        ''' SD_GuardarArticulo: el artículo, y en el local su alta (si es nuevo) y sus categorías. Conserva los
        ''' descuentos especiales y patrocinios que ya tenía. Anota en posvitmovimientos lo que cambió desde
        ''' <paramref name="foto"/> y cada utilidad modificada. Sin transacción: las tablas son MyISAM.
        ''' </summary>
        Public Async Function GuardarAsync(a As Articulo, nuevo As Boolean, local As Long, uid As Long, estacion As String, categ As Integer, scateg As Integer, sscateg As Integer,
                                           foto As Dictionary(Of String, String), Optional ct As CancellationToken = Nothing) As Task
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim especiales = Await cn.QueryFirstOrDefaultAsync(Of Articulo)(New CommandDefinition(
                    "SELECT DescEspecial, NumDesc, Patrocinador, NumPatroc FROM posarticulos WHERE CodigoArt = @CodigoArt", New With {a.CodigoArt}, cancellationToken:=ct)).ConfigureAwait(False)
                If especiales IsNot Nothing Then
                    a.DescEspecial = especiales.DescEspecial
                    a.NumDesc = especiales.NumDesc
                    a.Patrocinador = especiales.Patrocinador
                    a.NumPatroc = especiales.NumPatroc
                End If
                Dim sql = SentenciaGuardar(a)
                Await cn.ExecuteAsync(New CommandDefinition(sql, cancellationToken:=ct)).ConfigureAwait(False)
                Await Replicador.ReplicarAsync(cn, ReplicaArticulos, sql, local, ct).ConfigureAwait(False)

                If nuevo Then
                    Await EjecutarYReplicarAsync(cn, SentenciaAltaLocal(a.CodigoArt, uid), ReplicaLocal, local, ct).ConfigureAwait(False)
                    Await EjecutarYReplicarAsync(cn, SentenciaAltaInventario(a.CodigoArt, categ, scateg, sscateg, uid), ReplicaLocal, local, ct).ConfigureAwait(False)
                Else
                    Dim existe = Await cn.ExecuteScalarAsync(Of String)(New CommandDefinition(
                        "SELECT CArticulo FROM posinventario WHERE CArticulo = @CodigoArt AND CLocal = @local", New With {a.CodigoArt, local}, cancellationToken:=ct)).ConfigureAwait(False)
                    Dim plantilla = If(String.IsNullOrEmpty(existe), SentenciaAltaInventario(a.CodigoArt, categ, scateg, sscateg, uid), SentenciaCategorias(a.CodigoArt, categ, scateg, sscateg))
                    Await EjecutarYReplicarAsync(cn, plantilla, ReplicaLocal, local, ct).ConfigureAwait(False)
                End If

                Dim despues = Await FotoAsync(cn, a.CodigoArt, ct).ConfigureAwait(False)
                For i = 1 To 6
                    Dim campo = $"Utilidad{i}"
                    Dim antes As String = Nothing
                    If foto IsNot Nothing AndAlso foto.TryGetValue(campo, antes) AndAlso despues.ContainsKey(campo) AndAlso Conversion.Val(antes) <> Conversion.Val(despues(campo)) Then
                        Await RegistrarAsync(cn, local, uid, $"Cambio de utilidad {i - 1}", $"Articulo: {a.CodigoArt} Valor actual: {antes} Valor nuevo: {despues(campo)}", estacion, ct).ConfigureAwait(False)
                    End If
                Next
                Dim cambios = Comparar(foto, despues)
                If cambios.Count > 0 Then
                    Await RegistrarAsync(cn, local, uid, Bitacora, TextoCambios($"Los datos modificados en {Bitacora}({a.CodigoArt}) son los siguientes:", cambios), estacion, ct).ConfigureAwait(False)
                End If
            End Using
        End Function

        ' ---- Borrar ----

        ''' <summary>ValidarBorrar: el aviso si tiene existencias o se vendió hace menos de POSTiempElimArtic días. Vacío si se puede.</summary>
        Public Async Function ValidarBorrarAsync(codigo As String, local As Long, Optional ct As CancellationToken = Nothing) As Task(Of String)
            Dim dias = CInt(Await New Parametros(_fabrica).ObtenerNumeroAsync(local, "POSTiempElimArtic", ct).ConfigureAwait(False))
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim stock = If(Await cn.ExecuteScalarAsync(Of Double?)(New CommandDefinition(
                    "SELECT Stock FROM posarticuloslocal WHERE CArticulo = @codigo AND CLocal = @local", New With {codigo, local}, cancellationToken:=ct)).ConfigureAwait(False), 0)
                If stock > 0 Then
                    Return $"El sistema detectó {stock.ToString("N2", CultureInfo.GetCultureInfo("es-CR"))} unidad(es) en existencia.{Environment.NewLine}" &
                           $"Para eliminar se requiere que el artículo no tenga existencias.{Environment.NewLine}{Environment.NewLine}El artículo no se puede eliminar en este momento."
                End If
                Dim venta = Await cn.ExecuteScalarAsync(Of Date?)(New CommandDefinition(
                    "SELECT MAX(A.FC) FROM posfacturaencabezado A INNER JOIN posfacturadetalle B ON B.local = A.local AND B.numfac = A.numfac " &
                    "WHERE B.codigoart = @codigo AND B.local = @local AND A.FC > (NOW() - INTERVAL @dias DAY)", New With {codigo, local, dias}, commandTimeout:=120, cancellationToken:=ct)).ConfigureAwait(False)
                If venta.HasValue Then
                    Dim cr = CultureInfo.GetCultureInfo("es-CR")
                    Return $"La última venta registrada del artículo fue el {venta.Value.ToString("D", cr)}.{Environment.NewLine}{Environment.NewLine}" &
                           $"No se permitirá eliminar este artículo hasta el {venta.Value.AddDays(dias).ToString("D", cr)}."
                End If
            End Using
            Return ""
        End Function

        ''' <summary>SD_BorrarArticulo: borra el artículo y sus códigos de barras, sin replicar, igual que VB6.</summary>
        Public Async Function BorrarAsync(codigo As String, local As Long, uid As Long, estacion As String, Optional ct As CancellationToken = Nothing) As Task
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition("DELETE FROM poscodart_codbarra WHERE articulo = @codigo", New With {codigo}, cancellationToken:=ct)).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition("DELETE FROM posarticulos WHERE CodigoArt = @codigo", New With {codigo}, cancellationToken:=ct)).ConfigureAwait(False)
                Await RegistrarAsync(cn, local, uid, "Eliminar artículo", $"Artículo {codigo}", estacion, ct).ConfigureAwait(False)
            End Using
        End Function

    End Class

End Namespace
