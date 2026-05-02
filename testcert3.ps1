Add-Type -AssemblyName System.Data
$connStr = 'Server=WPT-DEVSERVER;Database=WPTcomercial;User Id=sa;Password=zeus;TrustServerCertificate=True;'
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = 'SELECT NucleoCertificadoDigital, NucleoPasswordDigital FROM Nucleo WHERE NucleoRNC = ''130252181'''
$reader = $cmd.ExecuteReader()
if ($reader.Read()) {
    $bytes = $reader.GetValue(0)
    $pass = $reader.GetString(1)
    Write-Host "Bytes leidos (130252181): $($bytes.Length), Pass: $pass"
    try {
        $cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($bytes, $pass)
        Write-Host "Exito: $($cert.Subject)"
    } catch {
        Write-Host "ERROR: $($_.Exception.InnerException.Message)"
    }
}
$conn.Close()
