# URL-WPT — Servicios DGII para Certificación e-CF

> Sistema de recepción y validación de **Comprobantes Fiscales Electrónicos (e-CF)** ante la DGII (Dirección General de Impuestos Internos, República Dominicana).  
> Basado en **.NET 8 / ASP.NET Core** con arquitectura multi-tenant, resolución dinámica de base de datos y carga segura de certificados digitales `.p12`.

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![SQL Server](https://img.shields.io/badge/SQL%20Server-2019%2B-CC2927?logo=microsoftsqlserver)](https://www.microsoft.com/sql-server)
[![IIS](https://img.shields.io/badge/Deploy-IIS-0078D4?logo=windows)](https://www.iis.net/)

---

## 📋 Tabla de Contenidos

1. [Descripción del Sistema](#1-descripción-del-sistema)
2. [Estructura del Repositorio](#2-estructura-del-repositorio)
3. [Dominios y Ambientes](#3-dominios-y-ambientes)
4. [Endpoints Expuestos](#4-endpoints-expuestos)
5. [Arquitectura](#5-arquitectura)
6. [Configuración](#6-configuración)
7. [Inicio Rápido](#7-inicio-rápido)
8. [Despliegue en IIS](#8-despliegue-en-iis)
9. [Flujo de Autenticación DGII](#9-flujo-de-autenticación-dgii)
10. [Seguridad y Buenas Prácticas](#10-seguridad-y-buenas-prácticas)
11. [Roadmap](#11-roadmap)
12. [Documentación Adicional](#12-documentación-adicional)

---

## 1. Descripción del Sistema

**URL-WPT** actúa como intermediario entre los sistemas emisores de los clientes y la plataforma de certificación **e-CF de la DGII**. Implementa los tres servicios oficiales del protocolo:

| Servicio | Función |
|---|---|
| **Autenticación** | Genera semillas firmadas y emite tokens Bearer |
| **Recepción** | Recibe e-CF firmados y devuelve acuse de recibo con `trackId` |
| **Aprobación Comercial** | Procesa acuses de recibo (ACECF) firmados |

### Características principales

- 🔄 **Multi-tenant dinámico** — resuelve la BD externa por header `X-Db-Tenant` sin reiniciar
- 🔐 **Certificados sin caché** — carga `.p12` por petición con `Dispose` inmediato (política Fail-Fast)
- 📊 **Auditoría completa** — registra cada llamada a la API con tiempo de respuesta y RNC
- 🚀 **Auto-migraciones** — aplica migraciones EF Core al arrancar
- 📝 **Swagger integrado** — disponible en `/swagger` en entorno de desarrollo

---

## 2. Estructura del Repositorio

```
URL-WPT/
├── WPTServiciosDGII/                   # 🟦 API principal (ASP.NET Core 8)
│   ├── Controllers/
│   │   ├── AutenticacionController.cs  # GET /semilla · POST /validacioncertificado
│   │   ├── RecepcionController.cs      # POST /ecf (recepción e-CF)
│   │   ├── AprobacionComercialController.cs  # POST /ecf (ACECF)
│   │   ├── AdminController.cs          # POST /register-db · GET /health
│   │   └── LogsController.cs           # GET /logs (auditoría)
│   ├── Core/
│   │   ├── Dto/                        # NucleoExternoDto, etc.
│   │   └── Interfaces/                 # IDbResolver, ICertificadoLoader
│   ├── Infrastructure/
│   │   ├── Data/                       # DynamicDbResolver (multi-tenant)
│   │   ├── External/                   # NucleoRepository (consulta tabla Nucleo)
│   │   └── Security/                   # CertificadoLoader (carga .p12 segura)
│   ├── Data/
│   │   └── WptDbContext.cs             # EF Core DbContext (BD propia)
│   ├── Models/                         # SemillaGenerada, TokenEmitido, DocumentoRecibido, LogInteraccion
│   ├── Services/
│   │   └── LogInteraccionService.cs
│   ├── Migrations/                     # Auto-aplicadas al iniciar
│   ├── Program.cs                      # Startup y registro de servicios
│   ├── appsettings.json                # Configuración (sin secretos reales)
│   ├── web.config                      # IIS AspNetCoreModuleV2
│   ├── PRUEBAS.md                      # Guía de pruebas con Postman/cURL
│   ├── TROUBLESHOOTING.md              # Errores comunes y soluciones
│   └── CHECKLIST_SEGURIDAD.md         # Checklist de permisos y rotación
│
├── WPTManagerWeb/                      # 🟨 Portal de administración (React + Vite + TS)
│   ├── src/
│   ├── public/
│   └── dist/                          # Build de producción
│
├── Publish_API/                        # Artefactos de despliegue API
├── Publish_Web/                        # Artefactos de despliegue Web
├── PLAN DE TRABAJO.txt                 # Fases de implementación
├── PROMPT_MAESTRO_eCF.md              # Guía maestra del sistema e-CF
└── Instrucciones-IIS.html             # Guía de configuración IIS
```

---

## 3. Dominios y Ambientes

| Ambiente | URL Base |
|---|---|
| **Producción (principal)** | `https://cloud.wptsoftwares.net` |
| **Producción (alternativo)** | `https://wptsoftwares.giize.com/WPTexecutor` |
| **Desarrollo local** | `https://localhost:7259` |

---

## 4. Endpoints Expuestos

### 🔐 Autenticación

| Método | Ruta | Descripción |
|---|---|---|
| `GET` | `/fe/autenticacion/api/semilla` | Genera y devuelve una semilla XML firmable |
| `POST` | `/fe/autenticacion/api/validacioncertificado` | Valida XML de semilla firmado, devuelve token Bearer |

### 📥 Recepción e-CF

| Método | Ruta | Descripción |
|---|---|---|
| `POST` | `/fe/recepcion/api/ecf` | Recibe e-CF firmado, devuelve AcuseRecibo con `trackId` |

### ✅ Aprobación Comercial

| Método | Ruta | Descripción |
|---|---|---|
| `POST` | `/fe/aprobacioncomercial/api/ecf` | Recibe ACECF firmado, devuelve AcuseRecibo |

### ⚙️ Administración

| Método | Ruta | Descripción |
|---|---|---|
| `POST` | `/api/admin/register-db` | Registra una BD externa en tiempo de ejecución |
| `GET` | `/api/health` | Health check con estado de conexión |
| `GET` | `/api/logs` | Últimas interacciones (auditoría) |

---

## 5. Arquitectura

### Flujo de recepción multi-tenant

```
Sistema Emisor
    │
    ├─ Header: X-Db-Tenant: {tenant}
    ├─ Header: Authorization: Bearer {token}
    └─ Body: XML e-CF firmado
         │
         ▼
  RecepcionController
         │
         ├─► DynamicDbResolver ──► BD Externa (tabla Nucleo)
         │        └─ Obtiene ruta + password del .p12
         │
         ├─► CertificadoLoader ──► Carga .p12 (sin caché)
         │        └─ Firma → Dispose inmediato
         │
         └─► Respuesta: AcuseRecibo XML con trackId
```

### Tablas en SQL Server (BD propia)

| Tabla | Propósito |
|---|---|
| `SemillaGenerada` | Semillas generadas para autenticación |
| `TokenEmitido` | Tokens Bearer activos (vigencia ~1h) |
| `DocumentoRecibido` | e-CF y ACECF recibidos con `trackId` |
| `LogInteraccion` | **Auditoría completa** de cada llamada API |

---

## 6. Configuración

### `appsettings.json` — secciones clave

```jsonc
{
  "ConnectionStrings": {
    // BD propia del sistema (semillas, tokens, documentos, logs)
    "WptDatabase": "Server=SERVIDOR;Database=WPTServiciosDGII;..."
  },

  // Resolución multi-tenant de BDs externas
  "ExternalDbConfig": {
    "DefaultTenant": "demo",
    "Tenants": {
      "demo": {
        "ConnectionString": "Server=EXTERNO;Database=BD_EXTERNA;..."
      }
    }
  },

  // Mapeo de columnas de la tabla Nucleo (sin recompilación)
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

> ⚠️ **Nunca** incluir credenciales reales en `appsettings.json`. Usar `appsettings.Production.json` (excluido por `.gitignore`) o variables de entorno del sistema operativo.

---

## 7. Inicio Rápido

### Requisitos previos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- SQL Server 2019+ (local o remoto)
- Archivos `.p12` de certificado disponibles en servidor

### Pasos

```bash
# 1. Clonar el repositorio
git clone https://github.com/HmesaG/URL-WPT.git
cd URL-WPT/WPTServiciosDGII

# 2. Configurar cadena de conexión
#    Editar appsettings.Development.json (excluido de git)

# 3. Aplicar migraciones (o dejar que auto-apliquen al iniciar)
dotnet ef database update

# 4. Ejecutar en desarrollo
dotnet run --launch-profile https
# → API:     https://localhost:7259
# → Swagger: https://localhost:7259/swagger
```

---

## 8. Despliegue en IIS

```bash
# Publicar en modo Release
cd WPTServiciosDGII
dotnet publish -c Release -o ../Publish_API

# Copiar Publish_API/ al directorio del sitio IIS
# El web.config incluido configura AspNetCoreModuleV2 automáticamente
```

Ver `Instrucciones-IIS.html` en la raíz del repositorio para la configuración completa del Application Pool y enlace de dominio.

---

## 9. Flujo de Autenticación DGII

```
1. Sistema emisor  →  GET /fe/autenticacion/api/semilla
                   ←  XML: <SemillaModel><valor>GUID</valor><fecha>...</fecha></SemillaModel>

2. Sistema emisor firma el XML con su certificado .p12
   (XMLDSig Enveloped · RSA-SHA256 · C14N)

3. Sistema emisor  →  POST /fe/autenticacion/api/validacioncertificado
                        Body: XML semilla firmado
                   ←  XML: <TokenModel><token>GUID</token><expira>...</expira></TokenModel>

4. Sistema emisor  →  POST /fe/recepcion/api/ecf
                        Header: Authorization: Bearer <token>
                        Header: X-Db-Tenant: <tenant>
                        Body: XML e-CF firmado
                   ←  XML: <AcuseRecibo><trackId>...</trackId>...</AcuseRecibo>
```

---

## 10. Seguridad y Buenas Prácticas

| Principio | Implementación |
|---|---|
| **Sin credenciales hardcodeadas** | `DynamicDbResolver` + header `X-Db-Tenant` |
| **Sin logs de contraseñas** | Passwords de `.p12` nunca se imprimen en logs |
| **Fail-Fast fiscal** | Si el certificado falta o falla → `400 Bad Request` inmediato |
| **Sin caché de certificados** | Carga por petición + `Dispose` explícito post-firma |
| **Queries parametrizadas** | Parámetros `@Rnc` / `@Estado` (inmunes a SQLi) |
| **Permisos mínimos** | Usuario SQL con solo `db_datareader` sobre tabla `Nucleo` |

Ver `CHECKLIST_SEGURIDAD.md` para el checklist completo de permisos y rotación de credenciales.

---

## 11. Roadmap

| Fase | Estado | Descripción |
|---|---|---|
| **Fase 0** | ✅ Completada | Configuración dinámica de BD y certificados |
| **Fase 1** | ✅ Completada | `DynamicDbResolver` multi-tenant |
| **Fase 2** | ✅ Completada | `NucleoRepository` — consulta tabla Nucleo por RNC |
| **Fase 3** | ✅ Completada | `CertificadoLoader` — carga `.p12` sin caché, Fail-Fast |
| **Fase 4** | 🔄 En progreso | Integración completa en `RecepcionController` |
| **Fase 5** | ⏳ Pendiente | Pruebas end-to-end y despliegue validado |

---

## 12. Documentación Adicional

| Archivo | Descripción |
|---|---|
| [`WPTServiciosDGII/README.md`](WPTServiciosDGII/README.md) | Documentación técnica detallada de la API |
| [`WPTServiciosDGII/PRUEBAS.md`](WPTServiciosDGII/PRUEBAS.md) | Guía de pruebas con Postman y cURL |
| [`WPTServiciosDGII/TROUBLESHOOTING.md`](WPTServiciosDGII/TROUBLESHOOTING.md) | Errores comunes y soluciones |
| [`WPTServiciosDGII/CHECKLIST_SEGURIDAD.md`](WPTServiciosDGII/CHECKLIST_SEGURIDAD.md) | Checklist de seguridad operacional |
| [`PROMPT_MAESTRO_eCF.md`](PROMPT_MAESTRO_eCF.md) | Guía maestra del sistema e-CF DGII |
| [`Instrucciones-IIS.html`](Instrucciones-IIS.html) | Configuración de IIS paso a paso |

---

*Última actualización: Abril 2026 · Repositorio: [HmesaG/URL-WPT](https://github.com/HmesaG/URL-WPT)*
