namespace FraFactu.Application.DTOs.Invalidacion
{
    /// <summary>
    /// DTO para solicitar la invalidación de una factura desde el API
    /// </summary>
    public class AnularFacturaDto
    {
        /// <summary>
        /// Tipo de invalidación: 1=Error info, 2=Rescisión, 3=Otro
        /// </summary>
        public int TipoAnulacion { get; set; }

        /// <summary>
        /// Motivo de la invalidación (requerido si tipo=3)
        /// </summary>
        public string? MotivoAnulacion { get; set; }

        /// <summary>
        /// ID de la factura de reemplazo (si tipo=1 o 3)
        /// </summary>
        public int? FacturaReemplazoId { get; set; }

        // RESPONSABLE DE INVALIDAR
        public string NombreResponsable { get; set; } = string.Empty;
        public int CatTipoDocResponsableId { get; set; } = 2; // 2=DUI por defecto
        public string NumDocResponsable { get; set; } = string.Empty;

        // QUIEN SOLICITA INVALIDACIÓN
        public string NombreSolicita { get; set; } = string.Empty;
        public int CatTipoDocSolicitaId { get; set; } = 2; // 2=DUI por defecto
        public string NumDocSolicita { get; set; } = string.Empty;

        // GESTIÓN DE INVENTARIO
        /// <summary>
        /// Tipo de invalidación para inventario:
        /// ERROR_FACTURA: Error en la factura, se emitirá una nueva
        /// DEVOLUCION_SIMPLE: Cliente devuelve el producto
        /// CAMBIO_PRODUCTO: Cliente devuelve producto A y compra producto B
        /// </summary>
        public string? TipoInvalidacion { get; set; } = "ERROR_FACTURA";

        /// <summary>
        /// Indica si se debe revertir el inventario (true por defecto)
        /// </summary>
        public bool RevirtiInventario { get; set; } = true;
    }
}
