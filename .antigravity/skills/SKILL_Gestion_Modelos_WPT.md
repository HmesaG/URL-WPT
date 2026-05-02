# SKILL: Gestión y Replicación de Modelos WPT

Este Skill define el flujo de trabajo obligatorio para mantener la integridad de las múltiples instancias de la API (Multi-tenant/Multi-model).

## Principio Fundamental
**WPTExecutor** es el modelo de referencia (Staging). Ningún cambio debe aplicarse directamente a otros modelos (como eXcomercial) sin haber sido validado primero en WPTExecutor.

## Flujo de Trabajo Operativo

### 1. Desarrollo y Mejora
- Todo cambio de código se realiza en el proyecto base `WPTServiciosDGII`.
- Se compila y se publica inicialmente en `e:\Empresas\GMV\Proyectos Antigravity\URL WPT\WPTExecutor`.

### 2. Validación
- Se debe verificar el funcionamiento correcto accediendo a:
  `https://cloud.wptsoftwares.net/WPTexecutor/swagger/index.html`
- Se realizan pruebas de recepción de e-CF y firma digital.

### 3. Promoción a Producción o Nuevos Modelos
- Una vez validado en WPTExecutor, se utiliza el script `Tools/Promocionar-Cambios.ps1` para replicar los binarios a `eXcomercial` u otros destinos.
- **IMPORTANTE**: No olvidar actualizar el `InstanceName` en el `appsettings.json` de la nueva carpeta.

## Infraestructura y Despliegue (IIS)

Para registrar las carpetas en el servidor web, se debe utilizar el script `Tools/Configurar_IIS_WPT.ps1`.

### Uso Estándar (Automático)
Simplemente haz clic derecho y **"Ejecutar con PowerShell"** para sincronizar WPTExecutor y eXcomercial.

### Uso con Parámetros (Nuevos Modelos)
Si deseas crear un modelo nuevo (ej. `ClienteNuevo`), abre una terminal de PowerShell como Administrador y ejecuta:

```powershell
.\Tools\Configurar_IIS_WPT.ps1 -InstanceName "ClienteNuevo" -PhysicalPath "C:\Ruta\Al\Proyecto"
```

## Reglas de Oro
1. **NUNCA** editar el código directamente en las carpetas de publicación (`WPTExecutor`, `eXcomercial`).
2. **SIEMPRE** ejecutar el script de configuración de IIS tras crear una nueva carpeta de modelo.
3. **VALIDAR** que el Swagger de cada instancia muestre el `InstanceName` correcto en la descripción.
 API** para establecer su nombre de instancia y base de datos:
  `POST /api/admin/database`

## Mantenimiento de Archivos
- Los archivos `.p12` locales deben permanecer en `Certificados_Local/`.
- Los logs deben ser monitoreados individualmente por carpeta de instancia.

---
*Este Skill asegura que el crecimiento del sistema sea ordenado y que los errores nunca lleguen a producción sin ser detectados en el modelo de pruebas.*
