namespace FraFactu.Application.DTOs.Facturas
{
    /// <summary>
    /// DTO para creación de nueva Factura Electrónica
    /// </summary>
    public class CreateFacturaDto
    {
        // ==========================================
        // RELACIONES DE NEGOCIO
        // ==========================================
        // EmisorId se toma del contexto del usuario autenticado
        public int ReceptorId { get; set; }

        // ==========================================
        // IDENTIFICACIÓN (DTE)
        // ==========================================
        public int Version { get; set; } = 3;
        public string Ambiente { get; set; } = "00"; // 00=Prueba, 01=Producción

        public int CatTipoDocumentoId { get; set; }

        // NumeroControl y CodigoGeneracion se generan automáticamente en el servicio

        public int CatModeloFacturacionId { get; set; } = 1; // 1=Previo
        public int CatTipoTransmisionId { get; set; } = 1; // 1=Normal

        public DateTime FechaEmision { get; set; }
        public TimeSpan HoraEmision { get; set; }

        public int CatMonedaId { get; set; } = 1; // Por defecto USD

        // Contingencia (Opcional)
        public int? CatTipoContingenciaId { get; set; }
        public string? MotivoContingencia { get; set; }

        // ==========================================
        // RESUMEN (TOTALES) - Se calculan automáticamente
        // ==========================================
        public decimal PorcentajeDescuento { get; set; } = 0;
        public int CatCondicionOperacionId { get; set; }

        // ==========================================
        // OBSERVACIONES
        // ==========================================
        public string? Observaciones { get; set; }

        // ==========================================
        // LISTAS (DETALLES Y PAGOS)
        // ==========================================
        /// <summary>
        /// Items de la factura (requerido, al menos 1)
        /// </summary>
        public List<CreateFacturaDetalleDto> Detalles { get; set; } = new();

        /// <summary>
        /// Formas de pago (requerido, al menos 1)
        /// </summary>
        public List<CreatePagoDto> Pagos { get; set; } = new();
    }
}
