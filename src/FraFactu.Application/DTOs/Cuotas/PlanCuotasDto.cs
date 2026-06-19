namespace FraFactu.Application.DTOs.Cuotas
{
    public class PlanCuotasDto
    {
        public int Id { get; set; }
        public int? ReceptorId { get; set; }
        public string? ReceptorNombre { get; set; }
        public int CondicionOperacion { get; set; }
        public decimal MontoTotal { get; set; }
        public decimal MontoPagado { get; set; }
        public decimal SaldoAdeudado { get; set; }
        public string EstadoCobro { get; set; } = string.Empty;
        public List<CuotaDto> Cuotas { get; set; } = new();
    }
}
