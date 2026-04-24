using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WPTServiciosDGII.Models;

[Table("Api_TokenEmitido")]
public class TokenEmitido
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int TokenEmitidoId { get; set; }

    [MaxLength(2048)]
    public string TokenEmitidoValor { get; set; } = string.Empty;

    [MaxLength(15)]
    public string TokenEmitidoRnc { get; set; } = string.Empty;

    public DateTime TokenEmitidoFechaCreacion { get; set; } = DateTime.Now;

    public DateTime TokenEmitidoFechaExpiracion { get; set; }

    public bool TokenEmitidoActivo { get; set; } = true;
}
