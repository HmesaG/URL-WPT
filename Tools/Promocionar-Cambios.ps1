# Script de Promoción de Cambios - WPT
# Uso: .\Promocionar-Cambios.ps1 -Destino "eXcomercial"

param (
    [Parameter(Mandatory=$true)]
    [string]$Destino
)

$source = "WPTExecutor"
$basePath = "e:\Empresas\GMV\Proyectos Antigravity\URL WPT"
$sourcePath = Join-Path $basePath $source
$targetPath = Join-Path $basePath $Destino

if (-not (Test-Path $sourcePath)) {
    Write-Error "La carpeta de origen $source no existe."
    return
}

if (-not (Test-Path $targetPath)) {
    Write-Host "Creando nueva carpeta de modelo: $Destino" -ForegroundColor Cyan
    New-Item -ItemType Directory -Path $targetPath
}

Write-Host "--- Promocionando cambios de $source a $Destino ---" -ForegroundColor Cyan

# 1. Copiar todo EXCEPTO el archivo de configuración para no romper la BD del destino
Get-ChildItem $sourcePath -Exclude "appsettings.json", "appsettings.Development.json" | Copy-Item -Destination $targetPath -Recurse -Force

# 2. Si el destino es nuevo, copiar el appsettings y pedir configuración
if (-not (Test-Path (Join-Path $targetPath "appsettings.json"))) {
    Copy-Item (Join-Path $sourcePath "appsettings.json") -Destination $targetPath
    Write-Host "[!] Archivo appsettings.json creado. Recuerda configurar la instancia '$Destino' vía API." -ForegroundColor Yellow
}

Write-Host "[OK] Réplica completada con éxito." -ForegroundColor Green
