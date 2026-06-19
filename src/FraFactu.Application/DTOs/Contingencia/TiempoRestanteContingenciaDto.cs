namespace FraFactu.Application.DTOs.Contingencia;

/// <summary>
/// DTO con información del tiempo restante del plazo de 24 horas para contingencia
/// </summary>
public class TiempoRestanteContingenciaDto
{
    public DateTime FechaInicioContingencia { get; set; }
    public DateTime FechaLimite { get; set; }
    public double HorasRestantes { get; set; }
    public double MinutosRestantes { get; set; }
    public bool Vencido { get; set; }
}
