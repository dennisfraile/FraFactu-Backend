using FraFactu.Domain.Common;

namespace FraFactu.Domain.Entities;

/// <summary>
/// Representa un lote de documentos tributarios electrónicos (DTE)
/// enviados al Ministerio de Hacienda para procesamiento masivo
/// </summary>
public class Lote : BaseEntity
{
    /// <summary>
    /// FK al emisor propietario del lote
    /// </summary>
    public int EmisorId { get; set; }
    public Emisor Emisor { get; set; } = null!;

    /// <summary>
    /// Código único del lote generado localmente (GUID)
    /// </summary>
    public Guid CodigoLote { get; set; }

    /// <summary>
    /// Total de DTEs incluidos en el lote (máximo 100)
    /// </summary>
    public int TotalDtes { get; set; }

    /// <summary>
    /// Ambiente: "00" = Pruebas, "01" = Producción
    /// </summary>
    public string Ambiente { get; set; } = "00";

    /// <summary>
    /// Indica si el lote es para documentos de contingencia
    /// (permite envío 24/7 sin restricción de horario)
    /// </summary>
    public bool EsContingencia { get; set; }

    /// <summary>
    /// FK al evento de contingencia (OBLIGATORIO si EsContingencia = true)
    /// Vincula este lote al evento que generó los documentos
    /// </summary>
    public int? EventoContingenciaId { get; set; }
    public EventoContingencia? EventoContingencia { get; set; }

    /// <summary>
    /// Estado actual del lote: Pendiente, Enviando, Procesado, Error
    /// </summary>
    public string Estado { get; set; } = "Pendiente";

    /// <summary>
    /// Fecha y hora en que el lote fue enviado a MH
    /// </summary>
    public DateTime? FechaEnvio { get; set; }

    // ==========================================
    // RESPUESTA DEL MINISTERIO DE HACIENDA
    // ==========================================

    /// <summary>
    /// ID de envío asignado por MH
    /// </summary>
    public Guid? IdEnvio { get; set; }

    /// <summary>
    /// Fecha y hora de procesamiento reportada por MH
    /// </summary>
    public DateTime? FhProcesamiento { get; set; }

    /// <summary>
    /// Código de respuesta de MH: "001" = éxito
    /// </summary>
    public string? CodigoRespuesta { get; set; }

    /// <summary>
    /// Descripción de la respuesta de MH
    /// Ej: "LOTE RECIBIDO, VALIDADO Y PROCESADO"
    /// </summary>
    public string? DescripcionRespuesta { get; set; }

    /// <summary>
    /// JSON completo de la respuesta de MH (para auditoría)
    /// </summary>
    public string? JsonRespuesta { get; set; }

    /// <summary>
    /// JSON del lote enviado (para debugging/reenvío)
    /// </summary>
    public string? JsonEnviado { get; set; }

    // ==========================================
    // ESTADÍSTICAS (actualizadas después de consultas individuales)
    // ==========================================

    /// <summary>
    /// Cantidad de DTEs que fueron aprobados por MH
    /// </summary>
    public int TotalAprobados { get; set; }

    /// <summary>
    /// Cantidad de DTEs que fueron rechazados por MH
    /// </summary>
    public int TotalRechazados { get; set; }

    /// <summary>
    /// Cantidad de DTEs pendientes de consulta
    /// </summary>
    public int TotalPendientes { get; set; }

    // ==========================================
    // METADATA
    // ==========================================

    /// <summary>
    /// Usuario que creó el lote
    /// </summary>
    public string? CreadoPor { get; set; }

    /// <summary>
    /// Fecha de última modificación
    /// </summary>
    public DateTime? FechaModificacion { get; set; }

    /// <summary>
    /// Fecha de última consulta de estados individuales
    /// </summary>
    public DateTime? FechaUltimaConsulta { get; set; }

    // ==========================================
    // NAVEGACIÓN
    // ==========================================

    /// <summary>
    /// Detalles del lote (DTEs incluidos)
    /// </summary>
    public ICollection<LoteDetalle> Detalles { get; set; } = new List<LoteDetalle>();
}
