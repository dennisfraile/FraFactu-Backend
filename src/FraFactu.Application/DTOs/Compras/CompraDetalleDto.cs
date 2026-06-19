namespace FraFactu.Application.DTOs.Compras;

/// <summary>
/// DTO para mostrar un detalle de compra
/// </summary>
public class CompraDetalleDto
{
    public int Id { get; set; }
    public int CompraExternaId { get; set; }

    public int ProductoId { get; set; }
    public string ProductoCodigo { get; set; } = string.Empty;
    public string ProductoNombre { get; set; } = string.Empty;

    public int BodegaId { get; set; }
    public string BodegaNombre { get; set; } = string.Empty;

    public decimal Cantidad { get; set; }
    public decimal CostoUnitario { get; set; }
    public decimal Subtotal { get; set; }
    public decimal IVA { get; set; }
    public decimal Total { get; set; }

    public bool EsParaInventario { get; set; }
}
