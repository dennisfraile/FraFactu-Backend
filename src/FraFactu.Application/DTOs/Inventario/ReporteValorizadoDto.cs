namespace FraFactu.Application.DTOs.Inventario;

/// <summary>
/// DTO para reporte valorizado de inventario
/// </summary>
public class ReporteValorizadoDto
{
    public DateTime FechaCorte { get; set; }
    public int? BodegaId { get; set; }
    public string? BodegaNombre { get; set; }

    public List<ItemReporteValorizado> Items { get; set; } = new();

    // Totalizadores
    public decimal TotalCantidad { get; set; }
    public decimal TotalValor { get; set; }
    public int TotalProductos { get; set; }
}

public class ItemReporteValorizado
{
    public int ProductoId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public string? Marca { get; set; }

    public decimal Cantidad { get; set; }
    public decimal CostoPromedio { get; set; }
    public decimal ValorTotal { get; set; }

    // Desglose por bodega (si es reporte global)
    public List<StockPorBodegaDto>? DetallesBodegas { get; set; }
}
