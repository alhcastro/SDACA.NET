using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SDACA.Core.Licencias;
using SDBASE00;

namespace SDACA.Com
{
    /// <summary>
    /// Lo que todos los Clase.cls repiten: validar la clave de Init, guardar el contexto
    /// (ModPrin.bas: Conexion, Usuario, Registro, CONFIG) y extraer iconos con SD_LoadRes.
    /// </summary>
    public abstract class ModuloComBase
    {
        protected bool Autorizado { get; private set; }
        protected SDACA_InformacionConexion Conexion;
        protected SDACA_Usuario Usuario;
        protected SDACA_Registro Registro;
        protected SDACA_CONFIG Config;

        /// <summary>Código del módulo, igual a App.EXEName en VB6: "SD1701".</summary>
        protected abstract string Codigo { get; }
        protected abstract string Siglas { get; }

        public string Version()
        {
            var v = GetType().Assembly.GetName().Version;
            return v.Major + "." + v.Minor + "." + v.Build;
        }

        public bool Init(string pClave, ref SDACA_InformacionConexion pConexion, ref SDACA_Usuario pUsuario, ref SDACA_Registro pRegistro, ref SDACA_CONFIG pConfig)
        {
            Autorizado = false;
            if (LicenciaExtensiones.ClaveValida(Codigo, pClave))
            {
                Autorizado = true;
                // VB6 copia los UDT (Conexion = pConexion). Los parámetros llegan ByRef, pero el
                // módulo no los modifica: trabaja sobre su copia, igual que el original.
                Conexion = pConexion;
                Usuario = pUsuario;
                Registro = pRegistro;
                Config = pConfig;
                Config.MOD_SIGLAS = Siglas;
                Config.MOD_CODIGO = Codigo;
                Config.MOD_DESCRIPCION = GetType().Assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product ?? "";
                Config.INI_MOD = Codigo.Replace("REP", "") + ".ini";
                Config.INI_FULL_MOD = Config.INI_PATH + Config.INI_MOD;
            }
            else
            {
                MessageBox.Show("Clase no Registrada...", Codigo, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            return Autorizado;
        }

        public object? Preferencias() => null;

        /// <summary>
        /// Igual que SD_LoadRes de VB6: escribe el recurso en SDACADIR\Icons\&lt;codigo&gt;&lt;id&gt;.gif
        /// (si no existe) y lo abre con OLE. Devuelve null si algo falla.
        /// </summary>
        /// <remarks>
        /// El menú HTML usa el archivo .gif, aunque su contenido sea PNG. LoadPicture (OLE) no abre
        /// PNG, así que en ese caso el original devuelve Nothing; aquí también. VB6 escribe el
        /// recurso con el relleno a 4 bytes que trae dentro de la DLL; se reproduce para que el
        /// archivo quede idéntico.
        /// </remarks>
        public object? SD_LoadRes(int pKey)
        {
            if (!Autorizado) return null;
            try
            {
                var carpeta = Path.Combine(Conexion.SDACADIR ?? "", "Icons");
                if (!Directory.Exists(carpeta))
                {
                    // MkDir de VB6 no crea la carpeta padre: si SDACADIR no existe, falla.
                    if (!Directory.Exists(Conexion.SDACADIR)) return null;
                    Directory.CreateDirectory(carpeta);
                }
                var datos = LeerRecurso(pKey);
                if (datos == null) return null;

                var archivo = Path.Combine(carpeta, Codigo + pKey + ".gif");
                if (!File.Exists(archivo))
                {
                    var relleno = (4 - datos.Length % 4) % 4;
                    using var fs = new FileStream(archivo, FileMode.CreateNew, FileAccess.Write);
                    fs.Write(datos, 0, datos.Length);
                    if (relleno > 0) fs.Write(new byte[relleno], 0, relleno);
                }
                return CargarImagenOle(archivo);
            }
            catch
            {
                return null;
            }
        }

        private byte[]? LeerRecurso(int id)
        {
            using var s = GetType().Assembly.GetManifestResourceStream("Recurso." + id);
            if (s == null) return null;
            using var ms = new MemoryStream();
            s.CopyTo(ms);
            return ms.ToArray();
        }

        private static readonly Guid IID_IPictureDisp = new Guid("7BF80981-BF32-101A-8BBB-00AA00300CAB");

        [DllImport("oleaut32.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
        private static extern int OleLoadPicturePath(string szURLorPath, IntPtr punkCaller, int dwReserved, int clrReserved, ref Guid riid, [MarshalAs(UnmanagedType.IDispatch)] out object? ppvRet);

        private static object? CargarImagenOle(string archivo)
        {
            var iid = IID_IPictureDisp;
            var hr = OleLoadPicturePath(archivo, IntPtr.Zero, 0, 0, ref iid, out var pic);
            return hr >= 0 ? pic : null;
        }
    }
}
