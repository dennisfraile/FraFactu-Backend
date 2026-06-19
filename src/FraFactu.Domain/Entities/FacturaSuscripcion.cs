using FraFactu.Domain.Common;

namespace FraFactu.Domain.Entities
{
    /// <summary>
    /// Registro de cada factura/recordatorio de suscripción generado y enviado.
    /// </summary>
    public class FacturaSuscripcion : BaseEntity
    {
        public string NumeroFactura { get; set; } = string.Empty; // INV-YYYY-XXXX

        // FK a la suscripción
        public int SuscripcionId { get; set; }
        public Suscripcion Suscripcion { get; set; } = null!;

        public DateTime FechaEmision { get; set; }
        public DateTime PeriodoServicioInicio { get; set; }
        public DateTime PeriodoServicioFin { get; set; }
        public decimal Monto { get; set; }

        // Tracking de envío
        public bool EnviadoPorEmail { get; set; } = false;
        public DateTime? FechaEnvio { get; set; }
    }
}
