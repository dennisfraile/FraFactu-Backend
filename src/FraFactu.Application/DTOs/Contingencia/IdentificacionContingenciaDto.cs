namespace FraFactu.Application.DTOs.Contingencia;

/// <summary>
/// DTO para la sección de identificación del evento de contingencia
/// Corresponde a la sección "identificacion" del schema JSON v3
/// </summary>
public class IdentificacionContingenciaDto
{
    /// <summary>
    /// Versión del esquema (siempre 3)
    /// </summary>
    public int Version { get; set; } = 3;

    /// <summary>
    /// Ambiente: "00" = Pruebas, "01" = Producción
    /// </summary>
    public string Ambiente { get; set; } = string.Empty;

    /// <summary>
    /// Código de generación único (UUID v4)
    /// Formato: XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX
    /// </summary>
    public string CodigoGeneracion { get; set; } = string.Empty;

    /// <summary>
    /// Fecha de transmisión (formato yyyy-MM-dd)
    /// </summary>
    public string FTransmision { get; set; } = string.Empty;

    /// <summary>
    /// Hora de transmisión (formato HH:mm:ss)
    /// </summary>
    public string HTransmision { get; set; } = string.Empty;
}
