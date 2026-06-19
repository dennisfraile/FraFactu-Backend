using FraFactu.Domain.Common;

namespace FraFactu.Domain.Entities
{
    /// <summary>
    /// Entidad para Otros Documentos Asociados (específico de CCF)
    /// Relación 1:N con FacturaElectronica (máximo 10 por factura)
    /// </summary>
    public class OtroDocumento : BaseEntity
    {
        /// <summary>
        /// FK a FacturaElectronica
        /// </summary>
        public int FacturaElectronicaId { get; set; }
        public FacturaElectronica FacturaElectronica { get; set; } = null!;

        /// <summary>
        /// Código de Documento Asociado
        /// Valores: 1-4
        /// 3 = Médico (requiere MedicoServicio)
        /// </summary>
        public int CodDocAsociado { get; set; }

        /// <summary>
        /// Identificación del documento asociado
        /// MaxLength: 100
        /// Requerido solo si CodDocAsociado != 3
        /// </summary>
        public string? DescDocumento { get; set; }

        /// <summary>
        /// Descripción de documento asociado
        /// MaxLength: 300
        /// Requerido solo si CodDocAsociado != 3
        /// </summary>
        public string? DetalleDocumento { get; set; }

        /// <summary>
        /// Información del médico (solo si CodDocAsociado = 3)
        /// Relación 1:1 opcional
        /// </summary>
        public MedicoServicio? Medico { get; set; }
    }
}
