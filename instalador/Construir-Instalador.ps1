<#
.SYNOPSIS
    Arma el instalador de CoopeNet ERP: corre las pruebas, publica la aplicación autocontenida para Windows de
    64 bits y compila instalador\CoopeNet.iss con Inno Setup 6.

.DESCRIPTION
    El resultado queda en artefactos\instalador\CoopeNet-<versión>-Instalador.exe. La versión se toma de
    <Version> en Directory.Build.props. Requiere Inno Setup 6 (winget install JRSoftware.InnoSetup).

.PARAMETER SinPruebas
    No corre dotnet test antes de publicar.

.EXAMPLE
    .\instalador\Construir-Instalador.ps1
#>
[CmdletBinding()]
param(
    [switch]$SinPruebas
)

$ErrorActionPreference = 'Stop'
$raiz = Split-Path -Parent $PSScriptRoot
$publicado = Join-Path $raiz 'artefactos\publicado'

[xml]$props = Get-Content (Join-Path $raiz 'Directory.Build.props') -Encoding UTF8
$version = ($props.Project.PropertyGroup | ForEach-Object { $_.Version } | Where-Object { $_ }) | Select-Object -First 1
if (-not $version) { throw 'No se encontró <Version> en Directory.Build.props.' }

$iscc = @(
    (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe'),
    (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'),
    (Join-Path $env:ProgramFiles 'Inno Setup 6\ISCC.exe')
) | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $iscc) { throw 'No se encontró Inno Setup 6. Instálelo con: winget install JRSoftware.InnoSetup' }

Write-Host "CoopeNet ERP $version" -ForegroundColor Cyan

if (-not $SinPruebas) {
    Write-Host 'Pruebas...'
    dotnet test (Join-Path $raiz 'tests\CoopeNet.Core.Tests') -c Release --nologo -v quiet
    if ($LASTEXITCODE -ne 0) { throw 'Las pruebas fallaron: no se arma el instalador.' }
}

Write-Host 'Publicando...'
if (Test-Path $publicado) { Remove-Item $publicado -Recurse -Force }
dotnet publish (Join-Path $raiz 'src\CoopeNet.Erp\CoopeNet.Erp.vbproj') -c Release -r win-x64 --self-contained true -o $publicado --nologo -v quiet
if ($LASTEXITCODE -ne 0) { throw 'La publicación falló.' }

Write-Host 'Compilando el instalador...'
& $iscc /Q "/DVersion=$version" "/DPublicado=$publicado" (Join-Path $PSScriptRoot 'CoopeNet.iss')
if ($LASTEXITCODE -ne 0) { throw 'Inno Setup no pudo compilar el instalador.' }

$salida = Join-Path $raiz "artefactos\instalador\CoopeNet-$version-Instalador.exe"
$tamano = '{0:N1} MB' -f ((Get-Item $salida).Length / 1MB)
Write-Host "Listo: $salida ($tamano)" -ForegroundColor Green
