param(
  # Carpeta con el componente compilado (SDACA.Com.SDxxxx.dll y sus dependencias)
  [Parameter(Mandatory)][string]$Carpeta,
  [Parameter(Mandatory)][string]$Dll,           # ej. SDACA.Com.SD1701.dll
  [switch]$Desregistrar
)
# Registra un módulo COM .NET solo para el usuario actual (HKCU), sin permisos de administrador.
# HKCU tiene prioridad sobre HKLM, así que el menú VB6 de este usuario carga la versión .NET y
# los demás usuarios del equipo siguen con la de VB6. Desregistrar devuelve todo como estaba.
$ErrorActionPreference = 'Stop'
$regasm = "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\RegAsm.exe"   # 32 bits, como el menú
$tlbexp = "${env:ProgramFiles(x86)}\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8 Tools\TlbExp.exe"
$ruta = Join-Path $Carpeta $Dll
$registro = Join-Path $Carpeta ($Dll + '.registro.json')

Add-Type -TypeDefinition @"
using System; using System.Runtime.InteropServices; using System.Runtime.InteropServices.ComTypes;
namespace SDACA {
  public static class Tlb {
    [DllImport("oleaut32.dll", CharSet = CharSet.Unicode)] static extern int LoadTypeLibEx(string file, int regKind, out ITypeLib lib);
    [DllImport("oleaut32.dll", CharSet = CharSet.Unicode)] static extern int RegisterTypeLibForUser(ITypeLib lib, string path, string helpDir);
    [DllImport("oleaut32.dll")] static extern int UnRegisterTypeLibForUser(ref Guid libId, short major, short minor, int lcid, int sysKind);
    // Registra la typelib para el usuario y devuelve "guid|mayor|menor".
    public static string Registrar(string tlb, string carpeta) {
      ITypeLib lib; int hr = LoadTypeLibEx(tlb, 0, out lib); if (hr != 0) Marshal.ThrowExceptionForHR(hr);
      hr = RegisterTypeLibForUser(lib, tlb, carpeta); if (hr != 0) Marshal.ThrowExceptionForHR(hr);
      IntPtr p; lib.GetLibAttr(out p);
      var a = (System.Runtime.InteropServices.ComTypes.TYPELIBATTR)Marshal.PtrToStructure(p, typeof(System.Runtime.InteropServices.ComTypes.TYPELIBATTR)); lib.ReleaseTLibAttr(p);
      return a.guid.ToString("B") + "|" + a.wMajorVerNum + "|" + a.wMinorVerNum;
    }
    public static int Quitar(string guid, short mayor, short menor) { var g = new Guid(guid); return UnRegisterTypeLibForUser(ref g, mayor, menor, 0, 1); /* SYS_WIN32 */ }
  }
}
"@

function Convertir-RegAHkcu([string]$archivo) {
  $t = Get-Content $archivo -Raw
  $t -replace '\[HKEY_CLASSES_ROOT\\', '[HKEY_CURRENT_USER\Software\Classes\' -replace '\[-HKEY_CLASSES_ROOT\\', '[-HKEY_CURRENT_USER\Software\Classes\'
}
function Llaves([string]$texto) {
  [regex]::Matches($texto, '(?m)^\[HKEY_CURRENT_USER\\Software\\Classes\\([^\]]+)\]') | ForEach-Object { $_.Groups[1].Value } |
    ForEach-Object { ($_ -split '\\')[0..1] -join '\' } | Sort-Object -Unique
}

if ($Desregistrar) {
  if (-not (Test-Path $registro)) { "No hay registro previo en $registro"; return }
  $r = Get-Content $registro -Raw | ConvertFrom-Json
  foreach ($k in $r.Llaves) {
    foreach ($vista in '/reg:32', '/reg:64') { cmd.exe /c "reg delete `"HKCU\Software\Classes\$k`" /f $vista >nul 2>nul" }
  }
  if ($r.TypeLib) { [void][SDACA.Tlb]::Quitar($r.TypeLib, [int16]$r.Major, [int16]$r.Minor) }
  Remove-Item $registro
  "Desregistrado: $($r.Llaves.Count) llaves y la typelib. El menú vuelve a usar la versión VB6."
  return
}

$tmp = New-Item -ItemType Directory -Force (Join-Path $env:TEMP "sdaca_reg_$([guid]::NewGuid().ToString('N'))")
try {
  # 1. Clases, ProgID y Record del módulo
  cmd.exe /c "`"$regasm`" `"$ruta`" /codebase /regfile:`"$tmp\modulo.reg`" /nologo >nul 2>nul"; if (-not (Test-Path "$tmp\modulo.reg")) { throw "RegAsm no generó el .reg del módulo" }
  $modulo = Convertir-RegAHkcu "$tmp\modulo.reg"

  # 2. Solo los Record de los UDT de SDBASE00 (para recibir Conexion, Usuario, Registro y CONFIG en Init)
  $interop = Join-Path $Carpeta 'Interop.SDBASE00.dll'; cmd.exe /c "`"$regasm`" `"$interop`" /codebase /regfile:`"$tmp\sdbase00.reg`" /nologo >nul 2>nul"; if (-not (Test-Path "$tmp\sdbase00.reg")) { throw "RegAsm no generó el .reg de SDBASE00" }
  $base = Convertir-RegAHkcu "$tmp\sdbase00.reg"
  $bloques = [regex]::Split($base, '(?m)(?=^\[)') | Where-Object { $_ -match '^\[HKEY_CURRENT_USER\\Software\\Classes\\Record\\' }
  $base = "Windows Registry Editor Version 5.00`r`n`r`n" + ($bloques -join '')

  $todo = $modulo + "`r`n" + ($base -replace '^Windows Registry Editor Version 5.00\s*', '')
  [IO.File]::WriteAllText("$tmp\usuario.reg", $todo, [Text.Encoding]::Unicode)
  cmd.exe /c "reg import `"$tmp\usuario.reg`" /reg:32 >nul 2>nul"
  if ($LASTEXITCODE -ne 0) { throw "reg import falló ($LASTEXITCODE)" }

  # 3. Typelib del módulo (los UDT SD_Clase y SD_InfoMenu que el menú lee por nombre)
  $tlb = [IO.Path]::ChangeExtension($ruta, '.tlb')
  cmd.exe /c "`"$tlbexp`" `"$ruta`" /out:`"$tlb`" /win32 /nologo >nul 2>nul"; if (-not (Test-Path $tlb)) { throw "TlbExp no generó la typelib" }
  $info = [SDACA.Tlb]::Registrar($tlb, $Carpeta).Split('|')
  [pscustomobject]@{ Dll = $ruta; Fecha = (Get-Date).ToString('s'); Llaves = @(Llaves $todo); TypeLib = $info[0]; Major = [int]$info[1]; Minor = [int]$info[2] } |
    ConvertTo-Json | Set-Content $registro -Encoding UTF8
  "Registrado para ${env:USERNAME}: $Dll"
  (Get-Content $registro -Raw | ConvertFrom-Json).Llaves | ForEach-Object { "  HKCU\Software\Classes\$_" }
}
finally { Remove-Item $tmp -Recurse -Force -ErrorAction SilentlyContinue }
