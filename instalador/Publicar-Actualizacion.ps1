<#
.SYNOPSIS
    Publica una versión de CoopeNet en el servidor de actualizaciones: los equipos la bajan al abrir CoopeNet.

.DESCRIPTION
    Corre las pruebas, publica CoopeNet autocontenido (igual que Construir-Instalador.ps1) y lo sube con
    herramientas\Publicar al catálogo coopenet_actualizaciones. Solo sube los archivos que no estén ya en el
    servidor. La versión es <Version> de Directory.Build.props: súbala antes de publicar.

    Por omisión la versión queda en el canal piloto. Cuando esté probada en los equipos piloto:
        dotnet run --project herramientas\Publicar -- liberar <versión>
    Si sale con fallas:
        dotnet run --project herramientas\Publicar -- retirar <versión>

.PARAMETER Notas
    Qué trae la versión, para el listado.

.PARAMETER General
    La publica directo para todos los equipos, sin pasar por piloto.

.EXAMPLE
    .\instalador\Publicar-Actualizacion.ps1 -Notas "SD0502: Proveedores"
#>
[CmdletBinding()]
param(
    [string]$Notas = '',
    [switch]$General,
    [switch]$SinPruebas
)

$ErrorActionPreference = 'Stop'
$raiz = Split-Path -Parent $PSScriptRoot

[xml]$props = Get-Content (Join-Path $raiz 'Directory.Build.props') -Encoding UTF8
$version = ($props.Project.PropertyGroup | ForEach-Object { $_.Version } | Where-Object { $_ }) | Select-Object -First 1
if (-not $version) { throw 'No se encontró <Version> en Directory.Build.props.' }
$publicado = Join-Path $raiz "artefactos\actualizacion\$version"
$canal = if ($General) { 'general' } else { 'piloto' }

Write-Host "CoopeNet ERP $version, canal $canal" -ForegroundColor Cyan

if (-not $SinPruebas) {
    Write-Host 'Pruebas...'
    dotnet test (Join-Path $raiz 'tests\CoopeNet.Core.Tests') -c Release --nologo -v quiet
    if ($LASTEXITCODE -ne 0) { throw 'Las pruebas fallaron: no se publica.' }
}

Write-Host 'Publicando...'
if (Test-Path $publicado) { Remove-Item $publicado -Recurse -Force }
dotnet publish (Join-Path $raiz 'src\CoopeNet.Erp\CoopeNet.Erp.vbproj') -c Release -r win-x64 --self-contained true -o $publicado --nologo -v quiet
if ($LASTEXITCODE -ne 0) { throw 'La publicación falló.' }

Write-Host 'Subiendo al servidor...'
dotnet run --project (Join-Path $raiz 'herramientas\Publicar') -c Release -- publicar $publicado $version --canal $canal --notas $Notas
if ($LASTEXITCODE -ne 0) { throw 'No se pudo subir la versión.' }
