# Script para configurar IIS - WPTExecutor y eXcomercial
# EJECUTAR COMO ADMINISTRADOR

Import-Module WebAdministration

$siteName = "cloud.wptsoftwares.net"
$basePath = "e:\Empresas\GMV\Proyectos Antigravity\URL WPT"

Write-Host "--- Configurando IIS para WPT ---" -ForegroundColor Cyan

# 1. Actualizar WPTexecutor
$wptPath = Join-Path $basePath "WPTExecutor"
if (Test-Path "IIS:\Sites\$siteName\WPTexecutor") {
    Set-ItemProperty "IIS:\Sites\$siteName\WPTexecutor" -name physicalPath -value $wptPath
    Write-Host "[OK] WPTexecutor actualizado a: $wptPath" -ForegroundColor Green
} else {
    Write-Host "[!] No se encontró la aplicación WPTexecutor en el sitio $siteName" -ForegroundColor Yellow
}

# 2. Crear o actualizar eXcomercial
$exPath = Join-Path $basePath "eXcomercial"
if (-not (Test-Path "IIS:\Sites\$siteName\eXcomercial")) {
    New-WebApplication -Name "eXcomercial" -Site $siteName -PhysicalPath $exPath -ApplicationPool $siteName
    Write-Host "[OK] Aplicación eXcomercial creada en: $exPath" -ForegroundColor Green
} else {
    Set-ItemProperty "IIS:\Sites\$siteName\eXcomercial" -name physicalPath -value $exPath
    Write-Host "[OK] eXcomercial ya existía, ruta actualizada a: $exPath" -ForegroundColor Green
}

Write-Host "`n--- Proceso Finalizado ---" -ForegroundColor Cyan
Pause
