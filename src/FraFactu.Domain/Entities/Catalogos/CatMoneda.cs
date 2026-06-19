namespace FraFactu.Domain.Entities.Catalogos
{
    /// <summary>
    /// Catálogo de monedas
    /// </summary>
    public class CatMoneda : CatalogoBase
    {
        /// <summary>
        /// Símbolo de la moneda ($, €, etc.)
        /// </summary>
        public string? Simbolo { get; set; }
    }
}
