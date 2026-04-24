namespace WPTServiciosDGII.Core.Interfaces;

/// <summary>
/// Carga un certificado .p12 de forma segura y lo libera inmediatamente después del uso.
/// Política Fail-Fast: si la ruta no existe o la contraseña es incorrecta, lanza excepción.
/// Nunca cachea el certificado en memoria.
/// </summary>
public interface ICertificadoLoader
{
    /// <summary>
    /// Carga el certificado .p12 desde el sistema de archivos y ejecuta la acción provista.
    /// El certificado es descartado (Dispose) al finalizar la acción, sin importar si hay error.
    /// </summary>
    /// <param name="rutaArchivo">Ruta absoluta al archivo .p12</param>
    /// <param name="password">Contraseña del .p12 (nunca se loggea)</param>
    /// <param name="accion">Acción que recibe el certificado cargado</param>
    Task EjecutarConCertificadoAsync(
        string rutaArchivo,
        string password,
        Func<System.Security.Cryptography.X509Certificates.X509Certificate2, Task> accion);

    /// <summary>
    /// Valida que el archivo .p12 existe y puede cargarse con la contraseña dada.
    /// Útil para pruebas de configuración sin ejecutar una firma real.
    /// </summary>
    bool ValidarCertificado(string rutaArchivo, string password, out string error);
}
