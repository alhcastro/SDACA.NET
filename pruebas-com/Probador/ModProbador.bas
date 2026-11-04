Attribute VB_Name = "ModProbador"
Option Explicit

' Probador del contrato de modulos del menu SDACAERP.
' Hace las mismas llamadas que FrmMain3 (CargarModulos / Cargaropciones / loadimg)
' y escribe cada resultado en un archivo de texto para comparar VB6 contra .NET.
' Uso: Probador.exe <ProgID> <clave> <archivo de salida>

Private f As Integer

Private Sub Linea(ByVal pTexto As String)
    Print #f, pTexto
End Sub

Sub Main()
    Dim iArgs() As String
    Dim iProgId As String, iClave As String, iSalida As String
    iArgs = Split(Command$, " ")
    If UBound(iArgs) < 2 Then Exit Sub
    iProgId = iArgs(0): iClave = iArgs(1): iSalida = iArgs(2)

    f = FreeFile
    Open iSalida For Output As #f
    On Error GoTo Fallo

    Dim Conexion As SDACA_InformacionConexion
    Dim Usuario As SDACA_Usuario
    Dim Registro As SDACA_Registro
    Dim CONFIG As SDACA_CONFIG

    Conexion.Perfil = "PRUEBA"
    Conexion.Servidor = "localhost"
    Conexion.Catalogo = "pruebas"
    Conexion.Puerto = "3306"
    Conexion.SDACADIR = Environ$("TEMP") & "\sdaca_probador_" & iProgId
    On Error Resume Next
    MkDir Conexion.SDACADIR
    Kill Conexion.SDACADIR & "\Icons\*.*"
    On Error GoTo Fallo
    Set Conexion.Extra = New Collection
    Conexion.Extra.Add "Coopepalmares", "Empresa"

    Usuario.Usuario = "probador"
    Usuario.UID = 25
    Usuario.Local = 1
    Usuario.Nombre = "Nombre con tilde: Pe" & Chr$(241) & "a"
    Set Usuario.Privilegios = New Collection
    Usuario.Privilegios.Add "1", "17000"
    Set Usuario.Horario = New Collection
    Set Usuario.Extra = New Collection
    Usuario.Extra.Add "1", "SGCEmpresa"

    Registro.Cedula = "3-004-045003"
    Registro.NombreCorto = "COOPEPALMARES R.L."
    Registro.NumeroCliente = 7
    Registro.ModulosCount = 18
    Registro.Asociados = True
    Registro.FechaCompra = DateSerial(2003, 5, 1)

    CONFIG.INI_PATH = "C:\"
    CONFIG.DEBUG = False

    Dim SDClass As Variant
    Set SDClass = CreateObject(iProgId)
    Linea "CreateObject=OK"

    Linea "Init=" & SDClass.INIT(iClave, Conexion, Usuario, Registro, CONFIG)
    Linea "Init no cambia Usuario.Nombre=" & (Usuario.Nombre = "Nombre con tilde: Pe" & Chr$(241) & "a")

    Dim iSDCLASE As Variant
    iSDCLASE = SDClass.SDClase
    Linea "SDClase.TypeName=" & TypeName(iSDCLASE)
    Linea "SDClase.Clase=" & iSDCLASE.Clase
    Linea "SDClase.Titulo=" & iSDCLASE.Titulo
    Linea "SDClase.Descripcion=" & iSDCLASE.Descripcion
    Linea "SDClase.Privilegio=" & iSDCLASE.Privilegio
    Linea "SDClase.Icono=" & iSDCLASE.Icono
    Linea "SDClase.Picture=" & iSDCLASE.Picture
    Linea "SDClase.Reportes=[" & iSDCLASE.Reportes & "]"
    Linea "SDClase.Color=" & Hex$(iSDCLASE.Color)
    Linea "SDClase.Preferencias=[" & iSDCLASE.Preferencias & "]"

    Dim iMenu As Variant
    iMenu = SDClass.Menu("001")
    Linea "Menu(001)=" & iMenu.Key & "|" & iMenu.Etiqueta & "|" & iMenu.Icono48 & "|" & iMenu.Icono16 & "|" & iMenu.Privilegio
    iMenu = SDClass.Menu("999")
    Linea "Menu(999)=" & iMenu.Key & "|" & iMenu.Etiqueta & "|" & iMenu.Icono48 & "|" & iMenu.Icono16 & "|" & iMenu.Privilegio
    Linea "Menu(001).Icono48 como en loadimg=" & Right("000" & SDClass.Menu("001").Icono48, 3)

    Dim A As Variant
    A = SDClass.Menu_Order
    Linea "Menu_Order.Limites=" & LBound(A) & ".." & UBound(A)
    Linea "Menu_Order(0)=[" & A(0) & "] Empty(1)=" & IsEmpty(A(1))

    Dim iKey As String
    iKey = "000"
    SDClass.Accion iKey
    SDClass.Accion ("001")
    Linea "Accion=OK"

    Dim iPic As Variant
    Set iPic = SDClass.SD_LoadRes(CLng(101))
    Linea "SD_LoadRes(101) Is Nothing=" & (iPic Is Nothing)
    Linea "Icons\SD1701101.gif bytes=" & FileLen(Conexion.SDACADIR & "\Icons\SD1701101.gif")
    Set iPic = SDClass.SD_LoadRes(CLng(102))
    Linea "SD_LoadRes(102) Is Nothing=" & (iPic Is Nothing)

    Linea "Preferencias IsEmpty=" & IsEmpty(SDClass.Preferencias)
    Linea "Version=" & SDClass.Version

    Set SDClass = Nothing
    Linea "FIN"
    Close #f
    Exit Sub
Fallo:
    Linea "ERROR " & Err.Number & ": " & Err.Description & " (" & Err.Source & ")"
    Close #f
End Sub
