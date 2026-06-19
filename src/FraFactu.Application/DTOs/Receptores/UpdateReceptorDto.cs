namespace FraFactu.Application.DTOs.Receptores
{
    /// <summary>
    /// DTO para actualización de receptor/cliente
    /// </summary>
    public class UpdateReceptorDto
    {
        // Nullable: receptor FC "Sin documento" → null tanto en tipo como en número.
        public int? CatTipoDocumentoId { get; set; }
        public string NumeroDocumento { get; set; } = string.Empty;
        public string NombreRazonSocial { get; set; } = string.Empty;

        // Ubicación (opcional)
        public int? CatDepartamentoId { get; set; }
        public int? CatMunicipioId { get; set; }
        public int? CatDistritoId { get; set; } // CAT-008 (recomendado para V2.0)

        public string Direccion { get; set; } = string.Empty;

        // Información Adicional (opcional)
        public string? Nrc { get; set; }
        public string? CodigoActividad { get; set; }
        public string? DescripcionActividad { get; set; }

        public string CorreoElectronico { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public bool Activo { get; set; }
    }
}
