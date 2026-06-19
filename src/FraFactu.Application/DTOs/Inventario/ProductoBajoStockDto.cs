namespace FraFactu.Application.DTOs.Inventario;

/// <summary>
/// DTO para productos con stock bajo el punto de reorden
/// </summary>
public class ProductoBajoStockDto
{
    public int ProductoId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string CategoriaNombre { get; set; } = string.Empty;

    public decimal StockActual { get; set; }
    public decimal StockMinimo { get; set; }
    public decimal PuntoReorden { get; set; }
    public decimal StockFaltante { get; set; }

    public int? BodegaId { get; set; }
    public string? BodegaNombre { get; set; }

    public decimal PrecioVenta { get; set; }
    public decimal ValorFaltante { get; set; }

    /// <summary>
    /// Nivel de urgencia: CRITICO, BAJO, REORDEN
    /// </summary>
    public string NivelUrgencia { get; set; } = string.Empty;
}
