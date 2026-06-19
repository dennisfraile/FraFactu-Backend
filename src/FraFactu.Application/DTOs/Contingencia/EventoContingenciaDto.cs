namespace FraFactu.Application.DTOs.Contingencia;

/// <summary>
/// DTO principal del evento de contingencia
/// Estructura completa según schema JSON v3 para transmisión a MH
/// </summary>
/// </summary>
public class EventoContingenciaDto
{
    // ==========================================
    // METADATOS DE LA BASE DE DATOS (Para el frontend)
    // ==========================================

    /// <summary>
    /// ID del evento en la base de datos
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Estado de procesamiento en Hacienda (PENDIENTE, PROCESADO, etc.)
    /// </summary>
    public string EstadoHacienda { get; set; } = string.Empty;

    /// <summary>
    /// Sello de recibido de Hacienda (si fue procesado)
    /// </summary>
    public string? SelloRecibido { get; set; }

    /// <summary>
    /// JSON del evento (para mostrar en modal)
    /// </summary>
    public string? JsonEvento { get; set; }

    /// <summary>
    /// JSON de respuesta de MH
    /// </summary>
    public string? JsonRespuesta { get; set; }

    /// <summary>
    /// Total de DTEs incluidos en el evento
    /// </summary>
    public int TotalDtes { get; set; }

    /// <summary>
    /// Indica si el evento ya tiene un lote de contingencia asociado
    /// </summary>
    public bool TieneLote { get; set; }

    /// <summary>
    /// Cantidad de intentos fallidos de crear/enviar el lote automáticamente
    /// </summary>
    public int ReintentosLote { get; set; }

    // ==========================================
    // ESTRUCTURA DEL JSON PARA HACIENDA
    // ==========================================

    /// <summary>
    /// Sección de identificación del evento
    /// </summary>
    public IdentificacionContingenciaDto Identificacion { get; set; } = new();

    /// <summary>
    /// Sección de datos del emisor y responsable
    /// </summary>
    public EmisorContingenciaDto Emisor { get; set; } = new();

    /// <summary>
    /// Array de documentos incluidos en el evento (1 a 1000)
    /// </summary>
    public List<DetalleDocumentoDto> DetalleDTE { get; set; } = new();

    /// <summary>
    /// Sección de motivo de la contingencia
    /// </summary>
    public MotivoContingenciaDto Motivo { get; set; } = new();
}

