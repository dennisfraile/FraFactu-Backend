namespace FraFactu.Application.DTOs.Hacienda
{
    /// <summary>
    /// Respuesta del Ministerio de Hacienda para eventos de contingencia
    /// </summary>
    public class EventoContingenciaResponse
    {
        /// <summary>
        /// Estado de la solicitud: RECIBIDO, RECHAZADO
        /// </summary>
        public string Estado { get; set; } = string.Empty;

        /// <summary>
        /// Fecha y hora en que se procesó (formato dd/MM/yyyy HH:mm:ss)
        /// </summary>
        public string FechaHora { get; set; } = string.Empty;

        /// <summary>
        /// Mensaje descriptivo del resultado
        /// </summary>
        public string Mensaje { get; set; } = string.Empty;

        /// <summary>
        /// Sello de recibido (código alfanumérico) - Solo si fue RECIBIDO
        /// </summary>
        public string? SelloRecibido { get; set; }

        /// <summary>
        /// Observaciones o errores de validación
        /// </summary>
        public List<string> Observaciones { get; set; } = new();
    }
}
