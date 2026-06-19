namespace FraFactu.Application.DTOs.Receptores
{
    /// <summary>
    /// DTO completo de Receptor/Cliente (para lectura)
    /// </summary>
    public class ReceptorDto
    {
        public int Id { get; set; }

        // Tipo de Documento (ID y nombre). Nullable: receptor FC "Sin documento".
        public int? CatTipoDocumentoId { get; set; }
        public string TipoDocumentoNombre { get; set; } = string.Empty;

        public string NumeroDocumento { get; set; } = string.Empty;
        public string NombreRazonSocial { get; set; } = string.Empty;

        // Ubicación (opcional)
        public int? CatDepartamentoId { get; set; }
        public string? DepartamentoNombre { get; set; }
        public int? CatMunicipioId { get; set; }
        public string? MunicipioNombre { get; set; }
        public int? CatDistritoId { get; set; }
        public string? DistritoNombre { get; set; }

        public string Direccion { get; set; } = string.Empty;

        // Información Adicional
        public string? Nrc { get; set; }
        public string? CodigoActividad { get; set; }
        public string? DescripcionActividad { get; set; }

        public string CorreoElectronico { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public bool Activo { get; set; }
        public DateTime FechaCreacion { get; set; }

        // Información del Emisor al que pertenece (multi-tenant)
        public int EmisorId { get; set; }
        public string EmisorNombre { get; set; } = string.Empty;

        // Estadísticas opcionales
        public int? TotalFacturas { get; set; }
        public decimal? TotalFacturado { get; set; }
    }
}
