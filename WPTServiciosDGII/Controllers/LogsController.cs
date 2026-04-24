using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WPTServiciosDGII.Data;

namespace WPTServiciosDGII.Controllers;

[ApiController]
[Route("api/admin")]
public class LogsController : ControllerBase
{
    private readonly WptDbContext _db;

    public LogsController(WptDbContext db)
    {
        _db = db;
    }

    [HttpGet("logs")]
    public async Task<IActionResult> GetLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 25)
    {
        try 
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 25;

            var totalItems = await _db.LogInteracciones.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var logs = await _db.LogInteracciones
                .AsNoTracking()
                .OrderByDescending(l => l.LogInteraccionFecha)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Devolver el objeto con la estructura que la Web espera
            return Ok(new {
                items = logs,
                page = page,
                pageSize = pageSize,
                totalPages = totalPages,
                totalItems = totalItems
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
