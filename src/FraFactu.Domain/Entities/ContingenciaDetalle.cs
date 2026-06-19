using FraFactu.Domain.Common;
using FraFactu.Domain.Entities.Catalogos;

namespace FraFactu.Domain.Entities;

/// <summary>
/// Detalle de un documento incluido en un evento de contingencia
/// Relación many-to-many entre EventoContingencia y FacturaElectronica
/// </summary>
public class ContingenciaDetalle : BaseEntity
{
    /// <summary>
    /// FK al evento de contingencia
    /// </summary>
    public int EventoContingenciaId { get; set; }
    public EventoContingencia EventoContingencia { get; set; } = null!;

    /// <summary>
    /// FK a la factura electrónica incluida en contingencia
    /// </summary>
    public int FacturaElectronicaId { get; set; }
    public FacturaElectronica FacturaElectronica { get; set; } = null!;

    /// <summary>
    /// Número correlativo del item (1 a 1000)
    /// Único dentro del evento
    /// </summary>
    public int NoItem { get; set; }

    /// <summary>
    /// Código de generación (UUID) del DTE
    /// Copiado de la factura para referencia rápida
    /// </summary>
    public string CodigoGeneracion { get; set; } = string.Empty;

    /// <summary>
    /// FK al catálogo de tipo de documento (CAT-004)
    /// Copiado de la factura para referencia rápida en JSON
    /// </summary>
    public int CatTipoDocumentoId { get; set; }
    public CatTipoDocumento TipoDocumento { get; set; } = null!;
}
