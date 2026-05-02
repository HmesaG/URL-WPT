using System.Security.Cryptography.X509Certificates;
using WPTServiciosDGII.Core.Interfaces;

namespace WPTServiciosDGII.Infrastructure.Security;

/// <summary>
/// Carga certificados .p12 por petición con política Fail-Fast.
/// SEGURIDAD:
///   - Sin caché: cada llamada carga y hace Dispose al finalizar.
///   - Las contraseñas NUNCA aparecen en logs.
///   - Soporta carga desde disco (ruta) o desde BD (bytes varbinary).
///   - Si el certificado es inválido → excepción inmediata (Fail-Fast).
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

    // ── Desde ruta en disco ───────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task EjecutarConCertificadoAsync(
        string rutaArchivo,
        string password,
        Func<X509Certificate2, Task> accion)
    {
        if (string.IsNullOrWhiteSpace(rutaArchivo) || !File.Exists(rutaArchivo))
        {
            var msg = $"Certificado no encontrado en ruta: {rutaArchivo}";
            _logger.LogError("❌ {Msg}", msg);
            if (_failFast) throw new FileNotFoundException(msg);
        }

        var certBytes = await File.ReadAllBytesAsync(rutaArchivo);
        await EjecutarConCertificadoBytesAsync(certBytes, password, accion);
    }

    /// <inheritdoc/>
    public bool ValidarCertificado(string rutaArchivo, string password, out string error)
    {
        error = string.Empty;
        try
        {
            if (!File.Exists(rutaArchivo))
            {
                error = $"Archivo no encontrado: {rutaArchivo}";
                return false;
            }
            var bytes = File.ReadAllBytes(rutaArchivo);
            return ValidarCertificadoBytes(bytes, password, out error);
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    // ── Desde bytes en BD (varbinary) ─────────────────────────────────────────

    /// <inheritdoc/>
    public async Task EjecutarConCertificadoBytesAsync(
        byte[] certBytes,
        string password,
        Func<X509Certificate2, Task> accion)
    {
        if (certBytes is null || certBytes.Length == 0)
        {
            var msg = "El certificado digital está vacío o es nulo.";
            _logger.LogError("❌ {Msg}", msg);
            if (_failFast) throw new ArgumentException(msg);
        }

        X509Certificate2? cert = null;
        try
        {
            cert = CargarDesdeBytes(certBytes!, password);
            _logger.LogInformation("🔐 Certificado cargado desde BD: Subject='{Subject}', Expira={Exp}",
                cert.Subject, cert.NotAfter.ToString("yyyy-MM-dd"));

            // Ejecutar la acción con timeout
            using var cts = new CancellationTokenSource(_timeoutMs);
            await accion(cert).WaitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            _logger.LogError("⏱️  Timeout al ejecutar acción con certificado ({Ms}ms).", _timeoutMs);
            throw new TimeoutException($"La operación con el certificado superó el límite de {_timeoutMs}ms.");
        }
        finally
        {
            // Dispose explícito — sin caché
            cert?.Dispose();
        }
    }

    /// <inheritdoc/>
    public bool ValidarCertificadoBytes(byte[] certBytes, string password, out string error)
    {
        error = string.Empty;
        try
        {
            using var cert = CargarDesdeBytes(certBytes, password);
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

    // ── Helpers privados ──────────────────────────────────────────────────────

    private X509Certificate2 CargarDesdeBytes(byte[] certBytes, string password)
    {
        try
        {
            // EphemeralKeySet: evita escribir la clave privada en disco/perfil de usuario
            return new X509Certificate2(
                certBytes,
                password,
                X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.MachineKeySet);
        }
        catch (Exception ex)
        {
            _logger.LogError("❌ No se pudo cargar el certificado .p12. " +
                             "Verifique que la contraseña sea correcta. Error: {Tipo}", ex.GetType().Name);
            if (_failFast)
                throw new InvalidOperationException(
                    "Fail-Fast: No se pudo cargar el certificado .p12. " +
                    "Verifique la contraseña configurada en la tabla Nucleo.", ex);
            throw;
        }
    }
}
