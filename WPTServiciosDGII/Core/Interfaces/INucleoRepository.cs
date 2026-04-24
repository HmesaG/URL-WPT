namespace WPTServiciosDGII.Core.Interfaces;

/// <summary>
/// Repositorio para consultar datos del emisor en la tabla Nucleo de la BD externa.
/// Los nombres de columnas son configurables en appsettings.json (sección NucleoConfig).
/// </summary>
public interface INucleoRepository
{
    /// <summary>
    /// Busca un emisor activo por su RNC en la tabla Nucleo.
    /// Retorna null si no se encuentra o si el estado no es activo.
    /// </summary>
    /// <param name="rnc">RNC del contribuyente (sin guiones)</param>
    /// <param name="tenantKey">Clave del tenant para resolver la BD correcta</param>
    Task<Core.Dto.NucleoExternoDto?> ObtenerPorRncAsync(string rnc, string? tenantKey = null);
}
