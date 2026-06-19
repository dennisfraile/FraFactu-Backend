using FraFactu.Domain.Enums;

namespace FraFactu.Application.Interfaces;

/// <summary>
/// Clasificación del resultado de intentar ingestar un contenido como DTE recibido.
/// </summary>
public enum ResultadoIngestaDte
{
    /// <summary>Se validó y persistió un nuevo DTE recibido.</summary>
    Cargado,

    /// <summary>El DTE ya existía para este emisor (mismo código de generación).</summary>
    Duplicado,

    /// <summary>El contenido es un DTE válido pero no es un Comprobante de Crédito Fiscal (tipo "03").</summary>
    NoEsCCF,

    /// <summary>El receptor del DTE no coincide con el NIT del emisor (no es un documento dirigido a él).</summary>
    ReceptorInvalido,

    /// <summary>El contenido no es un DTE válido (no es JSON/JWT parseable o le faltan secciones).</summary>
    ContenidoInvalido
}

/// <summary>
/// Resultado tipado de una operación de ingesta. Pensado para reportes por
/// archivo (carga masiva) y para los contadores de la lectura de correo.
/// </summary>
public class IngestaDteResultado
{
    public ResultadoIngestaDte Resultado { get; set; }
    public string? CodigoGeneracion { get; set; }
    public int? DteRecibidoId { get; set; }
    public string Mensaje { get; set; } = string.Empty;

    public bool EsExito => Resultado == ResultadoIngestaDte.Cargado;

    public static IngestaDteResultado De(ResultadoIngestaDte resultado, string mensaje, string? codigoGeneracion = null, int? dteId = null)
        => new() { Resultado = resultado, Mensaje = mensaje, CodigoGeneracion = codigoGeneracion, DteRecibidoId = dteId };
}

/// <summary>
/// Metadatos de origen de la ingesta. Independientes de la fuente (correo o
/// carga manual) para que ambas reutilicen el mismo pipeline.
/// </summary>
public class IngestaContexto
{
    public int EmisorId { get; set; }
    public string EmisorNit { get; set; } = string.Empty;
    public FuenteRecepcionDte Fuente { get; set; }
    public int? CargadoPorUsuarioId { get; set; }
    public string? EmailOrigen { get; set; }
    public DateTime? FechaRecepcionEmail { get; set; }
}

/// <summary>
/// Pipeline único de ingesta de DTEs recibidos, agnóstico de la fuente.
/// Valida (es DTE → es CCF "03" → receptor == emisor), deduplica de forma
/// idempotente y persiste como PENDIENTE dentro de una operación atómica.
/// Tanto la lectura de correo como la carga manual lo consumen.
/// </summary>
public interface IDteIngestaService
{
    Task<IngestaDteResultado> IngestarAsync(string contenidoCrudo, IngestaContexto contexto, CancellationToken ct = default);
}
