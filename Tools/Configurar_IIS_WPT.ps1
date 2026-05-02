param(
    [string]$InstanceName = "",
    [string]$PhysicalPath = ""
)

# Script para configurar IIS - WPTExecutor y eXcomercial
# EJECUTAR COMO ADMINISTRADOR

Import-Module WebAdministration -ErrorAction SilentlyContinue

$siteName = "cloud.wptsoftwares.net"
$basePath = "e:\Empresas\GMV\Proyectos Antigravity\URL WPT"

Write-Host "--- Configurando IIS para WPT ---" -ForegroundColor Cyan

# Definir ruta de appcmd
$appcmd = "C:\Windows\System32\inetsrv\appcmd.exe"

# Función para procesar una instancia usando appcmd
function Configure-WptInstance {
    param($name, $path)
    
    if (-not $path) { $path = Join-Path $basePath $name }
    
    Write-Host "Configurando instancia: $name en $path..." -ForegroundColor White

    # Intentar añadir la aplicación (si falla porque ya existe, la actualizamos)
    & $appcmd add app /site.name:"$siteName" /path:"/$name" /physicalPath:"$path" /applicationPool:"$siteName" 2>$null
    
    if ($LASTEXITCODE -ne 0) {
        # Si ya existe, actualizamos su ruta física
        & $appcmd set app "$siteName/$name" /physicalPath:"$path"
        Write-Host "[OK] Aplicación $name actualizada con appcmd." -ForegroundColor Green
    } else {
        Write-Host "[OK] Aplicación $name creada con appcmd." -ForegroundColor Green
    }
}

# Si se pasó una instancia específica
if ($InstanceName -ne "") {
    Configure-WptInstance -name $InstanceName -path $PhysicalPath
} else {
    # Por defecto configurar ambas
    Configure-WptInstance -name "WPTExecutor"
    Configure-WptInstance -name "eXcomercial"
}

Write-Host "`n--- Proceso Finalizado ---" -ForegroundColor Cyan
