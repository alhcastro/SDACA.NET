; Instalador de CoopeNet ERP. Se compila con Construir-Instalador.ps1, que publica la aplicación
; (autocontenida, con .NET incluido) y le pasa la versión y la carpeta publicada.

#ifndef Version
  #define Version "1.0.0"
#endif
#ifndef Publicado
  #define Publicado "..\artefactos\publicado"
#endif

#define Producto "CoopeNet ERP"
#define Ejecutable "CoopeNet.exe"

[Setup]
; No cambiar AppId: con él Windows reconoce las actualizaciones de la misma aplicación.
AppId={{6B0F4E2A-3C9D-4A57-9E1B-2D7C8F5A1E34}
AppName={#Producto}
AppVersion={#Version}
AppVerName={#Producto} {#Version}
AppPublisher=Desarrollo Integral CP
VersionInfoVersion={#Version}
VersionInfoProductName={#Producto}
DefaultDirName={autopf}\CoopeNet
DefaultGroupName={#Producto}
DisableProgramGroupPage=yes
UninstallDisplayName={#Producto}
UninstallDisplayIcon={app}\{#Ejecutable}
SetupIconFile=..\src\CoopeNet.Erp\CoopeNet.ico
OutputDir=..\artefactos\instalador
OutputBaseFilename=CoopeNet-{#Version}-Instalador
Compression=lzma2/ultra64
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
PrivilegesRequired=admin
WizardStyle=modern
; Programa.vb abre el mutex "CoopeNet.Erp": si la aplicación está abierta, el instalador pide cerrarla.
AppMutex=CoopeNet.Erp
CloseApplications=yes

[Languages]
Name: "es"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "escritorio"; Description: "Crear un acceso directo en el escritorio"; GroupDescription: "Accesos directos:"

[Dirs]
; Los .ini de CoopeNet (conexiones, sdconfig.ini, CONFIG.INI y temp), de todos los usuarios del equipo. CoopeNet
; corre sin elevar, por eso la carpeta lleva permiso de modificar. Se conserva al desinstalar para no perder las
; conexiones. CarpetaDatos.Predeterminada en CoopeNet.Core debe apuntar al mismo lugar.
Name: "{commonappdata}\CoopeNet"; Permissions: users-modify; Flags: uninsneveruninstall

[Files]
Source: "{#Publicado}\*"; DestDir: "{app}"; Excludes: "*.pdb,*.xml"; Flags: ignoreversion recursesubdirs createallsubdirs
; La primera vez, CoopeNet empieza con las conexiones y la configuración de SDACA. Nunca reemplaza las suyas ni
; escribe en la carpeta de SDACA. Si el instalador no las encuentra, CoopeNet intenta lo mismo al iniciar.
Source: "{code:CarpetaBin}\sdcnn*.ini"; DestDir: "{commonappdata}\CoopeNet"; Flags: external onlyifdoesntexist skipifsourcedoesntexist uninsneveruninstall
Source: "{code:CarpetaBin}\sdconfig.ini"; DestDir: "{commonappdata}\CoopeNet"; Flags: external onlyifdoesntexist skipifsourcedoesntexist uninsneveruninstall
Source: "{code:CarpetaSdaca}\CONFIG.INI"; DestDir: "{commonappdata}\CoopeNet"; Flags: external onlyifdoesntexist skipifsourcedoesntexist uninsneveruninstall

[Icons]
Name: "{autoprograms}\{#Producto}"; Filename: "{app}\{#Ejecutable}"; WorkingDir: "{app}"
Name: "{autodesktop}\{#Producto}"; Filename: "{app}\{#Ejecutable}"; WorkingDir: "{app}"; Tasks: escritorio

[Run]
Filename: "{app}\{#Ejecutable}"; Description: "Abrir {#Producto}"; Flags: nowait postinstall skipifsilent

[Code]
var
  PaginaSdaca: TInputDirWizardPage;

{ Donde instala SDACA ERP sus .ini (CarpetaDatos.BinSdaca en CoopeNet.Core). }
function BinPredeterminado: String;
begin
  Result := ExpandConstant('{commonpf32}\SDACA\Bin');
end;

function TieneConexiones(Carpeta: String): Boolean;
var
  Busqueda: TFindRec;
begin
  Result := FindFirst(AddBackslash(Carpeta) + 'sdcnn*.ini', Busqueda);
  if Result then FindClose(Busqueda);
end;

procedure InitializeWizard;
begin
  PaginaSdaca := CreateInputDirPage(wpSelectDir, 'Conexiones de SDACA',
    '¿De dónde se copian las conexiones la primera vez?',
    'CoopeNet guarda sus propias conexiones y configuración en ' + ExpandConstant('{commonappdata}\CoopeNet') + '. ' +
    'Si SDACA ERP está instalado, la primera vez se copian de su carpeta Bin; después cada sistema tiene las suyas. ' +
    'Indique la carpeta Bin de SDACA y haga clic en Siguiente.', False, '');
  PaginaSdaca.Add('Carpeta Bin de SDACA:');
  { /BIN="carpeta" en la línea de comandos manda sobre lo recordado: sirve para instalar sin preguntas. }
  PaginaSdaca.Values[0] := ExpandConstant('{param:BIN|' + GetPreviousData('CarpetaBin', BinPredeterminado) + '}');
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  if (CurPageID = PaginaSdaca.ID) and not TieneConexiones(PaginaSdaca.Values[0]) then
    Result := MsgBox('No se encontró ningún archivo sdcnn*.ini en ' + PaginaSdaca.Values[0] + '.' + #13#10#13#10 +
      'No se copiará ninguna conexión: se crearán en CoopeNet con "Administrar conexiones". ' +
      '¿Desea continuar?', mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDYES;
end;

procedure RegisterPreviousData(PreviousDataKey: Integer);
begin
  SetPreviousData(PreviousDataKey, 'CarpetaBin', PaginaSdaca.Values[0]);
end;

function CarpetaBin(Param: String): String;
begin
  Result := RemoveBackslashUnlessRoot(PaginaSdaca.Values[0]);
end;

{ CONFIG.INI está en la carpeta de SDACA, la que contiene Bin. }
function CarpetaSdaca(Param: String): String;
begin
  Result := ExtractFileDir(CarpetaBin(''));
end;
