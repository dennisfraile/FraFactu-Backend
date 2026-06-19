using FraFactu.Domain.Common;
using FraFactu.Domain.Enums;

namespace FraFactu.Domain.Entities
{
    /// <summary>
    /// Cuota individual de un plan de cuotas. Al pagarse, se emite su DTE.
    /// </summary>
    public class Cuota : BaseEntity
    {
        public int PlanCuotasId { get; set; }
        public PlanCuotas PlanCuotas { get; set; } = null!;

        /// <summary>Número de cuota (1..N).</summary>
        public int Numero { get; set; }

        /// <summary>Monto de la cuota (con IVA).</summary>
        public decimal Monto { get; set; }

        /// <summary>Porcentaje del total, si se definió por %.</summary>
        public decimal? Porcentaje { get; set; }

        public DateTime FechaPactada { get; set; }

        public EstadoCuota Estado { get; set; } = EstadoCuota.Pendiente;

        public DateTime? FechaPago { get; set; }

        /// <summary>DTE emitido al pagar la cuota (null mientras esté pendiente).</summary>
        public int? FacturaId { get; set; }
        public FacturaElectronica? Factura { get; set; }

        /// <summary>True si es la última cuota (se factura por total + descuento).</summary>
        public bool EsCuotaFinal { get; set; }

        /// <summary>Interés por mora acumulado (Plan 3; aquí siempre 0).</summary>
        public decimal InteresMora { get; set; }

        /// <summary>True si ya se envió el recordatorio "por vencer" de esta cuota (dedup one-shot).</summary>
        public bool RecordatorioPorVencerEnviado { get; set; } = false;

        /// <summary>True si ya se envió el recordatorio "vencida" de esta cuota (dedup one-shot).</summary>
        public bool RecordatorioVencidaEnviado { get; set; } = false;
    }
}
