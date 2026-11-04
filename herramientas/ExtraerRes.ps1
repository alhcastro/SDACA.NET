param(
  [Parameter(Mandatory)][string]$Res,     # archivo .RES de VB6
  [Parameter(Mandatory)][string]$Salida,  # carpeta donde dejar los recursos
  [string]$Tipo = 'CUSTOM'                # tipo de recurso que usa LoadResData(id, "CUSTOM")
)
# Extrae los recursos de un .RES de 32 bits. Cada uno se guarda como <id>.<ext> según su contenido.
$b = [IO.File]::ReadAllBytes($Res)
New-Item -ItemType Directory -Force $Salida | Out-Null
function Leer-Id([byte[]]$b, [ref]$p) {
  if ([BitConverter]::ToUInt16($b, $p.Value) -eq 0xFFFF) { $v = [BitConverter]::ToUInt16($b, $p.Value + 2); $p.Value += 4; return "$v" }
  $s = New-Object Text.StringBuilder
  while (($c = [BitConverter]::ToUInt16($b, $p.Value)) -ne 0) { [void]$s.Append([char]$c); $p.Value += 2 }
  $p.Value += 2; return $s.ToString()
}
$pos = 0; $n = 0
while ($pos + 8 -le $b.Length) {
  $dataSize = [BitConverter]::ToUInt32($b, $pos); $headerSize = [BitConverter]::ToUInt32($b, $pos + 4)
  if ($headerSize -lt 16) { break }
  $p = $pos + 8
  $t = Leer-Id $b ([ref]$p); $nombre = Leer-Id $b ([ref]$p)
  $dataStart = $pos + $headerSize
  if ($t -eq $Tipo -and $dataSize -gt 0) {
    $datos = New-Object byte[] $dataSize; [Array]::Copy($b, $dataStart, $datos, 0, $dataSize)
    $ext = if ($datos[0] -eq 0x47 -and $datos[1] -eq 0x49) { 'gif' } elseif ($datos[0] -eq 0x89 -and $datos[1] -eq 0x50) { 'png' } elseif ($datos[0] -eq 0 -and $datos[2] -eq 1) { 'ico' } else { 'bin' }
    [IO.File]::WriteAllBytes((Join-Path $Salida "$nombre.$ext"), $datos); $n++
    "{0,-6} {1,-4} {2,7} bytes" -f $nombre, $ext, $dataSize
  }
  $pos = $dataStart + $dataSize; $pos = ($pos + 3) -band (-bnot 3)
}
"Recursos '$Tipo' extraídos: $n"
