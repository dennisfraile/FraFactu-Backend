namespace FraFactu.Application.DTOs.Usuarios
{
    /// <summary>
    /// DTO ligero de Usuario para listados
    /// </summary>
    public class UsuarioListDto
    {
        public int Id { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool Activo { get; set; }
        public int RolId { get; set; }
        public string RolNombre { get; set; } = string.Empty;
        public string EmisorNombre { get; set; } = string.Empty;
        public bool AccesoTodasSucursales { get; set; }
        public int CantidadSucursales { get; set; }
        public string SucursalesNombres { get; set; } = string.Empty;
        public List<int> SucursalIds { get; set; } = new();
        public List<CajaAsignadaDto> Cajas { get; set; } = new();
        public DateTime FechaCreacion { get; set; }
        public DateTime? UltimoAcceso { get; set; }
    }
}
