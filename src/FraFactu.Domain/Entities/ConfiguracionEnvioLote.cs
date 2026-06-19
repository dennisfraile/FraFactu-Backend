using FraFactu.Domain.Common;

namespace FraFactu.Domain.Entities;

/// <summary>
/// Configuración de envío automático de lotes por emisor
/// Permite programar la hora de envío diario de facturas pendientes
/// </summary>
public class ConfiguracionEnvioLote : BaseEntity
{
    /// <summary>
    /// FK al emisor propietario de la configuración
    /// </summary>
    public int EmisorId { get; set; }
    public Emisor Emisor { get; set; } = null!;

    /// <summary>
    /// Hora del día para envío automático (formato 24h)
    /// Ejemplo: 17:00 para enviar a las 5 PM
    /// </summary>
    public TimeSpan HoraEnvioAutomatico { get; set; } = new TimeSpan(17, 0, 0);

    /// <summary>
    /// Indica si el envío automático está habilitado
    /// </summary>
    public bool EnvioAutomaticoHabilitado { get; set; } = true;

    /// <summary>
    /// Zona horaria del emisor (ej: "America/El_Salvador")
    /// </summary>
    public string ZonaHoraria { get; set; } = "America/El_Salvador";

    /// <summary>
    /// Indica si se debe enviar recordatorio antes del vencimiento
    /// </summary>
    public bool EnviarRecordatorio { get; set; } = true;

    /// <summary>
    /// Minutos de anticipación para el recordatorio
    /// Ejemplo: 60 = recordar 1 hora antes
    /// </summary>
    public int MinutosAnticipacionRecordatorio { get; set; } = 60;
}
