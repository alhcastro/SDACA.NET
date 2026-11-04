using System.Runtime.InteropServices;
using SDACA.Core.Seguridad;

namespace SDACA.Com.SD1701
{
    /// <summary>
    /// SD1701 - GCG Consultas Gerenciales. Migración de Fuentes\SD1701\Clase.cls.
    /// </summary>
    /// <remarks>
    /// El menú no pide opciones a este módulo: para los códigos SD17 arma el árbol con
    /// getMantCGerenciales (tabla gatcgeren), que llama a los otros módulos. Aquí solo
    /// importan SDClase (para el nodo principal y el privilegio 17000) e Init.
    /// </remarks>
    [ComVisible(true)]
    [Guid("6F0C5B1E-2D7A-4E3B-9C41-5A8B1D7E1701")]
    [ProgId("SD1701.Clase")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(IModuloSdaca))]
    public sealed class Clase : ModuloComBase, IModuloSdaca
    {
        protected override string Codigo => "SD1701";

        // El original pone "ECC" (copiado de SD1501). Solo lo usa la copia interna de CONFIG,
        // así que se corrige a las siglas reales del módulo.
        protected override string Siglas => "GCG";

        public SD_Clase SDClase()
        {
            if (!Autorizado) return default;
            return new SD_Clase
            {
                Clase = "SD1701",
                Titulo = "GCG",
                Descripcion = "Consultas Gerenciales",
                Privilegio = "17000",
                Icono = 101,
                Picture = 0,
                Reportes = "",
                Color = 0xD9E4EE,
                Preferencias = "",
            };
        }

        public SD_InfoMenu Menu(string Index)
        {
            if (!Autorizado) return default;
            var m = new SD_InfoMenu { Key = Index, Etiqueta = "", Privilegio = "" };
            switch (Index)
            {
                case "001":
                    m.Etiqueta = "Mantenimiento";
                    m.Icono48 = 102;
                    m.Icono16 = 103;
                    m.Privilegio = "17000";
                    break;
            }
            return m;
        }

        public void Accion(string pKey)
        {
            if (!Autorizado) return;
            if (!Privilegios.Validar(p => ColeccionVb.Leer(Usuario.Privilegios, p) == "1", Menu(pKey).Privilegio)) return;
            switch (pKey)
            {
                case "000": // Mantenimientos: vacío en el original.
                    break;
            }
        }

        public object Menu_Order()
        {
            if (!Autorizado) return null!;
            return new object?[] { "", null };
        }
    }
}
