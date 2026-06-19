namespace FraFactu.Application.DTOs.Permisos
{
    /// <summary>
    /// DTO de Permiso (para lectura)
    /// </summary>
    public class PermisoDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public bool Activo { get; set; }
        public DateTime FechaCreacion { get; set; }
    }
}
