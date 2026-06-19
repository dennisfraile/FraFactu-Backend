namespace FraFactu.Application.DTOs.Suscripciones
{
    public class FacturaSuscripcionResponseDto
    {
        public int Id { get; set; }
        public string NumeroFactura { get; set; } = string.Empty;
        public int SuscripcionId { get; set; }
        public string EmisorNombre { get; set; } = string.Empty;
        public DateTime FechaEmision { get; set; }
        public DateTime PeriodoServicioInicio { get; set; }
        public DateTime PeriodoServicioFin { get; set; }
        public decimal Monto { get; set; }
        public bool EnviadoPorEmail { get; set; }
        public DateTime? FechaEnvio { get; set; }
    }
}
