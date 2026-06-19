using FraFactu.Domain.Common;

namespace FraFactu.Domain.Entities
{
    public class Suscripcion : BaseEntity
    {
        // FK al Emisor
        public int EmisorId { get; set; }
        public Emisor Emisor { get; set; } = null!;

        // Datos del plan
        public string NombrePlan { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public string? Notas { get; set; }

        // Cobro mensual: siempre día 1 de cada mes, se auto-avanza
        public DateTime FechaProximoCobro { get; set; }

        // Prorrateo: monto del primer mes si la suscripción inicia a mitad de mes (null = mes completo)
        public decimal? PrecioProrrateado { get; set; }

        // Días de gracia después de la fecha de cobro antes de marcar como vencido (default 3)
        public int DiasGracia { get; set; } = 3;

        // Control de pagos
        public DateTime? FechaUltimoPago { get; set; }
        public bool PagoAlDia { get; set; } = false;

        // Facturas generadas para esta suscripción
        public ICollection<FacturaSuscripcion> FacturasSuscripcion { get; set; } = new List<FacturaSuscripcion>();
    }
}
