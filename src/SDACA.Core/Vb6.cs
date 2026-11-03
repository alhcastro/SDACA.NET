using System.Globalization;

namespace SDACA.Core
{
    /// <summary>Funciones de VB6 que el código de negocio usa a menudo y que hay que reproducir igual.</summary>
    public static class Vb6
    {
        /// <summary>
        /// Val() de VB6: lee el número que hay al inicio del texto (ignora espacios) y se detiene
        /// en el primer carácter que no sirve. "12abc" → 12, "abc" → 0, " 3 4" → 34.
        /// Solo para enteros; los montos deben leerse con decimal.Parse e InvariantCulture.
        /// </summary>
        public static long Val(string? texto)
        {
            if (string.IsNullOrEmpty(texto)) return 0;
            var limpio = new System.Text.StringBuilder();
            var signo = 1;
            var i = 0;
            var t = texto!;
            while (i < t.Length && (t[i] == ' ' || t[i] == '\t')) i++;
            if (i < t.Length && (t[i] == '-' || t[i] == '+')) { if (t[i] == '-') signo = -1; i++; }
            for (; i < t.Length; i++)
            {
                var c = t[i];
                if (c == ' ' || c == '\t') continue;
                if (c < '0' || c > '9') break;
                limpio.Append(c);
            }
            if (limpio.Length == 0) return 0;
            return long.TryParse(limpio.ToString(), NumberStyles.None, CultureInfo.InvariantCulture, out var n) ? signo * n : 0;
        }
    }
}
