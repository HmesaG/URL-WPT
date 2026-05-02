# GuÃ­a TÃ©cnica Definitiva â€” FacturaciÃ³n ElectrÃ³nica DGII (e-CF)
> RepÃºblica Dominicana Â· Referencia tÃ©cnica para sistemas ERP en producciÃ³n

---

## 1. MARCO LEGAL

### 1.1 Ley 32-23 â€” Ley de ModernizaciÃ³n Fiscal
- Promulgada en 2023, establece la **obligatoriedad de la facturaciÃ³n electrÃ³nica** para contribuyentes del RNC.
- Crea el rÃ©gimen de **Comprobantes Fiscales ElectrÃ³nicos (e-CF)** como sustituto gradual de los NCF tradicionales.
- Otorga a la DGII la potestad de establecer plazos, estÃ¡ndares tÃ©cnicos y proceso de certificaciÃ³n.

### 1.2 Decreto 587-24 â€” Reglamento de e-CF
- Reglamenta la Ley 32-23 en cuanto a:
  - Obligaciones del emisor y receptor electrÃ³nico.
  - Requisitos del certificado digital y delegaciÃ³n de roles.
  - Plazos de incorporaciÃ³n por categorÃ­a de contribuyente (grandes, medianos, pequeÃ±os).
  - ConservaciÃ³n de documentos: **mÃ­nimo 10 aÃ±os** en formato electrÃ³nico.
  - RepresentaciÃ³n Impresa: cuÃ¡ndo es vÃ¡lida y quÃ© debe contener.

### 1.3 Norma General 01-2020 â€” Especificaciones TÃ©cnicas e-CF
- Define el **estÃ¡ndar XML** del e-CF (estructura, campos, tipos de datos).
- Establece el protocolo de **comunicaciÃ³n emisor-receptor** y los servicios web de la DGII.
- Especifica el algoritmo de firma digital: **XMLDSig Enveloped + RSA-SHA256**.
- Define los **14 pasos del proceso de certificaciÃ³n** y el set de pruebas oficial.

### 1.4 Obligatoriedad y Plazos
| CategorÃ­a | IncorporaciÃ³n obligatoria |
|---|---|
| Grandes contribuyentes | Ya incorporados (2022-2023) |
| Medianos contribuyentes | SegÃºn cronograma DGII 2024-2025 |
| PequeÃ±os contribuyentes | SegÃºn cronograma DGII 2025-2026 |
| Proveedores de software | Deben certificar antes de comercializar |

> **Nota:** Consultar el cronograma vigente en [dgii.gov.do](https://dgii.gov.do) ya que los plazos se actualizan periÃ³dicamente.

---

## 2. REQUISITOS PREVIOS

### 2.1 InscripciÃ³n RNC
- El emisor debe tener **RNC activo y en estado Normal** en el registro de la DGII.
- Verificar estado: `https://www.dgii.gov.do/app/WebApps/ConsultasWeb/consultas/rnc.aspx`
- El RNC del emisor debe coincidir exactamente con el del certificado digital.

### 2.2 Acceso OFV (Oficina Virtual DGII)
- Crear cuenta en: `https://ofv.dgii.gov.do`
- Roles mÃ­nimos requeridos:
  - **Administrador e-CF**: gestiona el proceso completo.
  - **Firmante**: firma la DeclaraciÃ³n Jurada (Paso 13).
  - **Aprobador Comercial**: aprueba o rechaza e-CF recibidos.
- La delegaciÃ³n de roles se hace desde la OFV â†’ AdministraciÃ³n â†’ DelegaciÃ³n de Roles.

### 2.3 Alta NCF â†’ e-NCF
- Solicitar autorizaciÃ³n de secuencias e-NCF en la OFV.
- Formato e-NCF: `E` + tipo (2 dÃ­gitos) + secuencia (10 dÃ­gitos) = **13 caracteres totales**.
  - Ejemplo: `E310000000001` (Factura de CrÃ©dito Fiscal, secuencia 1).
- La DGII asigna rangos de secuencias vÃ¡lidos â€” respetar el rango asignado.

### 2.4 Certificado Digital (.p12)
- **QuiÃ©n lo emite:** Entidades certificadoras autorizadas por INDOTEL (ej: Banreservas CA, DigiCert RD).
- **CÃ³mo obtenerlo:**
  1. Completar Formulario **FI-GDF-016** (disponible en OFV).
  2. Presentarse fÃ­sicamente con cÃ©dula/pasaporte y poder notarial (si aplica).
  3. La entidad certificadora valida identidad y emite el `.p12`.
- **Formato:** PKCS#12 (`.p12` o `.pfx`), contiene clave privada + certificado pÃºblico.
- **Vigencia:** TÃ­picamente 1 o 2 aÃ±os. Debe renovarse antes de expirar.
- **Uso en el sistema:** Ruta al archivo + contraseÃ±a, almacenadas en la BD (tabla Nucleo), nunca en cÃ³digo.

### 2.5 Formulario FI-GDF-016
- Solicitud formal ante la DGII para iniciar el proceso de certificaciÃ³n del software.
- Requiere: datos del proveedor de software, URL de los endpoints del sistema receptor, versiÃ³n del software.
- Se entrega junto con la solicitud de inicio del proceso (Etapa 1).

### 2.6 DelegaciÃ³n de Roles
| Rol | FunciÃ³n |
|---|---|
| Administrador | GestiÃ³n completa del proceso e-CF en OFV |
| Firmante | Firma XMLDSig con el certificado digital |
| Aprobador Comercial | Emite ACECF (aprobaciÃ³n/rechazo) |

---

## 3. TIPOS DE e-CF

### Tabla resumen

| CÃ³digo | Nombre | Uso principal |
|---|---|---|
| E31 | Factura de CrÃ©dito Fiscal | Ventas B2B (empresa a empresa) |
| E32 | Factura de Consumo | Ventas B2C (empresa a consumidor final) |
| E33 | Nota de DÃ©bito | Ajuste al alza de factura original |
| E34 | Nota de CrÃ©dito | Ajuste a la baja / devoluciÃ³n |
| E41 | Compras | Registro de compras al exterior |
| E43 | Gastos Menores | Gastos sin comprobante fiscal |
| E44 | RegÃ­menes Especiales | Zonas francas y regÃ­menes especiales |
| E45 | Gubernamental | Operaciones con el Estado |
| E46 | Exportaciones | Ventas al exterior |
| E47 | Pagos al Exterior | Remesas y pagos fuera del paÃ­s |

---

### E31 â€” Factura de CrÃ©dito Fiscal
- **CuÃ¡ndo se usa:** Ventas de bienes o servicios entre contribuyentes del RNC (B2B). Permite al comprador deducir ITBIS.
- **Campos obligatorios especÃ­ficos:**
  - `RNCComprador` (obligatorio, debe ser RNC vÃ¡lido y activo)
  - `RazonSocialComprador`
  - Desglose de ITBIS por Ã­tem
  - `FechaVencimientoSecuencia`
- **Regla clave:** El comprador debe estar registrado en DGII. Si el RNC comprador no existe, DGII rechaza.

### E32 â€” Factura de Consumo
- **CuÃ¡ndo se usa:** Ventas a consumidores finales (B2C). El comprador puede ser persona fÃ­sica sin RNC.
- **Subcasos crÃ­ticos:**
  - Monto **â‰¥ RD$250,000**: va por el flujo normal (endpoint `/ecf`).
  - Monto **< RD$250,000**: puede enviarse por **RFCE** (endpoint diferente en `fc.dgii.gov.do`).
- **Campos opcionales:** `RNCComprador` (si se tiene), `NombreComprador`.
- **Diferencia con E31:** No requiere RNC comprador vÃ¡lido.

### E33 â€” Nota de DÃ©bito
- **CuÃ¡ndo se usa:** Para aumentar el monto de una factura ya emitida (ej: intereses por mora, ajuste de precio).
- **Campos obligatorios especÃ­ficos:**
  - `eNCFModificado`: e-NCF del documento original que se modifica.
  - `RNCEmisorDocModificado`: RNC del emisor del documento original.
  - `FechaDocModificado`: fecha del documento original.
  - `CodigoModificacion`: motivo del ajuste.
- **Regla:** El documento original referenciado debe existir y estar en estado Aceptado.

### E34 â€” Nota de CrÃ©dito
- **CuÃ¡ndo se usa:** Para reducir el monto de una factura ya emitida (devoluciones, descuentos post-factura, anulaciÃ³n parcial).
- **Campos obligatorios especÃ­ficos:**
  - `eNCFModificado`, `RNCEmisorDocModificado`, `FechaDocModificado` (igual que E33).
  - `IndicadorNotaCredito`: **campo exclusivo de E34** (valores: 1=AnulaciÃ³n, 2=CorrecciÃ³n, 3=Descuento).
- **Orden en certificaciÃ³n:** Debe emitirse **despuÃ©s** del E31/E32 que referencia.

### E41 â€” Compras
- **CuÃ¡ndo se usa:** Registro de compras realizadas al exterior donde no se emite comprobante dominicano.
- **Campos:** Datos del proveedor extranjero, descripciÃ³n de la compra, ITBIS si aplica.

### E43 â€” Gastos Menores
- **CuÃ¡ndo se usa:** Gastos pequeÃ±os sin comprobante fiscal (parqueo, propinas, peajes).
- **LÃ­mite:** Generalmente hasta RD$50 por transacciÃ³n (verificar norma vigente).

### E44 â€” RegÃ­menes Especiales
- **CuÃ¡ndo se usa:** Transacciones dentro de zonas francas u otros regÃ­menes especiales de tributaciÃ³n.
- **Particularidad:** ITBIS generalmente exento o tasa diferenciada.

### E45 â€” Gubernamental
- **CuÃ¡ndo se usa:** Ventas o servicios prestados a entidades del Estado dominicano.
- **Campos:** IdentificaciÃ³n de la entidad gubernamental compradora.

### E46 â€” Exportaciones
- **CuÃ¡ndo se usa:** Venta de bienes o servicios al exterior.
- **Particularidad:** ITBIS tasa 0%. Requiere documentaciÃ³n aduanera de respaldo.

### E47 â€” Pagos al Exterior
- **CuÃ¡ndo se usa:** Pagos de servicios o remesas a proveedores en el extranjero.
- **Particularidad:** Sujeto a retenciones ISR segÃºn convenios vigentes.

---

### Orden obligatorio en el proceso de certificaciÃ³n DGII:
```
1ro: E31, E32(â‰¥250k), E41, E43, E44, E45, E46, E47
2do: E33, E34  â† referencian documentos emitidos en el 1er grupo
3ro: RFCE-E32  â† flujo especial fc.dgii.gov.do
4to: E32 < 250k â† flujo normal pero monto menor
```
## 4. ESTRUCTURA TÃ‰CNICA DEL XML

### 4.1 Estructura General del e-CF

```xml
<?xml version="1.0" encoding="UTF-8"?>
<ECF xmlns="urn:dgii.gov.do:ecf">
  <Encabezado>
    <Version>1.0</Version>
    <IdDoc>
      <TipoeCF>31</TipoeCF>
      <eNCF>E310000000001</eNCF>
      <FechaVencimientoSecuencia>31-12-2025</FechaVencimientoSecuencia>
      <IndicadorEnvioDiferido>0</IndicadorEnvioDiferido>
      <IndicadorMontoGravado>1</IndicadorMontoGravado>
      <TipoIngresos>01</TipoIngresos>
      <TipoPago>1</TipoPago>
      <FechaLimitePago>31-12-2024</FechaLimitePago>
      <TotalPaginas>1</TotalPaginas>
    </IdDoc>
    <Emisor>
      <RNCEmisor>131234567</RNCEmisor>
      <RazonSocialEmisor>EMPRESA EJEMPLO S.R.L.</RazonSocialEmisor>
      <DireccionEmisor>Calle Principal #1, Santo Domingo</DireccionEmisor>
      <FechaEmision>15-01-2024</FechaEmision>
    </Emisor>
    <Comprador>
      <RNCComprador>101000532</RNCComprador>
      <RazonSocialComprador>EMPRESA COMPRADORA S.A.</RazonSocialComprador>
    </Comprador>
    <Totales>
      <MontoGravadoTotal>1000.00</MontoGravadoTotal>
      <MontoGravadoI1>1000.00</MontoGravadoI1>
      <MontoExento>0.00</MontoExento>
      <ITBIS1>18</ITBIS1>
      <TotalITBIS>180.00</TotalITBIS>
      <TotalITBIS1>180.00</TotalITBIS1>
      <MontoTotal>1180.00</MontoTotal>
    </Totales>
  </Encabezado>
  <DetallesItems>
    <Item>
      <NumeroLinea>1</NumeroLinea>
      <NombreItem>Servicio de consultorÃ­a</NombreItem>
      <IndicadorFacturacion>1</IndicadorFacturacion>
      <CantidadItem>1</CantidadItem>
      <UnidadMedida>Unidad</UnidadMedida>
      <PrecioUnitarioItem>1000.00</PrecioUnitarioItem>
      <TablaSubDescuento>
        <SubDescuento>
          <TipoSubDescuento>01</TipoSubDescuento>
          <PorcentajeDescuento>0.00</PorcentajeDescuento>
        </SubDescuento>
      </TablaSubDescuento>
      <MontoItem>1000.00</MontoItem>
    </Item>
  </DetallesItems>
  <!-- AquÃ­ va la firma digital XMLDSig -->
  <Signature xmlns="http://www.w3.org/2000/09/xmldsig#">
    <!-- Generada automÃ¡ticamente por el sistema -->
  </Signature>
</ECF>
```

### 4.2 Firma Digital XMLDSig

**Algoritmos requeridos por DGII:**
| Elemento | Valor |
|---|---|
| Firma | `http://www.w3.org/2001/04/xmldsig-more#rsa-sha256` |
| Digest | `http://www.w3.org/2001/04/xmlenc#sha256` |
| CanonizaciÃ³n | `http://www.w3.org/TR/2001/REC-xml-c14n-20010315` |
| Transform | `http://www.w3.org/2000/09/xmldsig#enveloped-signature` |

**Estructura de la firma generada:**
```xml
<Signature xmlns="http://www.w3.org/2000/09/xmldsig#">
  <SignedInfo>
    <CanonicalizationMethod Algorithm="http://www.w3.org/TR/2001/REC-xml-c14n-20010315"/>
    <SignatureMethod Algorithm="http://www.w3.org/2001/04/xmldsig-more#rsa-sha256"/>
    <Reference URI="">
      <Transforms>
        <Transform Algorithm="http://www.w3.org/2000/09/xmldsig#enveloped-signature"/>
      </Transforms>
      <DigestMethod Algorithm="http://www.w3.org/2001/04/xmlenc#sha256"/>
      <DigestValue>BASE64_DEL_DIGEST</DigestValue>
    </Reference>
  </SignedInfo>
  <SignatureValue>BASE64_DE_LA_FIRMA</SignatureValue>
  <KeyInfo>
    <X509Data>
      <X509Certificate>BASE64_DEL_CERTIFICADO</X509Certificate>
    </X509Data>
  </KeyInfo>
</Signature>
```

**Reglas crÃ­ticas para la firma:**
- `URI=""` â†’ firma todo el documento (enveloped).
- `PreserveWhitespace = true` â†’ cualquier reformateo del XML **rompe la firma**.
- `XmlDsigEnvelopedSignatureTransform` â†’ excluye el nodo `<Signature>` del digest.
- `KeyInfoX509Data(cert)` â†’ DGII **requiere** el certificado embebido en la firma.
- La firma siempre va como **Ãºltimo hijo** del elemento raÃ­z.

### 4.3 Reglas de Formato del XML

| Regla | Detalle |
|---|---|
| CodificaciÃ³n | UTF-8 (declarar en `<?xml version="1.0" encoding="UTF-8"?>`) |
| Tags vacÃ­os | **Prohibidos** â€” si un campo no tiene valor, omitir el tag completo |
| Caracteres especiales | Usar entidades XML: `&amp;`, `&lt;`, `&gt;`, `&apos;`, `&quot;` |
| Caracteres invÃ¡lidos XML | No incluir caracteres de control (ASCII 0-31 excepto tab/LF/CR) |
| Campo vacÃ­o en Excel DGII | `#e` = campo no aplica â†’ **no incluir en XML** |
| Fechas | Formato `dd-mm-yyyy` (ej: `15-01-2024`) |
| Montos | Punto decimal, 2 decimales, sin separador de miles (ej: `1180.00`) |
| Booleanos | `0` = No, `1` = SÃ­ |

### 4.4 Formato e-NCF
```
E  +  TT  +  SSSSSSSSSS
â”‚      â”‚          â”‚
â”‚      â”‚          â””â”€â”€ Secuencia: 10 dÃ­gitos (con ceros a la izquierda)
â”‚      â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€ Tipo: 2 dÃ­gitos (31, 32, 33, etc.)
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€ Prefijo electrÃ³nico obligatorio

Ejemplos:
  E310000000001  â†’ Factura CrÃ©dito Fiscal, secuencia 1
  E320000000099  â†’ Factura Consumo, secuencia 99
  E340000000015  â†’ Nota CrÃ©dito, secuencia 15
```

---

## 5. AMBIENTES Y URLs

### 5.1 Tabla de Ambientes

| Ambiente | Dominio Base | PropÃ³sito |
|---|---|---|
| Pre-CertificaciÃ³n | `https://ecf.dgii.gov.do/TesteCF/` | Pruebas libres sin restricciones |
| CertificaciÃ³n | `https://ecf.dgii.gov.do/CerteCF/` | Proceso oficial de certificaciÃ³n |
| ProducciÃ³n | `https://ecf.dgii.gov.do/eCF/` | Solo post-certificaciÃ³n aprobada |

### 5.2 Endpoints por Ambiente

| Servicio | MÃ©todo | URL (reemplazar BASE por el dominio del ambiente) |
|---|---|---|
| Obtener semilla | GET | `{BASE}autenticacion/api/semilla` |
| Validar semilla (token) | POST | `{BASE}autenticacion/api/validacioncertificado` |
| RecepciÃ³n e-CF | POST | `{BASE}recepcion/api/ecf` |
| Consulta resultado (TrackId) | GET | `{BASE}recepcion/api/consultaresultado/{trackId}` |
| AprobaciÃ³n Comercial | POST | `{BASE}aprobacioncomercial/api/ecf` |
| AnulaciÃ³n (ANECF) | POST | `{BASE}anulacion/api/anulacion` |
| Directorio electrÃ³nico | GET | `{BASE}directorio/api/directorio/{rnc}` |
| Estatus servicios | GET | `{BASE}api/estatus` |

### 5.3 Endpoint RFCE (Resumen Facturas Consumo < RD$250,000)

| Ambiente | URL RFCE |
|---|---|
| Test | `https://fc.dgii.gov.do/TesteCF/facturaconsumidor/api/ecf` |
| CertificaciÃ³n | `https://fc.dgii.gov.do/CerteCF/facturaconsumidor/api/ecf` |
| ProducciÃ³n | `https://fc.dgii.gov.do/eCF/facturaconsumidor/api/ecf` |

> **Importante:** El dominio RFCE es `fc.dgii.gov.do`, **diferente** al dominio principal `ecf.dgii.gov.do`.

---

## 6. FLUJO DE AUTENTICACIÃ“N

### 6.1 Paso a Paso

```
1. GET /autenticacion/api/semilla
   â† Respuesta XML:
      <SemillaModel>
        <valor>GUID-ALEATORIO</valor>
        <fecha>2024-01-15T10:00:00</fecha>
      </SemillaModel>

2. Firmar el XML de semilla con el certificado digital (.p12)
   - Mismo algoritmo XMLDSig RSA-SHA256
   - URI=""  (firma todo el documento)

3. POST /autenticacion/api/validacioncertificado
   Body: XML de semilla firmado digitalmente
   Content-Type: application/xml
   
   â† Respuesta XML:
      <TokenModel>
        <token>GUID-DEL-TOKEN</token>
        <expira>2024-01-15T11:00:00</expira>
      </TokenModel>

4. Usar el token:
   Authorization: Bearer GUID-DEL-TOKEN
   (vÃ¡lido durante 1 hora desde emisiÃ³n)
```

### 6.2 Persistencia del Token

- El token tiene validez de **1 hora**.
- **Estrategia recomendada:** Cachear el token con su tiempo de expiraciÃ³n. Renovar cuando falten 5 minutos o cuando la API devuelva `401`.
- **Nunca** solicitar un nuevo token en cada request (impacto de rendimiento y lÃ­mites de rate).

```csharp
// PatrÃ³n de cachÃ© de token (pseudocÃ³digo):
if (_cachedToken == null || DateTime.UtcNow >= _tokenExpiry.AddMinutes(-5))
{
    var semilla = await ObtenerSemillaAsync();
    var semillaFirmada = FirmarXml(semilla, certificado);
    var tokenResponse = await ValidarSemillaAsync(semillaFirmada);
    _cachedToken = tokenResponse.Token;
    _tokenExpiry = tokenResponse.Expira;
}
return _cachedToken;
```

---

## 7. FLUJO COMPLETO DE EMISIÃ“N

```
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚  SISTEMA ERP/EMISOR                                             â”‚
â”œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
â”‚                                                                 â”‚
â”‚  1. Generar XML segÃºn tipo de e-CF                              â”‚
â”‚     â””â”€â”€ Aplicar reglas de negocio (campos obligatorios,         â”‚
â”‚         formato fechas, montos, ITBIS)                          â”‚
â”‚                                                                 â”‚
â”‚  2. Validar XML contra XSD oficial de DGII                      â”‚
â”‚     â””â”€â”€ XSD disponible en portal DGII                           â”‚
â”‚     â””â”€â”€ Si errores: corregir antes de continuar                 â”‚
â”‚                                                                 â”‚
â”‚  3. Firmar digitalmente el XML                                   â”‚
â”‚     â””â”€â”€ XMLDSig + RSA-SHA256 + certificado .p12                 â”‚
â”‚     â””â”€â”€ Verificar que PreserveWhitespace=true                   â”‚
â”‚                                                                 â”‚
â”‚  4. Autenticar â†’ obtener token Bearer                           â”‚
â”‚     â””â”€â”€ GET semilla â†’ firmar â†’ POST validar â†’ token             â”‚
â”‚     â””â”€â”€ Usar token cacheado si aÃºn vÃ¡lido                       â”‚
â”‚                                                                 â”‚
â”‚  5. Enviar e-CF a DGII                                          â”‚
â”‚     POST {BASE}recepcion/api/ecf                                â”‚
â”‚     Headers: Authorization: Bearer {token}                      â”‚
â”‚     Body: XML e-CF firmado (Content-Type: application/xml)      â”‚
â”‚                                                                 â”‚
â”‚  6. Recibir TrackId de DGII                                     â”‚
â”‚     â””â”€â”€ Guardar TrackId para consulta posterior                 â”‚
â”‚                                                                 â”‚
â”‚  7. Consultar estado (polling)                                   â”‚
â”‚     GET {BASE}recepcion/api/consultaresultado/{trackId}         â”‚
â”‚     â””â”€â”€ Reintentar cada 5-10 segundos hasta estado definitivo   â”‚
â”‚                                                                 â”‚
â”‚  ESTADOS POSIBLES DE DGII:                                      â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚  â”‚ 0 = No encontrado (TrackId invÃ¡lido o no procesado aÃºn)  â”‚   â”‚
â”‚  â”‚ 1 = Aceptado âœ…                                           â”‚   â”‚
â”‚  â”‚ 2 = Rechazado âŒ (ver mensajes de error en respuesta)    â”‚   â”‚
â”‚  â”‚ 3 = En proceso â³ (seguir consultando)                   â”‚   â”‚
â”‚  â”‚ 4 = Aceptado Condicional âš ï¸ (revisar observaciones)     â”‚   â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â”‚                                                                 â”‚
â”‚  8. Si Aceptado (1 o 4): Enviar e-CF al receptor               â”‚
â”‚     â””â”€â”€ Buscar en directorio DGII la URL del receptor           â”‚
â”‚     â””â”€â”€ POST a URL RecepciÃ³n del receptor                       â”‚
â”‚                                                                 â”‚
â”‚  9. Receptor envÃ­a Acuse de Recibo (ARECF)                      â”‚
â”‚     â””â”€â”€ Estados ARECF: 0=Recibido, 1=No Recibido               â”‚
â”‚                                                                 â”‚
â”‚  10. AprobaciÃ³n Comercial (ACECF) â€” si el receptor la emite    â”‚
â”‚     â””â”€â”€ POST /aprobacioncomercial/api/ecf                       â”‚
â”‚     â””â”€â”€ Estados ACECF: 1=Aceptado, 2=Rechazado                 â”‚
â”‚                                                                 â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
```

### 7.1 Estructura de Respuesta DGII (Consulta TrackId)

```xml
<consultaResultadoEmision>
  <trackId>ABC123</trackId>
  <codigo>1</codigo>
  <estado>Aceptado</estado>
  <eNCF>E310000000001</eNCF>
  <mensajes>
    <mensaje>
      <valor>El documento ha sido procesado exitosamente</valor>
    </mensaje>
  </mensajes>
</consultaResultadoEmision>
```

---

## 8. FLUJO RFCE (Resumen Consumo < RD$250,000)

### 8.1 CuÃ¡ndo Aplica
- **Solo** para `E32` (Factura de Consumo) con monto **menor a RD$250,000**.
- Es un flujo **simplificado** â€” DGII asigna el e-NCF automÃ¡ticamente.
- No requiere e-NCF previo ni validaciÃ³n XSD estricta del XML de items.

### 8.2 Diferencias con el Flujo Normal

| CaracterÃ­stica | Flujo Normal | RFCE |
|---|---|---|
| Endpoint | `ecf.dgii.gov.do` | `fc.dgii.gov.do` |
| e-NCF | Generado por el emisor | Asignado por DGII en respuesta |
| Respuesta | TrackId â†’ consulta | Respuesta inmediata con e-NCF |
| AutenticaciÃ³n | Token Bearer | Token Bearer (mismo flujo) |
| Monto | Cualquiera (E32 â‰¥ 250k usa flujo normal) | < RD$250,000 |

### 8.3 Respuesta RFCE

```json
{
  "codigo": "200",
  "estado": "Aceptado",
  "encf": "E320000000099",
  "secuenciaUtilizada": "99",
  "mensajes": []
}
```

### 8.4 Flujo RFCE Paso a Paso

```
1. Generar XML E32 (sin e-NCF â€” DGII lo asignarÃ¡)
2. Firmar XML con certificado .p12
3. Obtener token Bearer (mismo flujo de autenticaciÃ³n)
4. POST fc.dgii.gov.do/{ambiente}/facturaconsumidor/api/ecf
   Authorization: Bearer {token}
   Body: XML E32 firmado
5. DGII responde con e-NCF asignado, estado y secuencia
6. Guardar e-NCF recibido en la base de datos
7. Usar e-NCF para generar la RepresentaciÃ³n Impresa
```
## 9. PROCESO DE CERTIFICACIÃ“N DGII (14 PASOS)

### Etapa 1 â€” Solicitud de Ingreso al Proceso
- Completar Formulario FI-GDF-016 con:
  - Datos del proveedor de software (nombre, RNC, contacto tÃ©cnico).
  - **URL RecepciÃ³n** del sistema receptor (ej: `https://cloud.wptsoftwares.net/fe/recepcion/api/ecf`).
  - **URL AprobaciÃ³n Comercial** (ej: `.../fe/aprobacioncomercial/api/ecf`).
  - **URL AutenticaciÃ³n** (ej: `.../fe/autenticacion/api/semilla`).
  - VersiÃ³n del software.
- Entregar a la DGII (presencial u OFV).
- DGII asigna un **analista de certificaciÃ³n** y da acceso al ambiente CerteCF.

### Etapa 2 â€” Set de Pruebas (Pasos 1â€“13)

**Paso 1: Registro del Software**
- En el portal de certificaciÃ³n, registrar el software con las URLs informadas.
- DGII configura sus sistemas para llamar a estos endpoints durante las pruebas.
- Verificar que los endpoints son accesibles desde Internet (no localhost).

**Paso 2: Pruebas con Excel Oficial de DGII**
- DGII provee un archivo Excel con los casos de prueba (datos de facturas).
- Generar los XML correspondientes a cada caso.
- **Campos `#e`** en el Excel = campo no aplica â†’ omitir en el XML.

**Orden obligatorio de envÃ­o:**
```
1ro: E31, E32(â‰¥250k), E41, E43, E44, E45, E46, E47
2do: E33, E34  â† referencian eNCF del grupo anterior
3ro: RFCE-E32  â† vÃ­a fc.dgii.gov.do
4to: E32 < RD$250,000 (flujo normal)
```

**Paso 3: Prueba de AutenticaciÃ³n**
- DGII llama a `GET /semilla` de tu sistema.
- Tu sistema genera y devuelve el XML de semilla.
- DGII firma la semilla y la envÃ­a a `POST /validacioncertificado`.
- Tu sistema valida y devuelve token.

**Paso 4: RecepciÃ³n de e-CF**
- DGII envÃ­a e-CF firmados a `POST /recepcion/api/ecf`.
- Tu sistema devuelve ARECF firmado.
- DGII verifica la firma del ARECF con el certificado embebido.

**Paso 5: RepresentaciÃ³n Impresa (PDF)**
- Generar PDF de cada e-CF con todos los campos obligatorios (ver SecciÃ³n 10).
- El PDF no debe superar **10 MB**.
- Subir al portal de certificaciÃ³n DGII.

**Paso 6: Prueba AprobaciÃ³n Comercial**
- DGII envÃ­a ACECF a `POST /aprobacioncomercial/api/ecf`.
- Tu sistema registra y responde.

**Pasos 7â€“12: Verificaciones Adicionales**
- DGII verifica consistencia de datos, secuencias, montos, ITBIS.
- Analista DGII revisa logs y respuestas del sistema.
- Posibles correcciones y reenvÃ­os en esta etapa.

**Paso 13: DeclaraciÃ³n Jurada**
- XML firmado digitalmente que certifica que el software cumple las normas.
- Debe ser firmado por el **Firmante** (rol delegado en OFV).
- Estructura proporcionada por DGII.
- Subir en el portal de certificaciÃ³n.

### Etapa 3 â€” CertificaciÃ³n y HabilitaciÃ³n
- DGII revisa toda la documentaciÃ³n y pruebas.
- Si aprueba: emite **Certificado de Proveedor Autorizado**.
- Habilita acceso al ambiente de ProducciÃ³n (`eCF`).
- El contribuyente puede comenzar a emitir e-CF reales.

---

## 10. REPRESENTACIÃ“N IMPRESA (RI)

### 10.1 Campos Obligatorios en el PDF/Documento FÃ­sico

| Campo | DescripciÃ³n |
|---|---|
| e-NCF | NÃºmero completo (ej: E310000000001) â€” bien visible |
| Fecha Vencimiento Secuencia | Fecha lÃ­mite de validez del e-NCF |
| CÃ³digo de Seguridad | Hash/cÃ³digo generado por el sistema emisor |
| Fecha y Hora de Firma | Timestamp exacto de la firma digital |
| CÃ³digo QR | URL de verificaciÃ³n en portal DGII |
| RNC y razÃ³n social del emisor | Datos completos del emisor |
| RNC y razÃ³n social del comprador | Si aplica (E31 es obligatorio) |
| DirecciÃ³n del emisor | DirecciÃ³n fiscal registrada |
| Desglose ITBIS por Ã­tem | Tasa y monto ITBIS por cada lÃ­nea |
| Subtotal, ITBIS total, Total | Resumen de montos |

### 10.2 CÃ³digo QR
- URL formato: `https://ecf.dgii.gov.do/ConsultaECF?eNCF={eNCF}&RNCEmisor={rnc}`
- El ciudadano puede escanear para verificar autenticidad en portal DGII.

### 10.3 Reglas del PDF
- TamaÃ±o mÃ¡ximo: **10 MB** (para el paso 5 de certificaciÃ³n).
- Debe ser legible e incluir todos los campos obligatorios.
- La RI no reemplaza al XML â€” ambos son necesarios.
- La RI puede generarse en papel o formato digital (PDF).

---

## 11. COMUNICACIÃ“N EMISOR-RECEPTOR

### 11.1 Directorio ElectrÃ³nico DGII
- Los receptores e-CF registran sus URLs en el directorio de la DGII.
- El emisor consulta el directorio antes de enviar:
  ```
  GET {BASE}directorio/api/directorio/{rncReceptor}
  ```
- Respuesta incluye: URL RecepciÃ³n, URL AprobaciÃ³n Comercial del receptor.

### 11.2 Acuse de Recibo (ARECF)
El receptor (tu sistema en este proyecto) debe responder con ARECF firmado:

```xml
<ARECF>
  <RNCEmisor>131234567</RNCEmisor>
  <RNCComprador>101000532</RNCComprador>
  <eNCF>E310000000001</eNCF>
  <FechaHoraRecepcion>15-01-2024 10:30:00</FechaHoraRecepcion>
  <Estado>0</Estado>
  <!-- Si Estado=1 (No Recibido): -->
  <!-- <Motivo>2</Motivo> -->
</ARECF>
```

**Estados ARECF:**
| Valor | Significado |
|---|---|
| 0 | Recibido correctamente |
| 1 | No recibido (con motivo) |

**Motivos de No Recibido:**
| CÃ³digo | Motivo |
|---|---|
| 1 | Error de especificaciÃ³n (XML malformado) |
| 2 | Error de firma digital |
| 3 | EnvÃ­o duplicado (eNCF ya recibido) |
| 4 | RNC no corresponde |

### 11.3 AprobaciÃ³n Comercial (ACECF)
```xml
<ACECF>
  <RNCEmisor>131234567</RNCEmisor>
  <RNCComprador>101000532</RNCComprador>
  <eNCF>E310000000001</eNCF>
  <FechaHoraAprobacion>15-01-2024 11:00:00</FechaHoraAprobacion>
  <Estado>1</Estado>
  <!-- 1=Aceptado, 2=Rechazado -->
</ACECF>
```

---

## 12. ANULACIÃ“N (ANECF)

### 12.1 CuÃ¡ndo Anular
- e-NCF emitido pero no enviado a DGII (error antes del envÃ­o).
- Secuencias asignadas no utilizadas (ej: fin de rango).
- Error grave en el documento que no puede corregirse con Nota de CrÃ©dito.
- **Nota:** Un e-CF ya aceptado por DGII se corrige con E34 (Nota de CrÃ©dito), no con anulaciÃ³n.

### 12.2 Estructura XML ANECF
```xml
<ANECF>
  <RNCEmisor>131234567</RNCEmisor>
  <RazonSocialEmisor>EMPRESA EJEMPLO S.R.L.</RazonSocialEmisor>
  <FechaAnulacion>15-01-2024</FechaAnulacion>
  <Rangos>
    <Rango>
      <TipoeCF>31</TipoeCF>
      <eNCFDesde>E310000000050</eNCFDesde>
      <eNCFHasta>E310000000099</eNCFHasta>
    </Rango>
  </Rangos>
</ANECF>
```

### 12.3 LÃ­mites de AnulaciÃ³n
| RestricciÃ³n | LÃ­mite |
|---|---|
| Tipos de e-CF por ANECF | MÃ¡ximo 10 tipos |
| Secuencias por tipo | MÃ¡ximo 10,000 secuencias por tipo |

---

## 13. ERRORES COMUNES Y SOLUCIONES

| CÃ³digo/Error | Causa | SoluciÃ³n |
|---|---|---|
| **Firma invÃ¡lida** | `PreserveWhitespace=false` o reformateo del XML | Siempre `PreserveWhitespace=true`; no re-serializar el XML tras firmar |
| **Firma invÃ¡lida** | Certificado diferente al registrado en DGII | Usar exactamente el `.p12` registrado en OFV |
| **XSD validation error** | Tag vacÃ­o (`<Campo></Campo>`) | Omitir el tag si no tiene valor |
| **XSD validation error** | CarÃ¡cter invÃ¡lido en texto | Sanear con `SecurityElement.Escape()` o equivalente |
| **RNC inactivo** | RNC del emisor/comprador no activo en DGII | Verificar estado RNC en dgii.gov.do antes de emitir |
| **eNCF ya utilizado** | Secuencia duplicada | Llevar contador de secuencia en BD; nunca reusar |
| **eNCF fuera de rango** | Secuencia no autorizada por DGII | Solicitar nuevo rango en OFV antes de agotarlo |
| **Token expirado (401)** | Token usado despuÃ©s de 1 hora | Implementar renovaciÃ³n automÃ¡tica con cachÃ© + margen 5 min |
| **Semilla expirada** | Demora entre obtener y usar la semilla | Usar semilla inmediatamente tras obtenerla (< 5 min) |
| **500 en consulta TrackId** | TrackId invÃ¡lido o sistema DGII en mantenimiento | Reintentar con backoff exponencial; loggear el TrackId |
| **Cert no carga (.p12)** | Ruta incorrecta, password errÃ³neo, archivo corrupto | Validar con `/api/admin/validar-certificado` |
| **Error en RFCE** | eNCF incluido en el XML (DGII lo asigna) | No incluir eNCF en XML para RFCE |
| **AceptadoCondicional (4)** | Campos con observaciones pero dentro de tolerancia | Revisar mensajes de observaciÃ³n; corregir en prÃ³ximas emisiones |

---

## 14. CHECKLIST PRE-PRODUCCIÃ“N

### Infraestructura
- [ ] Sistema desplegado en servidor con IP/dominio pÃºblico accesible desde Internet.
- [ ] HTTPS habilitado con certificado SSL vÃ¡lido (no autofirmado).
- [ ] URLs de producciÃ³n configuradas en `appsettings.Production.json`.
- [ ] `appsettings.Production.json` en `.gitignore` (nunca en repositorio).
- [ ] Swagger deshabilitado en producciÃ³n.
- [ ] Endpoint `firmar-semilla-test` eliminado o protegido.
- [ ] Endpoint `/api/admin/*` protegido con autenticaciÃ³n API Key.

### Certificados
- [ ] Certificado `.p12` de producciÃ³n obtenido de entidad certificadora autorizada.
- [ ] Certificado almacenado en ruta segura (no dentro de la carpeta wwwroot).
- [ ] Ruta y contraseÃ±a del certificado en tabla Nucleo (BD), no en cÃ³digo.
- [ ] Verificar vigencia del certificado (alertar 30 dÃ­as antes de vencimiento).
- [ ] `CertificadosConfig.FailFast = true` en producciÃ³n.

### Base de Datos
- [ ] BD propia del sistema (WPTServiciosDGII) con migraciones aplicadas.
- [ ] Usuario SQL con permisos mÃ­nimos (`db_datareader` en tabla Nucleo).
- [ ] Backup automÃ¡tico configurado.
- [ ] Prueba de conexiÃ³n exitosa: `POST /api/admin/validar-certificado`.

### e-NCF y Secuencias
- [ ] Secuencias de producciÃ³n solicitadas y aprobadas en OFV.
- [ ] Contador de secuencia inicializado correctamente en BD.
- [ ] Alerta configurada cuando quedan < 100 secuencias disponibles.
- [ ] Proceso de solicitud de nuevas secuencias documentado.

### Validaciones DGII
- [ ] ValidaciÃ³n real de firma implementada en `validacioncertificado` (`SignedXml.CheckSignature()`).
- [ ] Todos los tipos de e-CF probados en CerteCF antes de producciÃ³n.
- [ ] Respuesta ARECF firmada correctamente verificada por DGII.
- [ ] Logs de auditorÃ­a completos y funcionando (`LogInteraccion`).

### OperaciÃ³n
- [ ] Monitoreo de salud: `GET /health` con alerta automÃ¡tica.
- [ ] Proceso documentado para renovaciÃ³n de certificados.
- [ ] Proceso documentado para registro de nuevos tenants.
- [ ] Equipo capacitado en procedimiento de troubleshooting.
- [ ] Contacto con analista DGII documentado para soporte.

---

## 15. LO QUE NORMALMENTE SE OLVIDA O CAUSA PROBLEMAS

### ðŸ”´ CrÃ­ticos (causan rechazo inmediato en DGII)

1. **`PreserveWhitespace = false`** â€” El error mÃ¡s comÃºn. Cualquier serializaciÃ³n del XML despuÃ©s de la firma invalida el digest. Nunca reformatear, indentar ni cambiar el XML firmado.

2. **Tags vacÃ­os en el XML** â€” DGII rechaza `<Campo></Campo>` o `<Campo/>`. Si el valor es `#e` en el Excel, no incluir el tag.

3. **Orden incorrecto en certificaciÃ³n** â€” Enviar E33/E34 antes de los documentos que referencian. Siempre: primero los documentos base, luego las notas.

4. **Certificado no embebido en KeyInfo** â€” Sin `KeyInfoX509Data(cert)`, DGII no puede verificar la firma.

5. **eNCF duplicado** â€” Usar la misma secuencia dos veces. Llevar control estricto en BD con constraint UNIQUE.

6. **URL del sistema no accesible** â€” El sistema debe ser accesible desde los servidores DGII durante las pruebas de certificaciÃ³n. No usar localhost ni IPs privadas.

### ðŸŸ¡ Frecuentes (causan problemas operativos)

7. **Token no cacheado** â€” Solicitar nueva autenticaciÃ³n en cada request satura el sistema y puede ser bloqueado por DGII. Implementar cachÃ© con renovaciÃ³n proactiva.

8. **No manejar estado "3 = En proceso"** â€” El sistema consulta TrackId una vez y da el documento por rechazado. Implementar polling con reintentos (hasta 5 intentos con 10s de espera).

9. **Fechas en formato incorrecto** â€” DGII espera `dd-mm-yyyy`. Usar `yyyy-MM-dd` (ISO) causa rechazo XSD.

10. **Caracteres especiales sin escapar** â€” Razones sociales con `&`, `<`, `>` sin encodear. Usar `SecurityElement.Escape()`.

11. **Certificado con path relativo** â€” La ruta del `.p12` en Nucleo debe ser absoluta. Paths relativos fallan en IIS porque el working directory cambia.

12. **No renovar certificado a tiempo** â€” El sistema falla silenciosamente si el `.p12` expira. Implementar alerta 30 dÃ­as antes.

13. **RFCE con eNCF incluido** â€” En el flujo RFCE, el emisor NO debe generar el eNCF; DGII lo asigna en la respuesta.

14. **Monto ITBIS no coincide** â€” El ITBIS calculado Ã­tem por Ã­tem debe coincidir con `TotalITBIS`. DGII lo recalcula y rechaza diferencias.

15. **No guardar el XML original recibido** â€” En caso de disputas o auditorÃ­as, el XML original (antes de cualquier procesamiento) debe estar almacenado Ã­ntegro en BD.

16. **Swagger en producciÃ³n** â€” Expone toda la API sin autenticaciÃ³n. Deshabilitar en `Program.cs` para entornos no-desarrollo.

17. **Semilla reutilizada** â€” Cada semilla es de un solo uso. Si se reintenta la autenticaciÃ³n, obtener una semilla nueva.

18. **Ignorar AceptadoCondicional (estado 4)** â€” Muchos sistemas solo manejan Aceptado/Rechazado. El estado 4 es vÃ¡lido pero tiene observaciones que deben revisarse.

---

*Documento generado: Abril 2026*
*Basado en: Norma General 01-2020, Decreto 587-24, Ley 32-23, documentaciÃ³n tÃ©cnica DGII*
*Proyecto: URL-WPT Â· Repositorio: HmesaG/URL-WPT*
