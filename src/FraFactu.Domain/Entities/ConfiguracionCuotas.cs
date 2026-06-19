using FraFactu.Domain.Common;

namespace FraFactu.Domain.Entities
{
    /// <summary>Configuración de mora del plan de cuotas, 1:1 por emisor.</summary>
    public class ConfiguracionCuotas : BaseEntity
    {
        public int EmisorId { get; set; }
        public Emisor Emisor { get; set; } = null!;

        /// <summary>Tasa de mora mensual (ej. 0.03 = 3% mensual).</summary>
        public decimal TasaMoraMensual { get; set; } = 0.03m;

        /// <summary>Días de gracia antes de aplicar mora.</summary>
        public int DiasGracia { get; set; } = 3;

        /// <summary>Si false, nunca se cobra mora.</summary>
        public bool MoraHabilitada { get; set; } = true;

        /// <summary>Si false, no se envían recordatorios de cuotas (pero el marcado de Vencida sigue).</summary>
        public bool RecordatoriosHabilitados { get; set; } = true;

        /// <summary>Días antes del vencimiento para el recordatorio "por vencer".</summary>
        public int DiasAntesRecordatorio { get; set; } = 3;
    }
}
