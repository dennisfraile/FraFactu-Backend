namespace FraFactu.Application.DTOs.Lotes;

/// <summary>
/// DTO para un DTE individual dentro de un lote
/// </summary>
public class LoteDetalleDto
{
    public int FacturaId { get; set; }
    public string NumeroControl { get; set; } = string.Empty;
    public string CodigoGeneracion { get; set; } = string.Empty;

    // Datos de la factura para el frontend
    public string? TipoDte { get; set; }
    public string? ReceptorNombre { get; set; }
    public decimal TotalPagar { get; set; }

    // Estado individual del DTE
    public string? EstadoDte { get; set; }
    public string? SelloRecibido { get; set; }
    public DateTime? FechaConsulta { get; set; }

    // Si rechazado
    public string? CodigoRechazo { get; set; }
    public string? ObservacionesRechazo { get; set; }
}
