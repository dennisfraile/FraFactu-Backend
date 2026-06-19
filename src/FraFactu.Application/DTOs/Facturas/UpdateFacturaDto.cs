namespace FraFactu.Application.DTOs.Facturas
{
    /// <summary>
    /// DTO para actualización de Factura Electrónica
    /// Nota: Solo se pueden actualizar facturas en estado BORRADOR
    /// </summary>
    public class UpdateFacturaDto
    {
        public int ReceptorId { get; set; }

        public int CatTipoDocumentoId { get; set; }
        public int CatModeloFacturacionId { get; set; }
        public int CatTipoTransmisionId { get; set; }

        public DateTime FechaEmision { get; set; }
        public TimeSpan HoraEmision { get; set; }

        public int CatMonedaId { get; set; }

        // Contingencia
        public int? CatTipoContingenciaId { get; set; }
        public string? MotivoContingencia { get; set; }

        public decimal PorcentajeDescuento { get; set; }
        public int CatCondicionOperacionId { get; set; }

        public string? Observaciones { get; set; }

        // Listas - Reemplazan completamente las existentes
        public List<CreateFacturaDetalleDto> Detalles { get; set; } = new();
        public List<CreatePagoDto> Pagos { get; set; } = new();
    }
}
