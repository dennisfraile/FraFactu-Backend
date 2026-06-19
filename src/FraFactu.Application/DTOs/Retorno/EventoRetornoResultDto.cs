namespace FraFactu.Application.DTOs.Retorno;

/// <summary>
/// Resultado de crear/consultar un Evento de Retorno (18).
/// </summary>
public class EventoRetornoResultDto
{
    public int Id { get; set; }
    public string CodigoGeneracion { get; set; } = string.Empty;
    public int Version { get; set; }
    public string Ambiente { get; set; } = string.Empty;
    public string FechaEmision { get; set; } = string.Empty;
    public string HoraEmision { get; set; } = string.Empty;

    public decimal SubTotalVentas { get; set; }
    public decimal MontoTotalOperacion { get; set; }
    public decimal TotalIva { get; set; }
    public decimal TotalPagar { get; set; }
    public string? TotalLetras { get; set; }

    public int TotalItems { get; set; }
    public int TotalDocumentosRelacionados { get; set; }

    public string EstadoHacienda { get; set; } = "PENDIENTE";
    public string? SelloRecibido { get; set; }
    public string? JsonEvento { get; set; }
    public string? JsonRespuesta { get; set; }
}
