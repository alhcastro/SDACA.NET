Imports System.Data
Imports System.Globalization
Imports System.Threading
Imports Dapper
Imports MySqlConnector
Imports SDACA.Core.Bancos
Imports SDACA.Core.Datos

Namespace Catalogos

    ''' <summary>Fila de <c>gacdp</c>. Tipo y Genero son texto, como en la tabla: hay géneros vacíos.</summary>
    Public NotInheritable Class Persona
        Public Const TipoJuridica As String = "1"
        Public Shared ReadOnly SinFecha As Date = #1900-01-01#

        Public Property Id As String = ""
        Public Property Tipo As String = "0"
        Public Property Nombre As String = ""
        Public Property Apellido1 As String = ""
        Public Property Apellido2 As String = ""
        Public Property [Alias] As String = ""
        Public Property Genero As String = "0"
        Public Property FNac As Date = SinFecha
        Public Property Prov As String = ""
        Public Property Cant As String = ""
        Public Property Dist As String = ""
        Public Property Caserio As String = ""
        Public Property Dir As String = ""
        Public Property Puesto As String = ""
        Public Property Cia As String = ""
        Public Property Pres As String = ""
        Public Property Nac As String = ""
        Public Property EstCiv As String = ""
        Public Property Categoria As String = "300"
        Public Property Activo As String = "1"
        Public Property FDef As Date = SinFecha

        Public ReadOnly Property EsJuridica As Boolean
            Get
                Return Tipo = TipoJuridica
            End Get
        End Property

        Public ReadOnly Property Fallecido As Boolean
            Get
                Return FDef > SinFecha
            End Get
        End Property

        Public ReadOnly Property NombreCompleto As String
            Get
                Return $"{Apellido1} {Apellido2} {Nombre}".Trim().ToUpperInvariant()
            End Get
        End Property
    End Class

    ''' <summary>Fila de <c>gactiposid</c>.</summary>
    Public NotInheritable Class TipoIdentificacion
        Public Property Tipo As Integer
        Public Property Detalle As String = ""
        Public Property Formato As String = ""
        Public Property Minimo As Integer
        Public Property Maximo As Integer

        ''' <summary>Solo los formatos hechos de # exigen dígitos; &amp; admite cualquier caracter.</summary>
        Public ReadOnly Property SoloDigitos As Boolean
            Get
                Return Formato.Length > 0 AndAlso Formato.All(Function(c) c = "#"c)
            End Get
        End Property

        Public Overrides Function ToString() As String
            Return Detalle
        End Function
    End Class

    ''' <summary>Código y descripción de los catálogos de países, estados civiles y ubicación.</summary>
    Public NotInheritable Class ItemCatalogo
        Public Property Codigo As String = ""
        Public Property Descripcion As String = ""

        Public Overrides Function ToString() As String
            Return Descripcion
        End Function
    End Class

    ''' <summary>Fila de <c>gacdpteltipos</c>.</summary>
    Public NotInheritable Class TipoContacto
        Public Property Tipo As Integer
        Public Property Descripcion As String = ""
    End Class

    ''' <summary>Fila de <c>gacdptel</c>.</summary>
    Public NotInheritable Class ContactoPersona
        Public Property Tipo As Integer
        Public Property Dato As String = ""
    End Class

    ''' <summary>Fila de <c>gacdprepresentante2</c> con el nombre de la persona.</summary>
    Public NotInheritable Class RepresentanteLegal
        Public Property IdRep As String = ""
        Public Property Nombre As String = ""
        Public Property Personeria As String = ""
        Public Property Fecha As Date = Persona.SinFecha
        Public Property Borrar As Boolean
    End Class

    ''' <summary>Código de la pestaña Códigos. Solo los de <c>gaccodclientes</c> se pueden eliminar aquí.</summary>
    Public NotInheritable Class CodigoPersona
        Public Property Codigo As String = ""
        Public Property Tipo As String = ""
        Public Property Eliminable As Boolean
    End Class

    ''' <summary>Fila de la lista de personas.</summary>
    Public NotInheritable Class PersonaLista
        Public Property Id As String = ""
        Public Property Nombre As String = ""

        Public Overrides Function ToString() As String
            Return Nombre
        End Function
    End Class

    ''' <summary>Parte de <c>gatimagenes</c> que usa Datos Personales. Mismos valores que Enum_SD_Tipo_Imagenes.</summary>
    Public Enum TipoImagen
        Identificacion = 0
        Fotografia = 1
        Firma = 2
    End Enum

    ''' <summary>
    ''' Datos Personales. Migración de SD0001\FrmDatosPersonales.frm, FrmAgregarTiposCodigos.frm,
    ''' FrmImgsCliente.frm y ActiveX\NumLoc2\NumLoc.ctl, con el esquema real. gacdp y
    ''' gacdprepresentante2 se replican como DP y gaccodclientes como posclientes. gacdptel y
    ''' gatimagenes no se replican, igual que en VB6.
    ''' </summary>
    Public NotInheritable Class DatosPersonalesRepositorio

        Private Const ReplicaPersonas As String = "DP"
        Private Const ReplicaCodigos As String = "posclientes"
        Private Const EtiquetaAsociado As String = "gac-caption"
        Private Const FiltroMinimo As Integer = 3
        Private Const FilasLista As Integer = 24
        Private Const CamposPersona As String =
            "id AS Id, tipo AS Tipo, nombre AS Nombre, apellido1 AS Apellido1, apellido2 AS Apellido2, alias AS `Alias`, " &
            "genero AS Genero, fnac AS FNac, prov AS Prov, cant AS Cant, dist AS Dist, caserio AS Caserio, dir AS Dir, " &
            "puesto AS Puesto, cia AS Cia, pres AS Pres, nac AS Nac, estciv AS EstCiv, categoria AS Categoria, activo AS Activo, fdef AS FDef"
        Private Const NombreSql As String = "UCASE(TRIM(CONCAT(apellido1, ' ', apellido2, ' ', nombre)))"

        Private ReadOnly _fabrica As IFabricaConexiones

        Public Sub New(fabrica As IFabricaConexiones)
            _fabrica = fabrica
        End Sub

        Private Async Function ListaAsync(Of T)(sql As String, parametros As Object, ct As CancellationToken) As Task(Of List(Of T))
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return (Await cn.QueryAsync(Of T)(New CommandDefinition(sql, parametros, cancellationToken:=ct)).ConfigureAwait(False)).ToList()
            End Using
        End Function

        Private Async Function EscalarAsync(Of T)(sql As String, parametros As Object, ct As CancellationToken) As Task(Of T)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Return Await cn.ExecuteScalarAsync(Of T)(New CommandDefinition(sql, parametros, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        Private Async Function TablaAsync(sql As String, parametros As Object, ct As CancellationToken) As Task(Of DataTable)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Using lector = Await cn.ExecuteReaderAsync(New CommandDefinition(sql, parametros, cancellationToken:=ct)).ConfigureAwait(False)
                    Dim tabla As New DataTable()
                    tabla.Load(lector)
                    Return tabla
                End Using
            End Using
        End Function

        Private Shared Async Function EjecutarAsync(cn As MySqlConnection, sql As String, ct As CancellationToken) As Task
            Await cn.ExecuteAsync(New CommandDefinition(sql, cancellationToken:=ct)).ConfigureAwait(False)
        End Function

        Private Shared Async Function EjecutarReplicadoAsync(cn As MySqlConnection, identificador As String, sql As String, local As Long, ct As CancellationToken) As Task
            Await EjecutarAsync(cn, sql, ct).ConfigureAwait(False)
            Await Replicador.ReplicarAsync(cn, identificador, sql, local, ct).ConfigureAwait(False)
        End Function

        ' ---- Catálogos ----

        Public Function TiposIdentificacionAsync(Optional ct As CancellationToken = Nothing) As Task(Of List(Of TipoIdentificacion))
            Return ListaAsync(Of TipoIdentificacion)("SELECT tipo AS Tipo, detalle AS Detalle, formato AS Formato, tammin AS Minimo, tammax AS Maximo FROM gactiposid WHERE estado = '1' ORDER BY tipo", Nothing, ct)
        End Function

        Public Function PaisesAsync(Optional ct As CancellationToken = Nothing) As Task(Of List(Of ItemCatalogo))
            Return ListaAsync(Of ItemCatalogo)("SELECT Codigo, Pais AS Descripcion FROM gatpais ORDER BY Pais", Nothing, ct)
        End Function

        Public Function EstadosCivilesAsync(Optional ct As CancellationToken = Nothing) As Task(Of List(Of ItemCatalogo))
            Return ListaAsync(Of ItemCatalogo)("SELECT Codigo, Descripcion FROM gatestadociv ORDER BY Descripcion", Nothing, ct)
        End Function

        Public Function TiposContactoAsync(Optional ct As CancellationToken = Nothing) As Task(Of List(Of TipoContacto))
            Return ListaAsync(Of TipoContacto)("SELECT tipo AS Tipo, descripcion AS Descripcion FROM gacdpteltipos ORDER BY tipo", Nothing, ct)
        End Function

        Public Function ProvinciasAsync(Optional ct As CancellationToken = Nothing) As Task(Of List(Of ItemCatalogo))
            Return ListaAsync(Of ItemCatalogo)("SELECT Codigo, Descripcion FROM gatprovincias ORDER BY Codigo", Nothing, ct)
        End Function

        Public Function CantonesAsync(provincia As String, Optional ct As CancellationToken = Nothing) As Task(Of List(Of ItemCatalogo))
            Return ListaAsync(Of ItemCatalogo)("SELECT Codigo, Descripcion FROM gatcantones WHERE Provincia = @provincia ORDER BY Codigo", New With {provincia}, ct)
        End Function

        Public Function DistritosAsync(provincia As String, canton As String, Optional ct As CancellationToken = Nothing) As Task(Of List(Of ItemCatalogo))
            Return ListaAsync(Of ItemCatalogo)("SELECT Codigo, Descripcion FROM gatdistritos WHERE Provincia = @provincia AND Canton = @canton ORDER BY Codigo", New With {provincia, canton}, ct)
        End Function

        Public Function SectoresAsync(provincia As String, canton As String, distrito As String, Optional ct As CancellationToken = Nothing) As Task(Of List(Of ItemCatalogo))
            Return ListaAsync(Of ItemCatalogo)("SELECT Codigo, Descripcion FROM gatsectores WHERE Provincia = @provincia AND Canton = @canton AND Distrito = @distrito ORDER BY Codigo", New With {provincia, canton, distrito}, ct)
        End Function

        Public Function TiposCodigoAsync(Optional ct As CancellationToken = Nothing) As Task(Of List(Of ItemCatalogo))
            Return ListaAsync(Of ItemCatalogo)("SELECT TIPO AS Codigo, DESCRIPCION AS Descripcion FROM gactiposcodigos WHERE ACTIVO = '1' ORDER BY TIPO", Nothing, ct)
        End Function

        ''' <summary>Provincia, cantón, distrito y sector que se proponen a una persona nueva.</summary>
        Public Async Function UbicacionPredeterminadaAsync(local As Long, Optional ct As CancellationToken = Nothing) As Task(Of String())
            Dim parametros As New Parametros(_fabrica)
            Return {
                Await parametros.ObtenerAsync(local, "gCod_Provincia", ct).ConfigureAwait(False),
                Await parametros.ObtenerAsync(local, "gCod_Canton", ct).ConfigureAwait(False),
                Await parametros.ObtenerAsync(local, "gCod_Distrito", ct).ConfigureAwait(False),
                Await parametros.ObtenerAsync(local, "gCod_Sector", ct).ConfigureAwait(False)
            }
        End Function

        Public Async Function GuardarUbicacionPredeterminadaAsync(provincia As String, canton As String, distrito As String, sector As String, uid As Long, local As Long, Optional ct As CancellationToken = Nothing) As Task
            Dim parametros As New Parametros(_fabrica)
            Await parametros.GuardarAsync(local, "gCod_Provincia", provincia, uid, ct).ConfigureAwait(False)
            Await parametros.GuardarAsync(local, "gCod_Canton", canton, uid, ct).ConfigureAwait(False)
            Await parametros.GuardarAsync(local, "gCod_Distrito", distrito, uid, ct).ConfigureAwait(False)
            Await parametros.GuardarAsync(local, "gCod_Sector", sector, uid, ct).ConfigureAwait(False)
        End Function

        ''' <summary>Texto con que se marcan los códigos de asociado, que no se pueden cambiar aquí.</summary>
        Public Async Function EtiquetaAsociadoAsync(local As Long, Optional ct As CancellationToken = Nothing) As Task(Of String)
            Dim valor = Await New Parametros(_fabrica).ObtenerAsync(local, EtiquetaAsociado, ct).ConfigureAwait(False)
            Return If(valor.Length = 0, "Cliente", valor)
        End Function

        ' ---- Consultas de personas ----

        ''' <summary>Los espacios del filtro valen por cualquier texto. Con menos de 3 caracteres no se filtra.</summary>
        Friend Shared Function PatronFiltro(filtro As String) As String
            Dim texto = If(filtro, "").Trim().Replace(" ", "%")
            Return If(texto.Length < FiltroMinimo, "", $"%{texto}%")
        End Function

        ''' <summary>Las primeras 24 personas que coinciden y el total, como Load_50_DP.</summary>
        Public Async Function ListarAsync(filtro As String, Optional ct As CancellationToken = Nothing) As Task(Of (Filas As List(Of PersonaLista), Total As Long))
            Dim patron = PatronFiltro(filtro)
            Dim condicion = If(patron.Length = 0, "", " WHERE id LIKE @patron OR CONCAT(apellido1, ' ', apellido2, ' ', nombre) LIKE @patron")
            Dim sql = "SELECT id AS Id, UCASE(TRIM(CONCAT(apellido1, ' ', apellido2, ' ', nombre, ', ', id))) AS Nombre FROM gacdp" &
                      condicion & $" ORDER BY apellido1, apellido2, nombre LIMIT {FilasLista}"
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim filas = (Await cn.QueryAsync(Of PersonaLista)(New CommandDefinition(sql, New With {patron}, cancellationToken:=ct)).ConfigureAwait(False)).ToList()
                Dim total = Await cn.ExecuteScalarAsync(Of Long)(New CommandDefinition("SELECT COUNT(id) FROM gacdp" & condicion, New With {patron}, cancellationToken:=ct)).ConfigureAwait(False)
                Return (filas, total)
            End Using
        End Function

        Public Function BuscarAsync(Optional ct As CancellationToken = Nothing) As Task(Of DataTable)
            Return TablaAsync($"SELECT {NombreSql} AS 'Nombre', id AS 'Identificación', IF(fnac < '1900-01-01', '', DATE_FORMAT(fnac, '%d/%m/%Y')) AS 'Fecha Nacimiento', alias AS 'Alias' FROM gacdp", Nothing, ct)
        End Function

        ''' <summary>Personas que pueden ser representante legal: mayores de 15 años, vivas y no jurídicas.</summary>
        Public Function BuscarRepresentantesAsync(Optional ct As CancellationToken = Nothing) As Task(Of DataTable)
            Return TablaAsync("SELECT UCASE(TRIM(CONCAT(D.apellido1, ' ', D.apellido2, ' ', D.nombre))) AS 'Nombre', D.id AS 'Identificación', T.detalle AS 'Tipo' " &
                              "FROM gacdp D LEFT JOIN gactiposid T ON T.tipo = D.tipo " &
                              "WHERE D.tipo != '1' AND D.fnac <= DATE_SUB(CURDATE(), INTERVAL 15 YEAR) AND D.fdef = '1900-01-01'", Nothing, ct)
        End Function

        Public Async Function ExisteAsync(id As String, Optional ct As CancellationToken = Nothing) As Task(Of Boolean)
            Return Await EscalarAsync(Of Long)("SELECT COUNT(id) FROM gacdp WHERE id = @id", New With {id}, ct).ConfigureAwait(False) > 0
        End Function

        ''' <summary>Las fechas en cero llegan como fecha mínima. Se muestran como 1900-01-01, igual que las vacías en VB6.</summary>
        Friend Shared Sub NormalizarFechas(p As Persona)
            If p.FNac < Persona.SinFecha Then p.FNac = Persona.SinFecha
            If p.FDef < Persona.SinFecha Then p.FDef = Persona.SinFecha
        End Sub

        Public Async Function ObtenerAsync(id As String, Optional ct As CancellationToken = Nothing) As Task(Of Persona)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim p = Await cn.QueryFirstOrDefaultAsync(Of Persona)(New CommandDefinition($"SELECT {CamposPersona} FROM gacdp WHERE id = @id", New With {id}, cancellationToken:=ct)).ConfigureAwait(False)
                If p IsNot Nothing Then NormalizarFechas(p)
                Return p
            End Using
        End Function

        Public Function NombreAsync(id As String, Optional ct As CancellationToken = Nothing) As Task(Of String)
            Return EscalarAsync(Of String)($"SELECT {NombreSql} FROM gacdp WHERE id = @id", New With {id}, ct)
        End Function

        Public Function ContactosAsync(id As String, Optional ct As CancellationToken = Nothing) As Task(Of List(Of ContactoPersona))
            Return ListaAsync(Of ContactoPersona)("SELECT tipo AS Tipo, dato AS Dato FROM gacdptel WHERE id = @id ORDER BY pos", New With {id}, ct)
        End Function

        Public Function RepresentantesAsync(id As String, Optional ct As CancellationToken = Nothing) As Task(Of List(Of RepresentanteLegal))
            Return ListaAsync(Of RepresentanteLegal)(
                "SELECT R.idrep AS IdRep, TRIM(IFNULL(UCASE(CONCAT(D.apellido1, ' ', D.apellido2, ' ', D.nombre)), 'NO ENCONTRADO')) AS Nombre, " &
                "R.numpersoneria AS Personeria, R.fechapersoneria AS Fecha " &
                "FROM gacdprepresentante2 R LEFT JOIN gacdp D ON D.id = R.idrep WHERE R.id = @id ORDER BY R.orden", New With {id}, ct)
        End Function

        ''' <summary>Códigos de asociado y de empleado, solo informativos, y los de gaccodclientes.</summary>
        Public Async Function CodigosAsync(id As String, etiquetaAsociado As String, Optional ct As CancellationToken = Nothing) As Task(Of List(Of CodigoPersona))
            Dim codigos As New List(Of CodigoPersona)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                For Each codigo In Await cn.QueryAsync(Of String)(New CommandDefinition("SELECT CODIGO FROM gacasociados WHERE id = @id AND ACTIVO = '1'", New With {id}, cancellationToken:=ct)).ConfigureAwait(False)
                    codigos.Add(New CodigoPersona With {.Codigo = codigo, .Tipo = etiquetaAsociado})
                Next
                For Each codigo In Await cn.QueryAsync(Of String)(New CommandDefinition("SELECT CodEmpleado FROM arhempleados WHERE id = @id AND Estado = '1'", New With {id}, cancellationToken:=ct)).ConfigureAwait(False)
                    codigos.Add(New CodigoPersona With {.Codigo = codigo, .Tipo = "CODIGO DE EMPLEADO"})
                Next
                Const internos As String = "SELECT C.CODIGO AS Codigo, IFNULL(UCASE(T.DESCRIPCION), 'CODIGO INTERNO') AS Tipo FROM gaccodclientes C " &
                                           "LEFT JOIN gactiposcodigos T ON T.TIPO = LEFT(C.CODIGO, 3) WHERE C.ID = @id"
                For Each codigo In Await cn.QueryAsync(Of CodigoPersona)(New CommandDefinition(internos, New With {id}, cancellationToken:=ct)).ConfigureAwait(False)
                    codigo.Eliminable = True
                    codigos.Add(codigo)
                Next
            End Using
            Return codigos
        End Function

        ''' <summary>
        ''' La persona del padrón con esa cédula, o Nothing. Como en VB6, no tener el catálogo padron
        ''' no es un error: el padrón es opcional.
        ''' </summary>
        Public Async Function BuscarEnPadronAsync(cedula As String, Optional ct As CancellationToken = Nothing) As Task(Of Persona)
            Const sql As String = "SELECT Cedula AS Id, Nombre, Apellido1, Apellido2, IF(Sexo = 'M', '0', '1') AS Genero, Nacimiento AS FNac FROM padron.padron WHERE Cedula = @cedula"
            Try
                Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                    Dim p = Await cn.QueryFirstOrDefaultAsync(Of Persona)(New CommandDefinition(sql, New With {cedula}, cancellationToken:=ct)).ConfigureAwait(False)
                    If p IsNot Nothing Then NormalizarFechas(p)
                    Return p
                End Using
            Catch ex As MySqlException When ex.ErrorCode = MySqlErrorCode.NoSuchTable OrElse ex.ErrorCode = MySqlErrorCode.UnknownDatabase OrElse ex.ErrorCode = MySqlErrorCode.DatabaseAccessDenied OrElse ex.ErrorCode = MySqlErrorCode.TableAccessDenied
                Return Nothing
            End Try
        End Function

        ' ---- Validación ----

        ''' <summary>El aviso de SD_Cumple_Requisitos_TipoID, o "" si el tamaño es correcto.</summary>
        Public Shared Function ValidarIdentificacion(id As String, tipo As TipoIdentificacion) As String
            If tipo Is Nothing Then Return "La identificación proporcionada no es válida."
            If id.Length >= tipo.Minimo AndAlso id.Length <= tipo.Maximo Then Return ""
            Return $"La identificación de tipo {tipo.Detalle} ({tipo.Formato}) debe tener un mínimo de {tipo.Minimo} dígitos y un máximo de {tipo.Maximo}. " &
                   $"La identificación indicada '{id}' tiene {id.Length}."
        End Function

        ' ---- Guardar ----

        ''' <summary>Una persona jurídica no lleva apellidos y su género es 2. La dirección va en una línea.</summary>
        Friend Shared Sub Normalizar(p As Persona)
            p.Nombre = p.Nombre.Trim()
            p.Apellido1 = If(p.EsJuridica, "", p.Apellido1.Trim())
            p.Apellido2 = If(p.EsJuridica, "", p.Apellido2.Trim())
            p.Alias = p.Alias.Trim()
            If p.EsJuridica Then p.Genero = "2"
            p.Dir = p.Dir.Replace(vbCrLf, "").Trim()
            p.Puesto = p.Puesto.Trim()
            p.Cia = p.Cia.Trim()
            p.Categoria = p.Categoria.Trim()
        End Sub

        ''' <summary>Texto entre comillas y escapado, listo para la sentencia que se replica.</summary>
        Private Shared Function Q(texto As String) As String
            Return $"'{LibroBancos.Literal(texto)}'"
        End Function

        Private Shared Function Fecha(valor As Date) As String
            Return valor.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
        End Function

        ''' <summary>A diferencia de VB6 no vacía numloc: aún guarda contactos que no están en gacdptel.</summary>
        Friend Shared Function SentenciaActualizar(p As Persona) As String
            Return $"UPDATE gacdp SET tipo = {Q(p.Tipo)}, nombre = {Q(p.Nombre)}, apellido1 = {Q(p.Apellido1)}, " &
                   $"apellido2 = {Q(p.Apellido2)}, alias = {Q(p.Alias)}, genero = {Q(p.Genero)}, fnac = '{Fecha(p.FNac)}', " &
                   $"prov = {Q(p.Prov)}, cant = {Q(p.Cant)}, dist = {Q(p.Dist)}, caserio = {Q(p.Caserio)}, " &
                   $"dir = {Q(p.Dir)}, puesto = {Q(p.Puesto)}, cia = {Q(p.Cia)}, pres = {Q(p.Pres)}, " &
                   $"nac = {Q(p.Nac)}, estciv = {Q(p.EstCiv)}, categoria = {Q(p.Categoria)}, activo = {Q(p.Activo)} " &
                   $"WHERE id = {Q(p.Id)}"
        End Function

        Friend Shared Function SentenciaInsertar(p As Persona, uid As Long) As String
            Return "INSERT INTO gacdp (id, tipo, nombre, apellido1, apellido2, alias, genero, fnac, numloc, prov, cant, dist, caserio, dir, puesto, cia, pres, nac, estciv, categoria, activo, uidc, fc) VALUES (" &
                   $"{Q(p.Id)}, {Q(p.Tipo)}, {Q(p.Nombre)}, {Q(p.Apellido1)}, {Q(p.Apellido2)}, " &
                   $"{Q(p.Alias)}, {Q(p.Genero)}, '{Fecha(p.FNac)}', '', {Q(p.Prov)}, {Q(p.Cant)}, " &
                   $"{Q(p.Dist)}, {Q(p.Caserio)}, {Q(p.Dir)}, {Q(p.Puesto)}, {Q(p.Cia)}, " &
                   $"{Q(p.Pres)}, {Q(p.Nac)}, {Q(p.EstCiv)}, {Q(p.Categoria)}, {Q(p.Activo)}, {uid}, NOW())"
        End Function

        ''' <summary>
        ''' Borra los que ya no están y graba los demás en su orden. En VB6 el COUNT comparaba contra un
        ''' texto fijo, siempre daba 0 y el INSERT chocaba con la llave; aquí un representante existente
        ''' se actualiza y conserva quién lo creó.
        ''' </summary>
        Friend Shared Function SentenciasRepresentantes(id As String, representantes As IEnumerable(Of RepresentanteLegal), uid As Long) As List(Of String)
            Dim empresa = Q(id)
            Dim vigentes = representantes.Where(Function(r) Not r.Borrar).ToList()
            Dim borrar = $"DELETE FROM gacdprepresentante2 WHERE id = {empresa}"
            If vigentes.Count > 0 Then borrar &= $" AND idrep NOT IN ({String.Join(", ", vigentes.Select(Function(r) Q(r.IdRep)))})"
            Dim sentencias As New List(Of String) From {borrar}
            For i = 0 To vigentes.Count - 1
                Dim r = vigentes(i)
                Dim personeria = Q(r.Personeria)
                Dim orden = i + 1
                sentencias.Add("INSERT INTO gacdprepresentante2 (id, idrep, orden, numpersoneria, fechapersoneria, uidc, fc) " &
                               $"VALUES ({empresa}, {Q(r.IdRep)}, {orden}, {personeria}, '{Fecha(r.Fecha)}', {uid}, NOW()) " &
                               $"ON DUPLICATE KEY UPDATE orden = {orden}, numpersoneria = {personeria}, fechapersoneria = '{Fecha(r.Fecha)}'")
            Next
            Return sentencias
        End Function

        ''' <summary>Reemplaza los contactos, como NumLoc.Guardar. Las filas sin dato se ignoran.</summary>
        Friend Shared Function SentenciasContactos(id As String, contactos As IEnumerable(Of ContactoPersona), uid As Long) As List(Of String)
            Dim persona = Q(id)
            Dim sentencias As New List(Of String) From {$"DELETE FROM gacdptel WHERE id = {persona}"}
            Dim validos = contactos.Where(Function(c) c.Dato.Trim().Length > 0).ToList()
            If validos.Count > 0 Then
                Dim filas = validos.Select(Function(c, i) $"({persona}, {i + 1}, {c.Tipo}, {Q(c.Dato.Trim())}, {uid}, NOW())")
                sentencias.Add($"INSERT INTO gacdptel (id, pos, tipo, dato, uidc, fc) VALUES {String.Join(", ", filas)}")
            End If
            Return sentencias
        End Function

        ''' <summary>
        ''' Graba la persona, sus representantes y sus contactos. Los representantes solo se tocan en
        ''' personas jurídicas, porque hay datos en otros tipos y su pestaña no se muestra.
        ''' </summary>
        Public Async Function GuardarAsync(p As Persona, contactos As IEnumerable(Of ContactoPersona), representantes As IEnumerable(Of RepresentanteLegal), uid As Long, local As Long, Optional ct As CancellationToken = Nothing) As Task
            Normalizar(p)
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Dim existe = Await cn.ExecuteScalarAsync(Of Long)(New CommandDefinition("SELECT COUNT(id) FROM gacdp WHERE id = @Id", New With {p.Id}, cancellationToken:=ct)).ConfigureAwait(False) > 0
                Await EjecutarReplicadoAsync(cn, ReplicaPersonas, If(existe, SentenciaActualizar(p), SentenciaInsertar(p, uid)), local, ct).ConfigureAwait(False)
                If p.EsJuridica Then
                    For Each sql In SentenciasRepresentantes(p.Id, representantes, uid)
                        Await EjecutarReplicadoAsync(cn, ReplicaPersonas, sql, local, ct).ConfigureAwait(False)
                    Next
                End If
                For Each sql In SentenciasContactos(p.Id, contactos, uid)
                    Await EjecutarAsync(cn, sql, ct).ConfigureAwait(False)
                Next
            End Using
        End Function

        ' ---- Códigos ----

        ''' <summary>La identificación que ya usa ese código, o "" si está libre.</summary>
        Public Async Function DuenoCodigoAsync(codigo As String, Optional ct As CancellationToken = Nothing) As Task(Of String)
            Const sql As String = "SELECT ID FROM gaccodclientes WHERE CODIGO = @codigo " &
                                  "UNION SELECT ID FROM gacasociados WHERE CODIGO = @codigo AND ACTIVO = '1' " &
                                  "UNION SELECT id FROM gacdp WHERE id = @codigo LIMIT 1"
            Return If(Await EscalarAsync(Of String)(sql, New With {codigo}, ct).ConfigureAwait(False), "")
        End Function

        Friend Shared Function RellenarCodigo(valor As Long) As String
            Return valor.ToString(CultureInfo.InvariantCulture).PadLeft(4, "0"c)
        End Function

        ''' <summary>Siguiente número para el prefijo, con al menos 4 dígitos.</summary>
        Public Async Function SiguienteCodigoAsync(prefijo As String, Optional ct As CancellationToken = Nothing) As Task(Of String)
            prefijo = prefijo.Trim()
            Dim inicio = prefijo.Length + 1
            Dim sql = If(prefijo.Length = 0,
                         "SELECT IFNULL(CONVERT(MAX(CODIGO), SIGNED), 0) + 1 FROM gaccodclientes WHERE FORMAT(CODIGO, 0) <> 0",
                         $"SELECT IFNULL(CONVERT(MAX(MID(CODIGO, {inicio})), SIGNED), 0) + 1 FROM gaccodclientes WHERE LEFT(CODIGO, {prefijo.Length}) = @prefijo AND FORMAT(MID(CODIGO, {inicio}), 0) <> 0")
            Return RellenarCodigo(Await EscalarAsync(Of Long)(sql, New With {prefijo}, ct).ConfigureAwait(False))
        End Function

        Friend Shared Function SentenciaAgregarCodigo(id As String, codigo As String, uid As Long, local As Long) As String
            Return $"INSERT INTO gaccodclientes (LOCAL, ID, CODIGO, UIDC, FE, FC) VALUES ({local}, {Q(id)}, {Q(codigo)}, {uid}, NOW(), NOW())"
        End Function

        Friend Shared Function SentenciaEliminarCodigo(id As String, codigo As String) As String
            Return $"DELETE FROM gaccodclientes WHERE ID = {Q(id)} AND CODIGO = {Q(codigo)}"
        End Function

        Public Async Function AgregarCodigoAsync(id As String, codigo As String, uid As Long, local As Long, Optional ct As CancellationToken = Nothing) As Task
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await EjecutarReplicadoAsync(cn, ReplicaCodigos, SentenciaAgregarCodigo(id, codigo, uid, local), local, ct).ConfigureAwait(False)
            End Using
        End Function

        Public Async Function EliminarCodigoAsync(id As String, codigo As String, local As Long, Optional ct As CancellationToken = Nothing) As Task
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await EjecutarReplicadoAsync(cn, ReplicaCodigos, SentenciaEliminarCodigo(id, codigo), local, ct).ConfigureAwait(False)
            End Using
        End Function

        ' ---- Imágenes ----

        Public Function ObtenerImagenAsync(id As String, tipo As TipoImagen, Optional ct As CancellationToken = Nothing) As Task(Of Byte())
            Return EscalarAsync(Of Byte())("SELECT imag FROM gatimagenes WHERE id = @id AND tipo = @tipo", New With {id, .tipo = CInt(tipo)}, ct)
        End Function

        Public Async Function GuardarImagenAsync(id As String, tipo As TipoImagen, imagen As Byte(), Optional ct As CancellationToken = Nothing) As Task
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition("REPLACE INTO gatimagenes (id, tipo, imag, fing) VALUES (@id, @tipo, @imagen, NOW())", New With {id, .tipo = CInt(tipo), imagen}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

        Public Async Function BorrarImagenAsync(id As String, tipo As TipoImagen, Optional ct As CancellationToken = Nothing) As Task
            Using cn = Await _fabrica.AbrirAsync(ct).ConfigureAwait(False)
                Await cn.ExecuteAsync(New CommandDefinition("DELETE FROM gatimagenes WHERE id = @id AND tipo = @tipo", New With {id, .tipo = CInt(tipo)}, cancellationToken:=ct)).ConfigureAwait(False)
            End Using
        End Function

    End Class

End Namespace
