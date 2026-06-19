namespace FraFactu.Application.DTOs.Facturas
{
    /// <summary>
    /// DTO ligero de Factura para listados
    /// </summary>
    public class FacturaListDto
    {
        public int Id { get; set; }
        public string NumeroControl { get; set; } = string.Empty;
        public string CodigoGeneracion { get; set; } = string.Empty;

        public DateTime FechaEmision { get; set; }
        public TimeSpan HoraEmision { get; set; }
        public string TipoDocumento { get; set; } = string.Empty; // Ej: "Factura"

        public string ReceptorNombre { get; set; } = string.Empty;
        public string ReceptorNumeroDocumento { get; set; } = string.Empty;

        public decimal TotalPagar { get; set; }
        public string EstadoHacienda { get; set; } = string.Empty;

        public DateTime FechaCreacion { get; set; }
        public bool Activo { get; set; }

        public string? SelloRecepcion { get; set; }
        public DateTime? FechaTransmision { get; set; }
        public TimeSpan? HoraTransmision { get; set; }
        public string TipoDte { get; set; } = string.Empty;

        public int? VendedorId { get; set; }
        public string? VendedorNombre { get; set; }
        public string? VendedorCodigo { get; set; }
        public string? CajaCodigo { get; set; }

        public int? UsuarioId { get; set; }
        public int CatTipoTransmisionId { get; set; }
        public int? LoteId { get; set; }
        public int? SucursalId { get; set; }
        public int? EventoContingenciaId { get; set; }
        public string Ambiente { get; set; } = string.Empty;
    }
}
