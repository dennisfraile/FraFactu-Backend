namespace FraFactu.Application.DTOs.Cuotas
{
    /// <summary>Datos del pago al cobrar una cuota (forma de pago del DTE de esa cuota).</summary>
    public class RegistrarPagoCuotaDto
    {
        public int CatFormaPagoId { get; set; }
        public string? Referencia { get; set; }
    }
}
