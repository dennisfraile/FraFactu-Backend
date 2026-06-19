namespace FraFactu.Domain.Common
{
    public abstract class BaseEntity
    {
        public int Id { get; set; }
        
        // Auditoría básica: Saber cuándo se creó cada registro
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        
        // Opcional: Para Soft Delete (Borrado lógico)
        public bool Activo { get; set; } = true;
    }
}