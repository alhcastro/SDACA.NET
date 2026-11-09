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
Name: "permisos"; Description: "Dar permiso de modificar a los usuarios del equipo sobre la carpeta de SDACA (hace falta para guardar conexiones y para SDACAERP sin elevar)"; GroupDescription: "SDACA:"; Check: HaySdaca

[Dirs]
; En un equipo sin SDACA la carpeta Bin no existe, y CoopeNet, que corre sin elevar, no puede crearla en Program
; Files para guardar las conexiones. Se crea aquí con permiso de modificar y se conserva al desinstalar.
Name: "{code:CarpetaBin}"; Permissions: users-modify; Flags: uninsneveruninstall; Check: FaltaBin

[Files]
Source: "{#Publicado}\*"; DestDir: "{app}"; Excludes: "*.pdb,*.xml"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#Producto}"; Filename: "{app}\{#Ejecutable}"; Parameters: "{code:Parametros}"; WorkingDir: "{app}"
Name: "{autodesktop}\{#Producto}"; Filename: "{app}\{#Ejecutable}"; Parameters: "{code:Parametros}"; WorkingDir: "{app}"; Tasks: escritorio

[Run]
; *S-1-5-32-545 es el grupo Usuarios con cualquier idioma de Windows. /C sigue aunque un archivo esté en uso.
Filename: "{sys}\icacls.exe"; Parameters: """{code:CarpetaSdaca}"" /grant *S-1-5-32-545:(OI)(CI)M /T /C /Q"; Flags: runhidden waituntilterminated; StatusMsg: "Dando permisos sobre la carpeta de SDACA..."; Tasks: permisos
Filename: "{app}\{#Ejecutable}"; Parameters: "{code:Parametros}"; Description: "Abrir {#Producto}"; Flags: nowait postinstall skipifsilent

[Code]
var
  PaginaSdaca: TInputDirWizardPage;

{ La carpeta de los .ini de SDACA que CoopeNet usa si no recibe -inipath (Programa.DirectorioIni). }
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
  PaginaSdaca := CreateInputDirPage(wpSelectDir, 'Carpeta de SDACA',
    '¿Dónde están los archivos de conexión de SDACA?',
    'CoopeNet usa los mismos perfiles de conexión (sdcnnNNN.ini) y el CONFIG.INI que SDACA ERP. ' +
    'Indique la carpeta Bin de SDACA y haga clic en Siguiente.', False, '');
  PaginaSdaca.Add('Carpeta Bin de SDACA:');
  { /BIN="carpeta" en la línea de comandos manda sobre lo recordado: sirve para instalar sin preguntas. }
  PaginaSdaca.Values[0] := ExpandConstant('{param:BIN|' + GetPreviousData('CarpetaBin', BinPredeterminado) + '}');
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  if CurPageID <> PaginaSdaca.ID then Exit;
  if not DirExists(PaginaSdaca.Values[0]) then
    Result := MsgBox('La carpeta ' + PaginaSdaca.Values[0] + ' no existe.' + #13#10#13#10 +
      'Se creará con permiso de modificar para los usuarios del equipo, y las conexiones se crearán en ella con ' +
      '"Administrar conexiones". ¿Desea continuar?', mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDYES
  else if not TieneConexiones(PaginaSdaca.Values[0]) then
    Result := MsgBox('No se encontró ningún archivo sdcnn*.ini en ' + PaginaSdaca.Values[0] + '.' + #13#10#13#10 +
      'Sin él no habrá perfiles de conexión para elegir al entrar; se podrán crear con "Administrar conexiones". ' +
      '¿Desea continuar con esta carpeta?', mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDYES;
end;

procedure RegisterPreviousData(PreviousDataKey: Integer);
begin
  SetPreviousData(PreviousDataKey, 'CarpetaBin', PaginaSdaca.Values[0]);
end;

function Bin: String;
begin
  Result := RemoveBackslashUnlessRoot(PaginaSdaca.Values[0]);
end;

{ Solo hace falta -inipath cuando SDACA no está en la carpeta de siempre. }
function Parametros(Param: String): String;
begin
  if CompareText(Bin, BinPredeterminado) = 0 then
    Result := ''
  else
    Result := '-inipath "' + Bin + '"';
end;

{ Los permisos van sobre la carpeta de SDACA (la que contiene Bin), como se resolvió el error 75 de SDACAERP. }
function CarpetaSdaca(Param: String): String;
begin
  Result := ExtractFileDir(Bin);
end;

function CarpetaBin(Param: String): String;
begin
  Result := Bin;
end;

function FaltaBin: Boolean;
begin
  Result := not DirExists(Bin);
end;

function HaySdaca: Boolean;
begin
  Result := DirExists(Bin) and DirExists(ExtractFileDir(Bin));
end;
