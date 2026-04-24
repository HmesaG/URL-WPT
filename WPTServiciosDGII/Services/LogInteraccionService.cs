using WPTServiciosDGII.Data;
using WPTServiciosDGII.Models;

namespace WPTServiciosDGII.Services;

public interface ILogInteraccionService
{
    Task RegistrarAsync(
        string servicio,
        string metodo,
        string endpoint,
        string ipOrigen,
        string? requestBody,
        string? responseBody,
        string estado,
        int msRespuesta,
        string? rnc = null,
        string? tokenId = null);
}

public class LogInteraccionService : ILogInteraccionService
{
    private readonly WptDbContext _db;
    private readonly ILogger<LogInteraccionService> _logger;

    public LogInteraccionService(WptDbContext db, ILogger<LogInteraccionService> logger)
    {
        _db     = db;
        _logger = logger;
    }

    public async Task RegistrarAsync(
        string servicio, string metodo, string endpoint,
        string ipOrigen, string? requestBody, string? responseBody,
        string estado, int msRespuesta,
        string? rnc = null, string? tokenId = null)
    {
        try
        {
            var log = new LogInteraccion
            {
                LogInteraccionFecha        = DateTime.Now,
                LogInteraccionServicio     = servicio,
                LogInteraccionMetodo       = metodo,
                LogInteraccionEndpoint     = endpoint,
                LogInteraccionIpOrigen     = ipOrigen,
                LogInteraccionRequestBody  = requestBody,
                LogInteraccionResponseBody = responseBody,
                LogInteraccionEstado       = estado,
                LogInteraccionMsRespuesta  = msRespuesta,
                LogInteraccionRnc          = rnc,
                LogInteraccionTokenId      = tokenId
            };

            _db.LogInteracciones.Add(log);
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // El log nunca debe romper el flujo principal
            _logger.LogWarning(ex, "No se pudo grabar LogInteraccion para {Servicio}", servicio);
        }
    }
}
