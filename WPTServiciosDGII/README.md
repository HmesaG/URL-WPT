# WPTServiciosDGII — API .NET Core 8

API de recepción para certificación DGII e-CF (Factura Electrónica República Dominicana).

## Dominios
| Ambiente | URL Base |
|---|---|
| Producción (principal) | `https://cloud.wptsoftwares.net` |
| Producción (alternativo) | `https://wptsoftwares.giize.com/WPTexecutor` |
| Desarrollo | `https://localhost:7259` |

---

## Endpoints DGII

### 🔐 Autenticación
| Método | Ruta | Descripción |
|---|---|---|
| `GET` | `/fe/autenticacion/api/semilla` | Genera y devuelve una semilla XML |
| `POST` | `/fe/autenticacion/api/validacioncertificado` | Valida certificado y devuelve token |

### 📥 Recepción
| Método | Ruta | Descripción |
|---|---|---|
| `POST` | `/fe/recepcion/api/ecf` | Recibe e-CF firmado, devuelve AcuseRecibo |

### ✅ Aprobación Comercial
| Método | Ruta | Descripción |
|---|---|---|
| `POST` | `/fe/aprobacioncomercial/api/ecf` | Recibe ACECF, devuelve AcuseRecibo |

### 🩺 Health Check
| Método | Ruta |
|---|---|
| `GET` | `/health` |

---

## Estructura del Proyecto

```
WPTServiciosDGII/
├── Controllers/
│   ├── AutenticacionController.cs       # GET /semilla, POST /validacioncertificado
│   ├── RecepcionController.cs           # POST /ecf (recepción)
│   └── AprobacionComercialController.cs # POST /ecf (aprobación)
├── Data/
│   └── WptDbContext.cs                  # EF Core DbContext
├── Migrations/                          # Migraciones EF Core (auto-apply al iniciar)
├── Models/
│   ├── SemillaGenerada.cs               # Tabla semillas
│   ├── TokenEmitido.cs                  # Tabla tokens
│   ├── DocumentoRecibido.cs             # Tabla e-CF / ACECF recibidos
│   └── LogInteraccion.cs               # Tabla auditoría de llamadas API
├── Services/
│   └── LogInteraccionService.cs         # Servicio de logging a BD
├── Program.cs                           # Configuración y startup
├── appsettings.json                     # Config producción
├── appsettings.Development.json         # Config desarrollo
└── web.config                           # Config IIS (AspNetCoreModuleV2)
```

---

## Tablas en SQL Server

| Tabla | Propósito |
|---|---|
| `SemillaGenerada` | Semillas generadas para autenticación |
| `TokenEmitido` | Tokens de sesión (Bearer, 1h de vigencia) |
| `DocumentoRecibido` | e-CF y ACECF recibidos con TrackId |
| `LogInteraccion` | **Auditoría completa** de cada llamada API |

---

## Configuración Inicial

### 1. Cadena de conexión (`appsettings.json`)
```json
"ConnectionStrings": {
  "WptDatabase": "Server=TU_SERVIDOR;Database=WPTServiciosDGII;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

### 2. Crear la base de datos
Las migraciones se aplican **automáticamente** al arrancar la aplicación (`db.Database.Migrate()`).
O manualmente:
```bash
dotnet ef database update
```

### 3. Ejecutar en desarrollo
```bash
dotnet run --launch-profile https
# Swagger → https://localhost:7259/swagger
```

### 4. Publicar en IIS
```bash
dotnet publish -c Release -o ./publish
# Copiar ./publish al sitio IIS configurado con cloud.wptsoftwares.net
```

---

## Flujo de Autenticación DGII

```
1. Sistema emisor  →  GET /fe/autenticacion/api/semilla
                   ←  XML: <SemillaModel><valor>GUID</valor><fecha>...</fecha></SemillaModel>

2. Sistema emisor firma el XML con su certificado .p12

3. Sistema emisor  →  POST /fe/autenticacion/api/validacioncertificado
                        Body: XML firmado (semilla)
                   ←  XML: <TokenModel><token>GUID</token><expira>...</expira></TokenModel>

4. Sistema emisor  →  POST /fe/recepcion/api/ecf
                        Header: Authorization: Bearer <token>
                        Body: XML e-CF firmado
                   ←  XML: <AcuseRecibo><trackId>...</trackId>...</AcuseRecibo>
```

---

## Auditoría — Query rápida

```sql
SELECT 
    LogInteraccionId,
    LogInteraccionFecha,
    LogInteraccionServicio,
    LogInteraccionMetodo,
    LogInteraccionEstado,
    LogInteraccionMsRespuesta,
    LogInteraccionRnc
FROM LogInteraccion
ORDER BY LogInteraccionFecha DESC;
```

---

## URLs para el Portal DGII — Paso 7

| Campo | Host | Path |
|---|---|---|
| Autenticación | `https://cloud.wptsoftwares.net` | `/fe/autenticacion/api/semilla` |
| Recepción | `https://cloud.wptsoftwares.net` | `/fe/recepcion/api/ecf` |
| Aprobación Comercial | `https://cloud.wptsoftwares.net` | `/fe/aprobacioncomercial/api/ecf` |
