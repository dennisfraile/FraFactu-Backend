namespace FraFactu.Application.DTOs.Invalidacion
{
    /// <summary>
    /// DTO para datos del documento a invalidar
    /// </summary>
    public class DocumentoInvalidarDto
    {
        public string TipoDte { get; set; } = string.Empty;
        public string CodigoGeneracion { get; set; } = string.Empty;
        public string SelloRecibido { get; set; } = string.Empty;
        public string NumeroControl { get; set; } = string.Empty;
        public string FecEmi { get; set; } = string.Empty;
        public decimal? MontoIva { get; set; }
        public string? CodigoGeneracionR { get; set; }
        public string TipoDocumento { get; set; } = string.Empty;
        public string NumDocumento { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public string? Correo { get; set; }
    }
}
