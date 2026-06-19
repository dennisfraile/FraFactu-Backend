namespace FraFactu.Application.DTOs.Configuracion;

/// <summary>
/// DTO para configuración de envío automático de lotes
/// </summary>
public class ConfiguracionEnvioLoteDto
{
    public int Id { get; set; }
    public int EmisorId { get; set; }

    /// <summary>
    /// Hora de envío automático en formato HH:mm (ej: "17:00")
    /// </summary>
    public string HoraEnvioAutomatico { get; set; } = "17:00";

    public bool EnvioAutomaticoHabilitado { get; set; }
    public string ZonaHoraria { get; set; } = "America/El_Salvador";
    public bool EnviarRecordatorio { get; set; }
    public int MinutosAnticipacionRecordatorio { get; set; }
}

/// <summary>
/// DTO para actualizar configuración de envío
/// </summary>
public class ActualizarConfiguracionEnvioDto
{
    public string HoraEnvioAutomatico { get; set; } = null!;
    public bool EnvioAutomaticoHabilitado { get; set; }
    public bool EnviarRecordatorio { get; set; }
    public int MinutosAnticipacionRecordatorio { get; set; }
}
