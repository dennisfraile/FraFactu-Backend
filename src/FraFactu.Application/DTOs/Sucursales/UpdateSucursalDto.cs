namespace FraFactu.Application.DTOs.Sucursales
{
    /// <summary>
    /// DTO para actualizar una Sucursal existente
    /// </summary>
    public class UpdateSucursalDto
    {
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? Direccion { get; set; }
        public string? Telefono { get; set; }
        public string? CorreoElectronico { get; set; }

        // Ubicación según MH (IDs de catálogo)
        public int CatDepartamentoId { get; set; }
        public int CatMunicipioId { get; set; }
        public int? CatDistritoId { get; set; } // CAT-008 (obligatorio para emitir V2.0)
        public int CatTipoEstablecimientoId { get; set; }

        // Códigos oficiales MH (opcionales)
        public string? CodigoEstablecimiento { get; set; }

        // Datos del responsable para contingencia (opcionales)
        public string? ContingenciaNombreResponsable { get; set; }
        public string? ContingenciaTipoDocResponsable { get; set; }
        public string? ContingenciaNumeroDocResponsable { get; set; }
    }
}
