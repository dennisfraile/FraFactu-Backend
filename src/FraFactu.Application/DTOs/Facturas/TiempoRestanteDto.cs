namespace FraFactu.Application.DTOs.Facturas;

/// <summary>
/// DTO con información del tiempo restante para envío de DTE
/// </summary>
public class TiempoRestanteDto
{
    public int FacturaId { get; set; }
    public string NumeroControl { get; set; } = null!;
    public DateTime FechaEmision { get; set; }
    public DateTime FechaLimiteEnvio { get; set; }

    /// <summary>
    /// Segundos restantes hasta el vencimiento
    /// </summary>
    public long SegundosRestantes { get; set; }

    /// <summary>
    /// Indica si la factura fue emitida el último día del mes (30min límite)
    /// </summary>
    public bool EsUltimoDiaMes { get; set; }

    /// <summary>
    /// Mensaje descriptivo para el usuario
    /// </summary>
    public string Mensaje { get; set; } = null!;

    /// <summary>
    /// true si quedan menos de 2 horas para el vencimiento
    /// </summary>
    public bool EstaPorVencer { get; set; }
}
