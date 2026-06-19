using FraFactu.Application.DTOs.Permisos;

namespace FraFactu.Application.DTOs.Roles
{
    /// <summary>
    /// DTO completo de Rol (para lectura)
    /// </summary>
    public class RolDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public bool Activo { get; set; }
        public DateTime FechaCreacion { get; set; }

        /// <summary>
        /// Lista de permisos asignados a este rol
        /// </summary>
        public List<PermisoDto> Permisos { get; set; } = new List<PermisoDto>();

        /// <summary>
        /// Cantidad de usuarios con este rol
        /// </summary>
        public int? TotalUsuarios { get; set; }
    }
}
