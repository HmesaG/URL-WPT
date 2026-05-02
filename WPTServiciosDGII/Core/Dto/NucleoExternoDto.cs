namespace WPTServiciosDGII.Core.Dto;

/// <summary>
/// Datos del emisor obtenidos de la tabla Nucleo de la BD externa.
/// Los nombres de las propiedades son genéricos; el mapeo a columnas reales
/// se configura en appsettings.json bajo la sección "NucleoConfig".
/// </summary>
public class NucleoExternoDto
{
    /// <summary>ID interno del registro en la tabla Nucleo.</summary>
    public int NucleoID { get; set; }

    /// <summary>RNC del emisor (sin guiones). Columna: NucleoRNC.</summary>
    public string Rnc { get; set; } = string.Empty;

    /// <summary>Estado del registro. Columna: NucleoEstado. ("A" = Activo).</summary>
    public string Estado { get; set; } = string.Empty;

    /// <summary>
    /// Bytes del archivo .p12 del emisor (almacenado como varbinary en BD).
    /// Columna: NucleoCertificadoDigital.
    /// NUNCA se loggea ni se expone en respuestas de API.
    /// </summary>
    public byte[] CertificadoBytes { get; set; } = [];

    /// <summary>
    /// Contraseña del archivo .p12.
    /// NUNCA se loggea ni se expone en respuestas de API.
    /// Columna: NucleoPasswordDigital.
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
