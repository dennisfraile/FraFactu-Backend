using Microsoft.EntityFrameworkCore;
using FraFactu.Application.Common;
using FraFactu.Application.Services;
using FraFactu.Application.DTOs.Reportes;
using FraFactu.Infrastructure.Persistence;

namespace FraFactu.Infrastructure.Services;

/// <summary>
/// Servicio de reportes de inventario
/// Usa LINQ donde es posible y Raw SQL solo cuando es técnicamente necesario
/// </summary>
public class InventarioReporteService : IInventarioReporteService
{
    private readonly ApplicationDbContext _context;

    public InventarioReporteService(ApplicationDbContext context)
    {
        _context = context;
    }

    // REPORTES DE STOCK

    public async Task<PagedResult<StockPorBodegaDto>> ObtenerStockPorBodegaAsync(
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
        bool sortDesc = false)
    {
        var query = _context.StocksBodega
            .Include(s => s.Producto)
            .Include(s => s.Bodega)
            .ThenInclude(b => b.Sucursal)
            .Where(s => s.Bodega!.Sucursal!.EmisorId == emisorId)
            .Where(s => s.Bodega!.Activa)
            .AsQueryable();

        if (sucursalIds != null && sucursalIds.Any())
            query = query.Where(s => s.Bodega!.SucursalId.HasValue && sucursalIds.Contains(s.Bodega!.SucursalId.Value));
        else if (sucursalId.HasValue)
            query = query.Where(s => s.Bodega!.SucursalId == sucursalId.Value);

        if (bodegaId.HasValue)
            query = query.Where(s => s.BodegaId == bodegaId.Value);

        if (productoId.HasValue)
            query = query.Where(s => s.ProductoId == productoId.Value);

        if (categoriaId.HasValue)
            query = query.Where(s => s.Producto != null && s.Producto.CategoriaId == categoriaId.Value);

        if (marcaId.HasValue)
            query = query.Where(s => s.Producto != null && s.Producto.MarcaId == marcaId.Value);

        if (soloBajoMinimo)
            query = query.Where(s => s.Producto != null && s.Producto.StockMinimo.HasValue && s.CantidadDisponible < s.Producto.StockMinimo.Value);

        if (soloSinStock)
            query = query.Where(s => s.CantidadDisponible + s.CantidadReservada == 0);

        if (!string.IsNullOrEmpty(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(s =>
                (s.Producto != null && s.Producto.Nombre.ToLower().Contains(searchLower)) ||
                (s.Producto != null && s.Producto.Codigo.ToLower().Contains(searchLower)) ||
                (s.Bodega != null && s.Bodega.Nombre.ToLower().Contains(searchLower)));
        }

        var totalItems = await query.CountAsync();

        // Ordenamiento dinámico
        IOrderedQueryable<Domain.Entities.StockBodega> orderedQuery = sortBy?.ToLower() switch
        {
            "codigo" => sortDesc ? query.OrderByDescending(s => s.Producto!.Codigo) : query.OrderBy(s => s.Producto!.Codigo),
            "nombre" => sortDesc ? query.OrderByDescending(s => s.Producto!.Nombre) : query.OrderBy(s => s.Producto!.Nombre),
            "cantidaddisponible" => sortDesc ? query.OrderByDescending(s => s.CantidadDisponible) : query.OrderBy(s => s.CantidadDisponible),
            "cantidadtotal" => sortDesc ? query.OrderByDescending(s => s.CantidadTotal) : query.OrderBy(s => s.CantidadTotal),
            "costopromedio" => sortDesc ? query.OrderByDescending(s => s.CostoPromedio) : query.OrderBy(s => s.CostoPromedio),
            "valorstock" => sortDesc ? query.OrderByDescending(s => s.CantidadTotal * s.CostoPromedio) : query.OrderBy(s => s.CantidadTotal * s.CostoPromedio),
            "bodega" => sortDesc ? query.OrderByDescending(s => s.Bodega!.Nombre) : query.OrderBy(s => s.Bodega!.Nombre),
            _ => sortDesc ? query.OrderByDescending(s => s.Producto!.Nombre) : query.OrderBy(s => s.Producto!.Nombre)
        };

        var stocks = await orderedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = stocks.Select(s => new StockPorBodegaDto
        {
            ProductoId = s.ProductoId,
            ProductoCodigo = s.Producto?.Codigo ?? "",
            ProductoNombre = s.Producto?.Nombre ?? "",
            BodegaId = s.BodegaId,
            BodegaNombre = s.Bodega?.Nombre ?? "",
            SucursalNombre = s.Bodega?.Sucursal?.Nombre ?? "",
            CantidadDisponible = s.CantidadDisponible,
            CantidadReservada = s.CantidadReservada,
            CantidadTotal = s.CantidadTotal,
            CostoPromedio = s.CostoPromedio,
            ValorStock = s.CantidadTotal * s.CostoPromedio,
            FechaUltimoMovimiento = null,
            StockMinimo = s.Producto?.StockMinimo,
            StockMaximo = s.Producto?.StockMaximo
        }).ToList();

        return new PagedResult<StockPorBodegaDto>
        {
            Items = items,
            TotalItems = totalItems,
            PageNumber = page,
            PageSize = pageSize
        };
    }

    public async Task<List<ProductoBajoMinimoDto>> ObtenerProductosBajoMinimoAsync(int emisorId, int? sucursalId = null)
    {
        // ⚡ Raw SQL necesario - agregaciones con nullable decimal
        var sucursalFilter = sucursalId.HasValue ? $"AND s.\"Id\" = {sucursalId.Value}" : "";

        var sql = $@"
            SELECT
                p.""Id"" as ""ProductoId"",
                p.""Codigo"" as ""ProductoCodigo"",
                p.""Nombre"" as ""ProductoNombre"",
                p.""StockMinimo"" as ""StockMinimo"",
                COALESCE(SUM(sb.""CantidadDisponible""), 0) as ""StockActual"",
                p.""StockMinimo"" - COALESCE(SUM(sb.""CantidadDisponible""), 0) as ""Diferencia"",
                CASE
                    WHEN COALESCE(SUM(sb.""CantidadDisponible""), 0) / NULLIF(p.""StockMinimo"", 0) < 0.25
                    THEN 'CRITICO'
                    ELSE 'BAJO'
                END as ""Estado""
            FROM ""TBL_ProductosServicios"" p
            LEFT JOIN stock_bodegas sb ON p.""Id"" = sb.""ProductoId""
            LEFT JOIN bodegas b ON sb.""BodegaId"" = b.""Id""
            LEFT JOIN ""TBL_Sucursales"" s ON b.""SucursalId"" = s.""Id""
            WHERE p.""Activo"" = true
              AND p.""StockMinimo"" IS NOT NULL
              AND p.""EmisorId"" = {{0}}
              {sucursalFilter}
            GROUP BY p.""Id"", p.""Codigo"", p.""Nombre"", p.""StockMinimo""
            HAVING COALESCE(SUM(sb.""CantidadDisponible""), 0) < p.""StockMinimo""
            ORDER BY ""Estado"", ""Diferencia"" DESC";

        return await _context.Database
            .SqlQueryRaw<ProductoBajoMinimoDto>(sql, emisorId)
            .ToListAsync();
    }

    public async Task<List<ProductoSinMovimientoDto>> ObtenerProductosSinMovimientoAsync(int emisorId, int dias = 30, int? sucursalId = null)
    {
        // ⚡ Raw SQL necesario - subconsultas complejas
        var fechaLimite = DateTime.UtcNow.AddDays(-dias);

        var sucursalJoin = sucursalId.HasValue ? "INNER JOIN bodegas b_filter ON sb.\"BodegaId\" = b_filter.\"Id\"" : "";
        var sucursalFilter = sucursalId.HasValue ? $"AND b_filter.\"SucursalId\" = {sucursalId.Value}" : "";

        var sql = $@"
            SELECT
                p.""Id"" as ""ProductoId"",
                p.""Codigo"" as ""ProductoCodigo"",
                p.""Nombre"" as ""ProductoNombre"",
                COALESCE(SUM(sb.""CantidadDisponible""), 0) as ""StockActual"",
                COALESCE(SUM(sb.""CantidadDisponible""), 0) *
                    COALESCE(AVG(NULLIF(sb.""CostoPromedio"", 0)), 0) as ""ValorStock"",
                COALESCE(
                    EXTRACT(DAY FROM (CURRENT_TIMESTAMP - MAX(mi.""FechaMovimiento""))),
                    9999
                ) as ""DiasSinMovimiento"",
                MAX(mi.""FechaMovimiento"") as ""UltimoMovimiento""
            FROM ""TBL_ProductosServicios"" p
            LEFT JOIN stock_bodegas sb ON p.""Id"" = sb.""ProductoId""
            {sucursalJoin}
            LEFT JOIN movimientos_inventario mi ON p.""Id"" = mi.""ProductoId""
            WHERE p.""Activo"" = true
              AND p.""EmisorId"" = {{1}}
              {sucursalFilter}
            GROUP BY p.""Id"", p.""Codigo"", p.""Nombre""
            HAVING(MAX(mi.""FechaMovimiento"") IS NULL OR MAX(mi.""FechaMovimiento"") < {{0}})
               AND COALESCE(SUM(sb.""CantidadDisponible""), 0) > 0
            ORDER BY ""DiasSinMovimiento"" DESC";

        return await _context.Database
            .SqlQueryRaw<ProductoSinMovimientoDto>(sql, fechaLimite, emisorId)
            .ToListAsync();
    }

    // REPORTES DE MOVIMIENTOS

    public async Task<PagedResult<MovimientoInventarioDto>> ObtenerMovimientosPorPeriodoAsync(
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
        bool sortDesc = false)
    {
        var query = _context.MovimientosInventario
            .Include(m => m.Producto)
            .Include(m => m.Bodega)
                .ThenInclude(b => b.Sucursal)
            .Include(m => m.Usuario)
            .Where(m => m.Bodega!.Sucursal!.EmisorId == emisorId)
            .Where(m => m.FechaMovimiento >= desde && m.FechaMovimiento <= hasta);

        if (sucursalIds != null && sucursalIds.Any())
            query = query.Where(m => m.Bodega!.SucursalId.HasValue && sucursalIds.Contains(m.Bodega!.SucursalId.Value));
        else if (sucursalId.HasValue)
            query = query.Where(m => m.Bodega!.SucursalId == sucursalId.Value);

        if (productoId.HasValue)
            query = query.Where(m => m.ProductoId == productoId.Value);

        if (bodegaId.HasValue)
            query = query.Where(m => m.BodegaId == bodegaId.Value);

        if (!string.IsNullOrEmpty(tipoMovimiento))
        {
            if (tipoMovimiento == "TRASLADO")
                query = query.Where(m => m.TipoDocumento == "TRASLADO");
            else
                query = query.Where(m => m.TipoMovimiento == tipoMovimiento);
        }

        if (ocultarAnulaciones)
            query = query.Where(m => m.TipoMovimiento != "AJUSTE_ENTRADA" && m.TipoMovimiento != "AJUSTE_SALIDA"
                && (m.TipoDocumento == null || !m.TipoDocumento.Contains("ANULACION")));

        if (!string.IsNullOrEmpty(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(m =>
                (m.Producto != null && m.Producto.Nombre.ToLower().Contains(searchLower)) ||
                (m.Producto != null && m.Producto.Codigo.ToLower().Contains(searchLower)) ||
                (m.Bodega != null && m.Bodega.Nombre.ToLower().Contains(searchLower)) ||
                (m.NumeroDocumento != null && m.NumeroDocumento.ToLower().Contains(searchLower)) ||
                (m.Observaciones != null && m.Observaciones.ToLower().Contains(searchLower)));
        }

        var totalItems = await query.CountAsync();

        // Ordenamiento dinámico
        IOrderedQueryable<Domain.Entities.MovimientoInventario> orderedQuery = sortBy?.ToLower() switch
        {
            "producto" => sortDesc ? query.OrderByDescending(m => m.Producto!.Nombre) : query.OrderBy(m => m.Producto!.Nombre),
            "bodega" => sortDesc ? query.OrderByDescending(m => m.Bodega!.Nombre) : query.OrderBy(m => m.Bodega!.Nombre),
            "cantidad" => sortDesc ? query.OrderByDescending(m => m.Cantidad) : query.OrderBy(m => m.Cantidad),
            "tipo" => sortDesc ? query.OrderByDescending(m => m.TipoMovimiento) : query.OrderBy(m => m.TipoMovimiento),
            _ => sortDesc ? query.OrderByDescending(m => m.FechaMovimiento) : query.OrderBy(m => m.FechaMovimiento)
        };

        var movimientos = await orderedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = movimientos.Select(m => new MovimientoInventarioDto
        {
            Id = m.Id,
            FechaMovimiento = m.FechaMovimiento,
            TipoMovimiento = m.TipoMovimiento,
            TipoDocumento = m.TipoDocumento ?? "",
            NumeroDocumento = m.NumeroDocumento,
            DocumentoId = m.DocumentoId,
            ProductoId = m.ProductoId,
            ProductoCodigo = m.Producto?.Codigo ?? "",
            ProductoNombre = m.Producto?.Nombre ?? "",
            BodegaId = m.BodegaId,
            BodegaNombre = m.Bodega?.Nombre ?? "",
            Cantidad = m.Cantidad,
            CostoUnitario = m.CostoUnitario,
            SaldoAnterior = m.SaldoAnterior ?? 0,
            NuevoSaldo = m.NuevoSaldo ?? 0,
            Observaciones = m.Observaciones,
            UsuarioId = m.UsuarioId,
            UsuarioNombre = m.Usuario?.NombreCompleto ?? m.Usuario?.Email ?? "-"
        }).ToList();

        return new PagedResult<MovimientoInventarioDto>
        {
            Items = items,
            TotalItems = totalItems,
            PageNumber = page,
            PageSize = pageSize
        };
    }

    public async Task<KardexProductoDto> ObtenerKardexProductoAsync(
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
        bool sortDesc = false)
    {
        desde ??= DateTime.UtcNow.AddMonths(-1);
        hasta ??= DateTime.UtcNow;

        var producto = await _context.ProductosServicios.FindAsync(productoId);
        if (producto == null)
            throw new KeyNotFoundException($"Producto {productoId} no encontrado");

        if (producto.EmisorId != emisorId)
            throw new UnauthorizedAccessException("El producto no pertenece al emisor");

        var saldoInicial = await ObtenerSaldoInicialAsync(productoId, emisorId, bodegaId, desde.Value);

        // Totales se calculan sobre TODOS los movimientos (sin paginación)
        var todosResult = await ObtenerMovimientosPorPeriodoAsync(emisorId, desde.Value, hasta.Value, productoId, bodegaId, null, null, 1, int.MaxValue, sucursalId, sucursalIds);
        var todosMovimientos = todosResult.Items;

        var totalEntradas = todosMovimientos.Where(m => m.TipoMovimiento == "ENTRADA").Sum(m => Math.Abs(m.Cantidad));
        var totalSalidas = todosMovimientos.Where(m => m.TipoMovimiento == "SALIDA").Sum(m => Math.Abs(m.Cantidad));
        var saldoFinal = saldoInicial + totalEntradas - totalSalidas;

        // Movimientos paginados (con búsqueda y filtros si aplican)
        var movimientosPaginados = await ObtenerMovimientosPorPeriodoAsync(emisorId, desde.Value, hasta.Value, productoId, bodegaId, tipoMovimiento, search, page, pageSize, sucursalId, sucursalIds,
            ocultarAnulaciones, sortBy, sortDesc);

        string? bodegaNombre = null;
        if (bodegaId.HasValue)
        {
            var bodega = await _context.Bodegas.FindAsync(bodegaId.Value);
            bodegaNombre = bodega?.Nombre;
        }

        return new KardexProductoDto
        {
            ProductoId = productoId,
            ProductoCodigo = producto.Codigo,
            ProductoNombre = producto.Nombre,
            BodegaId = bodegaId,
            BodegaNombre = bodegaNombre,
            FechaDesde = desde.Value,
            FechaHasta = hasta.Value,
            SaldoInicial = saldoInicial,
            TotalEntradas = totalEntradas,
            TotalSalidas = totalSalidas,
            SaldoFinal = saldoFinal,
            Movimientos = movimientosPaginados
        };
    }

    public async Task<List<ResumenMovimientoPorTipoDto>> ObtenerResumenMovimientosPorTipoAsync(int emisorId, DateTime desde, DateTime hasta, int? sucursalId = null, int? bodegaId = null)
    {
        var query = _context.MovimientosInventario
            .Include(m => m.Bodega)
                .ThenInclude(b => b.Sucursal)
            .Where(m => m.Bodega!.Sucursal!.EmisorId == emisorId)
            .Where(m => m.FechaMovimiento >= desde && m.FechaMovimiento <= hasta);

        if (bodegaId.HasValue)
            query = query.Where(m => m.BodegaId == bodegaId.Value);

        if (sucursalId.HasValue)
            query = query.Where(m => m.Bodega.SucursalId == sucursalId.Value);

        return await query
            .GroupBy(m => m.TipoMovimiento)
            .Select(g => new ResumenMovimientoPorTipoDto
            {
                TipoMovimiento = g.Key,
                Cantidad = g.Count(),
                ValorTotal = g.Sum(m => Math.Abs(m.Cantidad) * m.CostoUnitario)
            })
            .ToListAsync();
    }

    // REPORTES DE VALORACIÓN

    public async Task<ValoracionInventarioDto> ObtenerValoracionInventarioAsync(int emisorId, int? bodegaId = null, int? sucursalId = null)
    {
        // ⚡ Raw SQL necesario - agregaciones complejas con grouping
        var bodegaFilter = bodegaId.HasValue ? $"AND sb.\"BodegaId\" = {bodegaId.Value}" : "";
        var sucursalFilterVal = sucursalId.HasValue
            ? $"AND sb.\"BodegaId\" IN (SELECT \"Id\" FROM bodegas WHERE \"SucursalId\" = {sucursalId.Value})"
            : "";

        var sql = $@"
            WITH stock_valorizado AS (
                SELECT
                    p.""Id"" as producto_id,
                    p.""Codigo"" as codigo,
                    p.""Nombre"" as nombre,
                    SUM(sb.""CantidadDisponible"" + sb.""CantidadReservada"") as cantidad,
                    AVG(sb.""CostoPromedio"") as costo_promedio,
                    SUM((sb.""CantidadDisponible"" + sb.""CantidadReservada"") * sb.""CostoPromedio"") as valor_total
                FROM ""TBL_ProductosServicios"" p
                INNER JOIN stock_bodegas sb ON p.""Id"" = sb.""ProductoId""
                WHERE (sb.""CantidadDisponible"" + sb.""CantidadReservada"") > 0
                  AND p.""EmisorId"" = {0}
                  {bodegaFilter}
                  {sucursalFilterVal}
                GROUP BY p.""Id"", p.""Codigo"", p.""Nombre""
            )
            SELECT
                sv.producto_id as ""ProductoId"",
                sv.codigo as ""ProductoCodigo"",
                sv.nombre as ""ProductoNombre"",
                sv.cantidad as ""Cantidad"",
                sv.costo_promedio as ""CostoPromedio"",
                sv.valor_total as ""ValorTotal"",
                (sv.valor_total / NULLIF((SELECT SUM(valor_total) FROM stock_valorizado), 0) * 100) as ""PorcentajeDelTotal""
            FROM stock_valorizado sv
            ORDER BY sv.valor_total DESC";

        var detalles = await _context.Database
            .SqlQueryRaw<ValoracionPorProductoDto>(sql, emisorId)
            .ToListAsync();

        string? bodegaNombre = null;
        if (bodegaId.HasValue)
        {
            var bodega = await _context.Bodegas.FindAsync(bodegaId.Value);
            bodegaNombre = bodega?.Nombre;
        }

        return new ValoracionInventarioDto
        {
            BodegaId = bodegaId,
            BodegaNombre = bodegaNombre,
            TotalProductos = detalles.Count,
            CantidadTotalUnidades = detalles.Sum(d => d.Cantidad),
            ValorTotal = detalles.Sum(d => d.ValorTotal),
            DetalleProductos = detalles
        };
    }

    public async Task<decimal> ObtenerCostoMercanciaVendidaAsync(int emisorId, DateTime desde, DateTime hasta, int? sucursalId = null, int? bodegaId = null)
    {
        // ✅ LINQ funciona bien - sum simple
        var query = _context.MovimientosInventario
            .Include(m => m.Bodega)
                .ThenInclude(b => b.Sucursal)
            .Where(m => m.Bodega!.Sucursal!.EmisorId == emisorId)
            .Where(m => m.TipoMovimiento == "SALIDA")
            .Where(m => m.FechaMovimiento >= desde && m.FechaMovimiento <= hasta);

        List<int>? bodegaIds = null;

        if (sucursalId.HasValue)
        {
            bodegaIds = await _context.Bodegas
                .Where(b => b.SucursalId == sucursalId.Value)
                .Select(b => b.Id)
                .ToListAsync();
        }
        else if (bodegaId.HasValue)
        {
            bodegaIds = new List<int> { bodegaId.Value };
        }

        if (bodegaIds != null && bodegaIds.Any())
        {
            query = query.Where(m => bodegaIds.Contains(m.BodegaId));
        }

        var total = await query.SumAsync(m => (decimal?)(Math.Abs(m.Cantidad) * m.CostoUnitario));

        return total ?? 0;
    }

    public async Task<List<RotacionAbcItemDto>> ObtenerRotacionAbcAsync(int emisorId, DateTime desde, DateTime hasta, int? sucursalId = null, int? bodegaId = null)
    {
        var query = _context.MovimientosInventario
            .Include(m => m.Producto)
            .Include(m => m.Bodega)
                .ThenInclude(b => b.Sucursal)
            .Where(m => m.Bodega!.Sucursal!.EmisorId == emisorId)
            .Where(m => m.TipoMovimiento == "SALIDA")
            .Where(m => m.FechaMovimiento >= desde && m.FechaMovimiento <= hasta);

        List<int>? bodegaIds = null;
        if (sucursalId.HasValue)
        {
            bodegaIds = await _context.Bodegas
                .Where(b => b.SucursalId == sucursalId.Value)
                .Select(b => b.Id)
                .ToListAsync();
        }
        else if (bodegaId.HasValue)
        {
            bodegaIds = new List<int> { bodegaId.Value };
        }

        if (bodegaIds != null && bodegaIds.Any())
        {
            query = query.Where(m => bodegaIds.Contains(m.BodegaId));
        }

        var movimientos = await query
            .Select(m => new
            {
                m.ProductoId,
                m.Producto!.Codigo,
                m.Producto.Nombre,
                m.Producto.PrecioVenta,
                m.Cantidad
            })
            .ToListAsync();

        var agrupados = movimientos
            .GroupBy(m => new { m.ProductoId, m.Codigo, m.Nombre, m.PrecioVenta })
            .Select(g => new
            {
                g.Key.ProductoId,
                g.Key.Codigo,
                g.Key.Nombre,
                Unidades = g.Sum(x => Math.Abs(x.Cantidad)),
                Valor = g.Sum(x => Math.Abs(x.Cantidad) * g.Key.PrecioVenta)
            })
            .OrderByDescending(x => x.Valor)
            .ToList();

        var result = new List<RotacionAbcItemDto>();
        var totalValor = agrupados.Sum(x => x.Valor);
        if (totalValor == 0) return result;

        decimal acumulado = 0;
        foreach (var item in agrupados)
        {
            acumulado += item.Valor;
            var porcentaje = Math.Round((acumulado / totalValor) * 100, 2);
            var clasificacion = porcentaje <= 80 ? "A" : porcentaje <= 95 ? "B" : "C";

            result.Add(new RotacionAbcItemDto
            {
                ProductoId = item.ProductoId,
                ProductoCodigo = item.Codigo,
                ProductoNombre = item.Nombre,
                UnidadesVendidas = item.Unidades,
                ValorVendido = item.Valor,
                PorcentajeAcumulado = porcentaje,
                Clasificacion = clasificacion
            });
        }

        return result;
    }

    public async Task<List<RotacionProductoDto>> ObtenerRotacionInventarioAsync(int emisorId, int meses = 12, int? sucursalId = null, int? bodegaId = null)
    {
        // ⚡ Raw SQL necesario - window functions y cálculos complejos
        var fechaDesde = DateTime.UtcNow.AddMonths(-meses);

        // Resolver bodegaIds desde sucursalId o bodegaId
        List<int>? bodegaIds = null;
        if (sucursalId.HasValue)
        {
            bodegaIds = await _context.Bodegas
                .Where(b => b.SucursalId == sucursalId.Value)
                .Select(b => b.Id)
                .ToListAsync();
        }
        else if (bodegaId.HasValue)
        {
            bodegaIds = new List<int> { bodegaId.Value };
        }

        var bodegaFilterVentas = bodegaIds != null && bodegaIds.Any()
            ? $"AND \"BodegaId\" IN ({string.Join(",", bodegaIds)})"
            : "";

        var bodegaFilterStocksFixed = bodegaIds != null && bodegaIds.Any()
            ? $"AND sb.\"BodegaId\" IN ({string.Join(",", bodegaIds)})"
            : "";

        var sql = $@"
            WITH ventas AS (
                SELECT 
                    ""ProductoId"",
                    SUM(ABS(""Cantidad"")) as cantidad_vendida
                FROM movimientos_inventario
                WHERE ""TipoMovimiento"" = 'SALIDA'
                  AND ""FechaMovimiento"" >= {{0}}
                  {bodegaFilterVentas}
                GROUP BY ""ProductoId""
            ),
            stocks_promedio AS (
                SELECT 
                    sb.""ProductoId"",
                    AVG(sb.""CantidadDisponible"" + sb.""CantidadReservada"") as promedio_stock
                FROM stock_bodegas sb
                LEFT JOIN bodegas b ON sb.""BodegaId"" = b.""Id""
                WHERE b.""SucursalId"" IN (SELECT ""Id"" FROM ""TBL_Sucursales"" WHERE ""EmisorId"" = {{2}})
                   {bodegaFilterStocksFixed}
                GROUP BY sb.""ProductoId""
            )
            SELECT 
                p.""Id"" as ""ProductoId"",
                p.""Codigo"" as ""ProductoCodigo"",
                p.""Nombre"" as ""ProductoNombre"",
                COALESCE(v.cantidad_vendida, 0) as ""CantidadVendida"",
                COALESCE(sp.promedio_stock, 0) as ""PromedioStock"",
                CASE 
                    WHEN COALESCE(sp.promedio_stock, 0) > 0 
                    THEN COALESCE(v.cantidad_vendida, 0) / sp.promedio_stock
                    ELSE 0
                END as ""IndiceRotacion"",
                CASE 
                    WHEN COALESCE(v.cantidad_vendida, 0) / NULLIF(sp.promedio_stock, 0) > 0
                    THEN CAST((365.0 * {{1}} / {{1}}) / (COALESCE(v.cantidad_vendida, 0) / sp.promedio_stock) AS INTEGER)
                    ELSE 0
                END as ""DiasPromediInventario"",
                CASE 
                    WHEN COALESCE(v.cantidad_vendida, 0) / NULLIF(sp.promedio_stock, 0) >= 6 THEN 'A'
                    WHEN COALESCE(v.cantidad_vendida, 0) / NULLIF(sp.promedio_stock, 0) >= 3 THEN 'B'
                    ELSE 'C'
                END as ""Clasificacion""
            FROM ""TBL_ProductosServicios"" p
            LEFT JOIN ventas v ON p.""Id"" = v.""ProductoId""
            LEFT JOIN stocks_promedio sp ON p.""Id"" = sp.""ProductoId""
            WHERE p.""Activo"" = true
              AND p.""EmisorId"" = {{2}}
              AND COALESCE(v.cantidad_vendida, 0) > 0
              AND COALESCE(sp.promedio_stock, 0) > 0
            ORDER BY ""IndiceRotacion"" DESC";

        return await _context.Database
            .SqlQueryRaw<RotacionProductoDto>(sql, fechaDesde, meses, emisorId)
            .ToListAsync();
    }

    // DASHBOARDS

    public async Task<InventarioKPIsDto> ObtenerKPIsAsync(int emisorId, int? sucursalId = null)
    {
        var hoy = DateTime.UtcNow;
        var inicioMes = new DateTime(hoy.Year, hoy.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var finMes = inicioMes.AddMonths(1).AddDays(-1);

        // ✅ Queries simples con LINQ
        var totalProductos = await _context.ProductosServicios.CountAsync(p => p.Activo && p.EmisorId == emisorId);
        var valoracion = await ObtenerValoracionInventarioAsync(emisorId, null, sucursalId);
        var productosBajoMinimo = await ObtenerProductosBajoMinimoAsync(emisorId, sucursalId);

        var productosSinStock = await _context.Database
            .SqlQueryRaw<int>(@"
                SELECT COUNT(DISTINCT p.""Id"")::integer as ""Value""
                FROM ""TBL_ProductosServicios"" p
                LEFT JOIN stock_bodegas sb ON p.""Id"" = sb.""ProductoId"" AND (sb.""CantidadDisponible"" + sb.""CantidadReservada"") > 0
                WHERE p.""Activo"" = true AND sb.""Id"" IS NULL AND p.""EmisorId"" = {0}
            ", emisorId)
            .FirstOrDefaultAsync();

        var movimientosStats = await _context.MovimientosInventario
            .Include(m => m.Bodega)
                .ThenInclude(b => b.Sucursal)
            .Where(m => m.Bodega!.Sucursal!.EmisorId == emisorId)
            .Where(m => m.FechaMovimiento >= inicioMes && m.FechaMovimiento <= finMes)
            .GroupBy(m => m.TipoMovimiento)
            .Select(g => new { Tipo = g.Key, Count = g.Count() })
            .ToListAsync();

        var totalMovimientos = movimientosStats.Sum(m => m.Count);
        var totalEntradas = movimientosStats.FirstOrDefault(m => m.Tipo == "ENTRADA")?.Count ?? 0;
        var totalSalidas = movimientosStats.FirstOrDefault(m => m.Tipo == "SALIDA")?.Count ?? 0;

        var cmvMesActual = await ObtenerCostoMercanciaVendidaAsync(emisorId, inicioMes, finMes);

        var valorCompras = await _context.ComprasExternas
            .Include(c => c.Sucursal)
            .Where(c => c.Estado == "CONFIRMADA")
            .Where(c => c.Sucursal.EmisorId == emisorId)
            .Where(c => c.FechaEmision >= inicioMes && c.FechaEmision <= finMes)
            .SumAsync(c => (decimal?)c.Total) ?? 0;

        var rotaciones = await ObtenerRotacionInventarioAsync(emisorId, 12);
        var rotacionPromedio = rotaciones.Any() ? rotaciones.Average(r => r.IndiceRotacion) : 0;
        var diasPromedio = rotaciones.Any() ? (int)rotaciones.Average(r => r.DiasPromediInventario) : 0;

        return new InventarioKPIsDto
        {
            TotalProductos = totalProductos,
            ValorTotalInventario = valoracion.ValorTotal,
            ProductosBajoMinimo = productosBajoMinimo.Count,
            ProductosSinStock = productosSinStock,
            TotalMovimientosUltimoMes = totalMovimientos,
            TotalEntradasUltimoMes = totalEntradas,
            TotalSalidasUltimoMes = totalSalidas,
            CostoMercanciaVendidaMesActual = cmvMesActual,
            ValorComprasMesActual = valorCompras,
            RotacionPromedioAnual = rotacionPromedio,
            DiasPromedioInventario = diasPromedio
        };
    }

    // MÉTODOS AUXILIARES

    private async Task<decimal> ObtenerSaldoInicialAsync(int productoId, int emisorId, int? bodegaId, DateTime fecha)
    {
        // ✅ LINQ funciona bien
        var query = _context.MovimientosInventario
            .Include(m => m.Bodega)
                .ThenInclude(b => b.Sucursal)
            .Where(m => m.ProductoId == productoId)
            .Where(m => m.Bodega!.Sucursal!.EmisorId == emisorId)
            .Where(m => m.FechaMovimiento < fecha);

        if (bodegaId.HasValue)
            query = query.Where(m => m.BodegaId == bodegaId.Value);

        var ultimoMovimiento = await query
            .OrderByDescending(m => m.FechaMovimiento)
            .FirstOrDefaultAsync();

        return ultimoMovimiento?.NuevoSaldo ?? 0;
    }
}

