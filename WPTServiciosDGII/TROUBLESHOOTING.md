# TROUBLESHOOTING — WPT Servicios DGII

## Error: "Tenant no encontrado"

**Síntoma:** `400 - El tenant de BD 'X' no está registrado.`

**Causa:** El header `X-Db-Tenant` apunta a un tenant que no existe en `ExternalDbConfig:Tenants`.

**Solución:**
1. Verificar tenants disponibles: `GET /api/admin/tenants`
2. Registrar el tenant: `POST /api/admin/register-db`
3. O agregar en `appsettings.json` bajo `ExternalDbConfig:Tenants`

---

## Error: "No se pudo cargar el certificado .p12"

**Síntoma:** `400 - Fail-Fast: No se pudo cargar el certificado .p12.`

**Causas posibles:**
- Contraseña incorrecta en la columna `PasswordCertificado` de la tabla Nucleo
- Archivo .p12 corrupto
- El archivo fue movido o eliminado

**Solución:**
1. Validar el certificado directamente: `POST /api/admin/validar-certificado`
2. Verificar que la ruta en la columna `RutaCertificado` sea absoluta y accesible por el usuario del servidor IIS
3. Verificar permisos de lectura del archivo .p12 para el usuario de la app pool

---

## Error: "SQL Server no encontrado"

**Síntoma:** `500 - A network-related or instance-specific error occurred while establishing a connection to SQL Server`

**Causas posibles:**
- La cadena de conexión del tenant apunta a un servidor incorrecto
- El servidor SQL no tiene conexiones remotas habilitadas
- Firewall bloqueando el puerto 1433

**Solución:**
1. Verificar la cadena de conexión con `GET /api/admin/tenants` (muestra descripción, no la cadena)
2. Registrar el tenant con la cadena correcta: `POST /api/admin/register-db`
3. Probar conectividad: `Test-NetConnection -ComputerName MI_SERVIDOR -Port 1433`

---

## Error: "El RNCEmisor no está activo en el sistema"

**Síntoma:** `400 - El RNCEmisor 'XXXXXXXXX' no está activo en el sistema.`

**Causas posibles:**
- El RNC no existe en la tabla Nucleo de la BD externa
- El estado del registro no es el valor activo (`A` por defecto)
- El XML del e-CF no contiene el campo `RNCEmisor`

**Solución:**
1. Verificar el RNC directamente en la BD: `SELECT * FROM Nucleo WHERE Rnc = 'XXXXXXXXX'`
2. Si el estado es incorrecto, actualizar en la BD o cambiar `NucleoConfig:EstadoActivo` en appsettings
3. Si el XML no tiene RNCEmisor, revisar la estructura del e-CF

---

## Error: "Certificado expirado"

**Síntoma:** `400 - El certificado expiró el YYYY-MM-DD.`

**Solución:**
1. Obtener nuevo certificado .p12 de la DGII
2. Actualizar la ruta y contraseña en la tabla Nucleo para el RNC correspondiente
3. No es necesario reiniciar la app — el siguiente request usará el nuevo certificado

---

## Error: Locks en dotnet build

**Síntoma:** `Access to the path '...WPTServiciosDGII.exe' is denied. The file is locked by: "WPTServiciosDGII (XXXXX)"`

**Solución:**
```powershell
Stop-Process -Id XXXXX -Force
dotnet build
```

---

## Configuración de columnas de Nucleo

Si los nombres reales de las columnas difieren de los genéricos, editar `appsettings.json`:

```json
"NucleoConfig": {
    "TableName": "Nucleo",
    "ColumnRnc": "NombreRealColumnaRnc",
    "ColumnEstado": "NombreRealColumnaEstado",
    "ColumnRutaCertificado": "NombreRealColumnaRuta",
    "ColumnPasswordCertificado": "NombreRealColumnaPassword",
    "EstadoActivo": "A"
}
```

> ✅ No requiere recompilar — solo reiniciar la aplicación.
