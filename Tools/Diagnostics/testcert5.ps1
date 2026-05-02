$filePath = 'e:\Empresas\GMV\Proyectos Antigravity\URL WPT\20260304-2025380-S5JCDCD14.p12'
$bytes = [System.IO.File]::ReadAllBytes($filePath)

Add-Type -AssemblyName System.Data
$connStr = 'Server=WPT-DEVSERVER;Database=WPTcomercial;User Id=sa;Password=zeus;TrustServerCertificate=True;'
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()

$cmd = $conn.CreateCommand()
$cmd.CommandText = 'UPDATE Nucleo SET NucleoCertificadoDigital = @cert WHERE NucleoRNC = ''131215912'''
$param = $cmd.CreateParameter()
$param.ParameterName = '@cert'
$param.SqlDbType = [System.Data.SqlDbType]::VarBinary
$param.Size = -1
$param.Value = $bytes
$cmd.Parameters.Add($param) | Out-Null

$rows = $cmd.ExecuteNonQuery()
Write-Host "Filas actualizadas: $rows"

$conn.Close()
