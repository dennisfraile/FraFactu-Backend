using System.Text.Json.Serialization;

namespace FraFactu.Application.DTOs.Hacienda
{
    /// <summary>
    /// Respuesta de Hacienda al anular un DTE
    /// </summary>
    public class InvalidacionResponseDto
    {
        /// <summary>
        /// Estado de la transacción: PROCESADO, RECHAZADO, RECIBIDO
        /// </summary>
        public string Estado { get; set; } = string.Empty;

        /// <summary>
        /// Código de mensaje de respuesta
        /// </summary>
        public string CodigoMsg { get; set; } = string.Empty;

        /// <summary>
        /// Descripción del mensaje
        /// </summary>
        public string DescripcionMsg { get; set; } = string.Empty;

        /// <summary>
        /// Sello de recepción de la anulación (si fue exitosa)
        /// </summary>
        public string? SelloRecibido { get; set; }

        /// <summary>
        /// Fecha de procesamiento
        /// </summary>
        public string? FhProcesamiento { get; set; }

        /// <summary>
        /// Lista de observaciones o errores
        /// </summary>
        public List<string>? Observaciones { get; set; }
    }
}
