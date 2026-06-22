using FraFactu.Application.Common;

namespace FraFactu.Application.DTOs.Reportes;

/// <summary>
/// Stock de un producto en una bodega
/// </summary>
public class StockPorBodegaDto
{
    public int ProductoId { get; set; }
    public string ProductoCodigo { get; set; } = string.Empty;
    public string ProductoNombre { get; set; } = string.Empty;
    public int BodegaId { get; set; }
    public string BodegaNombre { get; set; } = string.Empty;
    public string SucursalNombre { get; set; } = string.Empty;
    public decimal CantidadDisponible { get; set; }
    public decimal CantidadReservada { get; set; }
    public decimal CantidadTotal { get; set; }
    public decimal CostoPromedio { get; set; }
    public decimal ValorStock { get; set; }
    public DateTime? FechaUltimoMovimiento { get; set; }

    // Configuración de alertas
    public decimal? StockMinimo { get; set; }
    public decimal? StockMaximo { get; set; }
}

/// <summary>
/// Producto con stock bajo mínimo
/// </summary>
public class ProductoBajoMinimoDto
{
    public int ProductoId { get; set; }
    public string ProductoCodigo { get; set; } = string.Empty;
    public string ProductoNombre { get; set; } = string.Empty;
    public decimal StockMinimo { get; set; }
    public decimal StockActual { get; set; }
    public decimal Diferencia { get; set; }
    public string Estado { get; set; } = "CRITICO"; // CRITICO, BAJO
}

/// <summary>
/// Producto sin movimiento
/// </summary>
public class ProductoSinMovimientoDto
{
    public int ProductoId { get; set; }
    public string ProductoCodigo { get; set; } = string.Empty;
    public string ProductoNombre { get; set; } = string.Empty;
    public decimal StockActual { get; set; }
    public decimal ValorStock { get; set; }
    public int DiasSinMovimiento { get; set; }
    public DateTime? UltimoMovimiento { get; set; }
}

/// <summary>
/// Movimiento de inventario para reportes
/// </summary>
public class MovimientoInventarioDto
{
    public int Id { get; set; }
    public DateTime FechaMovimiento { get; set; }
    public string TipoMovimiento { get; set; } = string.Empty;
    public string TipoDocumento { get; set; } = string.Empty;
    public string? NumeroDocumento { get; set; }
    public int? DocumentoId { get; set; }
    public int ProductoId { get; set; }
    public string ProductoCodigo { get; set; } = string.Empty;
    public string ProductoNombre { get; set; } = string.Empty;
    public int BodegaId { get; set; }
    public string BodegaNombre { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public decimal CostoUnitario { get; set; }
    public decimal SaldoAnterior { get; set; }
    public decimal NuevoSaldo { get; set; }
    public string? Observaciones { get; set; }

    // Usuario que registró el movimiento
    public int? UsuarioId { get; set; }
    public string? UsuarioNombre { get; set; }
}

/// <summary>
/// Resumen de movimientos por tipo con valor monetario
/// </summary>
public class ResumenMovimientoPorTipoDto
{
    public string TipoMovimiento { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal ValorTotal { get; set; }
}

/// <summary>
/// Kardex de un producto
/// </summary>
public class KardexProductoDto
{
    public int ProductoId { get; set; }
    public string ProductoCodigo { get; set; } = string.Empty;
    public string ProductoNombre { get; set; } = string.Empty;
    public int? BodegaId { get; set; }
    public string? BodegaNombre { get; set; }
    public DateTime FechaDesde { get; set; }
    public DateTime FechaHasta { get; set; }
    public decimal SaldoInicial { get; set; }
    public decimal TotalEntradas { get; set; }
    public decimal TotalSalidas { get; set; }
    public decimal SaldoFinal { get; set; }
    public PagedResult<MovimientoInventarioDto> Movimientos { get; set; } = new();
}

/// <summary>
/// Valoración del inventario
/// </summary>
public class ValoracionInventarioDto
{
    public int? BodegaId { get; set; }
    public string? BodegaNombre { get; set; }
    public int TotalProductos { get; set; }
    public decimal CantidadTotalUnidades { get; set; }
    public decimal ValorTotal { get; set; }
    public List<ValoracionPorProductoDto> DetalleProductos { get; set; } = new();
}

/// <summary>
/// Valoración por producto
/// </summary>
public class ValoracionPorProductoDto
{
    public int ProductoId { get; set; }
    public string ProductoCodigo { get; set; } = string.Empty;
    public string ProductoNombre { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public decimal CostoPromedio { get; set; }
    public decimal ValorTotal { get; set; }
    public decimal PorcentajeDelTotal { get; set; }
}

/// <summary>
/// Rotación de producto
/// </summary>
public class RotacionProductoDto
{
    public int ProductoId { get; set; }
    public string ProductoCodigo { get; set; } = string.Empty;
    public string ProductoNombre { get; set; } = string.Empty;
    public decimal CantidadVendida { get; set; }
    public decimal PromedioStock { get; set; }
    public decimal IndiceRotacion { get; set; }
    public int DiasPromediInventario { get; set; }
    public string Clasificacion { get; set; } = string.Empty; // A, B, C
}

/// <summary>
/// Ítem de rotación ABC: clasificación por valor vendido acumulado (F3 G3).
/// </summary>
public class RotacionAbcItemDto
{
    public int ProductoId { get; set; }
    public string ProductoCodigo { get; set; } = string.Empty;
    public string ProductoNombre { get; set; } = string.Empty;
    public decimal UnidadesVendidas { get; set; }
    public decimal ValorVendido { get; set; }
    public decimal PorcentajeAcumulado { get; set; }
    public string Clasificacion { get; set; } = string.Empty; // A, B, C
}

/// <summary>
/// KPIs del inventario
/// </summary>
public class InventarioKPIsDto
{
    // Stock
    public int TotalProductos { get; set; }
    public decimal ValorTotalInventario { get; set; }
    public int ProductosBajoMinimo { get; set; }
    public int ProductosSinStock { get; set; }

    // Movimientos (último mes)
    public int TotalMovimientosUltimoMes { get; set; }
    public int TotalEntradasUltimoMes { get; set; }
    public int TotalSalidasUltimoMes { get; set; }

    // Financiero
    public decimal CostoMercanciaVendidaMesActual { get; set; }
    public decimal ValorComprasMesActual { get; set; }

    // Rotación
    public decimal RotacionPromedioAnual { get; set; }
    public int DiasPromedioInventario { get; set; }
}
