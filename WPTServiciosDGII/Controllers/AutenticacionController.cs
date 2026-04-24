using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Xml;
using WPTServiciosDGII.Data;
using WPTServiciosDGII.Models;
using WPTServiciosDGII.Services;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;

namespace WPTServiciosDGII.Controllers;

/// <summary>
/// Servicio de Autenticación DGII
/// GET  /fe/autenticacion/api/semilla
/// POST /fe/autenticacion/api/validacioncertificado
/// </summary>
[ApiController]
[Route("fe/autenticacion/api")]
[Produces("application/xml")]
public class AutenticacionController : ControllerBase
{
    private readonly WptDbContext _db;
    private readonly ILogInteraccionService _log;
    private readonly IConfiguration _config;
    private readonly ILogger<AutenticacionController> _logger;

    public AutenticacionController(
        WptDbContext db,
        ILogInteraccionService log,
        IConfiguration config,
        ILogger<AutenticacionController> logger)
    {
        _db     = db;
        _log    = log;
        _config = config;
        _logger = logger;
    }

    private string GetClientIp() =>
        HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    // ─────────────────────────────────────────────────────────────
    // GET /fe/autenticacion/api/semilla
    // ─────────────────────────────────────────────────────────────
    [HttpGet("semilla")]
    [ProducesResponseType(typeof(string), 200)]
    public async Task<IActionResult> ObtenerSemilla()
    {
        var sw = Stopwatch.StartNew();
        var ip = GetClientIp();

        var semillaVal = Guid.NewGuid().ToString();
        var fechaAct   = DateTime.Now;

        // Guardar semilla en BD
        var semilla = new SemillaGenerada
        {
            SemillaGeneradaValor = semillaVal,
            SemillaGeneradaFecha = fechaAct,
            SemillaGeneradaUsada = false,
            SemillaGeneradaRnc   = string.Empty
        };
        _db.SemillasGeneradas.Add(semilla);
        await _db.SaveChangesAsync();

        // Construir XML de respuesta (formato DGII SemillaModel)
        var xml = BuildXml(writer =>
        {
            writer.WriteStartElement("SemillaModel");
            writer.WriteElementString("valor", semillaVal);
            writer.WriteElementString("fecha", fechaAct.ToString("yyyy-MM-ddTHH:mm:ss"));
            writer.WriteEndElement();
        });

        sw.Stop();
        await _log.RegistrarAsync(
            "Autenticacion", "GET",
            $"{_config["AppSettings:BaseUrl"]}/fe/autenticacion/api/semilla",
            ip, null, xml, "OK", (int)sw.ElapsedMilliseconds);

        return Content(xml, "application/xml");
    }

    // ─────────────────────────────────────────────────────────────
    // POST /fe/autenticacion/api/validacioncertificado
    // ─────────────────────────────────────────────────────────────
    [HttpPost("validacioncertificado")]
    [Consumes("application/xml", "text/xml", "multipart/form-data")]
    [ProducesResponseType(typeof(string), 200)]
    [ProducesResponseType(typeof(string), 401)]
    public async Task<IActionResult> ValidarCertificado()
    {
        var sw = Stopwatch.StartNew();
        var ip = GetClientIp();

        // Leer body XML de la semilla firmada
        string semillaFirmadaXml;
        using (var reader = new StreamReader(Request.Body))
            semillaFirmadaXml = await reader.ReadToEndAsync();

        // TODO FASE 2: Validar firma digital real con WPTFirmaDigital.dll
        // Por ahora: aceptar cualquier XML no vacío como válido (modo certificación)
        bool valido = !string.IsNullOrWhiteSpace(semillaFirmadaXml);

        string responseXml;
        string estado;
        string tokenVal = string.Empty;

        if (valido)
        {
            tokenVal    = Guid.NewGuid().ToString();
            var fechaAct = DateTime.Now;
            var fechaExp = fechaAct.AddHours(1);

            // Guardar token en BD
            var token = new TokenEmitido
            {
                TokenEmitidoValor           = tokenVal,
                TokenEmitidoRnc             = string.Empty,
                TokenEmitidoFechaCreacion   = fechaAct,
                TokenEmitidoFechaExpiracion = fechaExp,
                TokenEmitidoActivo          = true
            };
            _db.TokensEmitidos.Add(token);
            await _db.SaveChangesAsync();

            responseXml = BuildXml(writer =>
            {
                writer.WriteStartElement("TokenModel");
                writer.WriteElementString("token", tokenVal);
                writer.WriteElementString("expira", fechaExp.ToString("yyyy-MM-ddTHH:mm:ss"));
                writer.WriteEndElement();
            });
            estado = "OK";
        }
        else
        {
            responseXml = BuildXml(writer =>
            {
                writer.WriteStartElement("Error");
                writer.WriteElementString("codigo", "99");
                writer.WriteElementString("mensaje", "Certificado invalido o semilla vacía.");
                writer.WriteEndElement();
            });
            estado = "ERROR";
        }

        sw.Stop();
        await _log.RegistrarAsync(
            "Autenticacion", "POST",
            $"{_config["AppSettings:BaseUrl"]}/fe/autenticacion/api/validacioncertificado",
            ip, semillaFirmadaXml, responseXml, estado,
            (int)sw.ElapsedMilliseconds, null, tokenVal);

        return valido
            ? Content(responseXml, "application/xml")
            : Content(responseXml, "application/xml");
    }

    // ─────────────────────────────────────────────────────────────
    // POST /fe/autenticacion/api/firmar-semilla-test 
    // (HELPER DE DESARROLLO / PRUEBAS)
    // ─────────────────────────────────────────────────────────────
    [HttpPost("firmar-semilla-test")]
    [ApiExplorerSettings(IgnoreApi = true)] // Oculto de Swagger oficial
    public async Task<IActionResult> FirmarSemillaTest()
    {
        try
        {
            // 1. Leer XML Crudo (La Semilla)
            using var reader = new StreamReader(Request.Body);
            var xmlCrudo = await reader.ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(xmlCrudo)) return BadRequest("XML Vacío");

            // 2. Cargar Certificado .p12 local (del storage/path provisto)
            var certPath = @"e:\Empresas\GMV\Proyectos Antigravity\URL WPT\20240703-1500447-A5GFP98SJ.p12";
            var certPass = "Wpt2025001phil";
            
            var certificado = new X509Certificate2(certPath, certPass, 
                                X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);

            // 3. Crear Domicilio XML
            var xmlDoc = new XmlDocument { PreserveWhitespace = true };
            xmlDoc.LoadXml(xmlCrudo);

            // 4. Firmar (XMLDSig Enveloped)
            var signedXml = new SignedXml(xmlDoc) { SigningKey = certificado.GetRSAPrivateKey() };
            
            // Añadir KeyInfo (Opcional por estandar pero requerido por DGII)
            var keyInfo = new KeyInfo();
            keyInfo.AddClause(new KeyInfoX509Data(certificado));
            signedXml.KeyInfo = keyInfo;

            // Referencia y Transformación
            var reference = new Reference { Uri = "" }; // Aplica la firma a todo el documento
            var env = new XmlDsigEnvelopedSignatureTransform();
            reference.AddTransform(env);
            signedXml.AddReference(reference);

            signedXml.ComputeSignature();
            var xmlDigitalSignature = signedXml.GetXml();

            // 5. Adjuntar firma al Xml Document
            xmlDoc.DocumentElement!.AppendChild(xmlDoc.ImportNode(xmlDigitalSignature, true));

            return Content(xmlDoc.OuterXml, "application/xml");
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────────────
    // Helper: XMLWriter → string limpio UTF-8
    // ─────────────────────────────────────────────────────────────
    private static string BuildXml(Action<XmlWriter> buildAction)
    {
        using var sw = new StringWriter();
        var settings = new XmlWriterSettings { Indent = true, Encoding = System.Text.Encoding.UTF8 };
        using var xw = XmlWriter.Create(sw, settings);
        xw.WriteStartDocument();
        buildAction(xw);
        xw.WriteEndDocument();
        xw.Flush();
        return sw.ToString();
    }
}
