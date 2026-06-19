using FraFactu.Domain.Common;

namespace FraFactu.Domain.Entities
{
    /// <summary>
    /// Control de saldos por DTE para Notas de Crédito (RN-003).
    /// Registra el monto original de un DTE y cuánto ha sido acreditado por NCEs.
    /// </summary>
    public class SaldoDte : BaseEntity
    {
        /// <summary>
        /// CodigoGeneracion (UUID) del DTE original
        /// </summary>
        public string DteCodigoGeneracion { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de DTE original: "01" (FCF), "03" (CCF), "14" (FSE)
        /// </summary>
        public string TipoDte { get; set; } = string.Empty;

        /// <summary>
        /// Emisor propietario del DTE
        /// </summary>
        public int EmisorId { get; set; }
        public Emisor Emisor { get; set; } = null!;

        /// <summary>
        /// Monto total de operación del DTE original
        /// </summary>
        public decimal MontoOriginal { get; set; }

        /// <summary>
        /// Monto total acreditado por NCEs emitidas
        /// </summary>
        public decimal MontoAcreditado { get; set; }

        /// <summary>
        /// Saldo disponible para nuevas NCEs (MontoOriginal - MontoAcreditado)
        /// </summary>
        public decimal SaldoDisponible => MontoOriginal - MontoAcreditado;

        /// <summary>
        /// Cantidad de NCEs emitidas contra este DTE
        /// </summary>
        public int NceCount { get; set; }

        /// <summary>
        /// Fecha de última actualización del saldo
        /// </summary>
        public DateTime FechaActualizacion { get; set; } = DateTime.UtcNow;
    }
}
