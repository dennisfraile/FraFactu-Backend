namespace FraFactu.Application.DTOs.Sucursales
{
    /// <summary>
    /// DTO ligero de Sucursal para listados
    /// </summary>
    public class SucursalListDto
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public bool Activo { get; set; }
        public string Direccion { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string? ContingenciaNombreResponsable { get; set; }
        public string? ContingenciaTipoDocResponsable { get; set; }
        public string? ContingenciaNumeroDocResponsable { get; set; }
    }
}
