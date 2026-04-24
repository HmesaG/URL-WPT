using System.Security.Cryptography.X509Certificates;
using WPTServiciosDGII.Core.Interfaces;

namespace WPTServiciosDGII.Infrastructure.Security;

/// <summary>
/// Carga certificados .p12 por petición con política Fail-Fast.
/// SEGURIDAD:
///   - Sin caché: cada llamada carga desde disco y hace Dispose al finalizar.
///   - Las contraseñas NUNCA aparecen en logs.
///   - Si la ruta o contraseña son inválidas → excepción inmediata (400 Bad Request).
/// </summary>
public class CertificadoLoader : ICertificadoLoader
{
    private readonly ILogger<CertificadoLoader> _logger;
    private readonly bool _failFast;
    private readonly int _timeoutMs;

    public CertificadoLoader(IConfiguration config, ILogger<CertificadoLoader> logger)
    {
        _logger    = logger;
        _failFast  = config.GetValue<bool>("CertificadosConfig:FailFast", true);
        _timeoutMs = config.GetValue<int>("CertificadosConfig:TimeoutCargaMs", 5000);
    }

    /// <inheritdoc/>
    public async Task EjecutarConCertificadoAsync(
        string rutaArchivo,
        string password,
        Func<X509Certificate2, Task> accion)
    {
        ValidarParametros(rutaArchivo);

        X509Certificate2? cert = null;
        try
        {
            cert = CargarCertificado(rutaArchivo, password);
            _logger.LogInformation("🔐 Certificado cargado: Subject='{Subject}', Expira={Exp}",
                cert.Subject, cert.NotAfter.ToString("yyyy-MM-dd"));

            // Ejecutar la acción con timeout
            using var cts = new CancellationTokenSource(_timeoutMs);
            await accion(cert).WaitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            _logger.LogError("⏱️  Timeout al ejecutar acción con certificado ({Ms}ms). Ruta: {Ruta}",
                _timeoutMs, rutaArchivo);
            throw new TimeoutException($"La operación con el certificado superó el límite de {_timeoutMs}ms.");
        }
        finally
        {
            // Dispose explícito — sin caché
            cert?.Dispose();
        }
    }

    /// <inheritdoc/>
    public bool ValidarCertificado(string rutaArchivo, string password, out string error)
    {
        error = string.Empty;
        try
        {
            ValidarParametros(rutaArchivo);
            using var cert = CargarCertificado(rutaArchivo, password);
            if (cert.NotAfter < DateTime.Now)
            {
                error = $"El certificado expiró el {cert.NotAfter:yyyy-MM-dd}.";
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    // ── Métodos privados ──────────────────────────────────────────────────────

    private void ValidarParametros(string rutaArchivo)
    {
        if (string.IsNullOrWhiteSpace(rutaArchivo))
        {
            if (_failFast)
                throw new ArgumentException("La ruta del certificado .p12 no puede estar vacía.");
        }

        if (!File.Exists(rutaArchivo))
        {
            _logger.LogError("❌ Certificado no encontrado: {Ruta}", rutaArchivo);
            if (_failFast)
                throw new FileNotFoundException(
                    $"El certificado .p12 no existe en la ruta configurada: {rutaArchivo}");
        }
    }

    private X509Certificate2 CargarCertificado(string rutaArchivo, string password)
    {
        try
        {
            // X509KeyStorageFlags.EphemeralKeySet: evita escribir la clave privada en disco/perfil
            return new X509Certificate2(
                rutaArchivo,
                password,
                X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.MachineKeySet);
        }
        catch (Exception ex)
        {
            // Log sin exponer la contraseña
            _logger.LogError("❌ No se pudo cargar el certificado desde '{Ruta}'. " +
                             "Verifique que la contraseña sea correcta. Error: {Tipo}",
                             rutaArchivo, ex.GetType().Name);

            if (_failFast)
                throw new InvalidOperationException(
                    $"Fail-Fast: No se pudo cargar el certificado .p12. " +
                    $"Verifique la ruta y contraseña configuradas en la tabla Nucleo.", ex);
            throw;
        }
    }
}
