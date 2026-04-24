namespace WPTServiciosDGII.Core.Dto;

/// <summary>
/// Datos del emisor obtenidos de la tabla Nucleo de la BD externa.
/// Los nombres de las propiedades son genéricos; el mapeo a columnas reales
/// se configura en appsettings.json bajo la sección "NucleoConfig".
/// </summary>
public class NucleoExternoDto
{
    /// <summary>RNC del emisor (sin guiones).</summary>
    public string Rnc { get; set; } = string.Empty;

    /// <summary>Estado del registro (ej: "A" = Activo).</summary>
    public string Estado { get; set; } = string.Empty;

    /// <summary>
    /// Ruta absoluta al archivo .p12 del certificado del emisor.
    /// Mapeado desde la columna configurada en NucleoConfig:ColumnRutaCertificado.
    /// </summary>
    public string RutaCertificado { get; set; } = string.Empty;

    /// <summary>
    /// Contraseña del archivo .p12.
    /// NUNCA se loggea ni se expone en respuestas de API.
    /// Mapeado desde la columna configurada en NucleoConfig:ColumnPasswordCertificado.
    /// </summary>
    public string PasswordCertificado { get; set; } = string.Empty;
}

/// <summary>
/// Request para el endpoint POST /api/recepcion/recibir.
/// </summary>
public class EcfRecepcionRequest
{
    /// <summary>XML del e-CF firmado a enviar a la DGII.</summary>
    public string XmlEcf { get; set; } = string.Empty;

    /// <summary>RNC del comprador tal como llega del servicio DGII.</summary>
    public string RncComprador { get; set; } = string.Empty;

    /// <summary>Tenant de BD a usar. Si es null se usa el tenant por defecto.</summary>
    public string? Tenant { get; set; }
}

/// <summary>
/// Respuesta del endpoint POST /api/recepcion/recibir.
/// </summary>
public class EcfRecepcionResponse
{
    public bool   Exitoso    { get; set; }
    public string? Ncf       { get; set; }
    public string? Mensaje   { get; set; }
    public string? CodigoError { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
}
