using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SDACA.Core.Configuracion
{
    /// <summary>Un perfil de conexiones.ini, por ejemplo [CP.LOCAL].</summary>
    public sealed class PerfilConexion
    {
        public string Nombre { get; set; } = "";
        public string Servidor { get; set; } = "";
        public uint Puerto { get; set; } = 3306;
        public string Catalogo { get; set; } = "";
        /// <summary>Último usuario que entró con este perfil (USUARIO=).</summary>
        public string UltimoUsuario { get; set; } = "";
        /// <summary>Historial de usuarios (USUARIO0=, USUARIO1=, ...).</summary>
        public IList<string> Usuarios { get; } = new List<string>();
    }

    /// <summary>
    /// Lee el conexiones.ini que usa hoy el menú VB6, para que la versión .NET ofrezca los
    /// mismos perfiles sin configurar nada aparte.
    /// </summary>
    /// <remarks>
    /// La sección [GENERAL] no se lee a propósito: su clave CNN guarda una contraseña en texto
    /// plano. Las contraseñas en .NET se guardan con <see cref="Secretos"/>.
    /// </remarks>
    public static class ConexionesIni
    {
        public static IReadOnlyList<PerfilConexion> Leer(string ruta)
        {
            // El archivo lo escribe VB6 con la página de códigos de Windows (1252).
            var texto = File.ReadAllText(ruta, CodificacionWindows1252());
            return Parsear(texto);
        }

        public static IReadOnlyList<PerfilConexion> Parsear(string texto)
        {
            var perfiles = new List<PerfilConexion>();
            PerfilConexion? actual = null;
            foreach (var crudo in texto.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
            {
                var linea = crudo.Trim();
                if (linea.Length == 0 || linea.StartsWith(";")) continue;
                if (linea.StartsWith("[") && linea.EndsWith("]"))
                {
                    var nombre = linea.Substring(1, linea.Length - 2).Trim();
                    actual = string.Equals(nombre, "GENERAL", StringComparison.OrdinalIgnoreCase) ? null : new PerfilConexion { Nombre = nombre };
                    if (actual != null) perfiles.Add(actual);
                    continue;
                }
                if (actual == null) continue;
                var igual = linea.IndexOf('=');
                if (igual <= 0) continue;
                var clave = linea.Substring(0, igual).Trim().ToUpperInvariant();
                var valor = linea.Substring(igual + 1).Trim();
                switch (clave)
                {
                    case "SERVIDOR": actual.Servidor = valor; break;
                    case "CATALOGO": actual.Catalogo = valor; break;
                    case "PUERTO": actual.Puerto = uint.TryParse(valor, out var p) && p > 0 ? p : 3306u; break;
                    case "USUARIO": actual.UltimoUsuario = valor; break;
                    default:
                        if (clave.StartsWith("USUARIO") && clave.Substring(7).All(char.IsDigit) && !actual.Usuarios.Contains(valor, StringComparer.OrdinalIgnoreCase))
                            actual.Usuarios.Add(valor);
                        break;
                }
            }
            return perfiles;
        }

        internal static Encoding CodificacionWindows1252()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            return Encoding.GetEncoding(1252);
        }
    }
}
