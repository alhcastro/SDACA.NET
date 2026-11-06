Imports MySqlConnector
Imports SDACA.Core.Seguridad

Namespace Datos

    ''' <summary>
    ''' Arma la cadena de conexión a MySQL con las mismas reglas que
    ''' <c>Comun\SD_MOD_CNN.bas → Get_ConnectionString</c>, pero para MySqlConnector en lugar de ODBC.
    ''' </summary>
    ''' <remarks>
    ''' Reglas del original, en orden:
    ''' <list type="number">
    ''' <item>"Administrador" (sin importar mayúsculas) entra a MySQL como <c>root</c>.</item>
    ''' <item>Sin catálogo se usa <c>mysql</c>. Sin usuario se usa <c>sdaca</c>.</item>
    ''' <item>El usuario <c>sdaca</c> sin contraseña usa <c>MD5("sdaca")</c>.</item>
    ''' <item>Para cualquier usuario que no sea root, si la contraseña tiene menos de 32 caracteres
    ''' se le aplica MD5. Si ya tiene 32 o más, se toma como hash ya calculado.</item>
    ''' <item>root usa la contraseña tal cual.</item>
    ''' </list>
    ''' </remarks>
    Public NotInheritable Class CadenaConexion

        Private Sub New()
        End Sub

        Public NotInheritable Class Credenciales
            Public Sub New(usuarioMySql As String, contrasena As String)
                Me.UsuarioMySql = usuarioMySql
                Me.Contrasena = contrasena
            End Sub

            Public ReadOnly Property UsuarioMySql As String
            Public ReadOnly Property Contrasena As String
        End Class

        ''' <summary>Aplica las reglas de usuario y contraseña del original.</summary>
        Public Shared Function ResolverCredenciales(usuario As String, contrasena As String) As Credenciales
            usuario = If(usuario, "")
            contrasena = If(contrasena, "")

            Dim usuarioMySql = If(String.Equals(usuario, "administrador", StringComparison.OrdinalIgnoreCase), "root", usuario)
            If usuarioMySql.Length = 0 Then usuarioMySql = "sdaca"

            If contrasena.Length = 0 AndAlso usuarioMySql = "sdaca" Then
                contrasena = Md5Vb6.Calcular("sdaca")
            ElseIf usuarioMySql <> "root" AndAlso contrasena.Length < 32 Then
                ' Igual que el original: una contraseña vacía de un usuario distinto de sdaca
                ' también se convierte en MD5("").
                contrasena = Md5Vb6.Calcular(contrasena)
            End If

            Return New Credenciales(usuarioMySql, contrasena)
        End Function

        ''' <summary>Cadena para MySqlConnector (sin ODBC).</summary>
        Public Shared Function Crear(servidor As String, catalogo As String, puerto As UInteger, usuario As String, contrasena As String) As String
            Dim cred = ResolverCredenciales(usuario, contrasena)
            Dim b As New MySqlConnectionStringBuilder With {
                .Server = servidor,
                .Port = If(puerto = 0UI, 3306UI, puerto),
                .Database = If(String.IsNullOrEmpty(catalogo), "mysql", catalogo),
                .UserID = cred.UsuarioMySql,
                .Password = cred.Contrasena,
                .CharacterSet = "utf8mb4",
                .ConvertZeroDateTime = True,
                .AllowZeroDateTime = False,
                .AllowUserVariables = True,
                .UseAffectedRows = False,
                .Pooling = True
            }
            ' CharacterSet: las tablas son latin1 (ENGINE=MYISAM DEFAULT CHARSET=latin1). La conexión
            '   en utf8mb4 deja que MySQL convierta y evita textos con caracteres dañados.
            ' ConvertZeroDateTime: OPTION=16384 / 18475 del ODBC, convierte fechas 0000-00-00 en vez de fallar.
            ' AllowUserVariables: el código VB6 usa muchas sentencias múltiples y variables de usuario (@var).
            ' UseAffectedRows = False: el ODBC 3.51 devolvía las filas encontradas, no solo las modificadas.
            Return b.ConnectionString
        End Function

        ''' <summary>
        ''' Convierte la cadena ODBC que el menú VB6 entrega en <c>Conexion.ConnectionString</c> a una
        ''' de MySqlConnector. Los módulos migrados la usan para abrir la misma base que el menú.
        ''' </summary>
        ''' <remarks>UID y PWD ya vienen resueltos: las reglas de <see cref="ResolverCredenciales"/> no los cambian.</remarks>
        Public Shared Function DesdeOdbc(cadenaOdbc As String) As String
            Dim propiedades = LeerPropiedadesOdbc(cadenaOdbc)
            Dim valor = Function(llave As String) If(propiedades.ContainsKey(llave), propiedades(llave), "")
            Dim puerto As UInteger
            UInteger.TryParse(valor("PORT"), puerto)
            Return Crear(valor("SERVER"), valor("DATABASE"), puerto, valor("UID"), valor("PWD"))
        End Function

        Private Shared Function LeerPropiedadesOdbc(cadenaOdbc As String) As Dictionary(Of String, String)
            Dim propiedades As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
            Dim texto = If(cadenaOdbc, "")
            Const marca As String = "Extended Properties="""
            Dim inicio = texto.IndexOf(marca, StringComparison.OrdinalIgnoreCase)
            If inicio >= 0 Then
                inicio += marca.Length
                Dim fin = texto.LastIndexOf(""""c)
                If fin > inicio Then texto = texto.Substring(inicio, fin - inicio)
            End If
            For Each parte In texto.Split(";"c)
                Dim igual = parte.IndexOf("="c)
                If igual > 0 Then propiedades(parte.Substring(0, igual).Trim()) = parte.Substring(igual + 1)
            Next
            Return propiedades
        End Function

        ''' <summary>
        ''' Cadena ODBC exacta del original (driver 3.51), solo para comparar en pruebas
        ''' o para pasarla a módulos VB6 que todavía usan ADO.
        ''' </summary>
        Public Shared Function CrearOdbcLegacy(servidor As String, catalogo As String, puerto As Long, usuario As String, contrasena As String) As String
            Dim cred = ResolverCredenciales(usuario, contrasena)
            Dim cat = If(String.IsNullOrEmpty(catalogo), "mysql", catalogo)
            If cred.Contrasena.Length = 0 Then
                Return "Provider=MSDASQL.1;Extended Properties=""DATABASE=" & cat & ";DRIVER={MySQL ODBC 3.51 Driver};OPTION=18475;PORT=" & puerto & ";SERVER=" & servidor & ";UID=" & cred.UsuarioMySql & ";"""
            End If
            Return "Provider=MSDASQL.1;Password=" & cred.Contrasena & ";Extended Properties=""DATABASE=" & cat & ";DRIVER={MySQL ODBC 3.51 Driver};OPTION=18475;PWD=" & cred.Contrasena & ";PORT=" & puerto & ";SERVER=" & servidor & ";UID=" & cred.UsuarioMySql & ";"""
        End Function

    End Class

End Namespace
