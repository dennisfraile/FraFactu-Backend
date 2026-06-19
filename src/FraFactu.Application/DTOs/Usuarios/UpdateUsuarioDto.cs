namespace FraFactu.Application.DTOs.Usuarios
{
    /// <summary>
    /// DTO para actualización de usuario
    /// </summary>
    public class UpdateUsuarioDto
    {
        public string NombreCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool RequiereCambioPwd { get; set; }
        public bool PermiteCambioPwd { get; set; }
        public bool Activo { get; set; }
        public int RolId { get; set; }

        /// <summary>
        /// Si es true, el usuario tendrá acceso a todas las sucursales del emisor.
        /// Si es false, solo tendrá acceso a las sucursales en SucursalIds.
        /// </summary>
        public bool AccesoTodasSucursales { get; set; } = false;

        /// <summary>
        /// Lista de IDs de sucursales asignadas (requerido si AccesoTodasSucursales = false)
        /// </summary>
        public List<int> SucursalIds { get; set; } = new();

        /// <summary>
        /// Lista de IDs de cajas asignadas (solo para rol Cajero)
        /// </summary>
        public List<int> CajaIds { get; set; } = new();

        /// <summary>
        /// Password opcional para resetear contraseña de otro usuario (solo admins)
        /// Si es null, no se cambia la contraseña
        /// </summary>
        public string? Password { get; set; }

        // Nota: No se permite cambiar EmisorId por seguridad (multi-tenant)
    }
}
