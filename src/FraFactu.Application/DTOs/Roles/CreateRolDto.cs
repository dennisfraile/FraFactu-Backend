namespace FraFactu.Application.DTOs.Roles
{
    /// <summary>
    /// DTO para creación de nuevo rol
    /// </summary>
    public class CreateRolDto
    {
        public string Nombre { get; set; } = string.Empty;

        /// <summary>
        /// Lista de IDs de permisos a asignar al rol
        /// </summary>
        public List<int> PermisosIds { get; set; } = new List<int>();
    }
}
