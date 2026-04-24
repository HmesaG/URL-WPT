using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WPTServiciosDGII.Models;

[Table("Api_DocumentoRecibido")]
public class DocumentoRecibido
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int DocumentoRecibidoId { get; set; }

    /// <summary>ID del tenant (NucleoID). Nullable si el RNC no está registrado.</summary>
    public int? NucleoID { get; set; }

    /// <summary>Tipo: ECF | ACECF</summary>
    [MaxLength(20)]
    public string DocumentoRecibidoTipo { get; set; } = string.Empty;

    [MaxLength(20)]
    public string DocumentoRecibidoNCF { get; set; } = string.Empty;

    [MaxLength(15)]
    public string DocumentoRecibidoRncEmisor { get; set; } = string.Empty;

    [MaxLength(15)]
    public string DocumentoRecibidoRncReceptor { get; set; } = string.Empty;

    public string DocumentoRecibidoXML { get; set; } = string.Empty;

    [MaxLength(50)]
    public string DocumentoRecibidoTrackId { get; set; } = string.Empty;

    /// <summary>Recibido | Aceptado | Rechazado | Pendiente</summary>
    [MaxLength(20)]
    public string DocumentoRecibidoEstado { get; set; } = "Recibido";

    public DateTime DocumentoRecibidoFecha { get; set; } = DateTime.Now;

    [MaxLength(500)]
    public string DocumentoRecibidoMensaje { get; set; } = string.Empty;
}
