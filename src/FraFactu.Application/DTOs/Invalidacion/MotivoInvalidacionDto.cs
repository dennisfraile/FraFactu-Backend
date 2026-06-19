namespace FraFactu.Application.DTOs.Invalidacion
{
    /// <summary>
    /// DTO para motivo de invalidación
    /// </summary>
    public class MotivoInvalidacionDto
    {
        /// <summary>
        /// Tipo de invalidación: 1=Error info, 2=Rescisión, 3=Otro
        /// </summary>
        public int TipoAnulacion { get; set; }

        /// <summary>
        /// Descripción del motivo (requerido si tipo=3)
        /// </summary>
        public string? MotivoAnulacion { get; set; }

        public string NombreResponsable { get; set; } = string.Empty;
        public string TipDocResponsable { get; set; } = string.Empty;
        public string NumDocResponsable { get; set; } = string.Empty;
        public string NombreSolicita { get; set; } = string.Empty;
        public string TipDocSolicita { get; set; } = string.Empty;
        public string NumDocSolicita { get; set; } = string.Empty;
    }
}
