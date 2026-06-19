namespace FraFactu.Application.DTOs.Sucursales
{
    /// <summary>
    /// DTO para crear una nueva Sucursal
    /// </summary>
    public class CreateSucursalDto
    {
        public string? Codigo { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Direccion { get; set; }
        public string? Telefono { get; set; }
        public string? CorreoElectronico { get; set; }

        // Ubicación según MH (IDs de catálogo)
        public int CatDepartamentoId { get; set; } = 6; // 6 = San Salvador
        public int CatMunicipioId { get; set; } = 14; // 14 = San Salvador municipio
        public int? CatDistritoId { get; set; } // CAT-008 (obligatorio para emitir V2.0)
        public int CatTipoEstablecimientoId { get; set; } = 2; // 2 = Sucursal

        // Códigos oficiales MH (opcionales)
        public string? CodigoEstablecimiento { get; set; }

        // Datos del responsable para contingencia (opcionales)
        public string? ContingenciaNombreResponsable { get; set; }
        public string? ContingenciaTipoDocResponsable { get; set; }
        public string? ContingenciaNumeroDocResponsable { get; set; }

        // EmisorId se infiere del usuario autenticado
    }
}
