namespace FraFactu.Application.DTOs.Facturas
{
    /// <summary>
    /// DTO para médico que presta servicios (parte de OtrosDocumentos)
    /// </summary>
    public class MedicoDto
    {
        /// <summary>
        /// Nombre de médico que presta el Servicio
        /// </summary>
        public string Nombre { get; set; } = string.Empty;

        /// <summary>
        /// NIT de médico que presta el Servicio
        /// Pattern: 9 o 14 dígitos
        /// Requerido si DocIdentificacion es null
        /// </summary>
        public string? Nit { get; set; }

        /// <summary>
        /// Documento de identificación de médico no domiciliados
        /// Requerido si Nit es null
        /// </summary>
        public string? DocIdentificacion { get; set; }

        /// <summary>
        /// Código del Servicio realizado
        /// Valores: 1-6
        /// </summary>
        public int TipoServicio { get; set; }
    }
}
