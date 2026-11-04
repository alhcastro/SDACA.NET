Imports System.IO
Imports System.Reflection
Imports System.Runtime.InteropServices
Imports SDACA.Core.Licencias
Imports SDBASE00

''' <summary>
''' Lo que todos los Clase.cls repiten: validar la clave de Init, guardar el contexto
''' (ModPrin.bas: Conexion, Usuario, Registro, CONFIG) y extraer iconos con SD_LoadRes.
''' Cada módulo hereda de aquí y completa SDClase, Menu, Accion y Menu_Order.
''' </summary>
Public MustInherit Class ModuloComBase
    Implements IModuloSdaca

    Protected Autorizado As Boolean
    Protected Conexion As SDACA_InformacionConexion
    Protected Usuario As SDACA_Usuario
    Protected Registro As SDACA_Registro
    Protected Config As SDACA_CONFIG

    ''' <summary>Código del módulo, igual a App.EXEName en VB6: "SD1701".</summary>
    Protected MustOverride ReadOnly Property Codigo As String
    Protected MustOverride ReadOnly Property Siglas As String

    Public MustOverride Function SDClase() As SD_Clase Implements IModuloSdaca.SDClase
    Public MustOverride Function Menu(Index As String) As SD_InfoMenu Implements IModuloSdaca.Menu
    Public MustOverride Sub Accion(pKey As String) Implements IModuloSdaca.Accion
    Public MustOverride Function Menu_Order() As Object Implements IModuloSdaca.Menu_Order

    Public Function Version() As String Implements IModuloSdaca.Version
        Dim v = Me.GetType().Assembly.GetName().Version
        Return v.Major & "." & v.Minor & "." & v.Build
    End Function

    Public Function Init(pClave As String, ByRef pConexion As SDACA_InformacionConexion, ByRef pUsuario As SDACA_Usuario,
                         ByRef pRegistro As SDACA_Registro, ByRef pConfig As SDACA_CONFIG) As Boolean Implements IModuloSdaca.Init
        Autorizado = False
        If LicenciaExtensiones.ClaveValida(Codigo, pClave) Then
            Autorizado = True
            ' VB6 copia los UDT (Conexion = pConexion). Los parámetros llegan ByRef, pero el
            ' módulo no los modifica: trabaja sobre su copia, igual que el original.
            Conexion = pConexion
            Usuario = pUsuario
            Registro = pRegistro
            Config = pConfig
            Config.MOD_SIGLAS = Siglas
            Config.MOD_CODIGO = Codigo
            Config.MOD_DESCRIPCION = If(Me.GetType().Assembly.GetCustomAttribute(Of AssemblyProductAttribute)()?.Product, "")
            Config.INI_MOD = Codigo.Replace("REP", "") & ".ini"
            Config.INI_FULL_MOD = Config.INI_PATH & Config.INI_MOD
        Else
            MessageBox.Show("Clase no Registrada...", Codigo, MessageBoxButtons.OK, MessageBoxIcon.Information)
        End If
        Return Autorizado
    End Function

    Public Function Preferencias() As Object Implements IModuloSdaca.Preferencias
        Return Nothing
    End Function

    ''' <summary>
    ''' Igual que SD_LoadRes de VB6: escribe el recurso en SDACADIR\Icons\&lt;codigo&gt;&lt;id&gt;.gif
    ''' (si no existe) y lo abre con OLE. Devuelve Nothing si algo falla.
    ''' </summary>
    ''' <remarks>
    ''' El menú HTML usa el archivo .gif, aunque su contenido sea PNG. LoadPicture (OLE) no abre
    ''' PNG, así que en ese caso el original devuelve Nothing; aquí también. VB6 escribe el
    ''' recurso con el relleno a 4 bytes que trae dentro de la DLL; se reproduce para que el
    ''' archivo quede idéntico.
    ''' </remarks>
    Public Function SD_LoadRes(pKey As Integer) As Object Implements IModuloSdaca.SD_LoadRes
        If Not Autorizado Then Return Nothing
        Try
            Dim carpeta = Path.Combine(If(Conexion.SDACADIR, ""), "Icons")
            If Not Directory.Exists(carpeta) Then
                ' MkDir de VB6 no crea la carpeta padre: si SDACADIR no existe, falla.
                If Not Directory.Exists(Conexion.SDACADIR) Then Return Nothing
                Directory.CreateDirectory(carpeta)
            End If
            Dim datos = LeerRecurso(pKey)
            If datos Is Nothing Then Return Nothing

            Dim archivo = Path.Combine(carpeta, Codigo & pKey & ".gif")
            If Not File.Exists(archivo) Then
                Dim relleno = (4 - datos.Length Mod 4) Mod 4
                Using fs As New FileStream(archivo, FileMode.CreateNew, FileAccess.Write)
                    fs.Write(datos, 0, datos.Length)
                    If relleno > 0 Then fs.Write(New Byte(relleno - 1) {}, 0, relleno)
                End Using
            End If
            Return CargarImagenOle(archivo)
        Catch
            Return Nothing
        End Try
    End Function

    Private Function LeerRecurso(id As Integer) As Byte()
        Using s = Me.GetType().Assembly.GetManifestResourceStream("Recurso." & id)
            If s Is Nothing Then Return Nothing
            Using ms As New MemoryStream()
                s.CopyTo(ms)
                Return ms.ToArray()
            End Using
        End Using
    End Function

    Private Shared ReadOnly IID_IPictureDisp As New Guid("7BF80981-BF32-101A-8BBB-00AA00300CAB")

    <DllImport("oleaut32.dll", CharSet:=CharSet.Unicode, PreserveSig:=True)>
    Private Shared Function OleLoadPicturePath(szURLorPath As String, punkCaller As IntPtr, dwReserved As Integer, clrReserved As Integer,
                                               ByRef riid As Guid, <MarshalAs(UnmanagedType.IDispatch)> <Out> ByRef ppvRet As Object) As Integer
    End Function

    Private Shared Function CargarImagenOle(archivo As String) As Object
        Dim iid = IID_IPictureDisp
        Dim pic As Object = Nothing
        Dim hr = OleLoadPicturePath(archivo, IntPtr.Zero, 0, 0, iid, pic)
        Return If(hr >= 0, pic, Nothing)
    End Function

End Class
