namespace WPTServiciosDGII.Core.Interfaces;

/// <summary>
/// Carga un certificado .p12 de forma segura y lo libera inmediatamente después del uso.
/// Política Fail-Fast: si el certificado no puede cargarse, lanza excepción.
/// Nunca cachea el certificado en memoria.
/// Soporta dos modos:
///   1. Desde ruta en disco (rutaArchivo)
///   2. Desde bytes en BD (certBytes) — usado cuando NucleoCertificadoDigital es varbinary
/// </summary>
public interface ICertificadoLoader
{
    /// <summary>
    /// Carga el certificado .p12 desde el sistema de archivos y ejecuta la acción provista.
    /// El certificado es descartado (Dispose) al finalizar, sin importar si hay error.
    /// </summary>
    Task EjecutarConCertificadoAsync(
        string rutaArchivo,
        string password,
        Func<System.Security.Cryptography.X509Certificates.X509Certificate2, Task> accion);

    /// <summary>
    /// Carga el certificado .p12 desde un array de bytes (almacenado en BD como varbinary)
    /// y ejecuta la acción provista. El certificado es descartado al finalizar.
    /// </summary>
    Task EjecutarConCertificadoBytesAsync(
        byte[] certBytes,
        string password,
        Func<System.Security.Cryptography.X509Certificates.X509Certificate2, Task> accion);

    /// <summary>
    /// Valida que el archivo .p12 existe en disco y puede cargarse con la contraseña dada.
    /// </summary>
    bool ValidarCertificado(string rutaArchivo, string password, out string error);

    /// <summary>
    /// Valida que los bytes de un .p12 pueden cargarse con la contraseña dada.
    /// </summary>
    bool ValidarCertificadoBytes(byte[] certBytes, string password, out string error);
}
