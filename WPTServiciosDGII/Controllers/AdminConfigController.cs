using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace WPTServiciosDGII.Controllers
{
    [ApiController]
    [Route("api/admin")]
    public class AdminConfigController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;

        public AdminConfigController(IConfiguration configuration, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _env = env;
        }

        [HttpGet("config")]
        public IActionResult GetConfig()
        {
            return Ok(new
            {
                InstanceName = _configuration["AppSettings:InstanceName"] ?? "Unknown",
                DefaultConnection = _configuration.GetConnectionString("WptDatabase"),
                ExternalDb = _configuration["ExternalDbConfig:Tenants:comercial:ConnectionString"],
                Environment = _env.EnvironmentName
            });
        }

        [HttpPost("database")]
        public async Task<IActionResult> UpdateDatabase([FromBody] UpdateDbRequest request)
        {
            try
            {
                string appSettingsPath = Path.Combine(_env.ContentRootPath, "appsettings.json");
                if (!System.IO.File.Exists(appSettingsPath))
                {
                    return NotFound("No se encontró el archivo appsettings.json");
                }

                // Leer el archivo actual
                string json = await System.IO.File.ReadAllTextAsync(appSettingsPath);
                var root = JsonNode.Parse(json);

                if (root == null) return BadRequest("Error al parsear el archivo JSON");

                // Actualizar ConnectionStrings
                if (!string.IsNullOrEmpty(request.MainConnectionString))
                {
                    root["ConnectionStrings"]!["WptDatabase"] = request.MainConnectionString;
                }

                // Actualizar ExternalDbConfig (Tenant comercial por defecto)
                if (!string.IsNullOrEmpty(request.ExternalConnectionString))
                {
                    if (root["ExternalDbConfig"]?["Tenants"]?["comercial"] != null)
                    {
                        root["ExternalDbConfig"]!["Tenants"]!["comercial"]!["ConnectionString"] = request.ExternalConnectionString;
                    }
                }

                // Actualizar nombre de instancia si viene
                if (!string.IsNullOrEmpty(request.InstanceName))
                {
                    if (root["AppSettings"] != null)
                    {
                        root["AppSettings"]!["InstanceName"] = request.InstanceName;
                    }
                }

                // Guardar cambios con formato indentado
                var options = new JsonSerializerOptions { WriteIndented = true };
                await System.IO.File.WriteAllTextAsync(appSettingsPath, root.ToJsonString(options));

                return Ok(new { message = "Configuración actualizada correctamente. La API se recargará automáticamente.", instance = request.InstanceName });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }
    }

    public class UpdateDbRequest
    {
        public string? InstanceName { get; set; }
        public string? MainConnectionString { get; set; }
        public string? ExternalConnectionString { get; set; }
    }
}
