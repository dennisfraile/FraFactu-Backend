namespace FraFactu.Application.DTOs.Roles
{
    /// <summary>
    /// DTO para actualización de rol
    /// </summary>
    public class UpdateRolDto
    {
        public string Nombre { get; set; } = string.Empty;
        public bool Activo { get; set; }

        /// <summary>
        /// Lista de IDs de permisos a asignar al rol
        /// (Reemplaza los permisos existentes)
        /// </summary>
        public List<int> PermisosIds { get; set; } = new List<int>();
    }
}
