namespace FraFactu.Application.DTOs.Sucursales
{
    /// <summary>
    /// DTO completo de Sucursal (para lectura)
    /// </summary>
    public class SucursalDto
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? Direccion { get; set; }
        public string? Telefono { get; set; }
        public string? CorreoElectronico { get; set; }

        // Ubicación (IDs de catálogos)
        public int CatDepartamentoId { get; set; }
        public string DepartamentoNombre { get; set; } = string.Empty;

        public int CatMunicipioId { get; set; }
        public string MunicipioNombre { get; set; } = string.Empty;

        public int? CatDistritoId { get; set; }
        public string? DistritoNombre { get; set; }

        public int CatTipoEstablecimientoId { get; set; }
        public string TipoEstablecimientoNombre { get; set; } = string.Empty;

        // Códigos de Establecimiento MH
        public string? CodigoEstablecimiento { get; set; }


        // Datos del responsable para contingencia
        public string? ContingenciaNombreResponsable { get; set; }
        public string? ContingenciaTipoDocResponsable { get; set; }
        public string? ContingenciaNumeroDocResponsable { get; set; }

        public bool Activo { get; set; }
        public DateTime FechaCreacion { get; set; }

        // Emisor
        public int EmisorId { get; set; }
        public string EmisorNombre { get; set; } = string.Empty;

        // Estadísticas (opcional)
        public int? TotalFacturas { get; set; }
    }
}
