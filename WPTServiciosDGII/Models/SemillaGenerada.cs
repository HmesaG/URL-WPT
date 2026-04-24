using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WPTServiciosDGII.Models;

[Table("Api_SemillaGenerada")]
public class SemillaGenerada
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int SemillaGeneradaId { get; set; }

    [MaxLength(100)]
    public string SemillaGeneradaValor { get; set; } = string.Empty;

    public DateTime SemillaGeneradaFecha { get; set; } = DateTime.Now;

    public bool SemillaGeneradaUsada { get; set; } = false;

    [MaxLength(15)]
    public string SemillaGeneradaRnc { get; set; } = string.Empty;
}
