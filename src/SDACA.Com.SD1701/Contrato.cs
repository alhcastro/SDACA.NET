using System;
using System.Runtime.InteropServices;
using SDBASE00;

namespace SDACA.Com
{
    // Contrato COM que el menú SDACAERP espera de cada módulo (SDxxxx.Clase).
    // Los nombres, tipos y el orden de los campos tienen que coincidir con los UDT de VB6,
    // porque el menú lee los resultados por nombre con enlace tardío (iSDCLASE.Privilegio).

    /// <summary>UDT SD_Clase de cada Clase.cls: datos del módulo para el menú principal.</summary>
    [ComVisible(true)]
    [Guid("6F0C5B1E-2D7A-4E3B-9C41-5A8B1D7E0A01")]
    [StructLayout(LayoutKind.Sequential)]
    public struct SD_Clase
    {
        [MarshalAs(UnmanagedType.BStr)] public string Clase;
        [MarshalAs(UnmanagedType.BStr)] public string Titulo;
        [MarshalAs(UnmanagedType.BStr)] public string Descripcion;
        [MarshalAs(UnmanagedType.BStr)] public string Privilegio;
        public int Icono;
        public int Picture;
        [MarshalAs(UnmanagedType.BStr)] public string Reportes;
        /// <summary>OLE_COLOR: BGR, ej. &amp;HD9E4EE.</summary>
        public int Color;
        [MarshalAs(UnmanagedType.BStr)] public string Preferencias;
    }

    /// <summary>UDT SD_InfoMenu: una opción del menú del módulo.</summary>
    [ComVisible(true)]
    [Guid("6F0C5B1E-2D7A-4E3B-9C41-5A8B1D7E0A02")]
    [StructLayout(LayoutKind.Sequential)]
    public struct SD_InfoMenu
    {
        [MarshalAs(UnmanagedType.BStr)] public string Key;
        [MarshalAs(UnmanagedType.BStr)] public string Etiqueta;
        public int Icono48;
        public int Icono16;
        [MarshalAs(UnmanagedType.BStr)] public string Privilegio;
    }

    /// <summary>Interfaz que el menú llama con enlace tardío (IDispatch).</summary>
    [ComVisible(true)]
    [Guid("6F0C5B1E-2D7A-4E3B-9C41-5A8B1D7E0B01")]
    [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IModuloSdaca
    {
        [DispId(1)] string Version();

        [DispId(2)]
        bool Init(string pClave,
                  ref SDACA_InformacionConexion pConexion,
                  ref SDACA_Usuario pUsuario,
                  ref SDACA_Registro pRegistro,
                  ref SDACA_CONFIG pConfig);

        [DispId(3)] SD_Clase SDClase();

        [DispId(4)] SD_InfoMenu Menu(string Index);

        [DispId(5)] void Accion(string pKey);

        /// <summary>Arreglo de Variant con las llaves "001", "001.002", "001.002.003". Termina en "" o Empty.</summary>
        [DispId(6)] object Menu_Order();

        /// <summary>StdPicture del recurso, o Nothing.</summary>
        [DispId(7)] [return: MarshalAs(UnmanagedType.IDispatch)] object? SD_LoadRes(int pKey);

        [DispId(8)] object? Preferencias();
    }
}
