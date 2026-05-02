# PROMPT MAESTRO — Sistema e-CF DGII (Certificacion-eCF)

> **Propósito de este documento:** Guía completa para que cualquier agente de IA, desarrollador o colaborador entienda el proyecto, su estado actual, sus pendientes y cómo trabajar en él correctamente sin romper nada.

---

## 1. CONTEXTO DEL PROYECTO

Eres un asistente trabajando en el repositorio `HmesaG/Certificacion-eCF`. Este es un sistema .NET 8 + Python para emitir, firmar digitalmente, enviar y hacer seguimiento de Comprobantes Fiscales Electrónicos (e-CF) ante la DGII (Dirección General de Impuestos Internos) de la República Dominicana.

El sistema ya es **funcional y validado** para el flujo principal de certificación DGII. Antes de cualquier cambio, leer `AGENTS.md` en la raíz del repositorio.

### Stack tecnológico

- **Backend:** ASP.NET Core 8, C#, Entity Framework Core, SQL Server
- **Herramientas de generación XML:** Python 3, scripts en `tools/python/`
- **Frontend:** HTML/CSS/JS estático servido desde `wwwroot/`
- **Firma digital:** XMLDSig Enveloped, RSA-SHA256, certificado `.p12`

### Estructura principal

```
Certificacion-eCF/
├── FacturacionDGII/
│   ├── FacturacionDGII.Api/            # ASP.NET Core Web API + UI estática
│   │   ├── BackgroundServices/         # Polling de estados e-CF (EcfPollingBackgroundService)
│   │   ├── Controllers/                # FacturacionController, AprobacionComercialController, SimulacionEcfController
│   │   ├── wwwroot/                    # Portal web (index.html, simulacion-ecf.html, aprobaciones-comerciales.html)
│   │   ├── StoredXMLs/                 # XMLs firmados e-CF (ignorado en git)
│   │   ├── StoredRFCEs/                # RFCEs generados (ignorado en git)
│   │   ├── StoredACECFs/               # Acuses de recibo sin firmar
│   │   └── StoredACECFsSigned/         # Acuses de recibo firmados
│   ├── FacturacionDGII.Core/           # Modelos e interfaces de dominio
│   │   ├── Models/ECF/                 # Ecf31.cs ... Ecf48.cs, EcfBase.cs, EcfFactory.cs
│   │   └── Interfaces/                 # IECFService, ITokenService, IDgiiXmlSigner, etc.
│   ├── FacturacionDGII.Infrastructure/ # Implementaciones de servicios
│   │   └── Services/                   # ECFService, DgiiTokenService, DgiiXmlSignerService, ExcelMappingService, etc.
│   └── FacturacionDGII.Tests/          # Tests unitarios
├── tools/python/generators/            # Generadores XML por tipo de e-CF
├── XSD/                                # Esquemas XSD oficiales DGII
├── samples/                            # XMLs de casos de prueba (Case_1.xml … Case_25.xml)
├── docs/api/                           # OpenAPI YAML de los servicios DGII
├── Documentos/                         # PDFs y documentación oficial DGII
├── AGENTS.md                           # Reglas de resguardo del proyecto ← LEER PRIMERO
└── GUIA_PHP_DGII.md                    # Guía de implementación equivalente en PHP
```

---

## 2. FLUJOS PRINCIPALES PROTEGIDOS

### 2.1 Flujo ECF (Comprobante Fiscal Electrónico)

```
Excel con datos → ExcelMappingService → EcfFactory → Modelo C# → ECFService.GenerateXml()
→ DgiiXmlSignerService.SignXml() → StoredXMLs/ → POST /api/Recepcion
→ Respuesta DGII (trackId) → EcfPollingBackgroundService consulta estado
→ Estado final guardado en BD (EcfDocument)
```

**Tipos soportados:** E31, E32, E33, E34, E41, E43, E44, E45, E46, E47, E48

### 2.2 Flujo RFCE (Resumen Factura de Consumo Electrónica)

```
Facturas E32 almacenadas → RfceDocumentService.GenerateRfce()
→ Código de seguridad (6 chars de SignatureValue del E32 original)
→ StoredRFCEs/ → POST /api/recepcion/recepcionfc → Estado inmediato
```

**REGLA CRÍTICA:** ECF y RFCE son flujos completamente separados. No mezclar. No redirigir automáticamente documentos de un flujo al otro.

### 2.3 Flujo ACECF (Acuse de Recibo)

```
XML sin firmar en StoredACECFs/ → DgiiXmlSignerService.SignXml()
→ StoredACECFsSigned/ → AprobacionComercialController
```

### 2.4 Autenticación DGII

```
GET /Autenticacion/Semilla → XML con <semilla>
→ Firmar semilla con certificado .p12 (XMLDSig Enveloped)
→ POST /Autenticacion/ValidarSemilla → Bearer token (válido ~60 min)
→ DgiiTokenService cachea el token con SemaphoreSlim (thread-safe)
```

---

## 3. TIPOS DE e-CF Y SUS ENDPOINTS

| Código | Tipo                             | Endpoint DGII                          |
|--------|----------------------------------|----------------------------------------|
| E31    | Crédito Fiscal                   | /CerteCF/Recepcion/api/FacturasElectronicas |
| E32    | Consumo                          | /CerteCF/Recepcion/api/FacturasElectronicas |
| E33    | Nota de Débito                   | /CerteCF/Recepcion/api/FacturasElectronicas |
| E34    | Nota de Crédito                  | /CerteCF/Recepcion/api/FacturasElectronicas |
| E41    | Comprobante de Compras           | /CerteCF/Recepcion/api/FacturasElectronicas |
| E43    | Gastos Menores                   | /CerteCF/Recepcion/api/FacturasElectronicas |
| E44    | Regímenes Especiales             | /CerteCF/Recepcion/api/FacturasElectronicas |
| E45    | Gubernamental                    | /CerteCF/Recepcion/api/FacturasElectronicas |
| E46    | Comprobante para Zonas Francas   | /CerteCF/Recepcion/api/FacturasElectronicas |
| E47    | Exportaciones                    | /CerteCF/Recepcion/api/FacturasElectronicas |
| RFCE   | Resumen Consumo (B2C)            | /fc.dgii.gov.do/certecf/recepcionfc/api/recepcion/ecf |

---

## 4. REGLAS DE ORO DEL XML DGII

Estas reglas son absolutas. Violarlas causa rechazo inmediato:

1. **Orden de tags exacto:** `IdDoc → Emisor → Comprador → Totales → DetallesItems → SubTotales`
2. **Decimales:** todos los montos con exactamente **2 decimales** (`100.00`)
3. **FechaHoraFirma:** debe ir al **final** del nodo raíz, antes de la firma
4. **Sin tags vacíos:** si un campo opcional no tiene valor, omitir el tag completo
5. **Canonización:** `http://www.w3.org/TR/2001/REC-xml-c14n-20010315`
6. **Firma:** RSA-SHA256, XMLDSig Enveloped
7. **Código seguridad RFCE:** primeros 6 caracteres del `<SignatureValue>` del E32 original

---

## 5. CONFIGURACIÓN Y SECRETOS

### Variables de entorno requeridas

```bash
# Certificado digital
DgiiConfig__CertificatePath=/ruta/absoluta/certificado.p12
DgiiConfig__CertificatePassword=password_del_certificado

# Base de datos
ConnectionStrings__DefaultConnection=Server=HOST;Database=FacturacionDGII;User Id=USER;Password=PASS;TrustServerCertificate=True

# Ambiente DGII: Test | Certification | Production
DgiiConfig__Environment=Certification

# Polling en minutos
DgiiConfig__PollingIntervalMinutes=1
```

**NUNCA** poner valores reales en `appsettings.json` ni commitearlos. El `.gitignore` excluye `appsettings.Development.json` y `*.p12`.

### Ejecución local

```bash
cd FacturacionDGII/FacturacionDGII.Api
dotnet run
# Portal: https://localhost:5001
```

---

## 6. ESTADO ACTUAL DEL PROYECTO Y PENDIENTES

### 6.1 Lo que YA funciona (NO tocar sin autorización)

- Autenticación DGII con semilla firmada y caché de token
- Generación y firma de XMLs para todos los tipos de e-CF
- Envío de e-CF al endpoint correcto de DGII
- Polling de estado con `EcfPollingBackgroundService`
- Generación y envío de RFCE separado del flujo ECF
- Panel web con tabs ECF / RFCE / Simulación / Aprobaciones Comerciales
- Persistencia en SQL Server con Entity Framework
- Generadores Python por tipo de e-CF en `tools/python/generators/`

### 6.2 Pendientes priorizados (Plan de Trabajo — `Documentos/Plan_Trabajo_Auditoria_Sistema.md`)

#### FASE 1 — Seguridad (CRÍTICO, 1-3 días)
- [ ] Implementar autenticación en el API (JWT o API Key)
- [ ] Aplicar autorización por rol a endpoints sensibles: subir Excel, enviar e-CF, enviar RFCE, borrar documentos, descargar XML
- [ ] Sacar `CertificatePassword` de cualquier archivo versionado
- [ ] Deshabilitar o restringir el endpoint de borrado total
- [ ] Registrar acceso a endpoints críticos

#### FASE 2 — Trazabilidad documental (Alto, 3-5 días)
- [ ] Definir política única de firmado: firmar una sola vez y enviar ese mismo XML
- [ ] Guardar en BD: fecha de envío, hash del XML enviado, nombre del archivo enviado, endpoint usado, respuesta DGII
- [ ] Persistir estado de RFCE en BD (actualmente solo en logs de archivo)
- [ ] Unificar historial de e-CF y RFCE
- [ ] Corregir la consulta de logs para devolver registros más recientes primero

#### FASE 3 — Estabilidad operativa (Medio, 4-7 días)
- [ ] Centralizar rutas físicas en configuración (eliminar dependencia de `Directory.GetCurrentDirectory()`)
- [ ] Sanear nombres de archivo y validar entradas
- [ ] Mejorar manejo de cancelación en `EcfPollingBackgroundService`
- [ ] Revisar reintentos y concurrencia en envíos masivos
- [ ] Estandarizar respuestas y errores de integración DGII

#### FASE 4 — Pruebas y soporte (4-6 días)
- [ ] Separar tests unitarios de tests de integración
- [ ] Crear fixtures controlados (eliminar dependencias de archivos locales en tests)
- [ ] Tests de regresión: generación XML, firmado, envío e-CF, envío RFCE, polling
- [ ] Checklist operativo: cómo reenviar, cómo identificar duplicados, cómo interpretar logs

### 6.3 Pendiente específico en generadores Python

Los siguientes generadores omiten campos que los XSD sí esperan. Usar `ecf_45.py` como referencia (ya fue corregido):

| Archivo                  | Omisiones principales                                                      |
|--------------------------|----------------------------------------------------------------------------|
| `ecf_base.py`            | TablaFormasPago, TablaTelefonoEmisor, InformacionesAdicionales, Transporte, ImpuestosAdicionales, TablaSubcantidad, TablaImpuestoAdicional, OtraMonedaDetalle |
| `ecf_31.py`              | TablaFormasPago, TablaTelefonoEmisor, InformacionesAdicionales, Transporte, TablaSubcantidad, TablaImpuestoAdicional, OtraMonedaDetalle |
| `ecf_32.py`              | Igual que ecf_31.py                                                        |
| `ecf_33.py`              | Igual que ecf_31.py                                                        |
| `ecf_34.py`              | TablaTelefonoEmisor, InformacionesAdicionales, Transporte, TablaSubcantidad, TablaImpuestoAdicional, OtraMonedaDetalle |
| `ecf_41.py`              | TablaFormasPago, TablaTelefonoEmisor, OtraMonedaDetalle                    |
| `ecf_43.py`              | TablaTelefonoEmisor, OtraMonedaDetalle                                     |
| `ecf_44.py`              | TablaFormasPago, TablaTelefonoEmisor, InformacionesAdicionales, Transporte, TablaImpuestoAdicional, OtraMonedaDetalle |
| `ecf_46.py`              | TablaFormasPago, TablaTelefonoEmisor, InformacionesAdicionales, Transporte, OtraMonedaDetalle |
| `ecf_47.py`              | TablaFormasPago, TablaTelefonoEmisor, Transporte, OtraMonedaDetalle        |

**Prioridad alta para corrección:** `ecf_base.py`, `ecf_31.py`, `ecf_32.py`

### 6.4 Caso pendiente: E450000000007

Este comprobante está en contradicción operativa con DGII:

- Con valores del dataset: DGII rechaza por fórmula aritmética (`TotalITBIS1 ≠ MontoGravadoI1 × ITBIS1`)
- Con valores aritméticos: DGII rechaza por no coincidir con el dataset
- El dataset también tiene un descuadre de `0.01` en `MontoTotal`

**Acción requerida:** Escalar a soporte DGII con evidencia de ambos rechazos. No intentar resolver programáticamente sin respuesta oficial.

---

## 7. ENDPOINTS DEL API PROPIO

| Método | Ruta                                         | Descripción                              |
|--------|----------------------------------------------|------------------------------------------|
| GET    | /api/facturacion/documents                   | Lista todos los e-CF en BD               |
| GET    | /api/facturacion/rfce-documents              | Lista todos los RFCE                     |
| GET    | /api/facturacion/xml/{id}                    | Obtiene XML firmado de un e-CF           |
| GET    | /api/facturacion/xml-download/{id}           | Descarga XML firmado                     |
| POST   | /api/facturacion/upload-excel                | Sube Excel con datos para generar e-CF   |
| POST   | /api/facturacion/send/{id}                   | Envía e-CF a DGII                        |
| POST   | /api/facturacion/send-rfce/{ncf}             | Envía RFCE a DGII                        |
| GET    | /api/simulacion/ecf                          | Panel de simulación                      |
| POST   | /api/aprobacion-comercial/send/{ncf}         | Envía acuse de recibo ACECF              |

---

## 8. REGLAS PARA EL AGENTE

### Lo que NUNCA debes hacer sin autorización explícita del usuario

1. Modificar el flujo de generación, almacenamiento, firma, envío o seguimiento de ECF y RFCE
2. Mezclar documentos ECF con RFCE en cualquier lista o lógica
3. Cambiar botones, etiquetas o estados del panel que redirijan ECF hacia el flujo RFCE
4. Introducir reglas heurísticas que desvíen documentos entre flujos
5. Cambiar endpoints DGII sin validación documental
6. Reintroducir el estado artificial `ResumenB2C` como estado operativo del panel ECF
7. Cambiar el endpoint de producción sin confirmación

### Procedimiento antes de cualquier cambio en flujos protegidos

1. Confirmar con el usuario que desea cambiar un proceso ya estabilizado
2. Explicar claramente el riesgo funcional
3. Identificar si el cambio afecta ECF, RFCE, UI, almacenamiento, polling o endpoints
4. Evitar cambios amplios si el usuario no los pidió

### Procedimiento para incidencias de simulación

Antes de ejecutar CUALQUIER acción sobre el flujo de simulación, presentar un informe con:
1. El error reportado
2. El impacto esperado
3. El archivo/hoja/fila/columna del Excel que será intervenido
4. Las acciones exactas propuestas
5. Los posibles riesgos

**Esperar autorización explícita antes de ejecutar.**

---

## 9. GUÍA RÁPIDA PARA NUEVAS FUNCIONALIDADES

### Agregar un nuevo tipo de e-CF

1. Crear `FacturacionDGII.Core/Models/ECF/EcfXX.cs` (hereda de `EcfBase`)
2. Registrar en `EcfFactory.cs`
3. Agregar validación XSD correspondiente en `ECFService`
4. Crear generador Python `tools/python/generators/generators/ecf_XX.py`
5. Registrar en `tools/python/generators/generators/factory.py`
6. Usar `ecf_45.py` como plantilla de referencia (el más completo)

### Corregir un generador Python con omisiones

1. Abrir el XSD correspondiente en `XSD/e-CF XX v.1.0.xsd`
2. Identificar los nodos que el generador no implementa
3. Usar `ecf_45.py` como referencia de implementación correcta
4. No tocar `ecf_base.py` hasta tener plan completo (afecta todos los tipos)
5. Validar el XML generado contra el XSD antes de enviar

### Agregar autenticación al API (Fase 1 pendiente)

Opciones recomendadas:
- **API Key simple:** Middleware que valida header `X-Api-Key` contra variable de entorno
- **JWT:** Usar `Microsoft.AspNetCore.Authentication.JwtBearer`, configurar en `Program.cs`

Los endpoints prioritarios a proteger:
- POST `/api/facturacion/upload-excel`
- POST `/api/facturacion/send/{id}`
- POST `/api/facturacion/send-rfce/{ncf}`
- DELETE cualquier endpoint de borrado
- GET `/api/facturacion/xml-download/{id}`

---

## 10. AMBIENTES DGII

| Ambiente      | URL base                            | Uso                                 |
|---------------|-------------------------------------|-------------------------------------|
| Test          | `https://ecf.dgii.gov.do/TesteCF/`  | Pruebas libres                      |
| Certificación | `https://ecf.dgii.gov.do/CerteCF/`  | Proceso oficial de certificación    |
| Producción    | `https://ecf.dgii.gov.do/eCF/`      | Solo después de certificación DGII  |

El ambiente activo se controla en `DgiiConfig:Environment`. Los endpoints completos están en `appsettings.json` bajo `DgiiConfig:Endpoints`.

---

## 11. DOCUMENTACIÓN DE REFERENCIA

Todos los documentos oficiales están en `Documentos/`:

- `Descripcion-tecnica-de-facturacion-electronica.pdf` — Documento técnico principal
- `Firmado de e-CF.pdf` — Especificaciones de firma digital
- `Formato Comprobante Fiscal Electrónico (e-CF) V1.0.pdf` — Estructura XML oficial
- `Formato Acuse de Recibo v 1.0.pdf` — Estructura ACECF
- `Formato Aprobación Comercial v1.0.pdf` — Estructura ARECF
- `Proceso de Certificacion para ser Emisor Electronico.pdf` — Pasos del proceso DGII
- `XSD/` — Esquemas XSD oficiales, fuente de verdad para estructura XML

OpenAPI YAML de los servicios DGII en `docs/api/Api/`:
- `DGIIServiciosWPT.APIAutenticacion.yaml`
- `DGIIServiciosWPT.APIRecepcion.yaml`
- `DGIIServiciosWPT.APIAprobacionComercial.yaml`

---

## 12. CHECKLIST DE CALIDAD ANTES DE HACER UN PR

- [ ] El flujo ECF sigue separado del flujo RFCE
- [ ] No hay secretos en archivos versionados
- [ ] Los XMLs generados pasan validación XSD
- [ ] Los montos tienen exactamente 2 decimales
- [ ] `FechaHoraFirma` va al final del nodo raíz
- [ ] No hay tags vacíos en el XML
- [ ] El `EcfPollingBackgroundService` no genera logs ruidosos innecesarios
- [ ] Las rutas físicas vienen de configuración, no de `Directory.GetCurrentDirectory()`
- [ ] Los tests unitarios no dependen de archivos locales
- [ ] `AGENTS.md` sigue siendo válido y actualizado

---

*Última actualización basada en estado del repositorio: abril 2026*
*Referencia: `Documentos/Plan_Trabajo_Auditoria_Sistema.md` y `AGENTS.md`*
