using FraFactu.Domain.Common;

namespace FraFactu.Domain.Entities
{
    /// <summary>
    /// Entidad para información de Médico que presta servicios
    /// (parte de OtrosDocumentos cuando CodDocAsociado = 3)
    /// Relación 1:1 con OtroDocumento
    /// </summary>
    public class MedicoServicio : BaseEntity
    {
        /// <summary>
        /// FK a OtroDocumento
        /// </summary>
        public int OtroDocumentoId { get; set; }
        public OtroDocumento OtroDocumento { get; set; } = null!;

        /// <summary>
        /// Nombre de médico que presta el Servicio
        /// MaxLength: 100
        /// </summary>
        public string Nombre { get; set; } = string.Empty;

        /// <summary>
        /// NIT de médico que presta el Servicio
        /// Pattern: 9 o 14 dígitos
        /// Uno de Nit o DocIdentificacion es requerido
        /// </summary>
        public string? Nit { get; set; }

        /// <summary>
        /// Documento de identificación de médico no domiciliados
        /// MinLength: 2, MaxLength: 25
        /// Uno de Nit o DocIdentificacion es requerido
        /// </summary>
        public string? DocIdentificacion { get; set; }

        /// <summary>
        /// Código del Servicio realizado
        /// Valores: 1-6
        /// </summary>
        public int TipoServicio { get; set; }
    }
}
