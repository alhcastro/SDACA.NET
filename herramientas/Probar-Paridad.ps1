param(
  [Parameter(Mandatory)][string]$Modulo,     # ej. SD1701
  [string]$Raiz = (Split-Path $PSScriptRoot)
)
# Compara el módulo VB6 instalado contra su versión .NET con el mismo probador VB6.
# Al terminar deja registrada la versión .NET para el usuario actual.
$ErrorActionPreference = 'Stop'
$progId = "$Modulo.Clase"
$md5 = [Security.Cryptography.MD5]::Create()
$clave = ($md5.ComputeHash([Text.Encoding]::ASCII.GetBytes($Modulo)) | ForEach-Object { $_.ToString('x2') }) -join ''
$probador = Join-Path $Raiz 'pruebas-com\Probador\Probador.exe'
$pub = Join-Path $Raiz "publicado\$Modulo"
$dll = "SDACA.Com.$Modulo.dll"
$registrar = Join-Path $PSScriptRoot 'Registrar-ComUsuario.ps1'
$salida = Join-Path $Raiz 'pruebas-com'
$icono = Join-Path $env:TEMP "sdaca_probador_$progId\Icons"

function Correr([string]$archivo) {
  $p = Start-Process $probador -ArgumentList "$progId $clave $archivo" -PassThru -WindowStyle Hidden
  if (-not $p.WaitForExit(60000)) { $p.Kill(); throw "El probador no terminó en 60 s (¿un MsgBox abierto?)" }
  $iconos = @{}
  Get-ChildItem $icono -File -ErrorAction SilentlyContinue | ForEach-Object {
    $iconos[$_.Name] = ($md5.ComputeHash([IO.File]::ReadAllBytes($_.FullName)) | ForEach-Object { $_.ToString('x2') }) -join ''
  }
  return $iconos
}

& $registrar -Carpeta $pub -Dll $dll -Desregistrar | Out-Null
$iconosVb6 = Correr (Join-Path $salida "resultado_vb6_$Modulo.txt")
& $registrar -Carpeta $pub -Dll $dll | Out-Null
$iconosNet = Correr (Join-Path $salida "resultado_net_$Modulo.txt")

$vb6 = Get-Content (Join-Path $salida "resultado_vb6_$Modulo.txt") | Where-Object { $_ -notlike 'Version=*' }
$net = Get-Content (Join-Path $salida "resultado_net_$Modulo.txt") | Where-Object { $_ -notlike 'Version=*' }
$dif = Compare-Object $vb6 $net
$difIconos = @($iconosVb6.Keys + $iconosNet.Keys | Sort-Object -Unique | Where-Object { $iconosVb6[$_] -ne $iconosNet[$_] })

"VB6:  " + ((Get-Content (Join-Path $salida "resultado_vb6_$Modulo.txt")) -match '^Version=')
".NET: " + ((Get-Content (Join-Path $salida "resultado_net_$Modulo.txt")) -match '^Version=')
"Líneas comparadas: $($vb6.Count). Diferencias: $(@($dif).Count). Iconos distintos: $($difIconos.Count) de $($iconosVb6.Count)."
if ($dif) { $dif | ForEach-Object { "  {0} {1}" -f $(if ($_.SideIndicator -eq '<=') { 'VB6 ' } else { '.NET' }), $_.InputObject } }
$difIconos | ForEach-Object { "  icono distinto: $_" }
if (-not $dif -and $difIconos.Count -eq 0 -and ($net -contains 'FIN')) { "PARIDAD OK: $Modulo .NET se comporta igual que VB6." } else { "HAY DIFERENCIAS" ; exit 1 }
