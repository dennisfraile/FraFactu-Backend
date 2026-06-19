namespace FraFactu.Application.DTOs.OperacionesEspeciales;

/// <summary>
/// Resultado de crear/consultar un Evento de Operaciones Especiales (17).
/// </summary>
public class EventoOperacionEspecialResultDto
{
    public int Id { get; set; }
    public string CodigoGeneracion { get; set; } = string.Empty;
    public int Version { get; set; }
    public string Ambiente { get; set; } = string.Empty;
    public string FechaEmision { get; set; } = string.Empty;
    public string HoraEmision { get; set; } = string.Empty;

    public decimal TotalNoSuj { get; set; }
    public decimal TotalExenta { get; set; }
    public decimal TotalGravada { get; set; }
    public decimal SubTotal { get; set; }
    public decimal Total { get; set; }
    public string? TotalLetras { get; set; }

    public int TotalItems { get; set; }

    public string EstadoHacienda { get; set; } = "PENDIENTE";
    public string? SelloRecibido { get; set; }
    public string? JsonEvento { get; set; }
    public string? JsonRespuesta { get; set; }
}
