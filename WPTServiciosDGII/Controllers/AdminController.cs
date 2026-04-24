using Microsoft.AspNetCore.Mvc;
using WPTServiciosDGII.Core.Interfaces;

namespace WPTServiciosDGII.Controllers;

/// <summary>
/// Endpoints de administración para gestionar tenants de BD en tiempo de ejecución.
/// IMPORTANTE: En producción proteger estos endpoints con autenticación/autorización.
/// </summary>
[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly IDbResolver _dbResolver;
    private readonly ICertificadoLoader _certLoader;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IDbResolver dbResolver,
        ICertificadoLoader certLoader,
        ILogger<AdminController> logger)
    {
        _dbResolver  = dbResolver;
        _certLoader  = certLoader;
        _logger      = logger;
    }

    /// <summary>
    /// Registra o actualiza una BD externa sin reiniciar la aplicación.
    /// El tenant queda disponible inmediatamente para nuevas peticiones.
    /// </summary>
    [HttpPost("register-db")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public IActionResult RegisterDb([FromBody] RegisterDbRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TenantKey))
            return BadRequest(new { error = "TenantKey es requerido." });

        if (string.IsNullOrWhiteSpace(request.ConnectionString))
            return BadRequest(new { error = "ConnectionString es requerida." });

        try
        {
            _dbResolver.RegisterTenant(request.TenantKey, request.ConnectionString, request.Description ?? "");
            _logger.LogInformation("🔄 Tenant registrado vía API: '{Key}'", request.TenantKey);

            return Ok(new
            {
                mensaje   = $"Tenant '{request.TenantKey}' registrado correctamente.",
                timestamp = DateTime.Now
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar tenant '{Key}'", request.TenantKey);
            return StatusCode(500, new { error = "Error interno al registrar el tenant." });
        }
    }

    /// <summary>
    /// Lista los tenants registrados (sin exponer cadenas de conexión).
    /// </summary>
    [HttpGet("tenants")]
    [ProducesResponseType(200)]
    public IActionResult ListTenants()
    {
        var tenants = _dbResolver.ListTenants();
        return Ok(new { tenants, total = tenants.Count });
    }

    /// <summary>
    /// Valida que un certificado .p12 puede cargarse correctamente.
    /// Útil para verificar configuración antes de recibir documentos reales.
    /// </summary>
    [HttpPost("validar-certificado")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public IActionResult ValidarCertificado([FromBody] ValidarCertRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Ruta))
            return BadRequest(new { error = "Ruta del certificado es requerida." });

        var esValido = _certLoader.ValidarCertificado(request.Ruta, request.Password ?? "", out var error);

        if (esValido)
            return Ok(new { valido = true, mensaje = "El certificado se cargó correctamente." });

        return BadRequest(new { valido = false, error });
    }
}

// ── Request DTOs ──────────────────────────────────────────────────────────────

public class RegisterDbRequest
{
    /// <summary>Clave única del tenant (ej: "empresa_001").</summary>
    public string TenantKey       { get; set; } = string.Empty;
    /// <summary>Cadena de conexión completa a la BD externa.</summary>
    public string ConnectionString { get; set; } = string.Empty;
    /// <summary>Descripción legible (opcional).</summary>
    public string? Description    { get; set; }
}

public class ValidarCertRequest
{
    /// <summary>Ruta absoluta al archivo .p12.</summary>
    public string Ruta     { get; set; } = string.Empty;
    /// <summary>Contraseña del .p12 (no se loggea).</summary>
    public string? Password { get; set; }
}
