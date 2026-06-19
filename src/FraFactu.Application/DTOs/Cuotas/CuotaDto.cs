namespace FraFactu.Application.DTOs.Cuotas
{
    public class CuotaDto
    {
        public int Numero { get; set; }
        public decimal Monto { get; set; }
        public DateTime FechaPactada { get; set; }
        public string Estado { get; set; } = string.Empty;
        public DateTime? FechaPago { get; set; }
        public int? FacturaId { get; set; }
        public string? CodigoGeneracion { get; set; }
        public bool EsCuotaFinal { get; set; }
        public decimal InteresMora { get; set; }
    }
}
