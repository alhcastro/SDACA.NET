using System.Security.Cryptography;
using System.Text;

namespace SDACA.Core.Seguridad
{
    /// <summary>
    /// MD5 idéntico al de VB6 (<c>Comun\SD_CLS_MD5.cls</c> → <c>NEWMD5</c>).
    /// </summary>
    /// <remarks>
    /// El original toma el byte bajo de cada carácter UTF-16 (<c>AscB(Mid(s, i, 1))</c>)
    /// y devuelve hexadecimal en minúsculas. Con eso se calculan las contraseñas de MySQL de
    /// cada usuario, la clave que recibe <c>Init()</c> de cada módulo y los hashes de
    /// <c>gatusuariospriv2</c>. Si el resultado cambia en un solo carácter, el usuario ya no
    /// puede entrar, así que este método no debe "corregirse" a UTF-8.
    /// </remarks>
    public static class Md5Vb6
    {
        public static string Calcular(string? texto)
        {
            texto ??= string.Empty;
            var bytes = new byte[texto.Length];
            for (var i = 0; i < texto.Length; i++)
            {
                bytes[i] = unchecked((byte)(texto[i] & 0xFF));
            }

            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(bytes);
            var sb = new StringBuilder(32);
            foreach (var b in hash)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }
    }
}
