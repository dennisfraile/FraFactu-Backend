namespace FraFactu.Application.DTOs.Facturas
{
    /// <summary>
    /// DTO para resultados de búsqueda de DTEs para referenciar en Nota de Crédito
    /// </summary>
    public class BuscarParaNcResultDto
    {
        public int Id { get; set; }
        public string NumeroControl { get; set; } = string.Empty;
        public string CodigoGeneracion { get; set; } = string.Empty;
        public string TipoDte { get; set; } = string.Empty;
        public string TipoDocumentoNombre { get; set; } = string.Empty;
        public DateTime FechaEmision { get; set; }

        // Receptor
        public string ReceptorNombre { get; set; } = string.Empty;
        public string ReceptorNumDocumento { get; set; } = string.Empty;

        // Montos
        public decimal MontoTotalOperacion { get; set; }
        public decimal TotalPagar { get; set; }
        public string EstadoHacienda { get; set; } = string.Empty;

        // Saldo
        public decimal SaldoDisponible { get; set; }
        public decimal MontoAcreditado { get; set; }
        public int NceCount { get; set; }
    }

    /// <summary>
    /// DTO con detalle completo de un DTE para pre-cargar en la NC
    /// </summary>
    public class DetalleParaNcDto
    {
        public int Id { get; set; }
        public string NumeroControl { get; set; } = string.Empty;
        public string CodigoGeneracion { get; set; } = string.Empty;
        public string TipoDte { get; set; } = string.Empty;
        public DateTime FechaEmision { get; set; }
        public int CondicionOperacion { get; set; }

        // Receptor
        public int? ReceptorId { get; set; }
        public string ReceptorNombre { get; set; } = string.Empty;
        public string ReceptorNumDocumento { get; set; } = string.Empty;
        public string? ReceptorNit { get; set; }
        public string? ReceptorNrc { get; set; }
        public string? ReceptorCorreo { get; set; }
        public string? ReceptorTelefono { get; set; }

        // Montos
        public decimal TotalGravada { get; set; }
        public decimal TotalExenta { get; set; }
        public decimal TotalNoSuj { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TotalIva { get; set; }
        public decimal MontoTotalOperacion { get; set; }
        public decimal TotalPagar { get; set; }

        // Saldo
        public decimal SaldoDisponible { get; set; }
        public decimal MontoAcreditado { get; set; }
        public int NceCount { get; set; }

        // Items del DTE original
        public List<DetalleItemParaNcDto> Items { get; set; } = new();
    }

    public class DetalleItemParaNcDto
    {
        public int NumItem { get; set; }
        public int TipoItem { get; set; }
        public string? Codigo { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal VentaGravada { get; set; }
        public decimal VentaExenta { get; set; }
        public decimal VentaNoSuj { get; set; }
        public decimal MontoDescuento { get; set; }
        public decimal IvaItem { get; set; }
        public int? UnidadMedida { get; set; }
        public string? NumeroDocumentoRelacionado { get; set; }
    }
}
