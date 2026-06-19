namespace FraFactu.Application.DTOs.DtesRecibidos;

/// <summary>
/// DTO para mostrar un DTE recibido completo
/// </summary>
public class DteRecibidoDto
{
    public int Id { get; set; }
    public string CodigoGeneracion { get; set; } = string.Empty;
    public string? SelloRecibido { get; set; }
    public string TipoDte { get; set; } = string.Empty;
    public string? NumeroControl { get; set; }
    public DateTime FechaEmision { get; set; }

    // Emisor del DTE (proveedor)
    public string EmisorNit { get; set; } = string.Empty;
    public string EmisorNombre { get; set; } = string.Empty;
    public string? EmisorNrc { get; set; }

    // Receptor del DTE (nosotros)
    public string? ReceptorNit { get; set; }
    public string? ReceptorNombre { get; set; }

    // JSON completo
    public string JsonDte { get; set; } = string.Empty;

    // Montos
    public decimal MontoGravado { get; set; }
    public decimal MontoExento { get; set; }
    public decimal MontoNoSujeto { get; set; }
    public decimal SubTotal { get; set; }
    public decimal IVA { get; set; }
    public decimal Total { get; set; }

    // Estado
    public string Estado { get; set; } = string.Empty;
    public int? CompraExternaId { get; set; }
    public string? MotivoDescarte { get; set; }

    // Correo
    public string? EmailOrigen { get; set; }
    public DateTime? FechaRecepcionEmail { get; set; }

    // Auditoría
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaActualizacion { get; set; }
}

/// <summary>
/// DTO resumido para listados
/// </summary>
public class DteRecibidoResumenDto
{
    public int Id { get; set; }
    public string CodigoGeneracion { get; set; } = string.Empty;
    public string TipoDte { get; set; } = string.Empty;
    public string? NumeroControl { get; set; }
    public DateTime FechaEmision { get; set; }
    public string EmisorNit { get; set; } = string.Empty;
    public string EmisorNombre { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string Estado { get; set; } = string.Empty;
    public int? CompraExternaId { get; set; }
    public DateTime FechaCreacion { get; set; }
}

/// <summary>
/// DTO para descartar un DTE
/// </summary>
public class DescartarDteDto
{
    public string Motivo { get; set; } = string.Empty;
}

/// <summary>
/// DTO para configuración de lectura de correo
/// </summary>
public class ConfiguracionLecturaCorreoDto
{
    public bool LecturaCorreoHabilitada { get; set; }
}

/// <summary>
/// DTO para estadísticas de DTEs recibidos
/// </summary>
public class DteRecibidoEstadisticasDto
{
    public int TotalPendientes { get; set; }
    public int TotalVinculados { get; set; }
    public int TotalDescartados { get; set; }
    public int Total { get; set; }
    public decimal MontoTotalPendientes { get; set; }
    public DateTime? UltimaLectura { get; set; }
}

/// <summary>
/// Resultado del parseo de un DTE
/// </summary>
public class DteRecibidoParsedDto
{
    public string CodigoGeneracion { get; set; } = string.Empty;
    public string? SelloRecibido { get; set; }
    public string TipoDte { get; set; } = string.Empty;
    public string? NumeroControl { get; set; }
    public DateTime FechaEmision { get; set; }

    public string EmisorNit { get; set; } = string.Empty;
    public string EmisorNombre { get; set; } = string.Empty;
    public string? EmisorNrc { get; set; }

    public string? ReceptorNit { get; set; }
    public string? ReceptorNombre { get; set; }

    public decimal MontoGravado { get; set; }
    public decimal MontoExento { get; set; }
    public decimal MontoNoSujeto { get; set; }
    public decimal SubTotal { get; set; }
    public decimal IVA { get; set; }
    public decimal Total { get; set; }

    public string JsonOriginal { get; set; } = string.Empty;

    /// <summary>
    /// Items del cuerpo del documento (para crear detalles de compra)
    /// </summary>
    public List<DteItemParsedDto> Items { get; set; } = new();
}

/// <summary>
/// Item parseado del cuerpo del documento DTE
/// </summary>
public class DteItemParsedDto
{
    public int NumItem { get; set; }
    public string? Codigo { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal MontoDescuento { get; set; }
    public decimal VentaGravada { get; set; }
    public decimal VentaExenta { get; set; }
    public decimal VentaNoSujeta { get; set; }
    public int? UnidadMedida { get; set; }
}

/// <summary>
/// Resultado de la lectura de correos
/// </summary>
public class ResultadoLecturaCorreoDto
{
    public int CorreosLeidos { get; set; }
    public int DtesEncontrados { get; set; }
    public int DtesNuevos { get; set; }
    public int DtesDuplicados { get; set; }
    public int Errores { get; set; }
    public List<string> Mensajes { get; set; } = new();
}

/// <summary>
/// Archivo individual a ingestar en la carga manual. Se desacopla de
/// <c>IFormFile</c> para que la lógica viva en Application sin depender
/// de ASP.NET Core.
/// </summary>
public class ArchivoAIngestar
{
    public string NombreArchivo { get; set; } = string.Empty;
    public string Contenido { get; set; } = string.Empty;
}

/// <summary>
/// Resultado por archivo en una carga masiva manual de DTEs.
/// </summary>
public class CargaArchivoResultadoDto
{
    public string Archivo { get; set; } = string.Empty;

    /// <summary>
    /// Valores: "Cargado", "Duplicado", "NoEsCCF", "ReceptorInvalido",
    /// "ContenidoInvalido", "ErrorLectura".
    /// </summary>
    public string Resultado { get; set; } = string.Empty;

    public string? CodigoGeneracion { get; set; }
    public string Mensaje { get; set; } = string.Empty;
}

/// <summary>
/// Respuesta de una carga masiva manual: agregados + detalle por archivo.
/// </summary>
public class CargaMasivaDtesResponseDto
{
    public int TotalArchivos { get; set; }
    public int Cargados { get; set; }
    public int Duplicados { get; set; }
    public int Rechazados { get; set; }
    public List<CargaArchivoResultadoDto> Resultados { get; set; } = new();
}

/// <summary>
/// Request opcional para encolar una lectura manual de correo. Si se omite,
/// se lee el mes en curso. Formato de los meses: <c>YYYY-MM</c>.
/// </summary>
public class EncolarLecturaCorreoRequestDto
{
    /// <summary>Mes desde el cual leer, inclusivo. Formato <c>YYYY-MM</c>.</summary>
    public string? MesInicio { get; set; }

    /// <summary>Mes hasta el cual leer, inclusivo. Formato <c>YYYY-MM</c>.</summary>
    public string? MesFin { get; set; }
}

/// <summary>
/// Respuesta al encolar una solicitud de lectura de correo asíncrona.
/// El cliente debe hacer polling al endpoint de estado con el <c>JobId</c>
/// devuelto.
/// </summary>
public class EncolarLecturaCorreoResponseDto
{
    public int JobId { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
}

/// <summary>
/// Estado actual de un job de lectura de correo (para polling desde la UI).
/// </summary>
public class EstadoLecturaCorreoJobDto
{
    public int JobId { get; set; }
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public int CorreosProcesados { get; set; }
    public int DtesEncontrados { get; set; }
    public int DtesNuevos { get; set; }
    public int DtesDuplicados { get; set; }
    public int Errores { get; set; }
    public string? MensajeError { get; set; }

    /// <summary>
    /// F4: adjuntos descartados por no ser CCF, receptor invalido o contenido
    /// no parseable. No son errores tecnicos; son informativos para la UI.
    /// </summary>
    public int DtesIgnorados { get; set; }

    /// <summary>Rango de fechas leído por el job (inicio inclusivo, UTC).</summary>
    public DateTime? RangoDesde { get; set; }

    /// <summary>Rango de fechas leído por el job (fin exclusivo, UTC).</summary>
    public DateTime? RangoHasta { get; set; }

    /// <summary><c>true</c> si fue disparado por el scheduler en background.</summary>
    public bool EsAutomatico { get; set; }

    /// <summary><c>true</c> si Estado es COMPLETADO o FALLIDO.</summary>
    public bool Terminado { get; set; }
}
