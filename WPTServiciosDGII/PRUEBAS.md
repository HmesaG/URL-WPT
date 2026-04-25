# PRUEBAS — WPT Servicios DGII (Fases 0–4)

## Prerrequisitos

- API corriendo: `dotnet run` desde `WPTServiciosDGII/`
- URL base: `http://localhost:5190`
- Herramienta: Postman, Insomnia o PowerShell

---

## 1. Health Check

```powershell
Invoke-RestMethod -Uri "http://localhost:5190/health"
```

**Respuesta esperada (200):**
```json
{
  "status": "healthy",
  "time": "2026-04-24 10:57:10",
  "tenants_registrados": ["demo"],
  "ruta_certificados": "C:\\Certificados\\WPT"
}
```

---

## 2. Listar Tenants de BD

```powershell
Invoke-RestMethod -Uri "http://localhost:5190/api/admin/tenants"
```

**Respuesta esperada (200):**
```json
{
  "tenants": { "demo": "BD de ejemplo - reemplazar con datos reales" },
  "total": 1
}
```

---

## 3. Registrar BD en Caliente (sin reiniciar)

```powershell
$body = @{
    tenantKey = "empresa_001"
    connectionString = "Server=MI_SERVIDOR;Database=MI_BD;User Id=readonly;Password=MI_PASS;TrustServerCertificate=True;"
    description = "Empresa Real S.A."
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5190/api/admin/register-db" `
    -Method POST -Body $body -ContentType "application/json"
```

**Respuesta esperada (200):**
```json
{
  "mensaje": "Tenant 'empresa_001' registrado correctamente.",
  "timestamp": "2026-04-24T12:08:08"
}
```

> ✅ El tenant queda disponible inmediatamente para nuevas peticiones sin reiniciar la app.

---

## 4. Validar Certificado .p12

```powershell
$body = @{
    ruta = "C:\Certificados\WPT\mi_empresa.p12"
    password = "mi_contraseña"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5190/api/admin/validar-certificado" `
    -Method POST -Body $body -ContentType "application/json"
```

**Respuesta OK (200):**
```json
{ "valido": true, "mensaje": "El certificado se cargó correctamente." }
```

**Respuesta Error (400):**
```json
{ "valido": false, "error": "El certificado .p12 no existe en la ruta configurada: ..." }
```

---

## 5. Enviar e-CF (flujo completo)

```powershell
$xml = '<?xml version="1.0" encoding="utf-8"?>
<eCF xmlns="urn:dgii.gov.do:ecf">
  <Encabezado>
    <Version>1.0</Version>
    <RNCEmisor>131234567</RNCEmisor>
    <RNCComprador>101000532</RNCComprador>
    <eNCF>E310000000001</eNCF>
    <FechaVencimientoSecuencia>31-12-2025</FechaVencimientoSecuencia>
  </Encabezado>
</eCF>'

# Con BD externa configurada para el emisor:
Invoke-RestMethod -Uri "http://localhost:5190/fe/recepcion/api/ecf" `
    -Method POST -Body $xml -ContentType "application/xml" `
    -Headers @{ "X-Db-Tenant" = "empresa_001" }
```

**Respuesta OK (200):** XML ARECF firmado digitalmente.

**Respuesta Error (400):** Si el RNCEmisor no está en la tabla Nucleo o el certificado falla.

---

## 6. Tabla de Resultados

| Endpoint | Método | Estado | Resultado |
|---|---|---|---|
| `/health` | GET | ✅ | Healthy con tenants y ruta de certs |
| `/api/admin/tenants` | GET | ✅ | Lista tenants sin exponer passwords |
| `/api/admin/register-db` | POST | ✅ | Registro en caliente funciona |
| `/api/admin/validar-certificado` | POST | ✅ | Carga y valida .p12 correctamente |
| `/api/admin/validar-certificado` | POST | ✅ | Fail-Fast en ruta inexistente |
| `/api/admin/validar-certificado` | POST | ✅ | Fail-Fast en contraseña incorrecta |
| `/fe/recepcion/api/ecf` | POST | ✅ | Flujo completo (falla en BD externa no configurada, esperado) |
