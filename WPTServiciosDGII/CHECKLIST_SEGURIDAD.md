# CHECKLIST DE SEGURIDAD — WPT Servicios DGII

## Antes de pasar a producción, verificar TODOS los puntos:

---

### 🔒 Credenciales y Secretos

- [ ] **No hay passwords hardcodeados** en ningún archivo `.cs` (verificar con: `grep -r "Password=" *.cs`)
- [ ] `appsettings.json` NO contiene credenciales reales de producción
- [ ] Las cadenas de conexión de producción están en variables de entorno o `appsettings.Production.json`
- [ ] `appsettings.Production.json` está en `.gitignore` y NO se versiona
- [ ] Los passwords de los `.p12` se leen ÚNICAMENTE desde la tabla Nucleo de la BD externa

---

### 🗄️ Base de Datos Externa (tabla Nucleo)

- [ ] El usuario SQL usado tiene **solo `db_datareader`** en la tabla Nucleo — no escritura
- [ ] La conexión usa `TrustServerCertificate=True` solo en desarrollo; en producción usar certificado válido
- [ ] Los tenants se registran vía `appsettings.json` o `POST /api/admin/register-db` — nunca en código
- [ ] El endpoint `/api/admin/register-db` está protegido con autenticación en producción (pendiente implementar)

---

### 📜 Certificados .p12

- [ ] Los archivos `.p12` están en una ruta accesible por el usuario del App Pool de IIS
- [ ] El usuario del App Pool tiene solo permisos de **lectura** en la carpeta de certificados
- [ ] Los `.p12` NO están dentro del directorio del proyecto web (ni en `wwwroot`)
- [ ] Los `.p12` NO se versionan en Git (están en `.gitignore`)
- [ ] Se verifica la fecha de expiración antes de producción: `POST /api/admin/validar-certificado`
- [ ] La política `FailFast: true` está activa en producción

---

### 📝 Logs

- [ ] Ningún log imprime passwords, cadenas de conexión completas, ni contenido de `.p12`
- [ ] Los logs de error de certificado muestran solo el tipo de excepción, no el mensaje completo (que podría contener datos sensibles)
- [ ] Los logs de `NucleoRepository` no imprimen el valor de `PasswordCertificado`

---

### 🌐 Red y Endpoints

- [ ] CORS está configurado correctamente para los dominios de producción
- [ ] El endpoint `/api/admin/*` no es accesible públicamente sin autenticación
- [ ] HTTPS está forzado en producción (`app.UseHttpsRedirection()` activo)
- [ ] El Swagger UI está deshabilitado o protegido en producción

---

### 🔄 Operación sin Código

- [ ] Se puede cambiar la BD externa editando solo `appsettings.json` (sin recompilar)
- [ ] Se puede registrar un nuevo tenant en tiempo real con `POST /api/admin/register-db`
- [ ] Se puede cambiar los nombres de columnas de Nucleo en `appsettings.json` sin recompilar
- [ ] Se puede actualizar el certificado .p12 reemplazando el archivo — sin recompilar ni reiniciar

---

### 📊 SQL Security

- [ ] Todas las queries usan parámetros (`@Rnc`, `@Estado`) — nunca concatenación de strings
- [ ] No hay queries dinámicas con valores directos del usuario
- [ ] El usuario SQL de la BD externa tiene permisos mínimos (solo `SELECT` en tabla Nucleo)

---

**Firma de revisión:** _______________  
**Fecha:** _______________  
**Ambiente:** [ ] Desarrollo  [ ] Certificación  [ ] Producción
