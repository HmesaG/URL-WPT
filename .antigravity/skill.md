# SKILL MAESTRO — URL-WPT Servicios DGII (.NET 8)

> **Propósito:** Guía completa para trabajar en este repositorio sin necesidad de releer el código.
> Leer este archivo antes de cualquier cambio.
>
> **Triggers:** url-wpt, wpt dgii, ecf dgii, servicio recepcion dgii, certificacion ecf, firma digital dgii,
> multi-tenant dotnet, nucleo repository, dynamic db resolver, certificado p12 dgii

---

## 1. CONTEXTO DEL PROYECTO

Repositorio: `HmesaG/URL-WPT`
Descripción: API .NET 8 que actúa como **receptor e-CF** para la DGII (Dirección General de Impuestos Internos, República Dominicana). Simula los tres servicios del protocolo oficial de certificación e-CF.

### Stack
- **Backend:** ASP.NET Core 8, C#, EF Core 8, SQL Server, Dapper, Polly
- **Firma Digital:** XMLDSig Enveloped, RSA-SHA256, `System.Security.Cryptography.Xml`
- **Frontend admin:** React + Vite + TypeScript (`WPTManagerWeb/`)
- **Deploy:** IIS con AspNetCoreModuleV2, `web.config` incluido

### Dominios productivos
| Ambiente | URL |
|---|---|
| Producción principal | `https://cloud.wptsoftwares.net` |
| Producción alternativo | `https://wptsoftwares.giize.com/WPTexecutor` |
| Desarrollo local | `https://localhost:7259` |

---

## 2. ESTRUCTURA DE CARPETAS

```
URL-WPT/
├── WPTServiciosDGII/                        ← API principal
│   ├── Controllers/
│   │   ├── AutenticacionController.cs        # GET /semilla · POST /validacioncertificado
│   │   ├── RecepcionController.cs            # POST /ecf — flujo completo integrado
│   │   ├── AprobacionComercialController.cs  # POST /ecf (ACECF)
│   │   ├── AdminController.cs                # POST /register-db · GET /tenants · POST /validar-certificado
│   │   └── LogsController.cs                 # GET /logs
│   ├── Core/
│   │   ├── Dto/NucleoExternoDto.cs           # record: Rnc, Estado, RutaCertificado, PasswordCertificado
│   │   └── Interfaces/
│   │       ├── IDbResolver.cs                # GetConnectionString(tenant?), RegisterTenant, ListTenants
│   │       ├── INucleoRepository.cs          # ObtenerPorRncAsync(rnc, tenantKey?)
│   │       └── ICertificadoLoader.cs         # EjecutarConCertificadoAsync(ruta, pass, accion)
│   ├── Infrastructure/
│   │   ├── Data/DynamicDbResolver.cs         # Singleton, ConcurrentDictionary, thread-safe
│   │   ├── External/NucleoRepository.cs      # Scoped, Dapper, columnas configurables desde config
│   │   └── Security/CertificadoLoader.cs     # Transient, X509Certificate2, carga-uso-dispose
│   ├── Data/WptDbContext.cs                  # EF Core — BD propia del sistema
│   ├── Models/                               # SemillaGenerada, TokenEmitido, DocumentoRecibido, LogInteraccion
│   ├── Services/LogInteraccionService.cs
│   ├── Program.cs                            # Startup y registro de servicios
│   ├── appsettings.json                      # Config (sin secretos reales)
│   └── web.config                            # IIS AspNetCoreModuleV2
└── WPTManagerWeb/                            ← Portal React + Vite + TypeScript
```

---

## 3. ENDPOINTS COMPLETOS

### Autenticación DGII
```
GET  /fe/autenticacion/api/semilla                → XML SemillaModel {valor: GUID, fecha}
POST /fe/autenticacion/api/validacioncertificado  → XML TokenModel {token: GUID, expira}
POST /fe/autenticacion/api/firmar-semilla-test    → XML firmado (helper dev — ELIMINAR en prod)
```

### Recepción e-CF
```
POST /fe/recepcion/api/ecf
  Headers requeridos: Authorization: Bearer <token>
  Header opcional:    X-Db-Tenant: <tenantKey>   (si omite, usa DefaultTenant de config)
  Body:               XML e-CF firmado (application/xml)
  Respuesta OK:       XML ARECF firmado digitalmente con el .p12 del emisor
  Respuesta error:    400 si RNC inactivo | 400 Fail-Fast si cert falla | 500 si error inesperado
```

### Aprobación Comercial
```
POST /fe/aprobacioncomercial/api/ecf  → XML AcuseRecibo
```

### Administración
```
GET  /api/admin/tenants              → {tenants: {key: description}, total: N}  (sin passwords)
POST /api/admin/register-db          → Registra BD en caliente (sin reiniciar)
POST /api/admin/validar-certificado  → {valido: bool, mensaje/error: string}
GET  /health                         → {status, time, tenants_registrados, ruta_certificados}
GET  /api/logs                       → Últimas interacciones de auditoría
```

---

## 4. CONFIGURACIÓN (appsettings.json — secciones clave)

```jsonc
{
  "ConnectionStrings": {
    // BD propia del sistema (semillas, tokens, documentos, logs)
    "WptDatabase": "Server=X;Database=WPTServiciosDGII;..."
  },
  "AppSettings": {
    "BaseUrl": "https://cloud.wptsoftwares.net",
    "AltBaseUrl": "https://wptsoftwares.giize.com/WPTexecutor",
    "AllowedOrigins": ["https://cloud.wptsoftwares.net", "https://wptsoftwares.giize.com"]
  },
  // Resolución multi-tenant de BDs externas. El header X-Db-Tenant selecciona cuál usar.
  "ExternalDbConfig": {
    "DefaultTenant": "demo",
    "Tenants": {
      "demo": {
        "ConnectionString": "Server=EXTERNO;Database=BD_EXTERNA;User Id=readonly;Password=X;...",
        "Description": "BD de ejemplo"
      }
    }
  },
  // Mapeo de columnas de tabla Nucleo — cambiar sin recompilar
  "NucleoConfig": {
    "TableName": "Nucleo",
    "ColumnRnc": "Rnc",
    "ColumnEstado": "Estado",
    "ColumnRutaCertificado": "RutaCertificado",
    "ColumnPasswordCertificado": "PasswordCertificado",
    "EstadoActivo": "A"
  },
  // Configuración de certificados .p12
  "CertificadosConfig": {
    "RutaBase": "C:\\Certificados\\WPT",
    "FailFast": true,
    "TimeoutCargaMs": 5000
  }
}
```

> ⚠️ **NUNCA** poner credenciales reales en `appsettings.json`.
> Usar `appsettings.Production.json` (en `.gitignore`) o variables de entorno del SO.

---

## 5. REGISTRO DE SERVICIOS (Program.cs)

```csharp
builder.Services.AddDbContext<WptDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("WptDatabase")));

builder.Services.AddMemoryCache();

// ⚠️ Lifetimes críticos — NO cambiar sin entender el impacto:
builder.Services.AddSingleton<IDbResolver, DynamicDbResolver>();       // Singleton: diccionario compartido
builder.Services.AddScoped<INucleoRepository, NucleoRepository>();     // Scoped: una conexión por request
builder.Services.AddTransient<ICertificadoLoader, CertificadoLoader>(); // Transient: sin caché del cert

builder.Services.AddScoped<ILogInteraccionService, LogInteraccionService>();
```

---

## 6. FLUJO COMPLETO DE RECEPCIÓN e-CF (RecepcionController)

```
Sistema Emisor
    │
    ├─ Header: X-Db-Tenant: empresa_001  (opcional)
    ├─ Header: Authorization: Bearer <token>
    └─ Body: XML e-CF firmado
         │
         ▼
  RecepcionController.EnviarEcf()
    │
    ├─ 1. Lee XML (form file o raw stream)
    ├─ 2. ExtraerCamposXml() → rncEmisor, rncComprador, encf (vía XDocument)
    ├─ 3. NucleoRepository.ObtenerPorRncAsync(rncEmisor, tenant)
    │       ├─ DynamicDbResolver.GetConnectionString(tenant)  → connStr
    │       ├─ Dapper query → SELECT RutaCertificado, PasswordCertificado FROM Nucleo WHERE Rnc=@Rnc AND Estado=@Estado
    │       └─ Si null → return 400 "RNCEmisor no activo"
    ├─ 4. Construye XML ARECF (Acuse de Recibo)
    ├─ 5. CertificadoLoader.EjecutarConCertificadoAsync(ruta, password, cert => FirmarXml())
    │       ├─ Carga X509Certificate2
    │       ├─ FirmarXml() → XMLDSig Enveloped + RSA-SHA256
    │       ├─ Dispose certificado inmediatamente
    │       └─ Si falla → return 400 Fail-Fast
    ├─ 6. LogInteraccionService.RegistrarAsync(...)
    └─ 7. return Content(signedXml, "application/xml")
```

---

## 7. FIRMA DIGITAL XMLDSig — CÓDIGO CORRECTO

```csharp
private static string FirmarXml(string xmlRaw, X509Certificate2 cert)
{
    // PreserveWhitespace = true es OBLIGATORIO — cualquier cambio de formato rompe la firma
    var xmlDoc = new XmlDocument { PreserveWhitespace = true };
    xmlDoc.LoadXml(xmlRaw);

    var signedXml = new SignedXml(xmlDoc) { SigningKey = cert.GetRSAPrivateKey() };

    // KeyInfo — DGII requiere el certificado embebido en la firma
    var keyInfo = new KeyInfo();
    keyInfo.AddClause(new KeyInfoX509Data(cert));
    signedXml.KeyInfo = keyInfo;

    // Uri="" = firma todo el documento (enveloped)
    var reference = new Reference { Uri = "" };
    reference.AddTransform(new XmlDsigEnvelopedSignatureTransform()); // excluye <Signature> del digest
    signedXml.AddReference(reference);

    signedXml.ComputeSignature();
    xmlDoc.DocumentElement!.AppendChild(xmlDoc.ImportNode(signedXml.GetXml(), true));

    return xmlDoc.OuterXml;
}
```

**Reglas críticas para DGII:**
- `PreserveWhitespace = true` — obligatorio
- `Uri = ""` — firma el documento completo
- `XmlDsigEnvelopedSignatureTransform` — excluye `<Signature>` del digest
- `KeyInfoX509Data(cert)` — DGII requiere el certificado embebido
- Algoritmo: RSA-SHA256 → `http://www.w3.org/2001/04/xmldsig-more#rsa-sha256`
- Canonización: C14N → `http://www.w3.org/TR/2001/REC-xml-c14n-20010315`

---

## 8. MULTI-TENANT: DynamicDbResolver (Singleton)

```csharp
// Obtener cadena de conexión:
var connStr = _dbResolver.GetConnectionString(tenantKey);
// Si tenantKey es null/empty → usa DefaultTenant
// Si no existe → throw KeyNotFoundException → capturar en controller → return 400

// Registrar tenant en caliente (POST /api/admin/register-db):
_dbResolver.RegisterTenant("empresa_001", "Server=X;...", "Empresa Real S.A.");
// Disponible INMEDIATAMENTE para nuevas requests, sin reiniciar la app

// Listar tenants (NUNCA expone ConnectionString, solo Description):
var lista = _dbResolver.ListTenants(); // IReadOnlyDictionary<string, string>
```

**Header de request:** `X-Db-Tenant: empresa_001`

---

## 9. NUCLEO REPOSITORY — CONSULTA TABLA EXTERNA (Scoped)

```csharp
// Columnas configurables en NucleoConfig de appsettings.json
// Query generada (con config default):
SELECT [Rnc]                 AS Rnc,
       [Estado]              AS Estado,
       [RutaCertificado]     AS RutaCertificado,
       [PasswordCertificado] AS PasswordCertificado
FROM [Nucleo]
WHERE [Rnc] = @Rnc           -- ← parámetro seguro (anti-SQLi)
  AND [Estado] = @Estado      -- ← valor de EstadoActivo ("A" por defecto)
```

**DTO resultado:**
```csharp
public record NucleoExternoDto(
    string Rnc,
    string Estado,
    string RutaCertificado,       // Ruta absoluta al archivo .p12
    string PasswordCertificado);  // Contraseña del .p12
```

---

## 10. CERTIFICADO LOADER — Fail-Fast (Transient)

```csharp
// Contrato: carga → ejecuta → dispose en cada petición. Sin caché nunca.

await _certLoader.EjecutarConCertificadoAsync(
    emisor.RutaCertificado,
    emisor.PasswordCertificado,
    cert =>
    {
        signedXml = FirmarXml(responseXmlStr, cert);
        return Task.CompletedTask;
    });
// Si lanza Exception → capturar → return 400 (Fail-Fast fiscal)
// NO emitir respuesta sin firma bajo ninguna circunstancia
```

---

## 11. TABLAS SQL SERVER (BD PROPIA — EF Core)

| Tabla | Propósito |
|---|---|
| `SemillaGenerada` | Semillas generadas para autenticación (Valor, Fecha, Usada, Rnc) |
| `TokenEmitido` | Tokens Bearer activos (Valor, Rnc, FechaCreacion, FechaExpiracion, Activo) |
| `DocumentoRecibido` | e-CF y ACECF recibidos (Tipo, Rnc, eNCF, TrackId, Xml) |
| `LogInteraccion` | **Auditoría completa** (Servicio, Metodo, Url, Ip, Request, Response, Estado, MsRespuesta, Rnc, Token) |

```sql
-- Query de auditoría rápida:
SELECT LogInteraccionId, LogInteraccionFecha, LogInteraccionServicio,
       LogInteraccionMetodo, LogInteraccionEstado, LogInteraccionMsRespuesta, LogInteraccionRnc
FROM LogInteraccion ORDER BY LogInteraccionFecha DESC;
```

---

## 12. PRUEBAS CON POWERSHELL

```powershell
$base = "http://localhost:5190"

# Health check
Invoke-RestMethod -Uri "$base/health"

# Listar tenants registrados
Invoke-RestMethod -Uri "$base/api/admin/tenants"

# Registrar BD externa en caliente
$body = @{
    tenantKey        = "empresa_001"
    connectionString = "Server=MI_SERVER;Database=MI_BD;User Id=readonly;Password=X;TrustServerCertificate=True;"
    description      = "Empresa Real S.A."
} | ConvertTo-Json
Invoke-RestMethod -Uri "$base/api/admin/register-db" -Method POST -Body $body -ContentType "application/json"

# Validar certificado .p12
$certBody = @{ ruta = "C:\Certificados\WPT\empresa.p12"; password = "mi_pass" } | ConvertTo-Json
Invoke-RestMethod -Uri "$base/api/admin/validar-certificado" -Method POST -Body $certBody -ContentType "application/json"

# Obtener semilla
Invoke-RestMethod -Uri "$base/fe/autenticacion/api/semilla"

# Firmar semilla y obtener token
$semillaXml = "<SemillaModel>...</SemillaModel>"  # XML obtenido del paso anterior
Invoke-RestMethod -Uri "$base/fe/autenticacion/api/validacioncertificado" `
    -Method POST -Body $semillaXml -ContentType "application/xml"

# Enviar e-CF
$xml = '<?xml version="1.0" encoding="utf-8"?><eCF xmlns="urn:dgii.gov.do:ecf"><Encabezado><Version>1.0</Version><RNCEmisor>131234567</RNCEmisor><RNCComprador>101000532</RNCComprador><eNCF>E310000000001</eNCF></Encabezado></eCF>'
Invoke-RestMethod -Uri "$base/fe/recepcion/api/ecf" -Method POST -Body $xml `
    -ContentType "application/xml" -Headers @{ "X-Db-Tenant" = "empresa_001"; "Authorization" = "Bearer MI_TOKEN" }
```

---

## 13. DESPLIEGUE IIS

```powershell
# Publicar
dotnet publish WPTServiciosDGII -c Release -o ./Publish_API
# Copiar Publish_API/ al directorio del sitio IIS
```

**Configuración App Pool:**
- .NET CLR Version: **No Managed Code**
- Modelo de alojamiento: **In-Process** (mejor rendimiento)
- Usuario del App Pool: solo lectura en carpeta certs + lectura/escritura en carpeta app

**Diagnóstico 500 en IIS:**
Habilitar temporalmente en `web.config`:
```xml
stdoutLogEnabled="true" stdoutLogFile=".\logs\stdout"
```

---

## 14. SEGURIDAD — REGLAS ABSOLUTAS

| Regla | Implementación |
|---|---|
| Sin credenciales hardcodeadas | `DynamicDbResolver` + header `X-Db-Tenant` |
| Sin log de passwords | `LogInteraccionService` nunca imprime `PasswordCertificado` |
| Fail-Fast fiscal | Cert falla → `400` inmediato, sin respuesta sin firma |
| Sin caché de certificados | `CertificadoLoader` Transient + `Dispose` explícito |
| Queries parametrizadas | `@Rnc`, `@Estado` en Dapper (anti-SQLi) |
| Permisos mínimos SQL | Solo `db_datareader` en tabla Nucleo |
| HTTPS forzado | `app.UseHttpsRedirection()` activo |
| Swagger protegido | Solo en dev — deshabilitar en producción |

---

## 15. AMBIENTES DGII

| Ambiente | URL Base | Uso |
|---|---|---|
| Test | `https://ecf.dgii.gov.do/TesteCF/` | Pruebas libres |
| Certificación | `https://ecf.dgii.gov.do/CerteCF/` | Proceso oficial |
| Producción | `https://ecf.dgii.gov.do/eCF/` | Solo post-certificación |

**Servicios que la DGII llama a este sistema:**
- `GET /fe/autenticacion/api/semilla`
- `POST /fe/autenticacion/api/validacioncertificado`
- `POST /fe/recepcion/api/ecf`
- `POST /fe/aprobacioncomercial/api/ecf`

El Portal DGII (Paso 7 del proceso de certificación) configura las URLs de este sistema como destino de sus llamadas.

---

## 16. ROADMAP — ESTADO DE FASES

| Fase | Estado | Descripción |
|---|---|---|
| Fase 0 | ✅ Completa | Config dinámica BD + certificados en `appsettings.json` |
| Fase 1 | ✅ Completa | `DynamicDbResolver` multi-tenant (Singleton + ConcurrentDictionary) |
| Fase 2 | ✅ Completa | `NucleoRepository` con Dapper + columnas configurables |
| Fase 3 | ✅ Completa | `CertificadoLoader` Fail-Fast + Dispose inmediato |
| Fase 4 | ✅ Completa | `RecepcionController` integrado con todos los componentes |
| Fase 5 | ⏳ Pendiente | Pruebas end-to-end con BD real + certificado real de producción |

### Mejoras pendientes prioritarias

**A. Autenticación del endpoint `/api/admin/*`** (CRÍTICO para producción)
```csharp
// Implementar ApiKeyAuthHandler y aplicar [Authorize] en AdminController
builder.Services.AddAuthentication("ApiKey")
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthHandler>("ApiKey", null);
```

**B. Validación real de firma en `validacioncertificado`**
Actualmente acepta cualquier XML no vacío. Implementar `SignedXml.CheckSignature()`.

**C. Deshabilitar Swagger en producción**
```csharp
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(...); }
```

**D. Eliminar hardcode en `firmar-semilla-test`**
`AutenticacionController.cs` línea ~177 tiene ruta y password de `.p12` hardcodeados.
⚠️ **Eliminar este endpoint antes de ir a producción.**

---

## 17. TROUBLESHOOTING RÁPIDO

| Error | Causa | Solución |
|---|---|---|
| `400 - Tenant 'X' no registrado` | Header `X-Db-Tenant` incorrecto o tenant no existe | `GET /api/admin/tenants` → `POST /api/admin/register-db` |
| `400 - Fail-Fast: No se pudo cargar .p12` | Ruta/password incorrecta o archivo corrupto/movido | `POST /api/admin/validar-certificado` |
| `400 - RNCEmisor no activo` | RNC no en Nucleo o estado ≠ "A" | `SELECT * FROM Nucleo WHERE Rnc='X'` |
| `400 - Certificado expirado` | `.p12` vencido | Renovar cert, actualizar columnas en Nucleo (sin reiniciar) |
| `500 - SQL Server no encontrado` | Cadena de conexión incorrecta o firewall | `Test-NetConnection -ComputerName HOST -Port 1433` |
| Build bloqueado (Access denied) | Proceso corriendo en background | `Stop-Process -Name WPTServiciosDGII -Force` |
| `500` en IIS sin detalle | Startup crash | `stdoutLogEnabled="true"` en `web.config` |
| XML sin `RNCEmisor` | Estructura incorrecta del e-CF | Verificar que el XML tiene nodo `<Encabezado><RNCEmisor>` |

---

## 18. PATRÓN PARA AGREGAR NUEVO ENDPOINT DGII

1. Crear controller en `Controllers/` con `[Route("fe/nuevo/api")]`
2. Inyectar: `ILogInteraccionService`, `INucleoRepository`, `ICertificadoLoader`
3. Leer body: `using var reader = new StreamReader(Request.Body);`
4. Extraer RNCEmisor del XML con `XDocument.Parse()`
5. Llamar `_nucleo.ObtenerPorRncAsync(rnc, tenant)` → Fail-Fast si null
6. Construir XML de respuesta (string interpolado)
7. Firmar: `_certLoader.EjecutarConCertificadoAsync(ruta, pass, cert => FirmarXml())`
8. Registrar: `_log.RegistrarAsync("Servicio", "POST", url, ip, req, resp, estado, ms)`
9. Retornar: `Content(xml, "application/xml")`

---

## 19. COMANDOS ÚTILES

```powershell
# Ejecutar en desarrollo
dotnet run --project WPTServiciosDGII --launch-profile https

# Crear nueva migración EF Core
dotnet ef migrations add NombreMigracion --project WPTServiciosDGII

# Aplicar migraciones manualmente
dotnet ef database update --project WPTServiciosDGII

# Publicar para IIS
dotnet publish WPTServiciosDGII -c Release -o ./Publish_API

# Verificar conectividad SQL
Test-NetConnection -ComputerName MI_SERVIDOR -Port 1433

# Verificar/matar proceso bloqueando build
Get-Process | Where-Object { $_.Name -like "*WPTServiciosDGII*" }
Stop-Process -Name WPTServiciosDGII -Force

# Buscar credenciales hardcodeadas (seguridad)
Select-String -Path "WPTServiciosDGII\**\*.cs" -Pattern "Password=" -Recurse
```

---

*Última actualización: Abril 2026*
*Estado del repositorio: Fases 0–4 completadas. Siguiente: Fase 5 (pruebas end-to-end).*
