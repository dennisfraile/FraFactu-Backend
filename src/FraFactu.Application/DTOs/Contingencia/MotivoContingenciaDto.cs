namespace FraFactu.Application.DTOs.Contingencia;

/// <summary>
/// DTO para el motivo de la contingencia
/// Corresponde a la sección "motivo" del schema JSON v3
/// </summary>
public class MotivoContingenciaDto
{
    /// <summary>
    /// Fecha de inicio de la contingencia (formato yyyy-MM-dd)
    /// </summary>
    public string FInicio { get; set; } = string.Empty;

    /// <summary>
    /// Fecha de finalización de la contingencia (formato yyyy-MM-dd)
    /// </summary>
    public string FFin { get; set; } = string.Empty;

    /// <summary>
    /// Hora de inicio de la contingencia (formato HH:mm:ss)
    /// </summary>
    public string HInicio { get; set; } = string.Empty;

    /// <summary>
    /// Hora de fin de la contingencia (formato HH:mm:ss)
    /// </summary>
    public string HFin { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de contingencia (CAT-005)
    /// 1=No disponibilidad sistema MH
    /// 2=No disponibilidad sistema emisor
    /// 3=Falla Internet
    /// 4=Falla energía eléctrica
    /// 5=Otro
    /// </summary>
    public int TipoContingencia { get; set; }

    /// <summary>
    /// Descripción del motivo de la contingencia
    /// OBLIGATORIO si TipoContingencia = 5, max 500 caracteres
    /// </summary>
    public string? MotivoContingencia { get; set; }
}
