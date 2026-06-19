namespace FraFactu.Application.DTOs.Inventario;

/// <summary>
/// DTO para dashboard de inventario
/// </summary>
public class DashboardInventarioDto
{
    public decimal ValorTotalInventario { get; set; }
    public int TotalProductos { get; set; }
    public int ProductosActivos { get; set; }
    public int ProductosBajoStock { get; set; }
    public int ProductosSinStock { get; set; }

    // Top productos
    public List<ProductoTopVentas> TopVentas { get; set; } = new();
    public List<ProductoTopValor> TopValor { get; set; } = new();

    // Movimientos recientes
    public List<MovimientoReciente> UltimosMovimientos { get; set; } = new();

    // Alertas
    public List<ProductoBajoStockDto> ProductosEnAlerta { get; set; } = new();

    // Por bodega
    public List<ResumenPorBodega> ResumenBodegas { get; set; } = new();
}

public class ProductoTopVentas
{
    public int ProductoId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public decimal CantidadVendida { get; set; }
    public decimal ValorVendido { get; set; }
}

public class ProductoTopValor
{
    public int ProductoId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public decimal Valor { get; set; }
}

public class MovimientoReciente
{
    public DateTime Fecha { get; set; }
    public string TipoMovimiento { get; set; } = string.Empty;
    public string ProductoNombre { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public string BodegaNombre { get; set; } = string.Empty;
}

public class ResumenPorBodega
{
    public int BodegaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int TotalProductos { get; set; }
    public decimal ValorInventario { get; set; }
}
