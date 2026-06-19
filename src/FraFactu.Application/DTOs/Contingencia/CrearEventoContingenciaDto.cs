namespace FraFactu.Application.DTOs.Contingencia;

/// <summary>
/// DTO de request para crear un evento de contingencia desde el API
/// Versión simplificada para el usuario (no incluye todos los campos del JSON)
/// </summary>
public class CrearEventoContingenciaDto
{
    /// <summary>
    /// Fecha de inicio de la contingencia
    /// </summary>
    public DateTime FechaInicioContingencia { get; set; }

    /// <summary>
    /// Fecha de finalización de la contingencia
    /// </summary>
    public DateTime FechaFinContingencia { get; set; }

    /// <summary>
    /// Hora de inicio de la contingencia
    /// </summary>
    public TimeSpan HoraInicioContingencia { get; set; }

    /// <summary>
    /// Hora de fin de la contingencia
    /// </summary>
    public TimeSpan HoraFinContingencia { get; set; }

    /// <summary>
    /// Tipo de contingencia (1-5)
    /// 1=No disponibilidad sistema MH
    /// 2=No disponibilidad sistema emisor
    /// 3=Falla Internet
    /// 4=Falla energía eléctrica
    /// 5=Otro
    /// </summary>
    public int TipoContingencia { get; set; }

    /// <summary>
    /// Motivo de la contingencia (OBLIGATORIO si TipoContingencia = 5)
    /// Max: 500 caracteres
    /// </summary>
    public string? MotivoContingencia { get; set; }

    /// <summary>
    /// Nombre del responsable del establecimiento
    /// Min: 5, Max: 100
    /// </summary>
    public string NombreResponsable { get; set; } = string.Empty;

    /// <summary>
    /// FK al catálogo de tipo de documento del responsable (CAT-22)
    /// 1=NIT, 2=DUI, 3=Carnet, 4=Pasaporte, 5=Otro
    /// </summary>
    public int CatTipoDocResponsableId { get; set; } = 1; // Default: NIT

    /// <summary>
    /// Número de documento del responsable
    /// Min: 5, Max: 25
    /// </summary>
    public string NumeroDocResponsable { get; set; } = string.Empty;

    /// <summary>
    /// Lista de IDs de facturas electrónicas a incluir en el evento
    /// Min: 1, Max: 1000
    /// </summary>
    public List<int> FacturaIds { get; set; } = new();
}
