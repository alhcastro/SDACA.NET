Imports System.Data
Imports System.Globalization
Imports System.Threading
Imports Dapper
Imports MySqlConnector
Imports CoopeNet.Core.Bancos
Imports CoopeNet.Core.Datos

Namespace Pos

    ''' <summary>Una fila de posarticuloslocal: las existencias y los límites de un artículo en un local.</summary>
    Public NotInheritable Class ExistenciaLocal
        Public Property CArticulo As String = ""
        Public Property Stock As Double
        Public Property Min As Double
        Public Property Max As Double
        Public Property Recom As Double
        Public Property InvInicial As Double
        Public Property InvInicialF As Date
        Public Property FM As Date
        Public Property FC As Date
        Public Property LinCredito As Long
        Public Property Localiz As String = ""
        Public Property CosInicial As Double
    End Class

    ''' <summary>Consultar el código, el siguiente o el anterior (Consultar de VB6).</summary>
    Public Enum BusquedaExistencia
        Consulta
        Siguiente
        Anterior
    End Enum

    ''' <summary>Una compra o una venta del historial del artículo.</summary>
    Public NotInheritable Class MovimientoExistencia
        Public Property Documento As String = ""
        ''' <summary>El proveedor en las compras y el cliente en las ventas.</summary>
        Public Property Tercero As String = ""
        Public Property Fecha As Date?
        Public Property Cantidad As Double
        Public Property Costo As Double?
        Public Property Precio As Double
        Public Property Usuario As String = ""
    End Class

    ''' <summary>Una fila de poslocalescnn.</summary>
    Friend NotInheritable Class ServidorLocal
        Public Property Servidor As String = ""
        Public Property Puerto As ULong
        Public Property Catalogo As String = ""
    End Class

    ''' <summary>Las listas de cmbLinea y cmbLocalizacion.</summary>
    Public NotInheritable Class OpcionesExistencia
        Public Property Lineas As New List(Of OpcionArticulo)
        Public Property Localizaciones As New List(Of OpcionArticulo)
    End Class

    ''' <summary>
    ''' Existencias del artículo por local. Migración de las consultas de SD0502\FrmMantLocales.frm
    ''' (SD_CargarLocal, SD_Detalle, SD_iGuardarLocal, SD_iBorrarLocal, cmdModif_Click, SD_Combos, SD_Combos2)
    ''' y de SD_ActPosInvent de ModPrin.bas.
    ''' </summary>
    ''' <remarks>
    ''' Los locales que están en poslocalescnn se consultan en su propio servidor, con el usuario sdaca, igual
    ''' que en VB6; los cambios siempre van al local del usuario y a la conexión principal. El REPLACE de un
    ''' registro nuevo conserva CosInicial: VB6 no la nombraba y la dejaba en 0.
    ''' </remarks>
    Public NotInheritable Class ExistenciasLocalRepositorio

        Public Const Replica As String = "posarticuloslocal"

        Private Shared ReadOnly Invariante As CultureInfo = CultureInfo.InvariantCulture
        Private ReadOnly _fabrica As IFabricaConexiones

        ''' <param name="fabrica">La conexión principal, la del local del usuario.</param>
        Public Sub New(fabrica As IFabricaConexiones)
            _fabrica = fabrica
        End Sub

        Public ReadOnly Property Principal As IFabricaConexiones
            Get
                Return _fabrica
            End Get
        End Property

        Private Shared Function Q(texto As String) As String
            Return $"'{LibroBancos.Literal(If(texto, ""))}'"
        End Function

        Private Shared Function N(valor As Double) As String
            Return valor.ToString("R", Invariante)
        End Function

        Private Shared Function F(fecha As Date) As String
            Return fecha.ToString("yyyy-MM-dd HH:mm:ss", Invariante)
        End Function

        ' ---- Catálogos ----

        ''' <summary>SD_Combos: todos los locales, como "1 - SUPER CENTRO".</summary>
        Public Async Function LocalesAsync(Optional ct As CancellationToken = Nothing) As Task(Of List(Of OpcionArticulo))
            Const sql As String = "SELECT CAST(LOCAL AS CHAR) AS Codigo, CONCAT(LOCAL, ' - ', Descripcion) AS Descripcion FROM gatlocales ORDER BY LOCAL"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return (Await cn.QueryAsync(Of OpcionArticulo)(New CommandDefinition(sql, cancellationToken:=ct)).ConfigureAwait(False)).ToList()
            End Using
        End Function

        ''' <summary>
        ''' SD_Combos2: las líneas de crédito (cxctiposcxc del módulo 5, precedidas de "0 - N/A") y las
        ''' localizaciones del local.
        ''' </summary>
        Public Async Function OpcionesAsync(local As Long, Optional ct As CancellationToken = Nothing) As Task(Of OpcionesExistencia)
            Dim o As New OpcionesExistencia()
            o.Lineas.Add(New OpcionArticulo With {.Codigo = "0", .Descripcion = "0 - N/A"})
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                o.Lineas.AddRange(Await cn.QueryAsync(Of OpcionArticulo)(New CommandDefinition(
                    "SELECT CAST(TIPO AS CHAR) AS Codigo, CONCAT(TIPO, ' - ', DETALLE) AS Descripcion FROM cxctiposcxc WHERE MODULO = 5 AND LOCAL = @local ORDER BY TIPO",
                    New With {local}, cancellationToken:=ct)).ConfigureAwait(False))
                o.Localizaciones.AddRange(Await cn.QueryAsync(Of OpcionArticulo)(New CommandDefinition(
                    "SELECT CAST(CODIGO AS CHAR) AS Codigo, CONCAT(CODIGO, ' > ', IFNULL(DESCRIPCION, '')) AS Descripcion FROM poslocalizinv WHERE LOCAL = @local ORDER BY CODIGO",
                    New With {local}, cancellationToken:=ct)).ConfigureAwait(False))
            End Using
            Return o
        End Function

        ''' <summary>
        ''' CmbLocal_Click: el servidor de otro local según poslocalescnn, con el usuario sdaca. Nothing si el
        ''' local no tiene servidor propio.
        ''' </summary>
        Public Async Function ConexionDeLocalAsync(local As Long, Optional ct As CancellationToken = Nothing) As Task(Of IFabricaConexiones)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim fila = Await cn.QueryFirstOrDefaultAsync(Of ServidorLocal)(New CommandDefinition("SELECT srv AS Servidor, CAST(PORT AS UNSIGNED) AS Puerto, dbname AS Catalogo FROM poslocalescnn WHERE local2 = @local LIMIT 1", New With {local}, cancellationToken:=ct)).ConfigureAwait(False)
                If fila Is Nothing OrElse String.IsNullOrWhiteSpace(fila.Servidor) Then Return Nothing
                Return New FabricaConexiones(CadenaConexion.Crear(fila.Servidor.Trim(), fila.Catalogo.Trim(), CUInt(fila.Puerto), "", ""))
            End Using
        End Function

        ' ---- Consulta ----

        Friend Shared Function SqlCargar(busqueda As BusquedaExistencia, filtro As String) As String
            Dim condicion = CondicionBusqueda(busqueda)
            Return "SELECT A.CArticulo, A.Stock, A.Min, A.Max, A.Recom, A.InvInicial, A.InvInicialF, A.FM, A.FC, A.LinCredito, A.Localiz, A.CosInicial " &
                   "FROM posarticuloslocal A INNER JOIN posarticulos B ON A.CArticulo = B.CodigoArt " &
                   "INNER JOIN posinventario I ON I.CArticulo = A.CArticulo AND I.CLocal = @local " &
                   $"WHERE A.CLocal = @local{If(filtro.Length = 0, "", " AND " & filtro)} AND A.CArticulo {condicion}"
        End Function

        Private Shared Function CondicionBusqueda(busqueda As BusquedaExistencia) As String
            Select Case busqueda
                Case BusquedaExistencia.Siguiente : Return "> @codigo ORDER BY A.CArticulo LIMIT 1"
                Case BusquedaExistencia.Anterior : Return "< @codigo ORDER BY A.CArticulo DESC LIMIT 1"
                Case Else : Return "= @codigo LIMIT 1"
            End Select
        End Function

        ''' <summary>
        ''' SD_CargarLocal: el artículo del local (o el siguiente o el anterior, por código como texto),
        ''' siempre que esté en posinventario y cumpla el filtro de categorías. Nothing si no hay.
        ''' </summary>
        ''' <param name="datos">La conexión del local que se consulta.</param>
        Public Shared Async Function CargarAsync(datos As IFabricaConexiones, local As Long, codigo As String, busqueda As BusquedaExistencia,
                                                 categ As Integer, scateg As Integer, sscateg As Integer, Optional ct As CancellationToken = Nothing) As Task(Of ExistenciaLocal)
            Dim sql = SqlCargar(busqueda, ArticulosRepositorio.FiltroCategorias(categ, scateg, sscateg))
            Using cn = Await datos.AbrirAsync(ct).ConfigureAwait(False)
                Return Await cn.QueryFirstOrDefaultAsync(Of ExistenciaLocal)(New CommandDefinition(sql, New With {local, codigo}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>La descripción del artículo, o "Artículo desconocido".</summary>
        Public Shared Async Function DescripcionAsync(datos As IFabricaConexiones, codigo As String, Optional ct As CancellationToken = Nothing) As Task(Of String)
            Using cn = Await datos.AbrirAsync(ct).ConfigureAwait(False)
                Dim d = Await cn.ExecuteScalarAsync(Of String)(New CommandDefinition("SELECT Descripcion FROM posarticulos WHERE CodigoArt = @codigo", New With {codigo}, cancellationToken:=ct)).ConfigureAwait(False)
                Return If(String.IsNullOrEmpty(d), "Artículo desconocido", d)
            End Using
        End Function

        ''' <summary>txtCodigoArticulo_KeyDown vacío: los artículos del local con su existencia, filtrados por categoría.</summary>
        Public Async Function BuscarAsync(local As Long, categ As Integer, scateg As Integer, sscateg As Integer, Optional ct As CancellationToken = Nothing) As Task(Of DataTable)
            Dim filtro = ArticulosRepositorio.FiltroCategorias(categ, scateg, sscateg)
            Dim sql = "SELECT A.Descripcion AS ARTICULO, A.CodigoArt AS CODIGO, L.STOCK FROM posarticulos A " &
                      "INNER JOIN posarticuloslocal L ON L.CArticulo = A.CodigoArt AND L.CLocal = @local " &
                      "INNER JOIN posinventario I ON I.CArticulo = A.CodigoArt AND I.CLocal = @local" &
                      If(filtro.Length = 0, "", " WHERE " & filtro)
            Return Await TablaAsync(sql, New With {local}, ct).ConfigureAwait(False)
        End Function

        Private Async Function TablaAsync(sql As String, parametros As Object, ct As CancellationToken) As Task(Of DataTable)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Using lector = Await cn.ExecuteReaderAsync(New CommandDefinition(sql, parametros, commandTimeout:=120, cancellationToken:=ct)).ConfigureAwait(False)
                    Dim tabla As New DataTable()
                    tabla.Load(lector)
                    Return tabla
                End Using
            End Using
        End Function

        ' ---- Historial (SD_Detalle) ----

        ''' <summary>Las últimas compras del artículo en el local.</summary>
        Public Shared Async Function ComprasAsync(datos As IFabricaConexiones, local As Long, codigo As String, limite As Integer, Optional ct As CancellationToken = Nothing) As Task(Of List(Of MovimientoExistencia))
            Const sql As String =
                "SELECT C.FACT_COMP AS Documento, TRIM(CONCAT(IFNULL(D.apellido1, ''), ' ', IFNULL(D.apellido2, ''), ' ', IFNULL(D.nombre, ''))) AS Tercero, C.fm AS Fecha, " &
                "C.CANTIDAD AS Cantidad, C.PRE_COSTO AS Costo, C.PRECIOVENTA AS Precio, IFNULL(U.usuario, '') AS Usuario " &
                "FROM poscompraslineas C INNER JOIN poscompras A ON A.PROVEEDOR = C.PROVEEDOR AND A.FACT_COMP = C.FACT_COMP " &
                "LEFT JOIN gatusuarios U ON U.UID = A.Usuario LEFT JOIN gacdp D ON D.id = C.proveedor " &
                "WHERE A.Local = @local AND C.articulo = @codigo ORDER BY A.fc DESC LIMIT @limite"
            Using cn = Await datos.AbrirAsync(ct).ConfigureAwait(False)
                Return (Await cn.QueryAsync(Of MovimientoExistencia)(New CommandDefinition(sql, New With {local, codigo, limite}, cancellationToken:=ct)).ConfigureAwait(False)).ToList()
            End Using
        End Function

        ''' <summary>
        ''' Las últimas ventas del artículo en el local, de los últimos <paramref name="meses"/> meses. VB6 no
        ''' limita los meses cuando el usuario es del local 4; entonces se pasa Nothing.
        ''' </summary>
        Public Shared Async Function VentasAsync(datos As IFabricaConexiones, local As Long, codigo As String, limite As Integer, meses As Integer?, Optional ct As CancellationToken = Nothing) As Task(Of List(Of MovimientoExistencia))
            Dim sql =
                "SELECT CAST(E.numfac AS CHAR) AS Documento, CAST(IFNULL(TRIM(CONCAT(P.nombre, ' ', P.apellido1, ' ', P.apellido2)), E.cliente) AS CHAR) AS Tercero, E.fc AS Fecha, " &
                "D.cantidad AS Cantidad, D.costo / D.cantidad AS Costo, D.precio AS Precio, IFNULL(U.usuario, '') AS Usuario FROM posfacturadetalle D " &
                "INNER JOIN posfacturaencabezado E USE INDEX(FC) ON (E.Local = D.Local AND E.NumFac = D.NumFac) " &
                "LEFT JOIN gatusuarios U ON U.UID = D.Usuario LEFT JOIN gacdp P ON P.id = E.Cliente " &
                "WHERE D.Local = @local AND D.codigoart = @codigo" &
                If(meses.HasValue, " AND E.fc > DATE_SUB(NOW(), INTERVAL @meses MONTH)", "") &
                " ORDER BY E.local, E.fc DESC LIMIT @limite"
            Using cn = Await datos.AbrirAsync(ct).ConfigureAwait(False)
                Return (Await cn.QueryAsync(Of MovimientoExistencia)(New CommandDefinition(sql, New With {local, codigo, limite, meses}, commandTimeout:=120, cancellationToken:=ct)).ConfigureAwait(False)).ToList()
            End Using
        End Function

        ''' <summary>El estilo activo de posestiloshtml para las tablas impresas, o vacío.</summary>
        Public Shared Async Function EstiloHtmlAsync(datos As IFabricaConexiones, Optional ct As CancellationToken = Nothing) As Task(Of String)
            Using cn = Await datos.AbrirAsync(ct).ConfigureAwait(False)
                Return If(Await cn.ExecuteScalarAsync(Of String)(New CommandDefinition("SELECT CAST(estilo AS CHAR) FROM posestiloshtml WHERE objeto = 'tabla' AND activo = '1' LIMIT 1", cancellationToken:=ct)).ConfigureAwait(False), "")
            End Using
        End Function

        ' ---- Cambios de otros usuarios (SD_RespReg y SD_CompReg) ----

        Private Const SqlFoto As String = "SELECT * FROM posarticuloslocal WHERE CArticulo = @codigo AND CLocal = @local"

        Public Async Function FotoAsync(local As Long, codigo As String, Optional ct As CancellationToken = Nothing) As Task(Of Dictionary(Of String, String))
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Await ArticulosRepositorio.FotoDeAsync(cn, SqlFoto, New With {codigo, local}, ct).ConfigureAwait(False)
            End Using
        End Function

        Public Async Function CambiosDeOtrosAsync(local As Long, codigo As String, foto As Dictionary(Of String, String), Optional ct As CancellationToken = Nothing) As Task(Of List(Of CambioArticulo))
            Return ArticulosRepositorio.Comparar(foto, Await FotoAsync(local, codigo, ct).ConfigureAwait(False))
        End Function

        ' ---- Cambios ----

        ''' <summary>
        ''' SD_iGuardarLocal. Un registro nuevo es un REPLACE con el local escrito; uno existente, un UPDATE con
        ''' $Local, que cada destino de la réplica cambia por el suyo. Stock solo se escribe en el REPLACE.
        ''' </summary>
        Friend Shared Function SentenciaGuardar(e As ExistenciaLocal, nuevo As Boolean, local As Long, uid As Long) As String
            If nuevo Then
                Dim fc = If(e.FC.Year < 1900, Date.Now, e.FC)
                Return "REPLACE INTO posarticuloslocal (CLocal, CArticulo, Stock, Min, Max, Recom, FM, FC, Usuario, InvInicial, InvInicialF, LinCredito, Localiz, CosInicial) VALUES (" &
                       String.Join(", ", {Q(local.ToString(Invariante)), Q(e.CArticulo), Q(N(e.Stock)), Q(N(e.Min)), Q(N(e.Max)), Q(N(e.Recom)), "NOW()", Q(F(fc)),
                                          Q(uid.ToString(Invariante)), Q(N(e.InvInicial)), Q(F(e.InvInicialF)), Q(e.LinCredito.ToString(Invariante)), Q(e.Localiz), Q(N(e.CosInicial))}) & ")"
            End If
            Return $"UPDATE posarticuloslocal SET Min = {N(e.Min)}, Max = {N(e.Max)}, Recom = {N(e.Recom)}, FM = NOW(), Usuario = {uid.ToString(Invariante)}, " &
                   $"InvInicial = {N(e.InvInicial)}, InvInicialF = {Q(F(e.InvInicialF))}, LinCredito = {e.LinCredito.ToString(Invariante)}, Localiz = {Q(e.Localiz)} " &
                   $"WHERE CLocal = $Local AND CArticulo = {Q(e.CArticulo)}"
        End Function

        ''' <summary>SD_ActPosInvent: da de alta en posinventario lo que falte del local y le copia el stock de posarticuloslocal.</summary>
        Friend Const SqlAltaInventario As String =
            "INSERT INTO posinventario (CLocal, CArticulo, Stock, Min, Max, Recom, Categ, SCateg, SSCateg, FM, FC, Usuario, InvInicial, InvInicialF, LinCredito, Localiz) " &
            "SELECT @local, A.CodigoArt, 0, 0, 0, 0, -1, -1, -1, NOW(), NOW(), 0, 0, NOW(), 1, 1 FROM posarticulos A " &
            "INNER JOIN posarticuloslocal L ON L.CArticulo = A.CodigoArt AND L.CLocal = @local " &
            "LEFT JOIN posinventario I ON I.CArticulo = A.CodigoArt AND I.CLocal = @local WHERE I.CArticulo IS NULL"

        Friend Const SqlCopiarStock As String =
            "UPDATE posinventario I, posarticuloslocal L SET I.Stock = L.Stock WHERE I.CLocal = L.CLocal AND I.CArticulo = L.CArticulo AND L.CLocal = @local"

        ''' <summary>
        ''' Guarda en el local del usuario, encola la réplica si gatreplicar está en 1 (SD_Replicar) y corre
        ''' SD_ActPosInvent. Sin transacción: las tablas son MyISAM.
        ''' </summary>
        Public Async Function GuardarAsync(e As ExistenciaLocal, nuevo As Boolean, local As Long, uid As Long, Optional ct As CancellationToken = Nothing) As Task
            Dim plantilla = SentenciaGuardar(e, nuevo, local, uid)
            Dim replicar = (Await New Parametros(_fabrica).ObtenerAsync(local, "gatreplicar", ct).ConfigureAwait(False)) = "1"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition(plantilla.Replace("$Local", local.ToString(Invariante)), cancellationToken:=ct)).ConfigureAwait(False)
                If replicar Then Await Replicador.ReplicarAsync(cn, Replica, plantilla, local, ct).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition(SqlAltaInventario, New With {local}, commandTimeout:=300, cancellationToken:=ct)).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition(SqlCopiarStock, New With {local}, commandTimeout:=300, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>SD_iBorrarLocal: quita el artículo del local. VB6 no lo replica.</summary>
        Public Async Function BorrarAsync(local As Long, codigo As String, Optional ct As CancellationToken = Nothing) As Task
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition("DELETE FROM posarticuloslocal WHERE CLocal = @local AND CArticulo = @codigo", New With {local, codigo}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>
        ''' cmdModif_Click: cambia el inventario inicial en el momento, con la fecha de ahora, y lo anota en
        ''' posvitmovimientos como "Act.INV.Inicial". VB6 no lo replica.
        ''' </summary>
        Public Async Function GuardarInventarioInicialAsync(local As Long, codigo As String, anterior As Double, fechaAnterior As Date, nuevo As Double,
                                                            uid As Long, estacion As String, Optional ct As CancellationToken = Nothing) As Task(Of Date)
            Dim ahora = Date.Now
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition("UPDATE posarticuloslocal SET InvInicial = @nuevo, InvInicialF = @ahora WHERE CLocal = @local AND CArticulo = @codigo",
                                                            New With {nuevo, ahora, local, codigo}, cancellationToken:=ct)).ConfigureAwait(False)
                Dim descripcion = $"Articulo {codigo} Actual: {N(anterior)} Nuevo: {N(nuevo)} FechaActual: {fechaAnterior:dd/MM/yyyy} FechaNueva: {ahora:dd/MM/yyyy}"
                Await ArticulosRepositorio.RegistrarAsync(cn, local, uid, "Act.INV.Inicial", descripcion, estacion, ct).ConfigureAwait(False)
            End Using
            Return ahora.Date
        End Function

    End Class

End Namespace
