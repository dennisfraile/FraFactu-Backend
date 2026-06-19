using FraFactu.Application.DTOs.Common;

namespace FraFactu.Application.DTOs.Facturas
{
    /// <summary>
    /// DTO completo de Factura Electrónica (para lectura)
    /// </summary>
    public class FacturaElectronicaDto
    {
        // ==========================================
        // DATOS BÁSICOS
        // ==========================================
        public int Id { get; set; }
        public DateTime FechaCreacion { get; set; }
        public bool Activo { get; set; }

        // ==========================================
        // RELACIONES DE NEGOCIO (SaaS/Multi-Tenant)
        // ==========================================
        public int EmisorId { get; set; }
        public string EmisorNit { get; set; } = string.Empty;
        public string EmisorNombre { get; set; } = string.Empty;

        public int ReceptorId { get; set; }
        public string ReceptorNumeroDocumento { get; set; } = string.Empty;
        public string ReceptorNombre { get; set; } = string.Empty;

        // ==========================================
        // VENDEDOR Y CAJA
        // ==========================================
        public int? VendedorId { get; set; }
        public string? VendedorCodigo { get; set; }
        public string? VendedorNombre { get; set; }

        public int? CajaId { get; set; }
        public string? CajaCodigo { get; set; }

        // ==========================================
        // IDENTIFICACIÓN (DTE)
        // ==========================================
        public int Version { get; set; }
        public string Ambiente { get; set; } = string.Empty;

        public int CatTipoDocumentoId { get; set; }
        public CatalogoDto TipoDocumento { get; set; } = new();

        public string NumeroControl { get; set; } = string.Empty;
        public string CodigoGeneracion { get; set; } = string.Empty;

        public int CatModeloFacturacionId { get; set; }
        public int CatTipoTransmisionId { get; set; }

        public DateTime FechaEmision { get; set; }
        public TimeSpan HoraEmision { get; set; }

        public DateTime? FechaTransmision { get; set; }
        public TimeSpan? HoraTransmision { get; set; }

        public int CatMonedaId { get; set; }

        // Contingencia (Opcional)
        public int? CatTipoContingenciaId { get; set; }
        public string? MotivoContingencia { get; set; }

        // ==========================================
        // RESUMEN (TOTALES)
        // ==========================================
        public decimal TotalNoSujeto { get; set; }
        public decimal TotalExento { get; set; }
        public decimal TotalGravado { get; set; }
        public decimal SubTotalVentas { get; set; }

        public decimal DescuentoNoSujeto { get; set; }
        public decimal DescuentoExento { get; set; }
        public decimal DescuentoGravado { get; set; }
        public decimal PorcentajeDescuento { get; set; }
        public decimal TotalDescuento { get; set; }

        public decimal SubTotal { get; set; }

        public decimal IvaPercibido { get; set; }
        public decimal IvaRetenido { get; set; }
        public decimal RetencionRenta { get; set; }

        public decimal MontoTotalOperacion { get; set; }
        public decimal TotalNoGravado { get; set; }
        public decimal TotalPagar { get; set; }

        public string TotalLetras { get; set; } = string.Empty;
        public decimal SaldoFavor { get; set; }

        public int CatCondicionOperacionId { get; set; }

        // ==========================================
        // ESTADO DEL PROCESO
        // ==========================================
        public string EstadoHacienda { get; set; } = string.Empty;
        public string? SelloRecibido { get; set; }
        public string? Observaciones { get; set; }

        // ==========================================
        // LISTAS (DETALLES, TRIBUTOS, PAGOS)
        // ==========================================
        public List<FacturaDetalleDto> Detalles { get; set; } = new();
        public List<FacturaTributoDto> Tributos { get; set; } = new();
        public List<PagoDto> Pagos { get; set; } = new();
    }
}
