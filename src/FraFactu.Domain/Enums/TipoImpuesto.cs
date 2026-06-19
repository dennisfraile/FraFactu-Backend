namespace FraFactu.Domain.Enums
{
    /// <summary>
    /// Tipo de impuesto aplicable a un producto/servicio según Hacienda
    /// </summary>
    public enum TipoImpuesto
    {
        /// <summary>
        /// Operación gravada - Aplica IVA (13%)
        /// </summary>
        Gravado = 1,

        /// <summary>
        /// Operación exenta de IVA
        /// </summary>
        Exento = 2,

        /// <summary>
        /// Operación no sujeta a IVA
        /// </summary>
        NoSujeto = 3
    }
}
