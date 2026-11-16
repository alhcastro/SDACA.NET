Imports System.Data
Imports System.Threading
Imports Dapper
Imports MySqlConnector
Imports CoopeNet.Core.Datos

Namespace Pos

    ''' <summary>Una conexión remota del local (poslocalescnn): el servidor de otro departamento.</summary>
    Public NotInheritable Class ConexionRemota
        Public Property Nombre As String = ""
        Public Property Servidor As String = ""
        Public Property Puerto As Integer = 3306
        Public Property Catalogo As String = ""
        ''' <summary>Cliente o proveedor de la conexión (gacdp.id).</summary>
        Public Property Cedula As String = ""
        ''' <summary>local2: el departamento al que se conecta.</summary>
        Public Property Departamento As Long
    End Class

    ''' <summary>Un departamento del servidor remoto, para CmbLoc.</summary>
    Public NotInheritable Class DepartamentoRemoto
        Public Property Local As Long
        Public Property Descripcion As String = ""

        Public Overrides Function ToString() As String
            Return $"{Local} - {Descripcion}"
        End Function
    End Class

    ''' <summary>
    ''' Conexiones remotas del POS. Migración de SD0502\FrmConfigurarRemotasPOS.frm (SD_Cargar_Lista_Estaciones,
    ''' SD_Cargar_Estacion, SD_Guardar_Estacion, CmdCat y CmbCat). Existencias por local las usa para consultar el
    ''' inventario de otro departamento.
    ''' </summary>
    ''' <remarks>
    ''' Al servidor remoto se entra con el usuario sdaca, como SD_ConectarServidorDatos2. VB6 no borra conexiones ni
    ''' las replica; aquí tampoco. Si la tabla no existía, VB6 creaba gatlocalescnn (otra tabla) y reintentaba sin fin;
    ''' eso no se migra.
    ''' </remarks>
    Public NotInheritable Class ConexionesRemotasRepositorio

        Public Const LargoNombre As Integer = 32
        Public Const LargoServidor As Integer = 16
        Public Const LargoCatalogo As Integer = 16
        Public Const LargoCedula As Integer = 16
        Public Const PuertoPredeterminado As Integer = 3306
        Public Const SegundosConexion As Integer = 8

        ''' <summary>SD_Validar_Catalogo: un catálogo de SDACA tiene al menos 22 de estas tablas.</summary>
        Public Shared ReadOnly Property TablasDeSdaca As IReadOnlyList(Of String) = {
            "gatbancos", "gatbitacora", "gatcantones", "gatdistritos", "gatestaciones", "gatestadociv",
            "gatimagenes", "gatlocales", "gatmensagedest", "gatmensajes", "gatmodulos", "gatmoduloslocal",
            "gatpais", "gatparametros", "gatparentescos", "gatprivilegios", "gatprovincias", "gatrecursos",
            "gatredondeo", "gatsectores", "gatsysusuarios", "gattiposmomeda", "gatusuarios", "gatusuariospriv2"}
        Public Const MinimoTablas As Integer = 22

        Private ReadOnly _fabrica As IFabricaConexiones
        Private ReadOnly _remota As Func(Of String, Integer, String, String)

        ''' <param name="remota">Arma la cadena del servidor remoto (servidor, puerto, catálogo). Por omisión, con sdaca.</param>
        Public Sub New(fabrica As IFabricaConexiones, Optional remota As Func(Of String, Integer, String, String) = Nothing)
            _fabrica = fabrica
            _remota = If(remota, New Func(Of String, Integer, String, String)(Function(s, p, c) CadenaConexion.Crear(s, c, CUInt(p), "", "")))
        End Sub

        ''' <summary>¿El catálogo tiene las tablas de SDACA? Recibe los nombres de SHOW TABLES.</summary>
        Public Shared Function EsCatalogoDeSdaca(tablas As IEnumerable(Of String)) As Boolean
            Dim conocidas As New HashSet(Of String)(TablasDeSdaca, StringComparer.OrdinalIgnoreCase)
            Return tablas.Where(Function(t) conocidas.Contains(t)).Distinct(StringComparer.OrdinalIgnoreCase).Count() >= MinimoTablas
        End Function

        Public Shared Function Recortar(texto As String, largo As Integer) As String
            Dim t = If(texto, "").Trim()
            Return If(t.Length > largo, t.Substring(0, largo), t)
        End Function

        ''' <summary>SD_Cargar_Lista_Estaciones: los nombres de las conexiones del local.</summary>
        Public Async Function NombresAsync(local As Long, Optional ct As CancellationToken = Nothing) As Task(Of List(Of String))
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return (Await cn.QueryAsync(Of String)(New CommandDefinition(
                    "SELECT Nombre FROM poslocalescnn WHERE local = @local ORDER BY Nombre", New With {local}, cancellationToken:=ct)).ConfigureAwait(False)).ToList()
            End Using
        End Function

        ''' <summary>SD_Cargar_Estacion: Nothing si no está configurada.</summary>
        Public Async Function CargarAsync(local As Long, nombre As String, Optional ct As CancellationToken = Nothing) As Task(Of ConexionRemota)
            Const sql As String = "SELECT Nombre, srv AS Servidor, port AS Puerto, dbname AS Catalogo, Cedula, local2 AS Departamento FROM poslocalescnn WHERE local = @local AND Nombre = @nombre"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Await cn.QueryFirstOrDefaultAsync(Of ConexionRemota)(New CommandDefinition(sql, New With {local, nombre}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>SD_Guardar_Estacion: inserta o actualiza.</summary>
        Public Async Function GuardarAsync(local As Long, c As ConexionRemota, Optional ct As CancellationToken = Nothing) As Task
            Const sql As String =
                "INSERT INTO poslocalescnn (local, Nombre, srv, port, dbname, Cedula, local2) VALUES (@local, @nombre, @servidor, @puerto, @catalogo, @cedula, @departamento) " &
                "ON DUPLICATE KEY UPDATE srv = VALUES(srv), port = VALUES(port), dbname = VALUES(dbname), Cedula = VALUES(Cedula), local2 = VALUES(local2)"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition(sql, New With {
                    local, .nombre = Recortar(c.Nombre, LargoNombre), .servidor = Recortar(c.Servidor, LargoServidor), .puerto = c.Puerto,
                    .catalogo = Recortar(c.Catalogo, LargoCatalogo), .cedula = Recortar(c.Cedula, LargoCedula), .departamento = c.Departamento}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        ''' <summary>El nombre de la persona activa (lblNombre), o vacío.</summary>
        Public Async Function NombrePersonaAsync(id As String, Optional ct As CancellationToken = Nothing) As Task(Of String)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return If(Await cn.ExecuteScalarAsync(Of String)(New CommandDefinition(
                    "SELECT TRIM(CONCAT(nombre, ' ', apellido1, ' ', apellido2)) FROM gacdp WHERE activo = 1 AND fdef = '1900-01-01' AND id = @id",
                    New With {id}, cancellationToken:=ct)).ConfigureAwait(False), "")
            End Using
        End Function

        ''' <summary>frmBuscaPOS de sdtID: las personas activas. Columnas Nombre e ID.</summary>
        Public Async Function PersonasAsync(Optional ct As CancellationToken = Nothing) As Task(Of DataTable)
            Const sql As String = "SELECT TRIM(CONCAT(nombre, ' ', apellido1, ' ', apellido2)) AS Nombre, CAST(id AS CHAR) AS ID FROM gacdp WHERE activo = 1 AND fdef = '1900-01-01' ORDER BY Nombre"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Using lector = Await cn.ExecuteReaderAsync(New CommandDefinition(sql, cancellationToken:=ct)).ConfigureAwait(False)
                    Dim tabla As New DataTable()
                    tabla.Load(lector)
                    Return tabla
                End Using
            End Using
        End Function

        Private Async Function AbrirRemotaAsync(servidor As String, puerto As Integer, catalogo As String, ct As CancellationToken) As Task(Of MySqlConnection)
            Dim b As New MySqlConnectionStringBuilder(_remota(servidor.Trim(), puerto, catalogo)) With {.ConnectionTimeout = SegundosConexion, .Pooling = False}
            Dim cn As New MySqlConnection(b.ConnectionString)
            Try
                Await cn.OpenAsync(ct).ConfigureAwait(False)
            Catch
                cn.Dispose()
                Throw
            End Try
            Return cn
        End Function

        ''' <summary>CmdCat: los catálogos de SDACA del servidor remoto.</summary>
        Public Async Function CatalogosRemotosAsync(servidor As String, puerto As Integer, Optional ct As CancellationToken = Nothing) As Task(Of List(Of String))
            Dim validos As New List(Of String)
            Using cn = Await AbrirRemotaAsync(servidor, puerto, "", ct).ConfigureAwait(False)
                Dim bases = (Await cn.QueryAsync(Of String)(New CommandDefinition("SHOW DATABASES", cancellationToken:=ct)).ConfigureAwait(False)).ToList()
                For Each base In bases
                    Try
                        Dim tablas = Await cn.QueryAsync(Of String)(New CommandDefinition($"SHOW TABLES FROM `{base.Replace("`", "``")}`", cancellationToken:=ct)).ConfigureAwait(False)
                        If EsCatalogoDeSdaca(tablas) Then validos.Add(base)
                    Catch ex As MySqlException
                        ' Un catálogo sin permiso no es de SDACA para este usuario.
                    End Try
                Next
            End Using
            Return validos
        End Function

        ''' <summary>CmbCat_Click: los departamentos del catálogo remoto que tienen módulos.</summary>
        Public Async Function DepartamentosRemotosAsync(servidor As String, puerto As Integer, catalogo As String, Optional ct As CancellationToken = Nothing) As Task(Of List(Of DepartamentoRemoto))
            Const sql As String = "SELECT DISTINCT A.Local, A.Descripcion FROM gatlocales A INNER JOIN gatmoduloslocal B ON A.Local = B.Local ORDER BY A.Local"
            Using cn = Await AbrirRemotaAsync(servidor, puerto, catalogo, ct).ConfigureAwait(False)
                Return (Await cn.QueryAsync(Of DepartamentoRemoto)(New CommandDefinition(sql, cancellationToken:=ct)).ConfigureAwait(False)).ToList()
            End Using
        End Function

    End Class

End Namespace
