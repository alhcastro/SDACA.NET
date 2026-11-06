Imports MySqlConnector
Imports CoopeNet.Core.Seguridad

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
