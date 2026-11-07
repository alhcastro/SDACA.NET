Imports System.IO
Imports System.Text
Imports System.Text.RegularExpressions
Imports CoopeNet.Core.Configuracion
Imports CoopeNet.Core.Datos
Imports MySqlConnector

''' <summary>
''' Confirma contra la base lo que Códigos, Cambio de Identificación y Fallecidos suponen del esquema.
''' Solo lee: Consultar rechaza cualquier sentencia que no sea SELECT o SHOW. La contraseña se digita
''' aquí y no se guarda. El informe trae esquema y conteos, no datos de personas.
''' </summary>
Public Module Programa

    Private Const RutaIni As String = "C:\Program Files (x86)\SDACA\Bin\sdcnn007.ini"
    Private Const PerfilPruebas As String = "WCENTRO_4"
    Private Const Muestra As Integer = 300
    Private Const EsperaSegundos As Integer = 180

    Private ReadOnly Informe As New StringBuilder()
    Private ReadOnly IdentificadorValido As New Regex("^[A-Za-z0-9_]+$")
    Private ReadOnly TipoNumerico As New Regex("^(tinyint|smallint|mediumint|int|bigint|decimal|double|float)", RegexOptions.IgnoreCase)

    Public Function Main(args As String()) As Integer
        Dim nombre = If(args.Length > 0, args(0), PerfilPruebas)
        Dim perfil = ConexionesIni.Leer(RutaIni).FirstOrDefault(Function(p) String.Equals(p.Nombre, nombre, StringComparison.OrdinalIgnoreCase))
        If perfil Is Nothing Then
            Console.WriteLine($"No existe el perfil {nombre} en {RutaIni}.")
            Return 1
        End If

        Console.WriteLine($"Perfil {perfil.Nombre}: {perfil.Servidor}:{perfil.Puerto}, catálogo {perfil.Catalogo}")
        Console.Write("Usuario: ")
        Dim usuario = If(Console.ReadLine(), "").Trim()
        Console.Write("Contraseña: ")
        Dim contrasena = LeerOculto()

        Dim cadena = CadenaConexion.Crear(perfil.Servidor, perfil.Catalogo, perfil.Puerto, usuario, contrasena)
        Try
            Using cn As New MySqlConnection(cadena)
                cn.Open()
                Escribir($"Verificación SD0001 {Date.Now:yyyy-MM-dd HH:mm:ss}, perfil {perfil.Nombre}, puerto {perfil.Puerto}, catálogo {perfil.Catalogo}")
                Seccion("Versión")
                Tabla(cn, "SELECT VERSION() AS version")
                Conteos(cn)
                Esquema(cn)
                CambioId(cn)
                Identificaciones(cn)
                TiposCodigo(cn)
                Asociados(cn)
                Padron(cn)
                Replicacion(cn)
            End Using
        Catch ex As MySqlException
            Console.WriteLine($"No se pudo conectar: {ex.Message}")
            Return 1
        End Try

        Dim ruta = Path.Combine(AppContext.BaseDirectory, $"verificacion-sd0001-{Date.Now:yyyyMMdd-HHmmss}.txt")
        File.WriteAllText(ruta, Informe.ToString(), Encoding.UTF8)
        Console.WriteLine($"Listo. Informe: {ruta}")
        Return 0
    End Function

    Private Sub Conteos(cn As MySqlConnection)
        Seccion("Conteos (comparar antes y después de la prueba)")
        For Each t In {"gacdp", "gacdptel", "gacdprepresentante2", "gaccodclientes", "gacasociados", "gactiposcodigos", "gatcambioid", "gatreplica"}
            Tabla(cn, $"SELECT '{t}' AS tabla, COUNT(*) AS filas FROM {t}")
        Next
        Tabla(cn, "SELECT COUNT(*) AS fallecidos FROM gacdp WHERE fdef > '1900-01-01'")
    End Sub

    Private Sub Esquema(cn As MySqlConnection)
        Seccion("Esquema")
        For Each t In {"gatcambioid", "gactiposcodigos", "gacasociados", "gaccodclientes", "gacdptel", "gacdpteltipos", "gatreplicadef"}
            Tabla(cn, $"SHOW CREATE TABLE {t}")
        Next
    End Sub

    Private Sub CambioId(cn As MySqlConnection)
        Seccion("gatcambioid")
        Tabla(cn, "SELECT tipo, COUNT(*) AS filas FROM gatcambioid GROUP BY tipo")
        Escribir("tabla | campo | condicion | tipo | llave única | filas | filas en cero")
        Try
            For Each fila In Consultar(cn, "SELECT tabla, campo, condicion FROM gatcambioid WHERE tipo = 'id'")
                Escribir($"{fila(0)} | {fila(1)} | {fila(2)} | {RevisarColumna(cn, fila(0), fila(1))}")
            Next
        Catch ex As MySqlException
            Escribir($"ERROR: {ex.Message}")
        End Try
        Escribir("")
    End Sub

    ''' <summary>
    ''' UPDATE IGNORE puede dejar filas sin cambiar si la columna está en una llave única. En una columna
    ''' numérica MySQL convierte la identificación a número: una que no empieza con dígitos vale 0 y
    ''' alcanzaría todas las filas en cero.
    ''' </summary>
    Private Function RevisarColumna(cn As MySqlConnection, tabla As String, campo As String) As String
        If Not IdentificadorValido.IsMatch(tabla) OrElse Not IdentificadorValido.IsMatch(campo) Then Return "nombre no revisable"
        Try
            Dim columna = Consultar(cn, $"SHOW COLUMNS FROM `{tabla}` LIKE '{campo}'")
            If columna.Count = 0 Then Return "FALTA LA COLUMNA"
            Dim tipo = columna(0)(1)
            Dim total = Consultar(cn, $"SELECT COUNT(*) FROM `{tabla}`")(0)(0)
            Dim ceros = If(TipoNumerico.IsMatch(tipo), Consultar(cn, $"SELECT COUNT(*) FROM `{tabla}` WHERE `{campo}` = 0")(0)(0), "-")
            Return $"{tipo} | {LlaveUnica(cn, tabla, campo)} | {total} | {ceros}"
        Catch ex As MySqlException
            Return $"ERROR: {ex.Message}"
        End Try
    End Function

    ''' <summary>"sola" si la columna es por sí misma una llave única, "compuesta" si solo es parte de una.</summary>
    Private Function LlaveUnica(cn As MySqlConnection, tabla As String, campo As String) As String
        Dim llaves = Consultar(cn, $"SHOW INDEX FROM `{tabla}` WHERE Non_unique = 0").GroupBy(Function(f) f(2)).ToList()
        Dim propias = llaves.Where(Function(g) g.Any(Function(f) String.Equals(f(4), campo, StringComparison.OrdinalIgnoreCase))).ToList()
        If propias.Count = 0 Then Return "no"
        Return If(propias.Any(Function(g) g.Count() = 1), "sola", "compuesta")
    End Function

    Private Sub Identificaciones(cn As MySqlConnection)
        Seccion("Forma de gacdp.id (solo conteos)")
        Tabla(cn, "SELECT SUM(id REGEXP '^[0-9]+$') AS solo_digitos, SUM(id NOT REGEXP '^[0-9]+$') AS otras, SUM(id + 0 = 0) AS valen_cero, SUM(id + 0 BETWEEN 1 AND 99999) AS valen_menos_de_100000 FROM gacdp")
    End Sub

    Private Sub TiposCodigo(cn As MySqlConnection)
        Seccion("gactiposcodigos")
        Tabla(cn, "SELECT TIPO, DESCRIPCION, SISTEMA, ACTIVO, UIDC, FE, FC FROM gactiposcodigos ORDER BY TIPO")
    End Sub

    Private Sub Asociados(cn As MySqlConnection)
        Seccion("gacasociados contra gac_fun_asociados")
        Tabla(cn, "SHOW CREATE FUNCTION gac_fun_asociados")
        Tabla(cn, "SELECT COUNT(*) AS codigos_repetidos FROM (SELECT CODIGO FROM gacasociados WHERE ACTIVO = '1' GROUP BY CODIGO HAVING COUNT(*) > 1) d")
        Tabla(cn, "SELECT LOCAL, ACTIVO, COUNT(*) AS filas FROM gacasociados GROUP BY LOCAL, ACTIVO ORDER BY LOCAL, ACTIVO")
        ' VB6 llama SD_Obtener_ID sin local, así que la función recibe 0.
        Tabla(cn, "SELECT COUNT(*) AS revisados, SUM(gac_fun_asociados(0, s.CODIGO) = s.ID) AS iguales_local_0, SUM(gac_fun_asociados(s.LOCAL, s.CODIGO) = s.ID) AS iguales_su_local " &
                  $"FROM (SELECT LOCAL, CODIGO, ID FROM gacasociados WHERE ACTIVO = '1' LIMIT {Muestra}) s")
        Tabla(cn, "SELECT COUNT(*) AS codigos_de_otra_persona FROM gacasociados a INNER JOIN gaccodclientes c ON c.CODIGO = a.CODIGO WHERE a.ID <> c.ID")
    End Sub

    ''' <summary>VB6 usa Sexo = 'H' y BuscarEnPadronAsync usa Sexo = 'M'.</summary>
    Private Sub Padron(cn As MySqlConnection)
        Seccion("padron.Sexo")
        Tabla(cn, "SELECT Sexo, COUNT(*) AS filas FROM padron.padron GROUP BY Sexo")
        ' FrmPadron busca por Cedula LIKE 'x%', Apellido1 y Apellido2 exactos y Nombre LIKE '%x%'.
        Seccion("Esquema del padrón")
        Tabla(cn, "SHOW CREATE TABLE padron.padron")
        Tabla(cn, "SHOW CREATE TABLE padron.padronj")
        Tabla(cn, "SELECT COUNT(*) AS juridicas FROM padron.padronj")
    End Sub

    Private Sub Replicacion(cn As MySqlConnection)
        Seccion("Destinos de replicación por identificador y local")
        Tabla(cn, "SELECT Ident, LOCAL, COUNT(*) AS destinos FROM gatreplicadef WHERE Ident IN ('DP', 'GATMANT', 'posclientes') GROUP BY Ident, LOCAL ORDER BY Ident, LOCAL")
    End Sub

    ''' <summary>Escribe el resultado. Un error queda en el informe y no detiene las demás revisiones.</summary>
    Private Sub Tabla(cn As MySqlConnection, sql As String)
        Escribir($"> {sql}")
        Try
            For Each fila In Consultar(cn, sql)
                Escribir(String.Join(" | ", fila))
            Next
        Catch ex As MySqlException
            Escribir($"ERROR: {ex.Message}")
        End Try
        Escribir("")
    End Sub

    Private Function Consultar(cn As MySqlConnection, sql As String) As List(Of String())
        If Not Regex.IsMatch(sql, "^\s*(SELECT|SHOW)\s", RegexOptions.IgnoreCase) OrElse sql.Contains(";"c) Then
            Throw New InvalidOperationException($"Esta herramienta solo lee: {sql}")
        End If
        Dim filas As New List(Of String())
        Using cmd As New MySqlCommand(sql, cn) With {.CommandTimeout = EsperaSegundos}
            Using lector = cmd.ExecuteReader()
                While lector.Read()
                    Dim valores(lector.FieldCount - 1) As String
                    For i = 0 To lector.FieldCount - 1
                        valores(i) = If(lector.IsDBNull(i), "NULL", Convert.ToString(lector.GetValue(i)))
                    Next
                    filas.Add(valores)
                End While
            End Using
        End Using
        Return filas
    End Function

    Private Function LeerOculto() As String
        Dim texto As New StringBuilder()
        Do
            Dim tecla = Console.ReadKey(intercept:=True)
            If tecla.Key = ConsoleKey.Enter Then Exit Do
            If tecla.Key = ConsoleKey.Backspace Then
                If texto.Length > 0 Then texto.Length -= 1
            ElseIf Not Char.IsControl(tecla.KeyChar) Then
                texto.Append(tecla.KeyChar)
            End If
        Loop
        Console.WriteLine()
        Return texto.ToString()
    End Function

    Private Sub Seccion(titulo As String)
        Escribir($"=== {titulo} ===")
    End Sub

    Private Sub Escribir(linea As String)
        Informe.AppendLine(linea)
    End Sub

End Module
