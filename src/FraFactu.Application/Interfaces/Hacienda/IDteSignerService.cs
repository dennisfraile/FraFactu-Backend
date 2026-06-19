namespace FraFactu.Application.Interfaces.Hacienda
{
    public interface IDteSignerService
    {
        /// <summary>
        /// Firma un documento JSON (DTE) utilizando la llave privada del emisor.
        /// Retorna el JWS compact serialization.
        /// </summary>
        string FirmarDocumento(string jsonPayload, string llavePrivada, string passwordPrivada);

        /// <summary>
        /// Obtiene el hash SHA-512 del documento original (antes de firmar)
        /// </summary>
        string GenerarHash(string jsonPayload);
    }
}
