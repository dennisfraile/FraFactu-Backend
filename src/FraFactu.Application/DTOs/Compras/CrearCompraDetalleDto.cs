namespace FraFactu.Application.DTOs.Compras;

/// <summary>
/// DTO para un detalle de compra (producto individual)
/// </summary>
public class CrearCompraDetalleDto
{
    /// <summary>
    /// ID del producto
    /// </summary>
    public int ProductoId { get; set; }

    /// <summary>
    /// ID de la bodega donde ingresa
    /// </summary>
    public int BodegaId { get; set; }

    /// <summary>
    /// Cantidad comprada
    /// </summary>
    public decimal Cantidad { get; set; }

    /// <summary>
    /// Costo unitario
    /// </summary>
    public decimal CostoUnitario { get; set; }

    /// <summary>
    /// Subtotal del ítem
    /// </summary>
    public decimal Subtotal { get; set; }

    /// <summary>
    /// IVA del ítem
    /// </summary>
    public decimal IVA { get; set; }

    /// <summary>
    /// Total del ítem
    /// </summary>
    public decimal Total { get; set; }

    /// <summary>
    /// Indica si entra al inventario (true) o es gasto administrativo (false)
    /// </summary>
    public bool EsParaInventario { get; set; } = true;
}
