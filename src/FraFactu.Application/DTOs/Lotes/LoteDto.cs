namespace FraFactu.Application.DTOs.Lotes;

/// <summary>
/// DTO con información completa de un lote
/// </summary>
public class LoteDto
{
    public int Id { get; set; }
    public string CodigoLote { get; set; } = string.Empty;
    public int TotalDtes { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string Ambiente { get; set; } = string.Empty;
    public bool EsContingencia { get; set; }
    public int? SucursalId { get; set; }

    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaEnvio { get; set; }
    public DateTime? FhProcesamiento { get; set; }

    // Respuesta MH
    public string? CodigoRespuesta { get; set; }
    public string? DescripcionRespuesta { get; set; }

    // Estadísticas
    public int TotalAprobados { get; set; }
    public int TotalRechazados { get; set; }
    public int TotalPendientes { get; set; }

    // Detalles de los DTEs
    public List<LoteDetalleDto> Detalles { get; set; } = new();
}
