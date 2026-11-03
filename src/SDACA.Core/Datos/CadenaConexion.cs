using System;
using MySqlConnector;
using SDACA.Core.Seguridad;

namespace SDACA.Core.Datos
{
    /// <summary>
    /// Arma la cadena de conexión a MySQL con las mismas reglas que
    /// <c>Comun\SD_MOD_CNN.bas → Get_ConnectionString</c>, pero para MySqlConnector en lugar de ODBC.
    /// </summary>
    /// <remarks>
    /// Reglas del original, en orden:
    /// <list type="number">
    /// <item>"Administrador" (sin importar mayúsculas) entra a MySQL como <c>root</c>.</item>
    /// <item>Sin catálogo se usa <c>mysql</c>. Sin usuario se usa <c>sdaca</c>.</item>
    /// <item>El usuario <c>sdaca</c> sin contraseña usa <c>MD5("sdaca")</c>.</item>
    /// <item>Para cualquier usuario que no sea root, si la contraseña tiene menos de 32 caracteres
    /// se le aplica MD5. Si ya tiene 32 o más, se toma como hash ya calculado.</item>
    /// <item>root usa la contraseña tal cual.</item>
    /// </list>
    /// </remarks>
    public static class CadenaConexion
    {
        public sealed class Credenciales
        {
            public Credenciales(string usuarioMySql, string contrasena)
            {
                UsuarioMySql = usuarioMySql;
                Contrasena = contrasena;
            }

            public string UsuarioMySql { get; }
            public string Contrasena { get; }
        }

        /// <summary>Aplica las reglas de usuario y contraseña del original.</summary>
        public static Credenciales ResolverCredenciales(string? usuario, string? contrasena)
        {
            usuario ??= "";
            contrasena ??= "";

            var usuarioMySql = string.Equals(usuario, "administrador", StringComparison.OrdinalIgnoreCase)
                ? "root"
                : usuario;
            if (usuarioMySql.Length == 0) usuarioMySql = "sdaca";

            if (contrasena.Length == 0 && usuarioMySql == "sdaca")
            {
                contrasena = Md5Vb6.Calcular("sdaca");
            }
            else if (usuarioMySql != "root" && contrasena.Length < 32)
            {
                // Igual que el original: una contraseña vacía de un usuario distinto de sdaca
                // también se convierte en MD5("").
                contrasena = Md5Vb6.Calcular(contrasena);
            }

            return new Credenciales(usuarioMySql, contrasena);
        }

        /// <summary>Cadena para MySqlConnector (sin ODBC).</summary>
        public static string Crear(string servidor, string? catalogo, uint puerto, string? usuario, string? contrasena)
        {
            var cred = ResolverCredenciales(usuario, contrasena);
            var b = new MySqlConnectionStringBuilder
            {
                Server = servidor,
                Port = puerto == 0 ? 3306u : puerto,
                Database = string.IsNullOrEmpty(catalogo) ? "mysql" : catalogo,
                UserID = cred.UsuarioMySql,
                Password = cred.Contrasena,
                // Las tablas son latin1 (ENGINE=MYISAM DEFAULT CHARSET=latin1). La conexión en
                // utf8mb4 deja que MySQL convierta y evita textos con caracteres dañados.
                CharacterSet = "utf8mb4",
                // OPTION=16384 / 18475 del ODBC: convierte fechas 0000-00-00 en vez de fallar.
                ConvertZeroDateTime = true,
                AllowZeroDateTime = false,
                // El código VB6 usa muchas sentencias múltiples y variables de usuario (@var).
                AllowUserVariables = true,
                // El ODBC 3.51 devolvía la cantidad de filas encontradas, no solo las modificadas.
                UseAffectedRows = false,
                Pooling = true,
            };
            return b.ConnectionString;
        }

        /// <summary>
        /// Cadena ODBC exacta del original (driver 3.51), solo para comparar en pruebas
        /// o para pasarla a módulos VB6 que todavía usan ADO.
        /// </summary>
        public static string CrearOdbcLegacy(string servidor, string? catalogo, long puerto, string? usuario, string? contrasena)
        {
            var cred = ResolverCredenciales(usuario, contrasena);
            var cat = string.IsNullOrEmpty(catalogo) ? "mysql" : catalogo;
            if (cred.Contrasena.Length == 0)
            {
                return "Provider=MSDASQL.1;Extended Properties=\"DATABASE=" + cat + ";DRIVER={MySQL ODBC 3.51 Driver};OPTION=18475;PORT=" + puerto + ";SERVER=" + servidor + ";UID=" + cred.UsuarioMySql + ";\"";
            }
            return "Provider=MSDASQL.1;Password=" + cred.Contrasena + ";Extended Properties=\"DATABASE=" + cat + ";DRIVER={MySQL ODBC 3.51 Driver};OPTION=18475;PWD=" + cred.Contrasena + ";PORT=" + puerto + ";SERVER=" + servidor + ";UID=" + cred.UsuarioMySql + ";\"";
        }
    }
}
