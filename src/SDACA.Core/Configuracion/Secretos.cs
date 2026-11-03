using System;
using System.Security.Cryptography;
using System.Text;

namespace SDACA.Core.Configuracion
{
    /// <summary>
    /// Cifra contraseñas con DPAPI de Windows, ligadas al usuario de Windows o al equipo.
    /// Reemplaza la contraseña en texto plano de conexiones.ini.
    /// </summary>
#if NET5_0_OR_GREATER
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
#endif
    public static class Secretos
    {
        private const string Prefijo = "dpapi:";
        private static readonly byte[] Entropia = Encoding.UTF8.GetBytes("SDACA.ERP/conexiones");

        public static string Proteger(string textoPlano, bool porEquipo = false)
        {
            var datos = Encoding.UTF8.GetBytes(textoPlano);
            var cifrado = ProtectedData.Protect(datos, Entropia, porEquipo ? DataProtectionScope.LocalMachine : DataProtectionScope.CurrentUser);
            return Prefijo + Convert.ToBase64String(cifrado);
        }

        public static string Desproteger(string valor, bool porEquipo = false)
        {
            if (!valor.StartsWith(Prefijo, StringComparison.Ordinal))
                throw new FormatException("El valor no está cifrado con Secretos.Proteger. Vuelve a guardarlo cifrado.");
            var cifrado = Convert.FromBase64String(valor.Substring(Prefijo.Length));
            var datos = ProtectedData.Unprotect(cifrado, Entropia, porEquipo ? DataProtectionScope.LocalMachine : DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(datos);
        }
    }
}
