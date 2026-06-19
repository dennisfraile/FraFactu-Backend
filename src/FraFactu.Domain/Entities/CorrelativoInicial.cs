using FraFactu.Domain.Common;

namespace FraFactu.Domain.Entities
{
    /// <summary>
    /// Correlativo inicial declarado por un emisor que migra desde otro sistema.
    /// Hace que la numeración del numeroControl continúe desde el último emitido afuera.
    /// Único por (EmisorId, TipoDte, Anio, Ambiente). Solo influye mientras no haya
    /// facturas reales para esa combinación (luego el MAX real domina).
    /// </summary>
    public class CorrelativoInicial : BaseEntity
    {
        public int EmisorId { get; set; }
        public Emisor Emisor { get; set; } = null!;

        /// <summary>Tipo de DTE: "01","03","05","06","14".</summary>
        public string TipoDte { get; set; } = string.Empty;

        /// <summary>Año de migración (el correlativo se reinicia por año por normativa).</summary>
        public int Anio { get; set; }

        /// <summary>Ambiente MH: "00" pruebas / "01" producción.</summary>
        public string Ambiente { get; set; } = string.Empty;

        /// <summary>Último correlativo emitido en el sistema anterior. El sistema emite este valor + 1.</summary>
        public int UltimoCorrelativo { get; set; }

        /// <summary>Fecha de la última modificación del registro (la asigna el servicio al actualizar).</summary>
        public DateTime? FechaActualizacion { get; set; }
    }
}
