namespace WPTServiciosDGII.Core.Interfaces;

/// <summary>
/// Resuelve la cadena de conexión correcta para un tenant dado.
/// El tenant se identifica mediante el header HTTP X-Db-Tenant.
/// Si no se provee header, se usa el tenant por defecto configurado en appsettings.json.
/// </summary>
public interface IDbResolver
{
    /// <summary>
    /// Retorna la cadena de conexión para el tenant indicado.
    /// Lanza <see cref="KeyNotFoundException"/> si el tenant no existe.
    /// </summary>
    string GetConnectionString(string? tenantKey = null);

    /// <summary>
    /// Registra o reemplaza un tenant en tiempo de ejecución (sin reiniciar la app).
    /// </summary>
    void RegisterTenant(string tenantKey, string connectionString, string description = "");

    /// <summary>
    /// Lista los tenants actualmente registrados (sin exponer passwords).
    /// </summary>
    IReadOnlyDictionary<string, string> ListTenants();
}
