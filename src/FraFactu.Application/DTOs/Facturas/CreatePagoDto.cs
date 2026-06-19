namespace FraFactu.Application.DTOs.Facturas
{
    /// <summary>
    /// DTO para creación de pago
    /// </summary>
    public class CreatePagoDto
    {
        public int CatFormaPagoId { get; set; }
        public decimal Monto { get; set; }
        public string? Referencia { get; set; }
        public int? CatPlazoId { get; set; }
        public decimal? Periodo { get; set; }
    }
}
