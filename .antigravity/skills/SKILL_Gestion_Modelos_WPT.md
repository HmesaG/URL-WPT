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

### 3. Promoción (Réplica)
- Una vez verificado, se utiliza el script de promoción para llevar los binarios a los modelos productivos.
- **Comando:** `.\Tools\Promocionar-Cambios.ps1 -Destino "eXcomercial"`
- **Regla de Oro:** Nunca sobreescribir el `appsettings.json` de un modelo productivo para preservar su conexión a base de datos específica.

### 4. Configuración Post-Réplica
- Si se crea un nuevo modelo, se debe usar el **Admin API** para establecer su nombre de instancia y base de datos:
  `POST /api/admin/database`

## Mantenimiento de Archivos
- Los archivos `.p12` locales deben permanecer en `Certificados_Local/`.
- Los logs deben ser monitoreados individualmente por carpeta de instancia.

---
*Este Skill asegura que el crecimiento del sistema sea ordenado y que los errores nunca lleguen a producción sin ser detectados en el modelo de pruebas.*
