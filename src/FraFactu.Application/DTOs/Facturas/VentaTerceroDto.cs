namespace FraFactu.Application.DTOs.Facturas
{
    /// <summary>
    /// DTO para Ventas por cuenta de Terceros (solo CCF)
    /// </summary>
    public class VentaTerceroDto
    {
        /// <summary>
        /// NIT por cuenta de Terceros
        /// Pattern: 9 o 14 dígitos
        /// </summary>
        public string Nit { get; set; } = string.Empty;

        /// <summary>
        /// Nombre, denominación o razón social del Tercero
        /// </summary>
        public string Nombre { get; set; } = string.Empty;
    }
}
