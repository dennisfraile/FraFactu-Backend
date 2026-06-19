using FraFactu.Application.Common;
using FraFactu.Application.DTOs.Reportes;

namespace FraFactu.Application.Services;

/// <summary>
/// Servicio para reportes de inventario
/// </summary>
public interface IInventarioReporteService
{
    // REPORTES DE STOCK
    /// <summary>
    /// Obtener stock actual de todos los productos por bodega
    /// </summary>
    /// <summary>
    /// Obtener stock actual de todos los productos por bodega
    /// </summary>
    Task<PagedResult<StockPorBodegaDto>> ObtenerStockPorBodegaAsync(
        int emisorId,
        int? bodegaId = null,
        int? productoId = null,
        string? search = null,
        int page = 1,
        int pageSize = 50,
        int? sucursalId = null,
        List<int>? sucursalIds = null,
        int? categoriaId = null,
        int? marcaId = null,
        bool soloBajoMinimo = false,
        bool soloSinStock = false,
        string? sortBy = null,
        bool sortDesc = false);

    /// <summary>
    /// Obtener productos con stock bajo mínimo
    /// </summary>
    /// <summary>
    /// Obtener productos con stock bajo mínimo
    /// </summary>
    Task<List<ProductoBajoMinimoDto>> ObtenerProductosBajoMinimoAsync(int emisorId, int? sucursalId = null);

    /// <summary>
    /// Obtener productos sin movimiento en X días
    /// </summary>
    /// <summary>
    /// Obtener productos sin movimiento en X días
    /// </summary>
    Task<List<ProductoSinMovimientoDto>> ObtenerProductosSinMovimientoAsync(int emisorId, int dias = 30, int? sucursalId = null);

    // REPORTES DE MOVIMIENTOS
    /// <summary>
    /// Obtener movimientos de inventario por período
    /// </summary>
    Task<PagedResult<MovimientoInventarioDto>> ObtenerMovimientosPorPeriodoAsync(
        int emisorId,
        DateTime desde,
        DateTime hasta,
        int? productoId = null,
        int? bodegaId = null,
        string? tipoMovimiento = null,
        string? search = null,
        int page = 1,
        int pageSize = 50,
        int? sucursalId = null,
        List<int>? sucursalIds = null,
        bool ocultarAnulaciones = false,
        string? sortBy = null,
        bool sortDesc = false);

    /// <summary>
    /// Obtener kardex de un producto (historial detallado)
    /// </summary>
    /// <summary>
    /// Obtener kardex de un producto (historial detallado)
    /// </summary>
    Task<KardexProductoDto> ObtenerKardexProductoAsync(
        int productoId,
        int emisorId,
        int? bodegaId = null,
        DateTime? desde = null,
        DateTime? hasta = null,
        string? search = null,
        int page = 1,
        int pageSize = 50,
        int? sucursalId = null,
        List<int>? sucursalIds = null,
        string? tipoMovimiento = null,
        bool ocultarAnulaciones = false,
        string? sortBy = null,
        bool sortDesc = false);

    /// <summary>
    /// Resumen de movimientos por tipo con valor monetario
    /// </summary>
    /// <summary>
    /// Resumen de movimientos por tipo con valor monetario
    /// </summary>
    Task<List<ResumenMovimientoPorTipoDto>> ObtenerResumenMovimientosPorTipoAsync(int emisorId, DateTime desde, DateTime hasta, int? sucursalId = null, int? bodegaId = null);

    // REPORTES DE VALORACIÓN
    /// <summary>
    /// Valoración total del inventario
    /// </summary>
    /// <summary>
    /// Valoración total del inventario
    /// </summary>
    Task<ValoracionInventarioDto> ObtenerValoracionInventarioAsync(int emisorId, int? bodegaId = null, int? sucursalId = null);

    /// <summary>
    /// Costo de mercancía vendida (CMV) en un período
    /// </summary>
    /// <summary>
    /// Costo de mercancía vendida (CMV) en un período
    /// </summary>
    Task<decimal> ObtenerCostoMercanciaVendidaAsync(int emisorId, DateTime desde, DateTime hasta, int? sucursalId = null, int? bodegaId = null);

    /// <summary>
    /// Rotación de inventario por producto
    /// </summary>
    /// <summary>
    /// Rotación de inventario por producto
    /// </summary>
    Task<List<RotacionProductoDto>> ObtenerRotacionInventarioAsync(int emisorId, int meses = 12, int? sucursalId = null, int? bodegaId = null);

    // DASHBOARDS
    /// <summary>
    /// Obtener KPIs principales del inventario
    /// </summary>
    /// <summary>
    /// Obtener KPIs principales del inventario
    /// </summary>
    Task<InventarioKPIsDto> ObtenerKPIsAsync(int emisorId, int? sucursalId = null);
}
