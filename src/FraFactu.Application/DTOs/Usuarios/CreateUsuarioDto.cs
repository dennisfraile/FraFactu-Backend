namespace FraFactu.Application.DTOs.Usuarios
{
    /// <summary>
    /// DTO para creación de nuevo usuario
    /// </summary>
    public class CreateUsuarioDto
    {
        public string NombreCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Password { get; set; }
        public bool RequiereCambioPwd { get; set; } = false;
        public bool PermiteCambioPwd { get; set; } = false;
        public int EmisorId { get; set; }
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
    }
}
