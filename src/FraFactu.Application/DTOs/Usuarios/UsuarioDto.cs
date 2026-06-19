namespace FraFactu.Application.DTOs.Usuarios
{
    /// <summary>
    /// DTO completo de Usuario (para lectura)
    /// </summary>
    public class UsuarioDto
    {
        public int Id { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool RequiereCambioPwd { get; set; }
        public DateTime? ExpiracionPwdTemporal { get; set; }
        public bool PermiteCambioPwd { get; set; }
        public bool Activo { get; set; }
        public DateTime FechaCreacion { get; set; }

        // Información del Emisor (Tenant)
        public int EmisorId { get; set; }
        public string EmisorNombre { get; set; } = string.Empty;
        public string EmisorNit { get; set; } = string.Empty;

        // Información del Rol
        public int RolId { get; set; }
        public string RolNombre { get; set; } = string.Empty;

        // Sucursales asignadas
        public bool AccesoTodasSucursales { get; set; }
        public List<SucursalAsignadaDto> Sucursales { get; set; } = new();

        // Cajas asignadas (solo para rol Cajero)
        public List<CajaAsignadaDto> Cajas { get; set; } = new();

        // Auditoría
        public DateTime? UltimoAcceso { get; set; }
    }

    public class SucursalAsignadaDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
    }

    public class CajaAsignadaDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? SucursalNombre { get; set; }
    }
}
