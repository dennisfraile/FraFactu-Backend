using FraFactu.Application.DTOs.Common;

namespace FraFactu.Application.DTOs.Facturas
{
    /// <summary>
    /// DTO de pago de factura
    /// </summary>
    public class PagoDto
    {
        public int Id { get; set; }

        // Forma de Pago
        public int CatFormaPagoId { get; set; }
        public CatalogoDto FormaPago { get; set; } = new();

        // Monto y Referencia
        public decimal Monto { get; set; }
        public string? Referencia { get; set; }

        // Plazo (si es crédito)
        public int? CatPlazoId { get; set; }
        public CatalogoDto? Plazo { get; set; }
        public decimal? Periodo { get; set; }
    }
}
