namespace FraFactu.Application.DTOs.Facturas
{
    /// <summary>
    /// DTO para Dirección según estándar de Hacienda
    /// </summary>
    public class DireccionDto
    {
        /// <summary>
        /// Código del Departamento (01-14)
        /// </summary>
        public string Departamento { get; set; } = string.Empty;

        /// <summary>
        /// Código del Municipio (01-99)
        /// </summary>
        public string Municipio { get; set; } = string.Empty;

        /// <summary>
        /// Código del Distrito (CAT-008). Obligatorio en la Normativa DTE V2.0 cuando
        /// el receptor lleva dirección (toda dirección de MH exige distrito).
        /// </summary>
        public string? Distrito { get; set; }

        /// <summary>
        /// Complemento de la dirección (detalle)
        /// Máximo 200 caracteres
        /// </summary>
        public string Complemento { get; set; } = string.Empty;
    }
}
