using FraFactu.Domain.Common;

namespace FraFactu.Domain.Entities;

/// <summary>
/// Borrador temporal generado por una app externa (SmartCare) para pre-hidratar
/// el wizard de creación de factura en la UI de Smartix. Idempotente por
/// CorrelationId. Expira a la hora de creación + TTL configurable.
/// El usuario lo "consume" al emitir la factura desde la UI; ese consume
/// adjunta los metadatos SmartCare al row de Factura recién creado.
/// </summary>
public class FacturaPrefill : BaseEntity
{
    /// <summary>UUID generado por la app cliente (SmartCare). Único.</summary>
    public string CorrelationId { get; set; } = string.Empty;

    public int EmisorId { get; set; }
    public Emisor Emisor { get; set; } = null!;

    /// <summary>Sucursal sugerida por la app cliente. El usuario puede cambiarla en el wizard.</summary>
    public int SucursalSmartixId { get; set; }

    /// <summary>Tipo de DTE solicitado: "01" Factura CF, "03" CCF.</summary>
    public string TipoDte { get; set; } = "01";

    /// <summary>
    /// Snapshot JSON del receptor enviado por la app cliente (shape FromSmartCareReceptorDto).
    /// La UI lo deserializa y pre-popula DatosGeneralesFactura.
    /// </summary>
    public string ReceptorJson { get; set; } = "{}";

    /// <summary>
    /// Snapshot JSON de las líneas (shape List&lt;FromSmartCareInvoiceLineDto&gt;).
    /// La UI lo deserializa y pre-popula CuerpoDocumento.
    /// </summary>
    public string LineasJson { get; set; } = "[]";

    public string? Observaciones { get; set; }

    /// <summary>Forma de pago sugerida. El usuario puede cambiarla.</summary>
    public string? FormaPagoSugerida { get; set; }

    // ====================== Metadatos SmartCare ======================

    /// <summary>
    /// ID de la clínica en SmartCare (UUID string). Lo guardamos opaco porque
    /// SmartCare lo trata como UUID y no como entero.
    /// </summary>
    public string? SmartCareClinicId { get; set; }

    /// <summary>
    /// ID de la visita en SmartCare (UUID string). Mismo motivo que ClinicId.
    /// </summary>
    public string? SmartCareVisitId { get; set; }

    /// <summary>URL a la que Smartix manda el webhook cuando se emite el DTE.</summary>
    public string SmartCareWebhookUrl { get; set; } = string.Empty;

    // ====================== Ciclo de vida ======================

    public DateTime ExpiresAt { get; set; }

    /// <summary>NULL mientras esté pendiente. NOW cuando el usuario emite y el factura.</summary>
    public DateTime? ConsumedAt { get; set; }

    /// <summary>FacturaId que generó el consume.</summary>
    public int? ConsumedFacturaId { get; set; }
    public FacturaElectronica? ConsumedFactura { get; set; }

    /// <summary>
    /// Plan B Hub-as-Emisor — Fase 2 (Opcion Hibrida).
    /// Snapshot point-in-time del payload fiscal (`emisorFiscal + sucursalFiscal`)
    /// que SmartCare envia desde SmartHub al armar el prefill. Persiste como JSON
    /// para mantener el shape evolucionable y queryable en Postgres
    /// (`SnapshotFiscalJson->'emisor'->>'nit'`). Null cuando el cliente no envia
    /// payload fiscal (compat con SmartCare pre-Fase 2). En el consume (B.2) se
    /// copia a `FacturaElectronica.SnapshotFiscalJson` para auditoria del DTE.
    /// </summary>
    public string? SnapshotFiscalJson { get; set; }
}
