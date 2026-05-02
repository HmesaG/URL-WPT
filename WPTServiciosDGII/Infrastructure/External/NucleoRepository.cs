using Dapper;
using Microsoft.Data.SqlClient;
using WPTServiciosDGII.Core.Dto;
using WPTServiciosDGII.Core.Interfaces;

namespace WPTServiciosDGII.Infrastructure.External;

/// <summary>
/// Consulta la tabla Nucleo en la BD externa usando Dapper.
/// Los nombres de tabla y columnas se leen desde appsettings.json (sección NucleoConfig),
/// lo que permite adaptar a diferentes esquemas sin recompilar.
/// SEGURIDAD: Usa parámetros (@Rnc, @Estado) para evitar SQL Injection.
/// </summary>
public class NucleoRepository : INucleoRepository
{
    private readonly IDbResolver _dbResolver;
    private readonly IConfiguration _config;
    private readonly ILogger<NucleoRepository> _logger;

    // Valores de configuración cargados una vez al construir
    private readonly string _tableName;
    private readonly string _colId;
    private readonly string _colRnc;
    private readonly string _colEstado;
    private readonly string _colRutaCert;
    private readonly string _colPasswordCert;
    private readonly string _estadoActivo;

    public NucleoRepository(
        IDbResolver dbResolver,
        IConfiguration config,
        ILogger<NucleoRepository> logger)
    {
        _dbResolver     = dbResolver;
        _config         = config;
        _logger         = logger;

        // Mapeo de columnas — configurable sin recompilar
        _tableName       = config["NucleoConfig:TableName"]                  ?? "Nucleo";
        _colId           = config["NucleoConfig:ColumnId"]                   ?? "NucleoID";
        _colRnc          = config["NucleoConfig:ColumnRnc"]                  ?? "NucleoRNC";
        _colEstado       = config["NucleoConfig:ColumnEstado"]               ?? "NucleoEstado";
        _colRutaCert     = config["NucleoConfig:ColumnRutaCertificado"]      ?? "NucleoCertificadoDigital";
        _colPasswordCert = config["NucleoConfig:ColumnPasswordCertificado"]  ?? "NucleoPasswordDigital";
        _estadoActivo    = config["NucleoConfig:EstadoActivo"]               ?? "A";
    }

    /// <inheritdoc/>
    public async Task<NucleoExternoDto?> ObtenerPorRncAsync(string rnc, string? tenantKey = null)
    {
        if (string.IsNullOrWhiteSpace(rnc))
        {
            _logger.LogWarning("⚠️  ObtenerPorRncAsync: RNC vacío o nulo.");
            return null;
        }

        var connStr = _dbResolver.GetConnectionString(tenantKey);

        // Query con columnas dinámicas pero parámetros seguros
        var sql = $"""
            SELECT
                [{_colId}]           AS {nameof(NucleoExternoDto.NucleoID)},
                [{_colRnc}]          AS {nameof(NucleoExternoDto.Rnc)},
                [{_colEstado}]       AS {nameof(NucleoExternoDto.Estado)},
                [{_colRutaCert}]     AS {nameof(NucleoExternoDto.CertificadoBytes)},
                [{_colPasswordCert}] AS {nameof(NucleoExternoDto.PasswordCertificado)}
            FROM [{_tableName}]
            WHERE [{_colRnc}] = @Rnc
              AND [{_colEstado}] = @Estado
            """;

        try
        {
            await using var conn = new SqlConnection(connStr);
            var resultado = await conn.QueryFirstOrDefaultAsync<NucleoExternoDto>(
                sql,
                new { Rnc = rnc.Trim(), Estado = _estadoActivo });

            if (resultado is null)
                _logger.LogWarning("⚠️  RNC '{Rnc}' no encontrado o inactivo en tabla {Tabla}.", rnc, _tableName);
            else
                _logger.LogInformation("✅ Emisor encontrado para RNC '{Rnc}'.", rnc);

            return resultado;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al consultar tabla {Tabla} para RNC '{Rnc}'.", _tableName, rnc);
            throw;
        }
    }
}
