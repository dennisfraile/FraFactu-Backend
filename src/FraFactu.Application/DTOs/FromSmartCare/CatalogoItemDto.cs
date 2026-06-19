namespace FraFactu.Application.DTOs.FromSmartCare;

/// <summary>
/// Representa un producto/servicio del catálogo del emisor para que SmartCare
/// construya el selector "Productos adicionales" en BillingStep.
/// </summary>
public class CatalogoItemDto
{
    public int ProductoId { get; set; }
    public required string Codigo { get; set; }
    public required string Descripcion { get; set; }
    public decimal PrecioUnitario { get; set; }
    public bool PrecioIncluyeIva { get; set; }
    public string? UnidadMedida { get; set; }
    public bool EsServicio { get; set; }

    /// <summary>
    /// Suma del stock disponible (<c>StockBodega.CantidadDisponible</c>)
    /// en todas las bodegas asociadas a la sucursal que ejecutó el lookup.
    /// Null para servicios (no aplica gestión de stock).
    /// </summary>
    public decimal? StockDisponible { get; set; }

    /// <summary>
    /// Régimen de impuesto del producto: 1=Gravado, 2=Exento, 3=NoSujeto.
    /// Espejo del campo TBL_ProductosServicios.TipoImpuesto (feature
    /// 2026-06-09 TipoImpuesto+IVA SI→Smartix). SmartCare lo propaga por
    /// línea al armar el prefill request para que el DTE salga con la
    /// distribución correcta (VentaGravada / VentaExenta / VentaNoSujeta).
    /// </summary>
    public int TipoImpuesto { get; set; } = 1;

    /// <summary>
    /// Porcentaje IVA cuando <see cref="TipoImpuesto"/>=1 (Gravado). Null
    /// para Exento/NoSujeto. Permite tasas distintas a 13% en el futuro
    /// (parametrización por país / régimen). Default null para que el FE
    /// caiga al 13% cuando viene Gravado sin valor explícito.
    /// </summary>
    public decimal? PorcentajeIVA { get; set; }
}

public class CatalogoBusquedaResponseDto
{
    public required List<CatalogoItemDto> Items { get; set; }
    public int Total { get; set; }
}
