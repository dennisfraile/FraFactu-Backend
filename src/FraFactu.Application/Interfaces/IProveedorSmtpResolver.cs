namespace FraFactu.Application.Interfaces
{
    /// <summary>
    /// Deduce el servidor SMTP saliente de un dominio de correo cuyo proveedor
    /// no se reconoce por el nombre literal (p. ej. dominios corporativos de
    /// Google Workspace o Microsoft 365 como <c>empresa.com</c>).
    ///
    /// La detección se hace consultando los registros MX del dominio: un dominio
    /// alojado en Google (MX <c>*.google.com</c>) usa <c>smtp.gmail.com</c>, uno
    /// en Microsoft 365 (<c>*.outlook.com</c>) usa <c>smtp.office365.com</c>, etc.
    /// </summary>
    public interface IProveedorSmtpResolver
    {
        /// <summary>
        /// Devuelve <c>(host, port)</c> del SMTP saliente si los registros MX del
        /// dominio identifican un proveedor conocido; <c>null</c> si no se puede
        /// determinar (dominio desconocido, sin MX, o fallo de resolución).
        /// La implementación es best-effort: nunca lanza por errores de red/DNS.
        /// </summary>
        Task<(string host, int port)?> ResolverPorMxAsync(string dominio, CancellationToken ct = default);
    }
}
