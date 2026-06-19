namespace FraFactu.Application.DTOs.Lotes;

/// <summary>
/// Respuesta del servicio de consulta individual de MH
/// </summary>
public class ConsultaDteResponse
{
    public string CodigoGeneracion { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty; // "APROBADO", "RECHAZADO"
    public string? SelloRecibido { get; set; }
    public string? CodigoRechazo { get; set; }
    public List<string>? Observaciones { get; set; }
}
