using FraFactu.Domain.Common;
using FraFactu.Domain.Entities.Catalogos;

namespace FraFactu.Domain.Entities;

/// <summary>
/// Producto del inventario
/// </summary>
public class Producto : BaseEntity
{
    // ==========================================
    // INFORMACIÓN BÁSICA
    // ==========================================

    /// <summary>
    /// Código interno único del producto
    /// </summary>
    public string Codigo { get; set; } = string.Empty;

    /// <summary>
    /// Código de barras (opcional)
    /// </summary>
    public string? CodigoBarras { get; set; }

    /// <summary>
    /// Nombre del producto
    /// </summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Descripción detallada
    /// </summary>
    public string? Descripcion { get; set; }

    // ==========================================
    // CLASIFICACIÓN
    // ==========================================

    /// <summary>
    /// FK a Categoría
    /// </summary>
    public int CategoriaId { get; set; }
    public Categoria Categoria { get; set; } = null!;

    /// <summary>
    /// FK a Marca (opcional)
    /// </summary>
    public int? MarcaId { get; set; }
    public Marca? Marca { get; set; }

    /// <summary>
    /// FK a Unidad de Medida del catálogo MH
    /// </summary>
    public int CatUnidadMedidaId { get; set; }
    public CatUnidadMedida UnidadMedida { get; set; } = null!;

    // ==========================================
    // CONTROL DE INVENTARIO
    // ==========================================

    /// <summary>
    /// Stock mínimo que debe mantenerse
    /// </summary>
    public decimal StockMinimo { get; set; }

    /// <summary>
    /// Stock máximo permitido
    /// </summary>
    public decimal StockMaximo { get; set; }

    /// <summary>
    /// Punto de reorden (genera alerta)
    /// </summary>
    public decimal PuntoReorden { get; set; }

    // ==========================================
    // PRECIOS Y COSTOS
    // ==========================================

    /// <summary>
    /// Precio de costo del producto
    /// </summary>
    public decimal PrecioCosto { get; set; }

    /// <summary>
    /// Precio de venta al público
    /// </summary>
    public decimal PrecioVenta { get; set; }

    /// <summary>
    /// Margen de utilidad calculado
    /// </summary>
    public decimal Margen => PrecioCosto > 0
        ? ((PrecioVenta - PrecioCosto) / PrecioCosto) * 100
        : 0;

    // ==========================================
    // IMPUESTOS
    // ==========================================

    /// <summary>
    /// Indica si el producto aplica IVA
    /// </summary>
    public bool AplicaIVA { get; set; } = true;

    /// <summary>
    /// Porcentaje de IVA aplicable (13% en El Salvador)
    /// </summary>
    public decimal PorcentajeIVA { get; set; } = 13;

    // ==========================================
    // CONFIGURACIÓN
    // ==========================================

    /// <summary>
    /// Indica si el producto está activo
    /// </summary>
    public new bool Activo { get; set; } = true;

    /// <summary>
    /// Permite ventas sin stock disponible
    /// </summary>
    public bool PermiteVentaSinStock { get; set; } = false;

    /// <summary>
    /// Producto es un servicio (no maneja stock físico)
    /// </summary>
    public bool EsServicio { get; set; } = false;

    // ==========================================
    // NAVEGACIÓN
    // ==========================================

    /// <summary>
    /// Stock del producto en cada bodega
    /// </summary>
    public ICollection<StockBodega> Stocks { get; set; } = new List<StockBodega>();

    /// <summary>
    /// Movimientos de inventario del producto
    /// </summary>
    public ICollection<MovimientoInventario> Movimientos { get; set; } = new List<MovimientoInventario>();
}
