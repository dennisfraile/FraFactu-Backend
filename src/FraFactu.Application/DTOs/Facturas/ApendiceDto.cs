namespace FraFactu.Application.DTOs.Facturas
{
    /// <summary>
    /// DTO para Apéndice del DTE
    /// Información adicional en formato clave-valor
    /// </summary>
    public class ApendiceDto
    {
        /// <summary>
        /// Campo (clave) del apéndice
        /// Máximo 25 caracteres
        /// </summary>
        public string Campo { get; set; } = string.Empty;

        /// <summary>
        /// Etiqueta descriptiva del campo
        /// Máximo 50 caracteres
        /// </summary>
        public string Etiqueta { get; set; } = string.Empty;

        /// <summary>
        /// Valor del campo
        /// Máximo 150 caracteres
        /// </summary>
        public string Valor { get; set; } = string.Empty;
    }
}
