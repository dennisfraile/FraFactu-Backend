namespace FraFactu.Application.DTOs.Invalidacion
{
    /// <summary>
    /// DTO para datos del emisor en evento de invalidación
    /// </summary>
    public class EmisorInvalidacionDto
    {
        public string Nit { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string TipoEstablecimiento { get; set; } = string.Empty;
        public string? NomEstablecimiento { get; set; }
        public string? CodEstableMH { get; set; }
        public string? CodEstable { get; set; }
        public string? CodPuntoVentaMH { get; set; }
        public string? CodPuntoVenta { get; set; }
        public string? Telefono { get; set; }
        public string Correo { get; set; } = string.Empty;
    }
}
