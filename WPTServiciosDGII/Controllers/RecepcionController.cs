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
/// FLUJO: El XML llega de un proveedor externo (RNCEmisor) hacia una empresa cliente
/// nuestra (RNCComprador). El ARECF lo firma el COMPRADOR con su propio certificado
/// almacenado en la tabla Nucleo. El RNCEmisor es externo, no está en nuestra BD.
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
    [Consumes("application/xml", "multipart/form-data", "text/xml")]
    public async Task<IActionResult> EnviarEcf(IFormFile? xmlFile)
    {
        string xmlContent = "";
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var tenant = Request.Headers["X-Db-Tenant"].FirstOrDefault();

        try
        {
            // ── 1. Leer XML recibido ──────────────────────────────────────────
            if (xmlFile != null)
            {
                using var reader = new StreamReader(xmlFile.OpenReadStream());
                xmlContent = await reader.ReadToEndAsync();
                _logger.LogInformation("📁 XML recibido como archivo: {FileName}", xmlFile.FileName);
            }
            else if (Request.HasFormContentType && Request.Form.Files.Count > 0)
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

            _logger.LogInformation("📨 e-CF recibido — RNCEmisor: {Emisor} → RNCComprador: {Comprador}, eNCF: {Encf}",
                rncEmisor, rncComprador, encf);

            // ── 3. Obtener certificado del COMPRADOR desde la BD ──────────────
            // El RNCEmisor es el proveedor externo que envió la factura.
            // El RNCComprador es nuestra empresa cliente que RECIBE y debe firmar el ARECF.
            // Por eso buscamos el certificado usando RNCComprador.
            if (string.IsNullOrWhiteSpace(rncComprador))
                return BadRequest(new { error = "El XML no contiene RNCComprador válido." });

            Core.Dto.NucleoExternoDto? comprador = null;
            try
            {
                comprador = await _nucleo.ObtenerPorRncAsync(rncComprador, tenant);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogError("❌ Tenant no encontrado: {Msg}", ex.Message);
                return BadRequest(new { error = $"Tenant de BD no registrado: {ex.Message}" });
            }

            if (comprador is null)
            {
                await _log.RegistrarAsync("Recepcion", "POST", "/fe/recepcion/api/ecf", ip,
                    xmlContent, $"Comprador no encontrado en Nucleo: {rncComprador}", "ERROR", 0);
                return BadRequest(new { error = $"El RNCComprador '{rncComprador}' no está registrado o no está activo en el sistema." });
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

            // ── 5. Firmar ARECF con el .p12 del COMPRADOR (cargado desde BD) ─
            // El comprador firma el acuse de recibo con su propio certificado digital.
            string signedXml = responseXmlStr;
            try
            {
                await _certLoader.EjecutarConCertificadoBytesAsync(
                    comprador.CertificadoBytes,
                    comprador.PasswordCertificado,
                    cert =>
                    {
                        signedXml = FirmarXml(responseXmlStr, cert);
                        return Task.CompletedTask;
                    });

                _logger.LogInformation("✅ ARECF firmado por RNCComprador: {Comprador}, eNCF: {Encf}",
                    rncComprador, encf);
            }
            catch (Exception ex)
            {
                // Fail-Fast: si el certificado falla, no enviamos respuesta sin firmar
                _logger.LogError(ex, "❌ Fallo al firmar ARECF para RNCComprador: {Comprador}", rncComprador);
                await _log.RegistrarAsync("Recepcion", "WARN", "Firma ARECF Falló", ip,
                    xmlContent, ex.Message, "ERROR", 0);
                return StatusCode(400, new { error = $"No se pudo firmar el ARECF. El comprador '{rncComprador}' no tiene certificado digital válido configurado." });
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
