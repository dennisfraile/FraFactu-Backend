using FraFactu.Domain.Common;

namespace FraFactu.Domain.Entities
{
    /// <summary>Registro auditable de un refinanciamiento de un plan de cuotas.</summary>
    public class HistorialRefinanciamiento : BaseEntity
    {
        public int PlanCuotasId { get; set; }
        public PlanCuotas PlanCuotas { get; set; } = null!;

        public int? UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }

        public string Motivo { get; set; } = string.Empty;

        /// <summary>Snapshot JSON de las cuotas pendientes anteriores.</summary>
        public string PlanAnteriorJson { get; set; } = string.Empty;

        /// <summary>Snapshot JSON de las cuotas nuevas.</summary>
        public string PlanNuevoJson { get; set; } = string.Empty;
    }
}
