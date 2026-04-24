using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;
using System.Xml.Linq;
using WPTServiciosDGII.Services;

namespace WPTServiciosDGII.Controllers;

[ApiController]
[Route("fe/recepcion/api")]
public class RecepcionController : ControllerBase
{
    private readonly ILogInteraccionService _log;

    public RecepcionController(ILogInteraccionService log)
    {
        _log = log;
    }

    [HttpPost("ecf")]
    [Consumes("application/xml", "multipart/form-data", "application/octet-stream", "text/xml")]
    public async Task<IActionResult> EnviarEcf()
    {
        string xmlContent = "";
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        try 
        {
            // 1. Obtener contenido del XML
            if (Request.HasFormContentType && Request.Form.Files.Count > 0)
            {
                var file = Request.Form.Files[0];
                using var reader = new StreamReader(file.OpenReadStream());
                xmlContent = await reader.ReadToEndAsync();
            }
            else 
            {
                using var reader = new StreamReader(Request.Body);
                xmlContent = await reader.ReadToEndAsync();
            }

            if (string.IsNullOrEmpty(xmlContent))
                return BadRequest("El contenido XML está vacío.");

            // 2. Extraer datos para el Acuse
            string rncEmisor = "";
            string rncComprador = "";
            string encf = "";

            try 
            {
                var xDoc = XDocument.Parse(xmlContent);
                var encabezado = xDoc.Descendants().FirstOrDefault(x => x.Name.LocalName == "Encabezado");
                if (encabezado != null)
                {
                    rncEmisor = encabezado.Descendants().FirstOrDefault(x => x.Name.LocalName == "RNCEmisor")?.Value ?? "";
                    rncComprador = encabezado.Descendants().FirstOrDefault(x => x.Name.LocalName == "RNCComprador")?.Value ?? "";
                    encf = encabezado.Descendants().FirstOrDefault(x => x.Name.LocalName == "eNCF")?.Value ?? "";
                }
            } catch { }

            // 3. Construir el XML de respuesta (Sin firmar aún)
            var fechaFull = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            var responseXmlStr = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<ARECF xmlns=""urn:dgii.gov.do:ARECF"">
  <DetalleAcuseDeRecibo>
    <Version>1.0</Version>
    <RNCEmisor>{rncEmisor}</RNCEmisor>
    <RNCComprador>{rncComprador}</RNCComprador>
    <eNCF>{encf}</eNCF>
    <Estado>0</Estado>
    <CodigoMotivoNoRecibido />
    <FechaHoraAcuseRecibo>{fechaFull}</FechaHoraAcuseRecibo>
  </DetalleAcuseDeRecibo>
</ARECF>";

            // 4. FIRMAR EL XML DIGITALMENTE
            string signedXml;
            try 
            {
                signedXml = FirmarRespuestaArecf(responseXmlStr);
            }
            catch (Exception ex)
            {
                // Si falla la firma, enviamos el normal con un log del error
                signedXml = responseXmlStr;
                await _log.RegistrarAsync("Recepcion", "WARN", "Firma ARECF Falló", ip, null, ex.Message, "WARN", 0);
            }

            await _log.RegistrarAsync("Recepcion", "POST", "/fe/recepcion/api/ecf", ip, xmlContent, signedXml, "OK", 45);

            return Content(signedXml, "application/xml");
        }
        catch (Exception ex)
        {
            await _log.RegistrarAsync("Recepcion", "POST", "/fe/recepcion/api/ecf", ip, xmlContent, ex.Message, "ERROR", 5);
            return StatusCode(500, ex.Message);
        }
    }

    private string FirmarRespuestaArecf(string xmlRaw)
    {
        var certPath = @"e:\Empresas\GMV\Proyectos Antigravity\URL WPT\20240703-1500447-A5GFP98SJ.p12";
        var certPass = "Wpt2025001phil";

        var certificado = new X509Certificate2(certPath, certPass, 
                            X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);

        var xmlDoc = new XmlDocument { PreserveWhitespace = true };
        xmlDoc.LoadXml(xmlRaw);

        var signedXml = new SignedXml(xmlDoc) { SigningKey = certificado.GetRSAPrivateKey() };
        
        var keyInfo = new KeyInfo();
        keyInfo.AddClause(new KeyInfoX509Data(certificado));
        signedXml.KeyInfo = keyInfo;

        var reference = new Reference { Uri = "" }; 
        reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
        signedXml.AddReference(reference);

        signedXml.ComputeSignature();
        var xmlDigitalSignature = signedXml.GetXml();

        xmlDoc.DocumentElement!.AppendChild(xmlDoc.ImportNode(xmlDigitalSignature, true));

        return xmlDoc.OuterXml;
    }
}
