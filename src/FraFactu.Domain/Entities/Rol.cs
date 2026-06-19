using FraFactu.Domain.Common;

namespace FraFactu.Domain.Entities
{
    public class Rol: BaseEntity
    {
        public string Nombre { get; set; } = string.Empty; 
    
        // Muchos a Muchos: Un rol tiene varios permisos (vía tabla intermedia)
        public ICollection<RolPermiso> RolesPermisos { get; set; } = new List<RolPermiso>();
        
        // Uno a Muchos: Un rol lo tienen varios usuarios
        public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
    }
}