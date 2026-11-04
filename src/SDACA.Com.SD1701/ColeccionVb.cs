using System;

namespace SDACA.Com
{
    /// <summary>Lectura de las Collection de VB6 que vienen dentro de los UDT (Privilegios, Extra).</summary>
    public static class ColeccionVb
    {
        /// <summary>
        /// Igual que SD_LoadFromCollection: el valor de la llave como texto, o "" si no existe.
        /// </summary>
        public static string Leer(VBA.Collection? coleccion, string llave)
        {
            if (coleccion == null) return "";
            try
            {
                object indice = llave;
                return Convert.ToString(coleccion.Item(ref indice)) ?? "";
            }
            catch
            {
                return "";
            }
        }
    }
}
