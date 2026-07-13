namespace FraFactu.Application.Interfaces
{
    /// <summary>
    /// Generación del JSON del DTE según el estándar de Hacienda (extraído de FacturaService, Fase 2).
    /// </summary>
    public interface IDteJsonBuilder
    {
        /// <summary>
        /// Genera el JSON del DTE (identificacion, emisor, receptor, cuerpo, resumen, etc.)
        /// para la factura indicada.
        /// </summary>
        Task<string> GenerateJsonDteAsync(int facturaId, int emisorId);
    }
}
