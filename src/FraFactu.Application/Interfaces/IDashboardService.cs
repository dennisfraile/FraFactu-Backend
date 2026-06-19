using FraFactu.Application.Common;

namespace FraFactu.Application.Interfaces;

/// <summary>
/// Interfaz para Dashboard y Analytics
/// </summary>
public interface IDashboardService
{
    Task<DashboardKPIs> ObtenerKPIsAsync(int emisorId, DateTime? fechaInicio = null, DateTime? fechaFin = null, int? sucursalId = null, List<int>? sucursalIds = null, DateTime? fechaAnteriorInicio = null, DateTime? fechaAnteriorFin = null, string? ambiente = null);
    Task<List<VentasPorDia>> ObtenerVentasPorDiaAsync(int emisorId, int dias = 30, int? sucursalId = null, List<int>? sucursalIds = null, string? ambiente = null);
    Task<List<ProductoMasVendido>> ObtenerProductosMasVendidosAsync(int emisorId, int top = 10, DateTime? fechaInicio = null, DateTime? fechaFin = null, int? sucursalId = null, List<int>? sucursalIds = null, string? ambiente = null);
    Task<List<VentasPorCategoria>> ObtenerVentasPorCategoriaAsync(int emisorId, int? sucursalId = null, DateTime? fechaInicio = null, DateTime? fechaFin = null, List<int>? sucursalIds = null, string? ambiente = null);

    /// <summary>
    /// Obtiene estadísticas del cajero (ventas por usuario)
    /// </summary>
    Task<DTOs.Dashboard.DashboardCajeroDto> ObtenerDashboardCajeroAsync(
        int emisorId,
        int usuarioId,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        string? ambiente = null);

    Task<List<VentasPorVendedor>> ObtenerVentasPorVendedorAsync(int emisorId, DateTime? fechaInicio = null, DateTime? fechaFin = null, int? sucursalId = null, List<int>? sucursalIds = null, string? ambiente = null);

    Task<PagedResult<VentaVendedorDetalleDto>> ObtenerVentasPorVendedorDetalleAsync(
        int emisorId,
        DateTime? fechaInicio = null,
        DateTime? fechaFin = null,
        int? sucursalId = null,
        int? vendedorId = null,
        string? search = null,
        int page = 1,
        int pageSize = 50,
        List<int>? sucursalIds = null,
        string? ambiente = null);

    Task<PagedResult<VentaCategoriaDetalleDto>> ObtenerVentasPorCategoriaDetalleAsync(
        int emisorId,
        DateTime? fechaInicio = null,
        DateTime? fechaFin = null,
        int? sucursalId = null,
        int? categoriaId = null,
        string? search = null,
        int page = 1,
        int pageSize = 50,
        List<int>? sucursalIds = null,
        string? ambiente = null);

    Task<PagedResult<ComparativoSucursalDto>> ObtenerComparativoSucursalesAsync(
        int emisorId,
        DateTime desde,
        DateTime hasta,
        string? search = null,
        int page = 1,
        int pageSize = 50,
        int? sucursalId = null,
        List<int>? sucursalIds = null,
        string? ambiente = null);
}

// DTOs  
public record DashboardKPIs(
    int TotalFacturas,
    decimal TotalVentas,
    decimal PromedioVenta,
    int FacturasAprobadas,
    int FacturasPendientes,
    int FacturasRechazadas,
    decimal CrecimientoVentas
);

public record VentasPorDia(
    DateTime Fecha,
    int CantidadFacturas,
    decimal TotalVentas
);

public record ProductoMasVendido(
    int ProductoId,
    string NombreProducto,
    decimal CantidadVendida,
    decimal TotalVentas
);

public record VentasPorCategoria(
    int CategoriaId,
    string NombreCategoria,
    decimal TotalVentas,
    decimal CantidadVendidaReales,
    int CantidadProductos
);

public record VentasPorVendedor(
    int VendedorId,
    string CodigoVendedor,
    string NombreVendedor,
    decimal TotalVentas,
    int CantidadFacturas,
    decimal PromedioVenta
);

public class VentaVendedorDetalleDto
{
    public int FacturaId { get; set; }
    public string NumeroControl { get; set; } = string.Empty;
    public string CodigoGeneracion { get; set; } = string.Empty;
    public DateTime FechaEmision { get; set; }
    public string ReceptorNombre { get; set; } = string.Empty;
    public decimal TotalPagar { get; set; }
    public string EstadoHacienda { get; set; } = string.Empty;
    public int? VendedorId { get; set; }
    public string? VendedorCodigo { get; set; }
    public string? VendedorNombre { get; set; }
    public int? SucursalId { get; set; }
    public string? SucursalNombre { get; set; }
}

public class ComparativoSucursalDto
{
    public int SucursalId { get; set; }
    public string CodigoSucursal { get; set; } = string.Empty;
    public string NombreSucursal { get; set; } = string.Empty;
    public int TotalFacturas { get; set; }
    public decimal TotalVentas { get; set; }
    public decimal PromedioVenta { get; set; }
    public decimal PorcentajeDelTotal { get; set; }
}

public class VentaCategoriaDetalleDto
{
    public int FacturaDetalleId { get; set; }
    public int FacturaId { get; set; }
    public string NumeroControl { get; set; } = string.Empty;
    public string CodigoGeneracion { get; set; } = string.Empty;
    public DateTime FechaEmision { get; set; }
    public int? ProductoId { get; set; }
    public string ProductoNombre { get; set; } = string.Empty;
    public int? CategoriaId { get; set; }
    public string? CategoriaNombre { get; set; }
    public decimal Cantidad { get; set; }
    public decimal TotalVenta { get; set; }
    public string ReceptorNombre { get; set; } = string.Empty;
    public int? SucursalId { get; set; }
    public string? SucursalNombre { get; set; }
}
