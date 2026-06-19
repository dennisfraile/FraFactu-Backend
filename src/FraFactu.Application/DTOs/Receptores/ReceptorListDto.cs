namespace FraFactu.Application.DTOs.Receptores
{
    /// <summary>
    /// DTO ligero de Receptor para listados
    /// </summary>
    public class ReceptorListDto
    {
        public int Id { get; set; }
        // Nullables: receptor FC "Sin documento" (par natural con la entidad Receptor).
        public int? CatTipoDocumentoId { get; set; }
        public string TipoDocumentoNombre { get; set; } = string.Empty;
        public string? NumeroDocumento { get; set; }
        public string NombreRazonSocial { get; set; } = string.Empty;
        public string CorreoElectronico { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;

        public string Nrc { get; set; } = string.Empty;
        public string CodigoActividad { get; set; } = string.Empty;
        public string DescripcionActividad { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public int? CatDepartamentoId { get; set; }
        public int? CatMunicipioId { get; set; }

        public bool Activo { get; set; }
        public DateTime FechaCreacion { get; set; }
    }
}
