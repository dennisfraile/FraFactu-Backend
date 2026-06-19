namespace FraFactu.Application.DTOs.Inventario;

/// <summary>
/// DTO para información de stock de un producto
/// </summary>
public class StockDto
{
    public int ProductoId { get; set; }
    public string ProductoCodigo { get; set; } = string.Empty;
    public string ProductoNombre { get; set; } = string.Empty;

    /// <summary>
    /// Stock por bodega
    /// </summary>
    public List<StockPorBodegaDto> StocksPorBodega { get; set; } = new();

    /// <summary>
    /// Stock total en todas las bodegas
    /// </summary>
    public decimal StockTotalDisponible { get; set; }
    public decimal StockTotalReservado { get; set; }
    public decimal StockTotalGeneral { get; set; }

    /// <summary>
    /// Valorización total
    /// </summary>
    public decimal CostoPromedioGlobal { get; set; }
    public decimal ValorInventarioTotal { get; set; }

    /// <summary>
    /// Configuración del producto
    /// </summary>
    public decimal StockMinimo { get; set; }
    public decimal PuntoReorden { get; set; }

    /// <summary>
    /// Estado de alerta
    /// </summary>
    public bool BajoStock => StockTotalDisponible <= PuntoReorden;
    public bool SinStock => StockTotalDisponible <= 0;
}

public class StockPorBodegaDto
{
    public int BodegaId { get; set; }
    public string BodegaNombre { get; set; } = string.Empty;
    public decimal CantidadDisponible { get; set; }
    public decimal CantidadReservada { get; set; }
    public decimal CantidadTotal { get; set; }
    public decimal CostoPromedio { get; set; }
    public decimal ValorInventario { get; set; }
}
