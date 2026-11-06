Imports System.Runtime.InteropServices
Imports SDBASE00

' Contrato COM que el menú SDACAERP espera de cada módulo (SDxxxx.Clase).
' Los nombres, tipos y el orden de los campos tienen que coincidir con los UDT de VB6,
' porque el menú lee los resultados por nombre con enlace tardío (iSDCLASE.Privilegio).

''' <summary>UDT SD_Clase de cada Clase.cls: datos del módulo para el menú principal.</summary>
<ComVisible(True)>
<Guid("6F0C5B1E-2D7A-4E3B-9C41-5A8B1D7E0A01")>
<StructLayout(LayoutKind.Sequential)>
Public Structure SD_Clase
    <MarshalAs(UnmanagedType.BStr)> Public Clase As String
    <MarshalAs(UnmanagedType.BStr)> Public Titulo As String
    <MarshalAs(UnmanagedType.BStr)> Public Descripcion As String
    <MarshalAs(UnmanagedType.BStr)> Public Privilegio As String
    Public Icono As Integer
    Public Picture As Integer
    <MarshalAs(UnmanagedType.BStr)> Public Reportes As String
    ''' <summary>OLE_COLOR: BGR, ej. &amp;HD9E4EE.</summary>
    Public Color As Integer
    <MarshalAs(UnmanagedType.BStr)> Public Preferencias As String
End Structure

''' <summary>UDT SD_InfoMenu: una opción del menú del módulo.</summary>
<ComVisible(True)>
<Guid("6F0C5B1E-2D7A-4E3B-9C41-5A8B1D7E0A02")>
<StructLayout(LayoutKind.Sequential)>
Public Structure SD_InfoMenu
    <MarshalAs(UnmanagedType.BStr)> Public Key As String
    <MarshalAs(UnmanagedType.BStr)> Public Etiqueta As String
    Public Icono48 As Integer
    Public Icono16 As Integer
    <MarshalAs(UnmanagedType.BStr)> Public Privilegio As String
End Structure

''' <summary>Interfaz que el menú llama con enlace tardío (IDispatch).</summary>
<ComVisible(True)>
<Guid("6F0C5B1E-2D7A-4E3B-9C41-5A8B1D7E0B01")>
<InterfaceType(ComInterfaceType.InterfaceIsIDispatch)>
Public Interface IModuloSdaca

    <DispId(1)> Function Version() As String

    <DispId(2)>
    Function Init(pClave As String,
                  ByRef pConexion As SDACA_InformacionConexion,
                  ByRef pUsuario As SDACA_Usuario,
                  ByRef pRegistro As SDACA_Registro,
                  ByRef pConfig As SDACA_CONFIG) As Boolean

    <DispId(3)> Function SDClase() As SD_Clase

    <DispId(4)> Function Menu(Index As String) As SD_InfoMenu

    <DispId(5)> Sub Accion(pKey As String)

    ''' <summary>Arreglo de Variant con las llaves "001", "001.002", "001.002.003". Termina en "" o Empty.</summary>
    <DispId(6)> Function Menu_Order() As Object

    ''' <summary>StdPicture del recurso, o Nothing.</summary>
    <DispId(7)> Function SD_LoadRes(pKey As Integer) As <MarshalAs(UnmanagedType.IDispatch)> Object

    <DispId(8)> Function Preferencias() As Object

End Interface
