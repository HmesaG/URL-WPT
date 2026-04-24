using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Xml;
using WPTServiciosDGII.Data;
using WPTServiciosDGII.Models;
using WPTServiciosDGII.Services;

namespace WPTServiciosDGII.Controllers;

/// <summary>
/// Servicio de Aprobación Comercial DGII
/// POST /fe/aprobacioncomercial/api/ecf
/// </summary>
[ApiController]
[Route("fe/aprobacioncomercial/api")]
[Produces("application/xml")]
public class AprobacionComercialController : ControllerBase
{
    private readonly WptDbContext _db;
    private readonly ILogInteraccionService _log;
    private readonly IConfiguration _config;
    private readonly ILogger<AprobacionComercialController> _logger;

    public AprobacionComercialController(
        WptDbContext db,
        ILogInteraccionService log,
        IConfiguration config,
        ILogger<AprobacionComercialController> logger)
    {
        _db     = db;
        _log    = log;
        _config = config;
        _logger = logger;
    }

    private string GetClientIp() =>
        HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    // ─────────────────────────────────────────────────────────────
    // POST /fe/aprobacioncomercial/api/ecf
    // ─────────────────────────────────────────────────────────────
    [HttpPost("ecf")]
    [Consumes("application/xml", "text/xml")]
    [ProducesResponseType(typeof(string), 200)]
    [ProducesResponseType(typeof(string), 401)]
    public async Task<IActionResult> RecibirAprobacion()
    {
        var sw = Stopwatch.StartNew();
        var ip = GetClientIp();

        // 1. Validar Bearer token
        var authHeader = Request.Headers["Authorization"].FirstOrDefault() ?? string.Empty;
        var tokenVal   = authHeader.Replace("Bearer ", "").Trim();
        var ahora      = DateTime.Now;

        var tokenValido = await _db.TokensEmitidos.AnyAsync(t =>
            t.TokenEmitidoValor           == tokenVal &&
            t.TokenEmitidoActivo          == true     &&
            t.TokenEmitidoFechaExpiracion >= ahora);

        if (!tokenValido)
        {
            var errXml = BuildXml(w =>
            {
                w.WriteStartElement("Error");
                w.WriteElementString("codigo", "401");
                w.WriteElementString("mensaje", "Token inválido o expirado.");
                w.WriteEndElement();
            });
            sw.Stop();
            await _log.RegistrarAsync(
                "AprobacionComercial", "POST",
                $"{_config["AppSettings:BaseUrl"]}/fe/aprobacioncomercial/api/ecf",
                ip, null, errXml, "TOKEN_INVALIDO", (int)sw.ElapsedMilliseconds,
                null, tokenVal);

            return Content(errXml, "application/xml");
        }

        // 2. Leer XML de la Aprobación/Rechazo Comercial (ACECF)
        string xmlAcecf;
        using (var reader = new StreamReader(Request.Body))
            xmlAcecf = await reader.ReadToEndAsync();

        // 3. Extraer campos del XML
        var (eNcf, rncEmisor, rncComprador) = ExtraerCamposAcecf(xmlAcecf);
        var trackId  = Guid.NewGuid().ToString();
        var fechaAct = DateTime.Now;

        // 4. Guardar en BD
        // En ACECF, nosotros somos el RNCEmisor (quien emitió la factura original)
        var doc = new DocumentoRecibido
        {
            DocumentoRecibidoTipo        = "ACECF",
            DocumentoRecibidoNCF         = eNcf,
            DocumentoRecibidoRncEmisor   = rncEmisor,
            DocumentoRecibidoRncReceptor = rncComprador,
            DocumentoRecibidoXML         = xmlAcecf,
            DocumentoRecibidoTrackId     = trackId,
            DocumentoRecibidoEstado      = "Recibido",
            DocumentoRecibidoFecha       = fechaAct,
            DocumentoRecibidoMensaje     = "Aprobación comercial recibida correctamente."
        };
        _db.DocumentosRecibidos.Add(doc);
        await _db.SaveChangesAsync();

        // 5. Construir AcuseRecibo XML
        var respuestaXml = BuildXml(w =>
        {
            w.WriteStartElement("AcuseRecibo");
            w.WriteElementString("trackId", trackId);
            w.WriteElementString("codigo",  "1");
            w.WriteElementString("estado",  "Recibido");
            w.WriteElementString("rnc",     rncEmisor);
            w.WriteElementString("encf",    eNcf);
            w.WriteStartElement("mensajes");
            w.WriteStartElement("mensaje");
            w.WriteElementString("valor", "Aprobación comercial recibida correctamente.");
            w.WriteEndElement(); // mensaje
            w.WriteEndElement(); // mensajes
            w.WriteEndElement(); // AcuseRecibo
        });

        sw.Stop();
        await _log.RegistrarAsync(
            "AprobacionComercial", "POST",
            $"{_config["AppSettings:BaseUrl"]}/fe/aprobacioncomercial/api/ecf",
            ip, xmlAcecf, respuestaXml, "OK", (int)sw.ElapsedMilliseconds,
            rncEmisor, tokenVal);

        return Content(respuestaXml, "application/xml");
    }

    private static (string eNcf, string rncEmisor, string rncComprador) ExtraerCamposAcecf(string xmlAcecf)
    {
        string eNcf = string.Empty, rncEmisor = string.Empty, rncComprador = string.Empty;
        try
        {
            var doc = new XmlDocument();
            doc.LoadXml(xmlAcecf);

            eNcf         = doc.GetElementsByTagName("eNCF")[0]?.InnerText         ?? string.Empty;
            rncEmisor    = doc.GetElementsByTagName("RNCEmisor")[0]?.InnerText    ?? string.Empty;
            rncComprador = doc.GetElementsByTagName("RNCComprador")[0]?.InnerText ?? string.Empty;
        }
        catch { /* XML malformado: dejamos vacíos */ }

        return (eNcf, rncEmisor, rncComprador);
    }

    private static string BuildXml(Action<XmlWriter> buildAction)
    {
        using var sw = new StringWriter();
        var settings = new XmlWriterSettings { Indent = true };
        using var xw = XmlWriter.Create(sw, settings);
        xw.WriteStartDocument();
        buildAction(xw);
        xw.WriteEndDocument();
        xw.Flush();
        return sw.ToString();
    }
}
