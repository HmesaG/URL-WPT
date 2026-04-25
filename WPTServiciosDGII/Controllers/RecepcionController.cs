using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;
using System.Xml.Linq;
using WPTServiciosDGII.Core.Interfaces;
using WPTServiciosDGII.Services;

namespace WPTServiciosDGII.Controllers;

/// <summary>
/// Endpoint de recepción de e-CF de la DGII.
/// FASE 4: Integra DynamicDbResolver + NucleoRepository + CertificadoLoader.
/// El certificado se obtiene de la tabla Nucleo usando el RNCEmisor del XML recibido.
/// Las credenciales NUNCA están hardcodeadas.
/// </summary>
[ApiController]
[Route("fe/recepcion/api")]
public class RecepcionController : ControllerBase
{
    private readonly ILogInteraccionService _log;
    private readonly INucleoRepository _nucleo;
    private readonly ICertificadoLoader _certLoader;
    private readonly ILogger<RecepcionController> _logger;

    public RecepcionController(
        ILogInteraccionService log,
        INucleoRepository nucleo,
        ICertificadoLoader certLoader,
        ILogger<RecepcionController> logger)
    {
        _log        = log;
        _nucleo     = nucleo;
        _certLoader = certLoader;
        _logger     = logger;
    }

    /// <summary>
    /// Recibe un e-CF firmado, construye el ARECF de respuesta y lo firma
    /// con el certificado .p12 del emisor obtenido desde la BD externa (tabla Nucleo).
    /// Header opcional X-Db-Tenant para seleccionar la BD del cliente.
    /// </summary>
    [HttpPost("ecf")]
    [Consumes("application/xml", "multipart/form-data", "application/octet-stream", "text/xml")]
    public async Task<IActionResult> EnviarEcf()
    {
        string xmlContent = "";
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var tenant = Request.Headers["X-Db-Tenant"].FirstOrDefault();

        try
        {
            // ── 1. Leer XML recibido ──────────────────────────────────────────
            if (Request.HasFormContentType && Request.Form.Files.Count > 0)
            {
                using var reader = new StreamReader(Request.Form.Files[0].OpenReadStream());
                xmlContent = await reader.ReadToEndAsync();
            }
            else
            {
                using var reader = new StreamReader(Request.Body);
                xmlContent = await reader.ReadToEndAsync();
            }

            if (string.IsNullOrWhiteSpace(xmlContent))
                return BadRequest(new { error = "El contenido XML está vacío." });

            // ── 2. Extraer campos del XML ──────────────────────────────────────
            var (rncEmisor, rncComprador, encf) = ExtraerCamposXml(xmlContent);

            _logger.LogInformation("📨 e-CF recibido — RNCEmisor: {Emisor}, eNCF: {Encf}", rncEmisor, encf);

            // ── 3. FASE 4: Obtener certificado desde la BD externa ────────────
            // Se busca el emisor en la tabla Nucleo usando el RNCEmisor del XML.
            // Si no se encuentra → Fail-Fast (400)
            if (string.IsNullOrWhiteSpace(rncEmisor))
                return BadRequest(new { error = "El XML no contiene RNCEmisor válido." });

            Core.Dto.NucleoExternoDto? emisor = null;
            try
            {
                emisor = await _nucleo.ObtenerPorRncAsync(rncEmisor, tenant);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogError("❌ Tenant no encontrado: {Msg}", ex.Message);
                return BadRequest(new { error = $"Tenant de BD no registrado: {ex.Message}" });
            }

            if (emisor is null)
            {
                await _log.RegistrarAsync("Recepcion", "POST", "/fe/recepcion/api/ecf", ip,
                    xmlContent, $"Emisor no encontrado en Nucleo: {rncEmisor}", "ERROR", 0);
                return BadRequest(new { error = $"El RNCEmisor '{rncEmisor}' no está activo en el sistema." });
            }

            // ── 4. Construir ARECF ────────────────────────────────────────────
            var fechaFull = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            var responseXmlStr = $"""
                <?xml version="1.0" encoding="utf-8"?>
                <ARECF xmlns="urn:dgii.gov.do:ARECF">
                  <DetalleAcuseDeRecibo>
                    <Version>1.0</Version>
                    <RNCEmisor>{rncEmisor}</RNCEmisor>
                    <RNCComprador>{rncComprador}</RNCComprador>
                    <eNCF>{encf}</eNCF>
                    <Estado>0</Estado>
                    <CodigoMotivoNoRecibido />
                    <FechaHoraAcuseRecibo>{fechaFull}</FechaHoraAcuseRecibo>
                  </DetalleAcuseDeRecibo>
                </ARECF>
                """;

            // ── 5. Firmar ARECF con el .p12 del emisor (carga-uso-dispose) ────
            string signedXml = responseXmlStr;
            try
            {
                await _certLoader.EjecutarConCertificadoAsync(
                    emisor.RutaCertificado,
                    emisor.PasswordCertificado,
                    cert =>
                    {
                        signedXml = FirmarXml(responseXmlStr, cert);
                        return Task.CompletedTask;
                    });

                _logger.LogInformation("✅ ARECF firmado correctamente para eNCF: {Encf}", encf);
            }
            catch (Exception ex)
            {
                // Fail-Fast: si el certificado falla, no enviamos respuesta sin firmar
                _logger.LogError(ex, "❌ Fallo al firmar ARECF para RNCEmisor: {Emisor}", rncEmisor);
                await _log.RegistrarAsync("Recepcion", "WARN", "Firma ARECF Falló", ip,
                    xmlContent, ex.Message, "ERROR", 0);
                return StatusCode(400, new { error = "No se pudo firmar el ARECF. Verifique el certificado del emisor." });
            }

            await _log.RegistrarAsync("Recepcion", "POST", "/fe/recepcion/api/ecf", ip,
                xmlContent, signedXml, "OK", 45);

            return Content(signedXml, "application/xml");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error inesperado en EnviarEcf");
            await _log.RegistrarAsync("Recepcion", "POST", "/fe/recepcion/api/ecf", ip,
                xmlContent, ex.Message, "ERROR", 5);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // ── Métodos privados ──────────────────────────────────────────────────────

    private static (string rncEmisor, string rncComprador, string encf) ExtraerCamposXml(string xml)
    {
        try
        {
            var xDoc = XDocument.Parse(xml);
            var encabezado = xDoc.Descendants().FirstOrDefault(x => x.Name.LocalName == "Encabezado");
            if (encabezado is null) return ("", "", "");

            return (
                encabezado.Descendants().FirstOrDefault(x => x.Name.LocalName == "RNCEmisor")?.Value    ?? "",
                encabezado.Descendants().FirstOrDefault(x => x.Name.LocalName == "RNCComprador")?.Value ?? "",
                encabezado.Descendants().FirstOrDefault(x => x.Name.LocalName == "eNCF")?.Value         ?? ""
            );
        }
        catch { return ("", "", ""); }
    }

    private static string FirmarXml(
        string xmlRaw,
        System.Security.Cryptography.X509Certificates.X509Certificate2 cert)
    {
        var xmlDoc = new XmlDocument { PreserveWhitespace = true };
        xmlDoc.LoadXml(xmlRaw);

        var signedXml = new SignedXml(xmlDoc) { SigningKey = cert.GetRSAPrivateKey() };

        var keyInfo = new KeyInfo();
        keyInfo.AddClause(new KeyInfoX509Data(cert));
        signedXml.KeyInfo = keyInfo;

        var reference = new Reference { Uri = "" };
        reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
        signedXml.AddReference(reference);

        signedXml.ComputeSignature();
        xmlDoc.DocumentElement!.AppendChild(xmlDoc.ImportNode(signedXml.GetXml(), true));

        return xmlDoc.OuterXml;
    }
}
