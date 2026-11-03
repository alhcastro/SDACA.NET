using System;
using System.Collections.Generic;

namespace SDACA.Core.Modelo
{
    // Equivalentes .NET de los UDT de SDBASE00\Tipos.cls. Son los cuatro parámetros que
    // el menú pasa a cada módulo en Init(clave, Conexion, Usuario, Registro, CONFIG).
    // Los nombres de las propiedades siguen a los campos originales para que el adaptador
    // COM (Fase 2) los copie uno a uno.

    /// <summary>SDACA_CONFIG: rutas de INI y datos del módulo activo.</summary>
    public sealed class ConfiguracionSdaca
    {
        /// <summary>ARGUMENTOS: línea de comandos del programa (Command$).</summary>
        public string Argumentos { get; set; } = "";
        /// <summary>INI_PATH: carpeta de los archivos INI, por ejemplo "C:\".</summary>
        public string RutaIni { get; set; } = "";
        public string RutaCompletaSdConfig { get; set; } = "";
        public string ArchivoSdConfig { get; set; } = "sdconfig.ini";
        public string RutaCompletaConexiones { get; set; } = "";
        public string ArchivoConexiones { get; set; } = "conexiones.ini";
        public string RutaCompletaIniModulo { get; set; } = "";
        public string ArchivoIniModulo { get; set; } = "";
        /// <summary>MOD_CODIGO: código del módulo activo (App.EXEName, ej. "SD0601").</summary>
        public string CodigoModulo { get; set; } = "";
        public string DescripcionModulo { get; set; } = "";
        /// <summary>MOD_SIGLAS: siglas del módulo, ej. "CXC".</summary>
        public string SiglasModulo { get; set; } = "";
        public bool Depuracion { get; set; }
        /// <summary>RENDERMODE: modo de render de ActiveReports.</summary>
        public int ModoRender { get; set; }
        public string ArchivoTemporal { get; set; } = "";
    }

    /// <summary>SDACA_InformacionConexion: perfil y servidor de base de datos.</summary>
    public sealed class InformacionConexion
    {
        /// <summary>Perfil de conexiones.ini, ej. "CP.LOCAL".</summary>
        public string Perfil { get; set; } = "";
        public string Usuario { get; set; } = "";
        public string Contrasena { get; set; } = "";
        public string Servidor { get; set; } = "";
        public string EstacionId { get; set; } = "";
        public string Estacion { get; set; } = "";
        public string Puerto { get; set; } = "3306";
        public string Ip { get; set; } = "";
        /// <summary>Base de datos (catálogo), ej. "sdprincipal".</summary>
        public string Catalogo { get; set; } = "";
        public string Version { get; set; } = "";
        public string Tipo { get; set; } = "";
        /// <summary>Cadena ADO/ODBC original. En .NET se usa <see cref="Datos.CadenaConexion"/>.</summary>
        public string ConnectionString { get; set; } = "";
        /// <summary>SDACADIR: carpeta principal de la instalación (iconos, reportes).</summary>
        public string DirectorioSdaca { get; set; } = "";
        public IDictionary<string, string> Extra { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>SDACA_Usuario: usuario conectado y su local.</summary>
    public sealed class Usuario
    {
        /// <summary>Usuario: nombre de inicio de sesión (también es el usuario de MySQL).</summary>
        public string Login { get; set; } = "";
        public string Contrasena { get; set; } = "";
        /// <summary>ID: cédula o identificación.</summary>
        public string Id { get; set; } = "";
        /// <summary>UID: número de usuario. 0 tiene todos los privilegios.</summary>
        public long Uid { get; set; }
        public string Nombre { get; set; } = "";
        public string Apellidos { get; set; } = "";
        /// <summary>0 masculino, 1 femenino, 2 jurídica.</summary>
        public string Genero { get; set; } = "";
        public IList<string> Privilegios { get; set; } = new List<string>();
        public IList<string> Horario { get; set; } = new List<string>();
        /// <summary>Local: código del local (sucursal) donde trabaja.</summary>
        public long Local { get; set; }
        public string LocalDescripcion { get; set; } = "";
        public string LocalTelefono { get; set; } = "";
        public string LocalFax { get; set; } = "";
        public string LocalEmail { get; set; } = "";
        /// <summary>Extra: datos adicionales, ej. "SGCEmpresa".</summary>
        public IDictionary<string, string> Extra { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>SDACA_Registro: datos de la licencia del cliente.</summary>
    public sealed class Registro
    {
        /// <summary>Cédula con guiones, como en reg.cls: "3-004-045003".</summary>
        public string Cedula { get; set; } = "";
        public string Nombre { get; set; } = "";
        public string NombreCorto { get; set; } = "";
        /// <summary>"0" persona física, "1" persona jurídica.</summary>
        public string Tipo { get; set; } = "";
        public DateTime FechaCompra { get; set; }
        /// <summary>1900-01-01 significa sin vencimiento.</summary>
        public DateTime FechaVencimiento { get; set; } = new DateTime(1900, 1, 1);
        public long NumeroCliente { get; set; }
        public string VersionRegistro { get; set; } = "";
        public int ModulosCount { get; set; }
        public bool Asociados { get; set; }
        public bool UnaEmpresa { get; set; }
        public string Telefono { get; set; } = "";
        public string Fax { get; set; } = "";
        public string Email { get; set; } = "";

        /// <summary>Cédula sin guiones ("3004045003"), como se usa en factura electrónica y privilegios.</summary>
        public string CedulaSinGuiones => Cedula.Replace("-", "");
    }
}
