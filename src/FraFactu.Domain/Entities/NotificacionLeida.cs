using FraFactu.Domain.Common;

namespace FraFactu.Domain.Entities
{
    /// <summary>Estado "leída" de una notificación para un usuario concreto.</summary>
    public class NotificacionLeida : BaseEntity
    {
        public int NotificacionId { get; set; }
        public Notificacion Notificacion { get; set; } = null!;
        public int UsuarioId { get; set; }
        public Usuario Usuario { get; set; } = null!;
        public DateTime FechaLeida { get; set; }
    }
}
