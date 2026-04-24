using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WPTServiciosDGII.Models;

[Table("Api_LogInteraccion")]
public class LogInteraccion
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int LogInteraccionId { get; set; }

    public DateTime LogInteraccionFecha { get; set; } = DateTime.Now;

    [MaxLength(50)]
    public string LogInteraccionServicio { get; set; } = string.Empty;  // Autenticacion | Recepcion | AprobacionComercial

    [MaxLength(10)]
    public string LogInteraccionMetodo { get; set; } = string.Empty;    // GET | POST

    [MaxLength(500)]
    public string LogInteraccionEndpoint { get; set; } = string.Empty;

    [MaxLength(50)]
    public string LogInteraccionIpOrigen { get; set; } = string.Empty;

    public string? LogInteraccionRequestBody { get; set; }

    public string? LogInteraccionResponseBody { get; set; }

    [MaxLength(20)]
    public string LogInteraccionEstado { get; set; } = "OK";            // OK | ERROR | TOKEN_INVALIDO

    public int LogInteraccionMsRespuesta { get; set; }

    [MaxLength(15)]
    public string? LogInteraccionRnc { get; set; }

    [MaxLength(100)]
    public string? LogInteraccionTokenId { get; set; }
}
