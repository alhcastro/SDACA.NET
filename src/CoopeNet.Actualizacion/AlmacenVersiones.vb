Imports System.IO
Imports System.IO.Compression
Imports System.Security.Cryptography
Imports System.Text
Imports MySqlConnector

''' <summary>Una versión publicada. <see cref="Manifiesto"/> y <see cref="Firma"/> solo vienen al pedir una.</summary>
Public NotInheritable Class VersionPublicada
    Public Property Version As String = ""
    Public Property Canal As String = Canales.General
    Public Property Activa As Boolean
    Public Property Publicada As DateTime
    Public Property Publicador As String = ""
    Public Property Notas As String = ""
    Public Property Manifiesto As String = ""
    Public Property Firma As String = ""

    Public ReadOnly Property Numero As System.Version
        Get
            Return Canales.Interpretar(Version)
        End Get
    End Property
End Class

''' <summary>
''' Piloto recibe las versiones antes que el resto. Un equipo en piloto ve las dos; uno en general, solo las
''' liberadas.
''' </summary>
Public NotInheritable Class Canales
    Public Const General As String = "general"
    Public Const Piloto As String = "piloto"

    Private Sub New()
    End Sub

    Public Shared Function Normalizar(canal As String) As String
        Return If(String.Equals(If(canal, "").Trim(), Piloto, StringComparison.OrdinalIgnoreCase), Piloto, General)
    End Function

    Public Shared Function Ve(canalEquipo As String, canalVersion As String) As Boolean
        Return Normalizar(canalEquipo) = Piloto OrElse Normalizar(canalVersion) = General
    End Function

    Public Shared Function Interpretar(texto As String) As Version
        Dim v As Version = Nothing
        If System.Version.TryParse(If(texto, ""), v) Then Return v
        Throw New FormatException($"Versión inválida: ""{texto}"". Use números como 1.0.2.")
    End Function

    ''' <summary>
    ''' La versión que debe tener un equipo: la mayor activa que su canal ve. Si se retira la más nueva, vuelve
    ''' sola a la anterior.
    ''' </summary>
    Public Shared Function Elegir(versiones As IEnumerable(Of VersionPublicada), canalEquipo As String) As VersionPublicada
        Return versiones.Where(Function(v) v.Activa AndAlso Ve(canalEquipo, v.Canal)).OrderByDescending(Function(v) v.Numero).FirstOrDefault()
    End Function
End Class

''' <summary>De dónde baja el lanzador. En las pruebas se reemplaza por uno en memoria.</summary>
Public Interface IOrigenVersiones
    Function VersionesAsync(ct As Threading.CancellationToken) As Task(Of IReadOnlyList(Of VersionPublicada))
    Function ObtenerAsync(version As String, ct As Threading.CancellationToken) As Task(Of VersionPublicada)
    ''' <summary>El contenido descomprimido. Quien lo pide verifica el hash.</summary>
    Function LeerArchivoAsync(hash As String, ct As Threading.CancellationToken) As Task(Of Byte())
End Interface

''' <summary>
''' El almacén de versiones en MySQL, en un catálogo propio (<see cref="Catalogo"/>), aparte de los de SDACA.
''' Cada archivo se guarda una sola vez por su SHA-256, comprimido y en partes de <see cref="TamanoParte"/>
''' para no pasar de max_allowed_packet. Así una versión nueva solo sube y baja lo que cambió.
''' </summary>
Public NotInheritable Class AlmacenVersiones
    Implements IOrigenVersiones

    Public Const Catalogo As String = "coopenet_actualizaciones"
    Public Const TamanoParte As Integer = 4 * 1024 * 1024

    Private ReadOnly _cadena As String

    Public Sub New(servidor As String, puerto As UInteger, Optional esperaSegundos As UInteger = 8UI)
        ' Mismo usuario que SDACA sin contraseña: sdaca con MD5("sdaca") (CadenaConexion en CoopeNet.Core).
        Dim clave = Convert.ToHexString(MD5.HashData(Encoding.ASCII.GetBytes("sdaca"))).ToLowerInvariant()
        _cadena = New MySqlConnectionStringBuilder With {
            .Server = servidor, .Port = puerto, .UserID = "sdaca", .Password = clave, .Database = Catalogo,
            .CharacterSet = "utf8mb4", .ConnectionTimeout = esperaSegundos, .DefaultCommandTimeout = 300, .Pooling = False
        }.ConnectionString
    End Sub

    Private Async Function AbrirAsync(ct As Threading.CancellationToken, Optional sinCatalogo As Boolean = False) As Task(Of MySqlConnection)
        Dim cadena = _cadena
        If sinCatalogo Then cadena = New MySqlConnectionStringBuilder(_cadena) With {.Database = ""}.ConnectionString
        Dim cn As New MySqlConnection(cadena)
        Try
            Await cn.OpenAsync(ct).ConfigureAwait(False)
        Catch
            cn.Dispose()
            Throw
        End Try
        Return cn
    End Function

    Private Shared Function Comando(cn As MySqlConnection, sql As String, ParamArray parametros As (Nombre As String, Valor As Object)()) As MySqlCommand
        Dim cmd As New MySqlCommand(sql, cn)
        For Each p In parametros
            cmd.Parameters.AddWithValue(p.Nombre, If(p.Valor, DBNull.Value))
        Next
        Return cmd
    End Function

    ' ---- Esquema ----

    ''' <summary>Crea el catálogo y las tablas si faltan. No borra ni cambia nada existente.</summary>
    Public Async Function CrearEsquemaAsync(ct As Threading.CancellationToken) As Task
        Using cn = Await AbrirAsync(ct, sinCatalogo:=True).ConfigureAwait(False)
            Dim sentencias = {
                $"CREATE DATABASE IF NOT EXISTS `{Catalogo}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci",
                $"CREATE TABLE IF NOT EXISTS `{Catalogo}`.versiones (
                    version VARCHAR(20) NOT NULL PRIMARY KEY,
                    canal VARCHAR(10) NOT NULL,
                    activa TINYINT NOT NULL DEFAULT 1,
                    publicada DATETIME NOT NULL,
                    publicador VARCHAR(64) NOT NULL,
                    notas TEXT NULL,
                    manifiesto MEDIUMTEXT NOT NULL,
                    firma VARCHAR(255) NOT NULL
                  ) ENGINE=InnoDB",
                $"CREATE TABLE IF NOT EXISTS `{Catalogo}`.archivos (
                    hash CHAR(64) NOT NULL PRIMARY KEY,
                    tamano BIGINT NOT NULL,
                    comprimido BIGINT NOT NULL,
                    partes INT NOT NULL,
                    subido DATETIME NOT NULL
                  ) ENGINE=InnoDB",
                $"CREATE TABLE IF NOT EXISTS `{Catalogo}`.partes (
                    hash CHAR(64) NOT NULL,
                    numero INT NOT NULL,
                    datos MEDIUMBLOB NOT NULL,
                    PRIMARY KEY (hash, numero)
                  ) ENGINE=InnoDB",
                $"CREATE TABLE IF NOT EXISTS `{Catalogo}`.equipos (
                    equipo VARCHAR(64) NOT NULL PRIMARY KEY,
                    usuario VARCHAR(64) NOT NULL,
                    canal VARCHAR(10) NOT NULL,
                    version VARCHAR(20) NOT NULL,
                    resultado VARCHAR(255) NOT NULL,
                    fecha DATETIME NOT NULL
                  ) ENGINE=InnoDB"
            }
            For Each sql In sentencias
                Using cmd = Comando(cn, sql)
                    Await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(False)
                End Using
            Next
        End Using
    End Function

    ' ---- Lectura (lanzador) ----

    Public Async Function VersionesAsync(ct As Threading.CancellationToken) As Task(Of IReadOnlyList(Of VersionPublicada)) Implements IOrigenVersiones.VersionesAsync
        Dim lista As New List(Of VersionPublicada)()
        Using cn = Await AbrirAsync(ct).ConfigureAwait(False)
            Using cmd = Comando(cn, "SELECT version, canal, activa, publicada, publicador, IFNULL(notas, '') FROM versiones")
                Using r = Await cmd.ExecuteReaderAsync(ct).ConfigureAwait(False)
                    While Await r.ReadAsync(ct).ConfigureAwait(False)
                        lista.Add(New VersionPublicada With {
                            .Version = r.GetString(0), .Canal = Canales.Normalizar(r.GetString(1)), .Activa = r.GetInt32(2) <> 0,
                            .Publicada = r.GetDateTime(3), .Publicador = r.GetString(4), .Notas = r.GetString(5)
                        })
                    End While
                End Using
            End Using
        End Using
        Return lista.OrderByDescending(Function(v) v.Numero).ToList()
    End Function

    Public Async Function ObtenerAsync(version As String, ct As Threading.CancellationToken) As Task(Of VersionPublicada) Implements IOrigenVersiones.ObtenerAsync
        Using cn = Await AbrirAsync(ct).ConfigureAwait(False)
            Using cmd = Comando(cn, "SELECT version, canal, activa, publicada, publicador, IFNULL(notas, ''), manifiesto, firma FROM versiones WHERE version = @v", ("@v", version))
                Using r = Await cmd.ExecuteReaderAsync(ct).ConfigureAwait(False)
                    If Not Await r.ReadAsync(ct).ConfigureAwait(False) Then Return Nothing
                    Return New VersionPublicada With {
                        .Version = r.GetString(0), .Canal = Canales.Normalizar(r.GetString(1)), .Activa = r.GetInt32(2) <> 0,
                        .Publicada = r.GetDateTime(3), .Publicador = r.GetString(4), .Notas = r.GetString(5),
                        .Manifiesto = r.GetString(6), .Firma = r.GetString(7)
                    }
                End Using
            End Using
        End Using
    End Function

    Public Async Function LeerArchivoAsync(hash As String, ct As Threading.CancellationToken) As Task(Of Byte()) Implements IOrigenVersiones.LeerArchivoAsync
        Using comprimido As New MemoryStream()
            Using cn = Await AbrirAsync(ct).ConfigureAwait(False)
                Dim esperadas As Integer
                Using cmd = Comando(cn, "SELECT partes FROM archivos WHERE hash = @h", ("@h", hash))
                    Dim valor = Await cmd.ExecuteScalarAsync(ct).ConfigureAwait(False)
                    If valor Is Nothing OrElse valor Is DBNull.Value Then Throw New FileNotFoundException($"El archivo {hash} no está en el almacén.")
                    esperadas = Convert.ToInt32(valor)
                End Using
                Dim leidas = 0
                Using cmd = Comando(cn, "SELECT datos FROM partes WHERE hash = @h ORDER BY numero", ("@h", hash))
                    Using r = Await cmd.ExecuteReaderAsync(ct).ConfigureAwait(False)
                        While Await r.ReadAsync(ct).ConfigureAwait(False)
                            Dim datos = DirectCast(r.GetValue(0), Byte())
                            Await comprimido.WriteAsync(datos, ct).ConfigureAwait(False)
                            leidas += 1
                        End While
                    End Using
                End Using
                If leidas <> esperadas Then Throw New InvalidDataException($"El archivo {hash} está incompleto en el almacén ({leidas} de {esperadas} partes).")
            End Using
            comprimido.Position = 0
            Using gz As New GZipStream(comprimido, CompressionMode.Decompress), salida As New MemoryStream()
                Await gz.CopyToAsync(salida, ct).ConfigureAwait(False)
                Return salida.ToArray()
            End Using
        End Using
    End Function

    ''' <summary>Deja constancia de qué versión tiene cada equipo. Un fallo aquí no detiene nada.</summary>
    Public Async Function ReportarAsync(equipo As String, usuario As String, canal As String, version As String, resultado As String, ct As Threading.CancellationToken) As Task
        Using cn = Await AbrirAsync(ct).ConfigureAwait(False)
            Using cmd = Comando(cn,
                "INSERT INTO equipos (equipo, usuario, canal, version, resultado, fecha) VALUES (@e, @u, @c, @v, @r, NOW())
                 ON DUPLICATE KEY UPDATE usuario = VALUES(usuario), canal = VALUES(canal), version = VALUES(version), resultado = VALUES(resultado), fecha = VALUES(fecha)",
                ("@e", Recortar(equipo, 64)), ("@u", Recortar(usuario, 64)), ("@c", Canales.Normalizar(canal)), ("@v", Recortar(version, 20)), ("@r", Recortar(resultado, 255)))
                Await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(False)
            End Using
        End Using
    End Function

    Private Shared Function Recortar(texto As String, largo As Integer) As String
        texto = If(texto, "")
        Return If(texto.Length > largo, texto.Substring(0, largo), texto)
    End Function

    ' ---- Escritura (publicación) ----

    Public Async Function TieneArchivoAsync(hash As String, ct As Threading.CancellationToken) As Task(Of Boolean)
        Using cn = Await AbrirAsync(ct).ConfigureAwait(False)
            Using cmd = Comando(cn, "SELECT COUNT(*) FROM archivos WHERE hash = @h", ("@h", hash))
                Return Convert.ToInt64(Await cmd.ExecuteScalarAsync(ct).ConfigureAwait(False)) > 0
            End Using
        End Using
    End Function

    ''' <summary>
    ''' Sube un archivo si no está. Las partes y la fila de archivos van en una transacción: la fila es la marca
    ''' de que el archivo está completo. Devuelve los bytes comprimidos subidos (0 si ya estaba).
    ''' </summary>
    Public Async Function SubirArchivoAsync(ruta As String, hash As String, ct As Threading.CancellationToken) As Task(Of Long)
        If Await TieneArchivoAsync(hash, ct).ConfigureAwait(False) Then Return 0
        Dim contenido = Await File.ReadAllBytesAsync(ruta, ct).ConfigureAwait(False)
        If Huella.DeBytes(contenido) <> hash Then Throw New InvalidDataException($"{ruta} cambió mientras se publicaba.")
        Dim comprimido As Byte()
        Using salida As New MemoryStream()
            Using gz As New GZipStream(salida, CompressionLevel.SmallestSize, leaveOpen:=True)
                gz.Write(contenido, 0, contenido.Length)
            End Using
            comprimido = salida.ToArray()
        End Using
        Dim partes = Math.Max(1, CInt(Math.Ceiling(comprimido.Length / CDbl(TamanoParte))))

        Using cn = Await AbrirAsync(ct).ConfigureAwait(False)
            Using tx = Await cn.BeginTransactionAsync(ct).ConfigureAwait(False)
                ' Restos de una subida cortada antes del COMMIT no deberían existir (InnoDB), pero no estorban.
                Using cmd = Comando(cn, "DELETE FROM partes WHERE hash = @h", ("@h", hash))
                    cmd.Transaction = tx
                    Await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(False)
                End Using
                For i = 0 To partes - 1
                    Dim inicio = i * TamanoParte
                    Dim trozo = comprimido.AsSpan(inicio, Math.Min(TamanoParte, comprimido.Length - inicio)).ToArray()
                    Using cmd = Comando(cn, "INSERT INTO partes (hash, numero, datos) VALUES (@h, @n, @d)", ("@h", hash), ("@n", i), ("@d", trozo))
                        cmd.Transaction = tx
                        Await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(False)
                    End Using
                Next
                Using cmd = Comando(cn, "INSERT INTO archivos (hash, tamano, comprimido, partes, subido) VALUES (@h, @t, @c, @p, NOW())",
                                    ("@h", hash), ("@t", contenido.LongLength), ("@c", comprimido.LongLength), ("@p", partes))
                    cmd.Transaction = tx
                    Await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(False)
                End Using
                Await tx.CommitAsync(ct).ConfigureAwait(False)
            End Using
        End Using
        Return comprimido.LongLength
    End Function

    Public Async Function RegistrarVersionAsync(manifiestoTexto As String, firmaBase64 As String, version As String, canal As String,
                                                publicador As String, notas As String, ct As Threading.CancellationToken) As Task
        Using cn = Await AbrirAsync(ct).ConfigureAwait(False)
            Using cmd = Comando(cn,
                "INSERT INTO versiones (version, canal, activa, publicada, publicador, notas, manifiesto, firma)
                 VALUES (@v, @c, 1, NOW(), @p, @n, @m, @f)",
                ("@v", version), ("@c", Canales.Normalizar(canal)), ("@p", Recortar(publicador, 64)), ("@n", notas), ("@m", manifiestoTexto), ("@f", firmaBase64))
                Await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(False)
            End Using
        End Using
    End Function

    ''' <summary>Cambia canal o estado de una versión. Devuelve False si no existe.</summary>
    Public Async Function CambiarAsync(version As String, canal As String, activa As Boolean?, ct As Threading.CancellationToken) As Task(Of Boolean)
        Using cn = Await AbrirAsync(ct).ConfigureAwait(False)
            Using cmd = Comando(cn,
                "UPDATE versiones SET canal = IFNULL(@c, canal), activa = IFNULL(@a, activa) WHERE version = @v",
                ("@v", version), ("@c", If(canal Is Nothing, Nothing, Canales.Normalizar(canal))), ("@a", If(activa.HasValue, CObj(If(activa.Value, 1, 0)), Nothing)))
                Return Await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(False) > 0
            End Using
        End Using
    End Function

    Public Async Function EquiposAsync(ct As Threading.CancellationToken) As Task(Of IReadOnlyList(Of String()))
        Dim lista As New List(Of String())()
        Using cn = Await AbrirAsync(ct).ConfigureAwait(False)
            Using cmd = Comando(cn, "SELECT equipo, usuario, canal, version, resultado, DATE_FORMAT(fecha, '%Y-%m-%d %H:%i') FROM equipos ORDER BY equipo")
                Using r = Await cmd.ExecuteReaderAsync(ct).ConfigureAwait(False)
                    While Await r.ReadAsync(ct).ConfigureAwait(False)
                        lista.Add(Enumerable.Range(0, 6).Select(Function(i) r.GetString(i)).ToArray())
                    End While
                End Using
            End Using
        End Using
        Return lista
    End Function

End Class
