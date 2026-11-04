' Equivalentes .NET de los UDT de SDBASE00\Tipos.cls. Son los cuatro parámetros que
' el menú pasa a cada módulo en Init(clave, Conexion, Usuario, Registro, CONFIG).
' Los nombres de las propiedades siguen a los campos originales para que el adaptador
' COM (Fase 2) los copie uno a uno.

Namespace Modelo

    ''' <summary>SDACA_CONFIG: rutas de INI y datos del módulo activo.</summary>
    Public NotInheritable Class ConfiguracionSdaca
        ''' <summary>ARGUMENTOS: línea de comandos del programa (Command$).</summary>
        Public Property Argumentos As String = ""
        ''' <summary>INI_PATH: carpeta de los archivos INI, por ejemplo "C:\".</summary>
        Public Property RutaIni As String = ""
        Public Property RutaCompletaSdConfig As String = ""
        Public Property ArchivoSdConfig As String = "sdconfig.ini"
        Public Property RutaCompletaConexiones As String = ""
        Public Property ArchivoConexiones As String = "conexiones.ini"
        Public Property RutaCompletaIniModulo As String = ""
        Public Property ArchivoIniModulo As String = ""
        ''' <summary>MOD_CODIGO: código del módulo activo (App.EXEName, ej. "SD0601").</summary>
        Public Property CodigoModulo As String = ""
        Public Property DescripcionModulo As String = ""
        ''' <summary>MOD_SIGLAS: siglas del módulo, ej. "CXC".</summary>
        Public Property SiglasModulo As String = ""
        Public Property Depuracion As Boolean
        ''' <summary>RENDERMODE: modo de render de ActiveReports.</summary>
        Public Property ModoRender As Integer
        Public Property ArchivoTemporal As String = ""
    End Class

    ''' <summary>SDACA_InformacionConexion: perfil y servidor de base de datos.</summary>
    Public NotInheritable Class InformacionConexion
        ''' <summary>Perfil de conexiones.ini, ej. "CP.LOCAL".</summary>
        Public Property Perfil As String = ""
        Public Property Usuario As String = ""
        Public Property Contrasena As String = ""
        Public Property Servidor As String = ""
        Public Property EstacionId As String = ""
        Public Property Estacion As String = ""
        Public Property Puerto As String = "3306"
        Public Property Ip As String = ""
        ''' <summary>Base de datos (catálogo), ej. "sdprincipal".</summary>
        Public Property Catalogo As String = ""
        Public Property Version As String = ""
        Public Property Tipo As String = ""
        ''' <summary>Cadena ADO/ODBC original. En .NET se usa <see cref="Datos.CadenaConexion"/>.</summary>
        Public Property ConnectionString As String = ""
        ''' <summary>SDACADIR: carpeta principal de la instalación (iconos, reportes).</summary>
        Public Property DirectorioSdaca As String = ""
        Public Property Extra As IDictionary(Of String, String) = New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
    End Class

    ''' <summary>SDACA_Usuario: usuario conectado y su local.</summary>
    Public NotInheritable Class Usuario
        ''' <summary>Usuario: nombre de inicio de sesión (también es el usuario de MySQL).</summary>
        Public Property Login As String = ""
        Public Property Contrasena As String = ""
        ''' <summary>ID: cédula o identificación.</summary>
        Public Property Id As String = ""
        ''' <summary>UID: número de usuario. 0 tiene todos los privilegios.</summary>
        Public Property Uid As Long
        Public Property Nombre As String = ""
        Public Property Apellidos As String = ""
        ''' <summary>0 masculino, 1 femenino, 2 jurídica.</summary>
        Public Property Genero As String = ""
        Public Property Privilegios As IList(Of String) = New List(Of String)()
        Public Property Horario As IList(Of String) = New List(Of String)()
        ''' <summary>Local: código del local (sucursal) donde trabaja.</summary>
        Public Property Local As Long
        Public Property LocalDescripcion As String = ""
        Public Property LocalTelefono As String = ""
        Public Property LocalFax As String = ""
        Public Property LocalEmail As String = ""
        ''' <summary>Extra: datos adicionales, ej. "SGCEmpresa".</summary>
        Public Property Extra As IDictionary(Of String, String) = New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
    End Class

    ''' <summary>SDACA_Registro: datos de la licencia del cliente.</summary>
    Public NotInheritable Class Registro
        ''' <summary>Cédula con guiones, como en reg.cls: "3-004-045003".</summary>
        Public Property Cedula As String = ""
        Public Property Nombre As String = ""
        Public Property NombreCorto As String = ""
        ''' <summary>"0" persona física, "1" persona jurídica.</summary>
        Public Property Tipo As String = ""
        Public Property FechaCompra As Date
        ''' <summary>1900-01-01 significa sin vencimiento.</summary>
        Public Property FechaVencimiento As Date = New Date(1900, 1, 1)
        Public Property NumeroCliente As Long
        Public Property VersionRegistro As String = ""
        Public Property ModulosCount As Integer
        Public Property Asociados As Boolean
        Public Property UnaEmpresa As Boolean
        Public Property Telefono As String = ""
        Public Property Fax As String = ""
        Public Property Email As String = ""

        ''' <summary>Cédula sin guiones ("3004045003"), como se usa en factura electrónica y privilegios.</summary>
        Public ReadOnly Property CedulaSinGuiones As String
            Get
                Return Cedula.Replace("-", "")
            End Get
        End Property
    End Class

End Namespace
