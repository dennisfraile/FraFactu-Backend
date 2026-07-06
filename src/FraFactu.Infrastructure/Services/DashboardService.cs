using FraFactu.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using FraFactu.Application.Common;
using FraFactu.Application.Interfaces;

namespace FraFactu.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;

    public DashboardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardKPIs> ObtenerKPIsAsync(int emisorId, DateTime? fechaInicio = null, DateTime? fechaFin = null, int? sucursalId = null, List<int>? sucursalIds = null, DateTime? fechaAnteriorInicio = null, DateTime? fechaAnteriorFin = null, string? ambiente = null)
    {
        fechaInicio ??= DateTime.UtcNow.AddDays(-30);
        fechaFin ??= DateTime.UtcNow;

        var query = _context.Facturas
            .AsNoTracking()
            .Where(f => f.EmisorId == emisorId && f.FechaEmision >= fechaInicio && f.FechaEmision <= fechaFin);

        if (!string.IsNullOrEmpty(ambiente))
            query = query.Where(f => f.Ambiente == ambiente);

        if (sucursalIds != null && sucursalIds.Any())
            query = query.Where(f => f.SucursalId.HasValue && sucursalIds.Contains(f.SucursalId.Value));
        else if (sucursalId.HasValue)
            query = query.Where(f => f.SucursalId == sucursalId.Value);

        // Agregación en SQL: una fila por estado (Count + Sum), en vez de
        // materializar todas las facturas y agregar en memoria.
        var agg = await query
            .GroupBy(f => f.EstadoHacienda)
            .Select(g => new { Estado = g.Key, Cantidad = g.Count(), Total = g.Sum(x => x.TotalPagar) })
            .ToListAsync();

        var procesado = agg.FirstOrDefault(a => a.Estado == "PROCESADO");
        var totalAprobadas = procesado?.Cantidad ?? 0;
        var totalVentas = procesado?.Total ?? 0m;

        return new DashboardKPIs(
            TotalFacturas: totalAprobadas,
            TotalVentas: totalVentas,
            PromedioVenta: totalAprobadas > 0 ? totalVentas / totalAprobadas : 0,
            FacturasAprobadas: totalAprobadas,
            FacturasPendientes: agg.FirstOrDefault(a => a.Estado == "PENDIENTE_ENVIO")?.Cantidad ?? 0,
            FacturasRechazadas: agg.FirstOrDefault(a => a.Estado == "RECHAZADO")?.Cantidad ?? 0,
            CrecimientoVentas: await CalcularCrecimientoAsync(emisorId, fechaInicio.Value, fechaFin.Value, sucursalId, sucursalIds, fechaAnteriorInicio, fechaAnteriorFin, ambiente)
        );
    }

    public async Task<List<VentasPorDia>> ObtenerVentasPorDiaAsync(int emisorId, int dias = 30, int? sucursalId = null, List<int>? sucursalIds = null, string? ambiente = null)
    {
        var fechaInicio = DateTime.UtcNow.AddDays(-dias);

        var query = _context.Facturas
            .Where(f => f.EmisorId == emisorId && f.FechaEmision >= fechaInicio && f.EstadoHacienda == "PROCESADO");

        if (!string.IsNullOrEmpty(ambiente))
            query = query.Where(f => f.Ambiente == ambiente);

        if (sucursalIds != null && sucursalIds.Any())
            query = query.Where(f => f.SucursalId.HasValue && sucursalIds.Contains(f.SucursalId.Value));
        else if (sucursalId.HasValue)
            query = query.Where(f => f.SucursalId == sucursalId.Value);

        var ventasData = await query
            .GroupBy(f => f.FechaEmision.Date)
            .Select(g => new { Fecha = g.Key, CantidadFacturas = g.Count(), TotalVentas = g.Sum(f => f.TotalPagar) })
            .OrderBy(v => v.Fecha)
            .ToListAsync();

        return ventasData.Select(v => new VentasPorDia(v.Fecha, v.CantidadFacturas, v.TotalVentas)).ToList();
    }

    public async Task<List<ProductoMasVendido>> ObtenerProductosMasVendidosAsync(int emisorId, int top = 10, DateTime? fechaInicio = null, DateTime? fechaFin = null, int? sucursalId = null, List<int>? sucursalIds = null, string? ambiente = null)
    {
        var query = _context.FacturaDetalles
            .Include(d => d.Producto)
            .Include(d => d.Factura)
            .Where(d => d.ProductoId != null && d.Factura.EmisorId == emisorId && d.Factura.EstadoHacienda == "PROCESADO");

        if (!string.IsNullOrEmpty(ambiente))
            query = query.Where(d => d.Factura.Ambiente == ambiente);

        if (fechaInicio.HasValue)
        {
            var inicio = DateTime.SpecifyKind(fechaInicio.Value.Date, DateTimeKind.Utc);
            query = query.Where(d => d.Factura.FechaEmision >= inicio);
        }

        if (fechaFin.HasValue)
        {
            var fin = DateTime.SpecifyKind(fechaFin.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            query = query.Where(d => d.Factura.FechaEmision <= fin);
        }

        if (sucursalIds != null && sucursalIds.Any())
            query = query.Where(d => d.Factura.SucursalId.HasValue && sucursalIds.Contains(d.Factura.SucursalId.Value));
        else if (sucursalId.HasValue)
            query = query.Where(d => d.Factura.SucursalId == sucursalId.Value);

        var productosData = await query
            .GroupBy(d => new { d.ProductoId, d.Producto!.Nombre })
            .Select(g => new
            {
                ProductoId = g.Key.ProductoId!.Value,
                NombreProducto = g.Key.Nombre,
                CantidadVendida = g.Sum(d => d.Cantidad),
                TotalVentas = g.Sum(d => d.VentaGravada + d.VentaExenta + d.VentaNoSujeta)
            })
            .OrderByDescending(p => p.CantidadVendida)
            .Take(top)
            .ToListAsync();

        return productosData.Select(p => new ProductoMasVendido(p.ProductoId, p.NombreProducto, p.CantidadVendida, p.TotalVentas)).ToList();
    }

    public async Task<List<VentasPorCategoria>> ObtenerVentasPorCategoriaAsync(int emisorId, int? sucursalId = null, DateTime? fechaInicio = null, DateTime? fechaFin = null, List<int>? sucursalIds = null, string? ambiente = null)
    {
        fechaInicio ??= DateTime.UtcNow.AddDays(-30);
        fechaFin ??= DateTime.UtcNow;

        var query = _context.FacturaDetalles
            .Include(d => d.Producto)
                .ThenInclude(p => p!.Categoria)
            .Include(d => d.Factura)
            .Where(d => d.Producto != null && d.Producto.Categoria != null && d.Factura.EmisorId == emisorId && d.Factura.EstadoHacienda == "PROCESADO");

        if (!string.IsNullOrEmpty(ambiente))
            query = query.Where(d => d.Factura.Ambiente == ambiente);

        if (fechaInicio.HasValue)
            query = query.Where(d => d.Factura.FechaEmision >= fechaInicio.Value);

        if (fechaFin.HasValue)
            query = query.Where(d => d.Factura.FechaEmision <= fechaFin.Value);

        if (sucursalIds != null && sucursalIds.Any())
            query = query.Where(d => d.Factura.SucursalId.HasValue && sucursalIds.Contains(d.Factura.SucursalId.Value));
        else if (sucursalId.HasValue)
            query = query.Where(d => d.Factura.SucursalId == sucursalId.Value);

        var ventasData = await query
            .GroupBy(d => new { d.Producto!.CategoriaId, d.Producto.Categoria!.Nombre })
            .Select(g => new
            {
                CategoriaId = g.Key.CategoriaId,
                NombreCategoria = g.Key.Nombre,
                TotalVentas = g.Sum(d => d.VentaGravada + d.VentaExenta + d.VentaNoSujeta),
                CantidadVendidaReales = g.Sum(d => d.Cantidad),
                CantidadProductos = g.Select(d => d.ProductoId).Distinct().Count()
            })
            .OrderByDescending(v => v.TotalVentas)
            .ToListAsync();

        return ventasData.Select(v => new VentasPorCategoria(
            v.CategoriaId ?? 0,
            v.NombreCategoria,
            v.TotalVentas,
            v.CantidadVendidaReales,
            v.CantidadProductos
        )).ToList();
    }

    public async Task<List<VentasPorVendedor>> ObtenerVentasPorVendedorAsync(int emisorId, DateTime? fechaInicio = null, DateTime? fechaFin = null, int? sucursalId = null, List<int>? sucursalIds = null, string? ambiente = null)
    {
        fechaInicio ??= DateTime.UtcNow.AddDays(-30);
        fechaFin ??= DateTime.UtcNow;

        var query = _context.Facturas
            .Include(f => f.Vendedor)
            .Where(f => f.EmisorId == emisorId && f.FechaEmision >= fechaInicio && f.FechaEmision <= fechaFin && f.EstadoHacienda == "PROCESADO" && f.VendedorId != null);

        if (!string.IsNullOrEmpty(ambiente))
            query = query.Where(f => f.Ambiente == ambiente);

        if (sucursalIds != null && sucursalIds.Any())
            query = query.Where(f => f.SucursalId.HasValue && sucursalIds.Contains(f.SucursalId.Value));
        else if (sucursalId.HasValue)
            query = query.Where(f => f.SucursalId == sucursalId.Value);

        var ventasData = await query
            .GroupBy(f => new { f.VendedorId, f.Vendedor!.Nombre, f.Vendedor.Codigo })
            .Select(g => new
            {
                VendedorId = g.Key.VendedorId!.Value,
                NombreVendedor = g.Key.Nombre,
                CodigoVendedor = g.Key.Codigo,
                CantidadFacturas = g.Count(),
                TotalVentas = g.Sum(f => f.TotalPagar)
            })
            .OrderByDescending(v => v.TotalVentas)
            .ToListAsync();

        return ventasData.Select(v => new VentasPorVendedor(
            v.VendedorId,
            v.CodigoVendedor,
            v.NombreVendedor,
            v.TotalVentas,
            v.CantidadFacturas,
            v.CantidadFacturas > 0 ? v.TotalVentas / v.CantidadFacturas : 0
        )).ToList();
    }

    public async Task<PagedResult<VentaVendedorDetalleDto>> ObtenerVentasPorVendedorDetalleAsync(
        int emisorId,
        DateTime? fechaInicio = null,
        DateTime? fechaFin = null,
        int? sucursalId = null,
        int? vendedorId = null,
        string? search = null,
        int page = 1,
        int pageSize = 50,
        List<int>? sucursalIds = null,
        string? ambiente = null)
    {
        fechaInicio ??= DateTime.UtcNow.AddDays(-30);
        fechaFin ??= DateTime.UtcNow;

        var query = _context.Facturas
            .Include(f => f.Vendedor)
            .Include(f => f.Receptor)
            .Include(f => f.Sucursal)
            .Where(f => f.EmisorId == emisorId
                && f.FechaEmision >= fechaInicio
                && f.FechaEmision <= fechaFin
                && f.EstadoHacienda == "PROCESADO"
                && f.VendedorId != null);

        if (!string.IsNullOrEmpty(ambiente))
            query = query.Where(f => f.Ambiente == ambiente);

        if (sucursalIds != null && sucursalIds.Any())
            query = query.Where(f => f.SucursalId.HasValue && sucursalIds.Contains(f.SucursalId.Value));
        else if (sucursalId.HasValue)
            query = query.Where(f => f.SucursalId == sucursalId.Value);

        if (vendedorId.HasValue)
            query = query.Where(f => f.VendedorId == vendedorId.Value);

        if (!string.IsNullOrEmpty(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(f =>
                f.NumeroControl.ToLower().Contains(searchLower) ||
                f.CodigoGeneracion.ToLower().Contains(searchLower) ||
                (f.Receptor != null && f.Receptor.NombreRazonSocial.ToLower().Contains(searchLower)) ||
                (f.Vendedor != null && f.Vendedor.Nombre.ToLower().Contains(searchLower)) ||
                (f.Vendedor != null && f.Vendedor.Codigo.ToLower().Contains(searchLower)));
        }

        var totalItems = await query.CountAsync();

        var items = await query
            .OrderByDescending(f => f.FechaEmision)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(f => new VentaVendedorDetalleDto
            {
                FacturaId = f.Id,
                NumeroControl = f.NumeroControl,
                CodigoGeneracion = f.CodigoGeneracion,
                FechaEmision = f.FechaEmision,
                ReceptorNombre = f.Receptor != null ? f.Receptor.NombreRazonSocial : "Consumidor Final",
                TotalPagar = f.TotalPagar,
                EstadoHacienda = f.EstadoHacienda,
                VendedorId = f.VendedorId,
                VendedorCodigo = f.Vendedor != null ? f.Vendedor.Codigo : null,
                VendedorNombre = f.Vendedor != null ? f.Vendedor.Nombre : null,
                SucursalId = f.SucursalId,
                SucursalNombre = f.Sucursal != null ? f.Sucursal.Nombre : null
            })
            .ToListAsync();

        return new PagedResult<VentaVendedorDetalleDto>
        {
            Items = items,
            TotalItems = totalItems,
            PageNumber = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<VentaCategoriaDetalleDto>> ObtenerVentasPorCategoriaDetalleAsync(
        int emisorId,
        DateTime? fechaInicio = null,
        DateTime? fechaFin = null,
        int? sucursalId = null,
        int? categoriaId = null,
        string? search = null,
        int page = 1,
        int pageSize = 50,
        List<int>? sucursalIds = null,
        string? ambiente = null)
    {
        fechaInicio ??= DateTime.UtcNow.AddDays(-30);
        fechaFin ??= DateTime.UtcNow;

        var query = _context.FacturaDetalles
            .Include(d => d.Producto)
                .ThenInclude(p => p!.Categoria)
            .Include(d => d.Factura)
                .ThenInclude(f => f.Receptor)
            .Include(d => d.Factura)
                .ThenInclude(f => f.Sucursal)
            .Where(d => d.Producto != null
                && d.Producto.Categoria != null
                && d.Factura.EmisorId == emisorId
                && d.Factura.EstadoHacienda == "PROCESADO"
                && d.Factura.FechaEmision >= fechaInicio
                && d.Factura.FechaEmision <= fechaFin);

        if (!string.IsNullOrEmpty(ambiente))
            query = query.Where(d => d.Factura.Ambiente == ambiente);

        if (sucursalIds != null && sucursalIds.Any())
            query = query.Where(d => d.Factura.SucursalId.HasValue && sucursalIds.Contains(d.Factura.SucursalId.Value));
        else if (sucursalId.HasValue)
            query = query.Where(d => d.Factura.SucursalId == sucursalId.Value);

        if (categoriaId.HasValue)
            query = query.Where(d => d.Producto!.CategoriaId == categoriaId.Value);

        if (!string.IsNullOrEmpty(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(d =>
                d.Factura.NumeroControl.ToLower().Contains(searchLower) ||
                d.Factura.CodigoGeneracion.ToLower().Contains(searchLower) ||
                d.Producto!.Nombre.ToLower().Contains(searchLower) ||
                d.Producto.Categoria!.Nombre.ToLower().Contains(searchLower) ||
                (d.Factura.Receptor != null && d.Factura.Receptor.NombreRazonSocial.ToLower().Contains(searchLower)));
        }

        var totalItems = await query.CountAsync();

        var items = await query
            .OrderByDescending(d => d.Factura.FechaEmision)
            .ThenBy(d => d.NumeroItem)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new VentaCategoriaDetalleDto
            {
                FacturaDetalleId = d.Id,
                FacturaId = d.FacturaId,
                NumeroControl = d.Factura.NumeroControl,
                CodigoGeneracion = d.Factura.CodigoGeneracion,
                FechaEmision = d.Factura.FechaEmision,
                ProductoId = d.ProductoId,
                ProductoNombre = d.Producto!.Nombre,
                CategoriaId = d.Producto.CategoriaId,
                CategoriaNombre = d.Producto.Categoria != null ? d.Producto.Categoria.Nombre : null,
                Cantidad = d.Cantidad,
                TotalVenta = d.VentaGravada + d.VentaExenta + d.VentaNoSujeta,
                ReceptorNombre = d.Factura.Receptor != null ? d.Factura.Receptor.NombreRazonSocial : "Consumidor Final",
                SucursalId = d.Factura.SucursalId,
                SucursalNombre = d.Factura.Sucursal != null ? d.Factura.Sucursal.Nombre : null
            })
            .ToListAsync();

        return new PagedResult<VentaCategoriaDetalleDto>
        {
            Items = items,
            TotalItems = totalItems,
            PageNumber = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<ComparativoSucursalDto>> ObtenerComparativoSucursalesAsync(
        int emisorId,
        DateTime desde,
        DateTime hasta,
        string? search = null,
        int page = 1,
        int pageSize = 50,
        int? sucursalId = null,
        List<int>? sucursalIds = null,
        string? ambiente = null)
    {
        var query = _context.Facturas
            .Include(f => f.Sucursal)
            .Where(f => f.EmisorId == emisorId
                && f.SucursalId != null
                && f.EstadoHacienda == "PROCESADO"
                && f.FechaEmision >= desde
                && f.FechaEmision <= hasta);

        if (!string.IsNullOrEmpty(ambiente))
            query = query.Where(f => f.Ambiente == ambiente);

        if (sucursalIds != null && sucursalIds.Any())
            query = query.Where(f => f.SucursalId.HasValue && sucursalIds.Contains(f.SucursalId.Value));
        else if (sucursalId.HasValue)
            query = query.Where(f => f.SucursalId == sucursalId.Value);

        var groupedQuery = query
            .GroupBy(f => new { f.SucursalId, f.Sucursal!.Codigo, f.Sucursal.Nombre })
            .Select(g => new
            {
                SucursalId = g.Key.SucursalId!.Value,
                CodigoSucursal = g.Key.Codigo,
                NombreSucursal = g.Key.Nombre,
                TotalFacturas = g.Count(),
                TotalVentas = g.Sum(f => f.TotalPagar)
            });

        if (!string.IsNullOrEmpty(search))
        {
            var searchLower = search.ToLower();
            groupedQuery = groupedQuery.Where(s =>
                s.NombreSucursal.ToLower().Contains(searchLower) ||
                s.CodigoSucursal.ToLower().Contains(searchLower));
        }

        var totalItems = await groupedQuery.CountAsync();

        var items = await groupedQuery
            .OrderByDescending(s => s.TotalVentas)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var granTotal = items.Sum(s => s.TotalVentas);

        var resultado = items.Select(s => new ComparativoSucursalDto
        {
            SucursalId = s.SucursalId,
            CodigoSucursal = s.CodigoSucursal,
            NombreSucursal = s.NombreSucursal,
            TotalFacturas = s.TotalFacturas,
            TotalVentas = s.TotalVentas,
            PromedioVenta = s.TotalFacturas > 0 ? s.TotalVentas / s.TotalFacturas : 0,
            PorcentajeDelTotal = granTotal > 0 ? (s.TotalVentas / granTotal) * 100 : 0
        }).ToList();

        return new PagedResult<ComparativoSucursalDto>
        {
            Items = resultado,
            TotalItems = totalItems,
            PageNumber = page,
            PageSize = pageSize
        };
    }

    private async Task<decimal> CalcularCrecimientoAsync(int emisorId, DateTime fechaInicio, DateTime fechaFin, int? sucursalId = null, List<int>? sucursalIds = null, DateTime? fechaAnteriorInicio = null, DateTime? fechaAnteriorFin = null, string? ambiente = null)
    {
        // Si el frontend envía las fechas del período anterior, usarlas directamente.
        // Si no, fallback a la lógica original (misma duración en días hacia atrás).
        DateTime periodoAntInicio;
        DateTime periodoAntFin;

        if (fechaAnteriorInicio.HasValue && fechaAnteriorFin.HasValue)
        {
            periodoAntInicio = fechaAnteriorInicio.Value;
            periodoAntFin = fechaAnteriorFin.Value;
        }
        else
        {
            var dias = (fechaFin - fechaInicio).Days;
            periodoAntInicio = fechaInicio.AddDays(-dias);
            periodoAntFin = fechaInicio.AddDays(-1);
        }

        var queryActual = _context.Facturas
            .AsNoTracking()
            .Where(f => f.EmisorId == emisorId && f.FechaEmision >= fechaInicio && f.FechaEmision <= fechaFin && f.EstadoHacienda == "PROCESADO");

        var queryAnterior = _context.Facturas
            .AsNoTracking()
            .Where(f => f.EmisorId == emisorId && f.FechaEmision >= periodoAntInicio && f.FechaEmision <= periodoAntFin && f.EstadoHacienda == "PROCESADO");

        if (!string.IsNullOrEmpty(ambiente))
        {
            queryActual = queryActual.Where(f => f.Ambiente == ambiente);
            queryAnterior = queryAnterior.Where(f => f.Ambiente == ambiente);
        }

        if (sucursalIds != null && sucursalIds.Any())
        {
            queryActual = queryActual.Where(f => f.SucursalId.HasValue && sucursalIds.Contains(f.SucursalId.Value));
            queryAnterior = queryAnterior.Where(f => f.SucursalId.HasValue && sucursalIds.Contains(f.SucursalId.Value));
        }
        else if (sucursalId.HasValue)
        {
            queryActual = queryActual.Where(f => f.SucursalId == sucursalId.Value);
            queryAnterior = queryAnterior.Where(f => f.SucursalId == sucursalId.Value);
        }

        var ventasActuales = await queryActual.SumAsync(f => f.TotalPagar);
        var ventasAnteriores = await queryAnterior.SumAsync(f => f.TotalPagar);

        if (ventasAnteriores == 0) return 0;

        return ((ventasActuales - ventasAnteriores) / ventasAnteriores) * 100;
    }

    public async Task<Application.DTOs.Dashboard.DashboardCajeroDto> ObtenerDashboardCajeroAsync(
        int emisorId,
        int usuarioId,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        string? ambiente = null)
    {
        // Valores por defecto: últimos 30 días
        fechaDesde ??= DateTime.UtcNow.AddDays(-30);
        fechaHasta ??= DateTime.UtcNow;

        // Obtener facturas del cajero
        var query = _context.Facturas
            .Where(f => f.EmisorId == emisorId &&
                       f.UsuarioId == usuarioId &&
                       f.FechaEmision >= fechaDesde &&
                       f.FechaEmision <= fechaHasta &&
                       f.EstadoHacienda == "PROCESADO");

        if (!string.IsNullOrEmpty(ambiente))
            query = query.Where(f => f.Ambiente == ambiente);

        var facturas = await query.ToListAsync();

        var montoTotal = facturas.Sum(f => f.TotalPagar);

        // Ventas agrupadas por día
        var ventasPorDia = facturas
            .GroupBy(f => f.FechaEmision.Date)
            .Select(g => new Application.DTOs.Dashboard.VentaPorDiaDto
            {
                Fecha = g.Key,
                CantidadFacturas = g.Count(),
                MontoTotal = g.Sum(f => f.TotalPagar)
            })
            .OrderBy(v => v.Fecha)
            .ToList();

        return new Application.DTOs.Dashboard.DashboardCajeroDto
        {
            TotalFacturas = facturas.Count,
            MontoTotalVendido = montoTotal,
            PromedioVenta = facturas.Any() ? montoTotal / facturas.Count : 0,
            VentasPorDia = ventasPorDia
        };
    }
}

// DTOs
public class DashboardKPIsDto
{
    public int TotalFacturas { get; set; }
    public decimal TotalVentas { get; set; }
    public decimal PromedioVenta { get; set; }
    public int FacturasAprobadas { get; set; }
    public int FacturasPendientes { get; set; }
    public int FacturasRechazadas { get; set; }
    public decimal CrecimientoVentas { get; set; } // Porcentaje
}

public class VentasPorDiaDto
{
    public DateTime Fecha { get; set; }
    public int CantidadFacturas { get; set; }
    public decimal TotalVentas { get; set; }
}

public class ProductoMasVendidoDto
{
    public int ProductoId { get; set; }
    public string NombreProducto { get; set; } = string.Empty;
    public decimal CantidadVendida { get; set; }
    public decimal TotalVentas { get; set; }
}

public class VentasPorCategoriaDto
{
    public int CategoriaId { get; set; }
    public string NombreCategoria { get; set; } = string.Empty;
    public decimal TotalVentas { get; set; }
    public int CantidadProductos { get; set; }
}
