using FraFactu.Domain.Common;
using FraFactu.Domain.Entities.Catalogos;

namespace FraFactu.Domain.Entities;

/// <summary>
/// Evento de Contingencia - Reporte de DTEs generados durante situación de fuerza mayor
/// según Manual Funcional del Sistema de Transmisión V1.2 - Ministerio de Hacienda
/// </summary>
public class EventoContingencia : BaseEntity
{
    // ==========================================
    // SECCIÓN 1: IDENTIFICACIÓN DEL EVENTO
    // ==========================================

    /// <summary>
    /// Versión del esquema JSON (4 según schema V2.0; eventos históricos conservan su versión persistida)
    /// </summary>
    public int Version { get; set; } = 4;

    /// <summary>
    /// Ambiente: 00 = Pruebas, 01 = Producción
    /// </summary>
    public string Ambiente { get; set; } = "00";

    /// <summary>
    /// Código de generación único (UUID v4) del evento
    /// </summary>
    public string CodigoGeneracion { get; set; } = string.Empty;

    /// <summary>
    /// Fecha de transmisión del evento a MH
    /// </summary>
    public DateTime FechaTransmision { get; set; }

    /// <summary>
    /// Hora de transmisión del evento a MH (HH:mm:ss)
    /// </summary>
    public TimeSpan HoraTransmision { get; set; }

    // ==========================================
    // SECCIÓN 2: EMISOR Y RESPONSABLE
    // ==========================================

    /// <summary>
    /// FK al Emisor
    /// </summary>
    public int EmisorId { get; set; }
    public Emisor Emisor { get; set; } = null!;

    /// <summary>
    /// Nombre del responsable del establecimiento (OBLIGATORIO)
    /// Min: 5, Max: 100 caracteres
    /// </summary>
    public string NombreResponsable { get; set; } = string.Empty;

    /// <summary>
    /// FK al catálogo de tipo de documento del responsable (CAT-022: Tipo de documento de identificación)
    /// 36=NIT, 13=DUI, 02=Carnet residente, 03=Pasaporte, 37=Otro
    /// </summary>
    public int CatTipoDocResponsableId { get; set; }
    public CatTipoDocumentoIdentificacionReceptor TipoDocResponsable { get; set; } = null!;

    /// <summary>
    /// Número de documento de identificación del responsable
    /// Min: 5, Max: 25 caracteres
    /// </summary>
    public string NumeroDocResponsable { get; set; } = string.Empty;

    /// <summary>
    /// FK al catálogo de tipo de establecimiento (01, 02, 04, 07, 20)
    /// </summary>
    public int CatTipoEstablecimientoId { get; set; }
    public CatTipoEstablecimiento TipoEstablecimiento { get; set; } = null!;

    /// <summary>
    /// Código de establecimiento otorgado por MH (4 caracteres, opcional)
    /// </summary>
    public string? CodigoEstablecimientoMH { get; set; }

    /// <summary>
    /// Código de punto de venta del contribuyente (1-15 caracteres, opcional)
    /// </summary>
    public string? CodigoPuntoVenta { get; set; }

    // ==========================================
    // SECCIÓN 3: MOTIVO DE LA CONTINGENCIA
    // ==========================================

    /// <summary>
    /// Fecha de inicio de la contingencia
    /// </summary>
    public DateTime FechaInicioContingencia { get; set; }

    /// <summary>
    /// Fecha de finalización de la contingencia
    /// </summary>
    public DateTime FechaFinContingencia { get; set; }

    /// <summary>
    /// Hora de inicio de la contingencia (HH:mm:ss)
    /// </summary>
    public TimeSpan HoraInicioContingencia { get; set; }

    /// <summary>
    /// Hora de fin de la contingencia (HH:mm:ss)
    /// </summary>
    public TimeSpan HoraFinContingencia { get; set; }

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
    /// Motivo de la contingencia (max 500 caracteres)
    /// OBLIGATORIO si TipoContingencia = 5
    /// </summary>
    public string? MotivoContingencia { get; set; }

    // ==========================================
    // SECCIÓN 4: RESPUESTA DE HACIENDA
    // ==========================================

    /// <summary>
    /// Fecha y hora de transmisión real a MH
    /// </summary>
    public DateTime? FechaTransmisionMH { get; set; }

    /// <summary>
    /// Sello de recepción otorgado por MH
    /// </summary>
    public string? SelloRecibido { get; set; }

    /// <summary>
    /// Estado del evento en Hacienda (PROCESADO, RECHAZADO, etc.)
    /// </summary>
    public string? EstadoHacienda { get; set; }

    /// <summary>
    /// Fecha en que MH rechazó el evento (si aplica)
    /// </summary>
    public DateTime? FechaRechazoMH { get; set; }

    /// <summary>
    /// JSON del evento generado para transmisión
    /// </summary>
    public string? JsonEvento { get; set; }

    /// <summary>
    /// JSON de respuesta de MH
    /// </summary>
    public string? JsonRespuesta { get; set; }

    // ==========================================
    // SECCIÓN 5: AUTOMATIZACIÓN
    // ==========================================
    public bool CreadoAutomaticamente { get; set; } = false;

    /// <summary>
    /// Cantidad de intentos fallidos de crear/enviar el lote automáticamente (máx 3)
    /// </summary>
    public int ReintentosLote { get; set; } = 0;

    // ==========================================
    // RELACIONES
    // ==========================================

    /// <summary>
    /// Detalle de documentos incluidos en este evento (1 a 1000)
    /// </summary>
    public ICollection<ContingenciaDetalle> Detalles { get; set; } = new List<ContingenciaDetalle>();
}
