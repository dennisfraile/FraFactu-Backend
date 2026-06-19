using FraFactu.Domain.Common;

namespace FraFactu.Domain.Entities;

/// <summary>
/// Ítem del cuerpo del documento de un Evento de Retorno (18).
/// </summary>
public class RetornoDetalle : BaseEntity
{
    public int EventoRetornoId { get; set; }
    public EventoRetorno EventoRetorno { get; set; } = null!;

    /// <summary>Número de ítem (1 a 2000).</summary>
    public int NumItem { get; set; }

    /// <summary>Tipo de ítem (CAT-011).</summary>
    public int TipoItem { get; set; }

    /// <summary>Código de generación del DTE relacionado.</summary>
    public string CodigoGeneracion { get; set; } = string.Empty;

    public decimal Cantidad { get; set; }
    public decimal PrecioUni { get; set; }

    /// <summary>Descripción del ítem (1 a 1500 caracteres).</summary>
    public string Descripcion { get; set; } = string.Empty;

    /// <summary>Código propio del ítem. Opcional.</summary>
    public string? Codigo { get; set; }

    /// <summary>Unidad de medida (CAT-014, 1-99).</summary>
    public int UniMedida { get; set; }

    public decimal MontoDescu { get; set; }

    /// <summary>Tributo sujeto a cálculo de IVA (2 chars). Opcional.</summary>
    public string? CodTributo { get; set; }

    public decimal VentaNoSuj { get; set; }
    public decimal VentaExenta { get; set; }
    public decimal VentaGravada { get; set; }

    /// <summary>Monto de compra (sujetos excluidos).</summary>
    public decimal Compra { get; set; }

    /// <summary>Códigos de tributo del ítem (CAT-015) serializados como JSON. Null si no lleva.</summary>
    public string? TributosJson { get; set; }

    public decimal Psv { get; set; }
    public decimal IvaItem { get; set; }
    public decimal NoGravado { get; set; }
    public decimal Seguro { get; set; }
    public decimal Flete { get; set; }
    public decimal IvaRete { get; set; }
    public decimal ReteRenta { get; set; }
}
