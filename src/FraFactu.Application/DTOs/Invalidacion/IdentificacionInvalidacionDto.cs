namespace FraFactu.Application.DTOs.Invalidacion
{
    /// <summary>
    /// DTO para la sección de identificación del evento de invalidación
    /// </summary>
    public class IdentificacionInvalidacionDto
    {
        /// <summary>
        /// Versión del esquema (siempre 2)
        /// </summary>
        public int Version { get; set; } = 2;

        /// <summary>
        /// Ambiente: 00=Pruebas, 01=Producción
        /// </summary>
        public string Ambiente { get; set; } = "00";

        /// <summary>
        /// Código de generación UUID del evento
        /// </summary>
        public string CodigoGeneracion { get; set; } = string.Empty;

        /// <summary>
        /// Fecha de invalidación (yyyy-MM-dd)
        /// </summary>
        public string FecAnula { get; set; } = string.Empty;

        /// <summary>
        /// Hora de invalidación (HH:mm:ss)
        /// </summary>
        public string HorAnula { get; set; } = string.Empty;
    }
}
