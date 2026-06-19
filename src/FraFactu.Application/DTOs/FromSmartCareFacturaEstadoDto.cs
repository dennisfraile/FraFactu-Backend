namespace FraFactu.Application.DTOs;

/// <summary>
/// Estado minimo de una Factura para el pull-mode polling de SmartCare cuando
/// no tiene el smartix_invoice_id (por ejemplo si el webhook se perdio). Lo
/// devuelve <c>GET /api/from-smartcare/facturas/by-correlation/{correlationId}</c>
/// usando el correlationId que SmartCare conoce desde la creacion del prefill.
/// </summary>
public class FromSmartCareFacturaEstadoDto
{
    /// <summary>Id de Factura en Smartix. SmartCare lo persiste en
    /// <c>visits.smartix_invoice_id</c> para futuros lookups directos.</summary>
    public int Id { get; set; }

    /// <summary>CorrelationId que matcheo el lookup.</summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>Estado en Hacienda (PROCESADO, RECHAZADO, etc.) — SmartCare lo
    /// mapea a sus propios estados internos via mapEstadoSmartixToSmartCare.</summary>
    public string EstadoHacienda { get; set; } = string.Empty;

    /// <summary>Numero de control del DTE emitido (null si todavia no se emite).</summary>
    public string? NumeroControl { get; set; }

    /// <summary>Codigo de generacion (GUID) del DTE.</summary>
    public string? CodigoGeneracion { get; set; }

    /// <summary>Observaciones de rechazo si Hacienda lo rechazo.</summary>
    public string? ObservacionesRechazo { get; set; }

    /// <summary>Fecha de emision MH (null si no emitida).</summary>
    public DateTime? FechaEmision { get; set; }

    /// <summary>Ultima actualizacion del row.</summary>
    public DateTime? FechaActualizacion { get; set; }
}
