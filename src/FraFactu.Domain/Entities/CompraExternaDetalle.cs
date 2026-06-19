using FraFactu.Domain.Common;

namespace FraFactu.Domain.Entities;

/// <summary>
/// Detalle de una compra externa (producto individual comprado)
/// </summary>
public class CompraExternaDetalle : BaseEntity
{
    // ==========================================
    // RELACIONES
    // ==========================================

    /// <summary>
    /// FK a la compra externa
    /// </summary>
    public int CompraExternaId { get; set; }
    public CompraExterna CompraExterna { get; set; } = null!;

    /// <summary>
    /// FK al producto
    /// </summary>
    public int ProductoId { get; set; }
    public ProductoServicio Producto { get; set; } = null!;

    /// <summary>
    /// FK a la bodega donde ingresa el producto
    /// </summary>
    public int BodegaId { get; set; }
    public Bodega Bodega { get; set; } = null!;

    // ==========================================
    // CANTIDADES Y COSTOS
    // ==========================================

    /// <summary>
    /// Cantidad comprada
    /// </summary>
    public decimal Cantidad { get; set; }

    /// <summary>
    /// Costo unitario del producto
    /// </summary>
    public decimal CostoUnitario { get; set; }

    /// <summary>
    /// Subtotal del ítem (sin IVA)
    /// </summary>
    public decimal Subtotal { get; set; }

    /// <summary>
    /// IVA del ítem
    /// </summary>
    public decimal IVA { get; set; }

    /// <summary>
    /// Total del ítem (Subtotal + IVA)
    /// </summary>
    public decimal Total { get; set; }

    // ==========================================
    // CLASIFICACIÓN
    // ==========================================

    /// <summary>
    /// Indica si el producto entra al inventario o es un gasto administrativo
    /// true = Afecta inventario
    /// false = Es gasto administrativo (no afecta stock)
    /// </summary>
    public bool EsParaInventario { get; set; } = true;
}
