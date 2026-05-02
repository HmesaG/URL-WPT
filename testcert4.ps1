$filePath = 'e:\Empresas\GMV\Proyectos Antigravity\URL WPT\20260304-2025380-S5JCDCD14.p12'
$pass = 'FAVILA2421'

if (Test-Path $filePath) {
    $bytes = [System.IO.File]::ReadAllBytes($filePath)
    Write-Host "Bytes en disco: $($bytes.Length)"
    
    try {
        $cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($bytes, $pass)
        Write-Host "Exito: $($cert.Subject)"
        
        # Comparar con DB
        Add-Type -AssemblyName System.Data
        $connStr = 'Server=WPT-DEVSERVER;Database=WPTcomercial;User Id=sa;Password=zeus;TrustServerCertificate=True;'
        $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
        $conn.Open()
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = 'SELECT DATALENGTH(NucleoCertificadoDigital) FROM Nucleo WHERE NucleoRNC = ''131215912'''
        $dbBytesLen = $cmd.ExecuteScalar()
        Write-Host "Bytes en DB para 131215912: $dbBytesLen"
        $conn.Close()
        
    } catch {
        Write-Host "ERROR CRIPTOGRAFICO: $($_.Exception.InnerException.Message)"
    }
} else {
    Write-Host "El archivo no existe en esa ruta."
}
