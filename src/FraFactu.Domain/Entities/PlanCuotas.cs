using FraFactu.Domain.Common;
using FraFactu.Domain.Enums;

namespace FraFactu.Domain.Entities
{
    /// <summary>
    /// Plan de cuotas de una venta a crédito (condición 2) o mixto (condición 3).
    /// Cada cuota se factura con su propio DTE.
    /// </summary>
    public class PlanCuotas : BaseEntity
    {
        public int EmisorId { get; set; }
        public Emisor Emisor { get; set; } = null!;

        public int? ReceptorId { get; set; }
        public Receptor? Receptor { get; set; }

        /// <summary>Condición de operación: 2=Crédito, 3=Mixto.</summary>
        public int CondicionOperacion { get; set; }

        /// <summary>Total de la operación (con IVA).</summary>
        public decimal MontoTotal { get; set; }

        /// <summary>Suma neta facturada en cuotas ya pagadas (con IVA).</summary>
        public decimal MontoPagado { get; set; }

        /// <summary>Saldo pendiente = MontoTotal - MontoPagado.</summary>
        public decimal SaldoAdeudado { get; set; }

        public EstadoCobroPlan EstadoCobro { get; set; } = EstadoCobroPlan.Pendiente;

        /// <summary>
        /// Snapshot del CreateFacturaElectronicaDto original de la venta (JSON).
        /// Se usa para reconstruir el DTE de la cuota final (detalle completo + descuento).
        /// </summary>
        public string VentaSnapshotJson { get; set; } = string.Empty;

        public string? Observaciones { get; set; }

        public ICollection<Cuota> Cuotas { get; set; } = new List<Cuota>();
    }
}
