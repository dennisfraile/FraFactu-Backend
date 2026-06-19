using System.Collections.Generic;

namespace FraFactu.Application.DTOs.Cuotas
{
    public class AgingTotalesDto
    {
        public decimal PorVencer { get; set; }
        public decimal D1a30 { get; set; }
        public decimal D31a60 { get; set; }
        public decimal D61a90 { get; set; }
        public decimal Mas90 { get; set; }
        public decimal Total { get; set; }
    }

    public class AgingPlanDto
    {
        public int PlanId { get; set; }
        public decimal PorVencer { get; set; }
        public decimal D1a30 { get; set; }
        public decimal D31a60 { get; set; }
        public decimal D61a90 { get; set; }
        public decimal Mas90 { get; set; }
        public decimal Total { get; set; }
    }

    public class AgingClienteDto
    {
        public int? ReceptorId { get; set; }
        public string ClienteNombre { get; set; } = string.Empty;
        public List<AgingPlanDto> Planes { get; set; } = new();
        public AgingTotalesDto Subtotal { get; set; } = new();
    }

    public class AgingReporteDto
    {
        public DateTime FechaCorte { get; set; }
        public List<AgingClienteDto> Clientes { get; set; } = new();
        public AgingTotalesDto Totales { get; set; } = new();
    }
}
