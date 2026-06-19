using FraFactu.Domain.Common;

namespace FraFactu.Domain.Entities
{
    /// <summary>Notificación in-app de un emisor (visible a todos sus usuarios).</summary>
    public class Notificacion : BaseEntity
    {
        public int EmisorId { get; set; }
        public Emisor Emisor { get; set; } = null!;
        public string Tipo { get; set; } = string.Empty;
        public string Nivel { get; set; } = "info";
        public string Titulo { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
        public string? Ruta { get; set; }
        public int? ReferenciaId { get; set; }
    }
}
