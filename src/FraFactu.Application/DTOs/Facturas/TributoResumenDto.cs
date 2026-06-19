namespace FraFactu.Application.DTOs.Facturas
{
    /// <summary>
    /// DTO para Resumen de Tributos (específico de CCF)
    /// </summary>
    public class TributoResumenDto
    {
        /// <summary>
        /// Código de Tributo
        /// Ejemplos: "20" (IVA), "C3", "D1", etc.
        /// </summary>
        public string Codigo { get; set; } = string.Empty;

        /// <summary>
        /// Nombre del Tributo
        /// Ejemplo: "Impuesto al Valor Agregado 13%"
        /// </summary>
        public string Descripcion { get; set; } = string.Empty;

        /// <summary>
        /// Valor del Tributo calculado
        /// </summary>
        public decimal Valor { get; set; }
    }
}
