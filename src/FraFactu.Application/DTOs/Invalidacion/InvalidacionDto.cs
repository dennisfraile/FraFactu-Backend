namespace FraFactu.Application.DTOs.Invalidacion
{
    /// <summary>
    /// DTO principal del evento de invalidación (estructura completa JSON)
    /// </summary>
    public class InvalidacionDto
    {
        public IdentificacionInvalidacionDto Identificacion { get; set; } = new();
        public EmisorInvalidacionDto Emisor { get; set; } = new();
        public DocumentoInvalidarDto Documento { get; set; } = new();
        public MotivoInvalidacionDto Motivo { get; set; } = new();

        // Propiedades de respuesta (no parte del esquema JSON de anulación, pero útiles para el cliente)
        public string? SelloRecibido { get; set; }
        public string? Estado { get; set; }
    }
}
