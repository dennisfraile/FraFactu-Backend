namespace FraFactu.Application.DTOs.Facturas
{
    /// <summary>
    /// DTO para la extensión de la factura (campos adicionales opcionales)
    /// </summary>
    public class ExtensionDto
    {
        /// <summary>
        /// Nombre de quien entrega
        /// </summary>
        public string? NombEntrega { get; set; }

        /// <summary>
        /// Documento de quien entrega
        /// </summary>
        public string? DocuEntrega { get; set; }

        /// <summary>
        /// Nombre de quien recibe
        /// </summary>
        public string? NombRecibe { get; set; }

        /// <summary>
        /// Documento de quien recibe
        /// </summary>
        public string? DocuRecibe { get; set; }

        /// <summary>
        /// Placa del vehículo (para entregas)
        /// </summary>
        public string? PlacaVehiculo { get; set; }

        /// <summary>
        /// Observaciones de la extensión
        /// </summary>
        public string? Observaciones { get; set; }
    }
}
