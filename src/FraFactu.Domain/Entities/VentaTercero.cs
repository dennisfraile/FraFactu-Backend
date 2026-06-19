using FraFactu.Domain.Common;

namespace FraFactu.Domain.Entities
{
    /// <summary>
    /// Entidad para Ventas por cuenta de Terceros (específico de CCF)
    /// Relación 1:1 opcional con FacturaElectronica
    /// </summary>
    public class VentaTercero : BaseEntity
    {
        /// <summary>
        /// FK a FacturaElectronica
        /// </summary>
        public int FacturaElectronicaId { get; set; }
        public FacturaElectronica FacturaElectronica { get; set; } = null!;

        /// <summary>
        /// NIT por cuenta de Terceros
        /// Pattern: 9 o 14 dígitos
        /// </summary>
        public string Nit { get; set; } = string.Empty;

        /// <summary>
        /// Nombre, denominación o razón social del Tercero
        /// MaxLength: 200
        /// </summary>
        public string Nombre { get; set; } = string.Empty;
    }
}
