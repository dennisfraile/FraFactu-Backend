namespace FraFactu.Application.DTOs.Contingencia;

/// <summary>
/// DTO de request para marcar un DTE como diferido (contingencia manual, tipos 2-5)
/// </summary>
public class MarcarDteDiferidoRequestDto
{
    /// <summary>
    /// ID de la factura electrónica a marcar como diferida
    /// </summary>
    public int FacturaId { get; set; }

    /// <summary>
    /// Tipo de contingencia (2-5)
    /// 2=No disponibilidad sistema emisor
    /// 3=Falla Internet
    /// 4=Falla energía eléctrica
    /// 5=Otro
    /// </summary>
    public int TipoContingencia { get; set; }

    /// <summary>
    /// Fecha y hora de inicio de la contingencia (hora local El Salvador)
    /// </summary>
    public DateTime FechaInicioContingencia { get; set; }

    /// <summary>
    /// Motivo detallado (OBLIGATORIO para tipo 5)
    /// </summary>
    public string? MotivoDetallado { get; set; }
}
