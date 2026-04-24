using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using WPTServiciosDGII.Core.Interfaces;

namespace WPTServiciosDGII.Infrastructure.Data;

/// <summary>
/// Resuelve cadenas de conexión por tenant de forma dinámica.
/// Los tenants iniciales se leen de appsettings.json (sección ExternalDbConfig:Tenants).
/// Nuevos tenants pueden registrarse en tiempo de ejecución sin reiniciar la app.
/// SEGURIDAD: Las contraseñas en las cadenas de conexión NUNCA se loggean.
/// </summary>
public class DynamicDbResolver : IDbResolver
{
    private readonly ConcurrentDictionary<string, TenantEntry> _tenants = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _defaultTenant;
    private readonly ILogger<DynamicDbResolver> _logger;

    public DynamicDbResolver(IConfiguration config, ILogger<DynamicDbResolver> logger)
    {
        _logger = logger;
        _defaultTenant = config["ExternalDbConfig:DefaultTenant"] ?? "demo";

        // Cargar tenants desde configuración al iniciar
        var tenantsSection = config.GetSection("ExternalDbConfig:Tenants");
        foreach (var tenant in tenantsSection.GetChildren())
        {
            var connStr     = tenant["ConnectionString"] ?? string.Empty;
            var description = tenant["Description"]      ?? string.Empty;

            if (string.IsNullOrWhiteSpace(connStr))
            {
                _logger.LogWarning("⚠️  Tenant '{Tenant}' no tiene cadena de conexión configurada.", tenant.Key);
                continue;
            }

            _tenants[tenant.Key] = new TenantEntry(connStr, description);
            _logger.LogInformation("✅ Tenant BD registrado: '{Tenant}' — {Desc}", tenant.Key, description);
        }
    }

    /// <inheritdoc/>
    public string GetConnectionString(string? tenantKey = null)
    {
        var key = string.IsNullOrWhiteSpace(tenantKey) ? _defaultTenant : tenantKey;

        if (_tenants.TryGetValue(key, out var entry))
            return entry.ConnectionString;

        _logger.LogError("❌ Tenant '{Tenant}' no encontrado. Tenants disponibles: {Lista}",
            key, string.Join(", ", _tenants.Keys));

        throw new KeyNotFoundException(
            $"El tenant de BD '{key}' no está registrado. " +
            $"Verifica el header X-Db-Tenant o la sección ExternalDbConfig en appsettings.json.");
    }

    /// <inheritdoc/>
    public void RegisterTenant(string tenantKey, string connectionString, string description = "")
    {
        if (string.IsNullOrWhiteSpace(tenantKey))
            throw new ArgumentException("La clave del tenant no puede estar vacía.", nameof(tenantKey));

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("La cadena de conexión no puede estar vacía.", nameof(connectionString));

        _tenants[tenantKey] = new TenantEntry(connectionString, description);
        _logger.LogInformation("🔄 Tenant BD actualizado en tiempo de ejecución: '{Tenant}'", tenantKey);
    }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, string> ListTenants()
    {
        // Retorna descripciones, NUNCA las cadenas de conexión completas
        return _tenants.ToDictionary(
            kvp => kvp.Key,
            kvp => string.IsNullOrWhiteSpace(kvp.Value.Description)
                ? "(sin descripción)"
                : kvp.Value.Description,
            StringComparer.OrdinalIgnoreCase);
    }

    // ── Tipos internos ────────────────────────────────────────────────────────
    private sealed record TenantEntry(string ConnectionString, string Description);
}
