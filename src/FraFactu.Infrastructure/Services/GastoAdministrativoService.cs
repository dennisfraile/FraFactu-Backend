using Microsoft.EntityFrameworkCore;
using FraFactu.Application.DTOs.Compras;
using FraFactu.Application.Common;
using FraFactu.Application.Services;
using FraFactu.Domain.Entities;
using FraFactu.Infrastructure.Persistence;
using FraFactu.Infrastructure.Helpers;

namespace FraFactu.Infrastructure.Services;

public class GastoAdministrativoService : IGastoAdministrativoService
{
    private readonly ApplicationDbContext _context;

    public GastoAdministrativoService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<GastoAdministrativoDto>> ObtenerPorPeriodoAsync(
        DateTime desde,
        DateTime hasta,
        int? tipoGastoId = null,
        string? centroCosto = null,
        int? sucursalId = null,
        int pagina = 1,
        int tamanoPagina = 10000,
        string? search = null,
        List<int>? sucursalIds = null,
        string? sortBy = null,
        bool sortDesc = false)
    {
        desde = FechaHelper.ToUtc(desde);
        hasta = FechaHelper.ToUtc(hasta);

        var query = _context.GastosAdministrativos
            .Include(g => g.TipoGasto)
            .Include(g => g.CompraExterna)
                .ThenInclude(c => c.Proveedor)
            .Include(g => g.CompraExterna.Sucursal)
            .Where(g => g.CompraExterna.Estado == "CONFIRMADA")
            .Where(g => g.CompraExterna.FechaEmision >= desde && g.CompraExterna.FechaEmision <= hasta);

        if (tipoGastoId.HasValue)
            query = query.Where(g => g.CatTipoGastoId == tipoGastoId.Value);

        if (!string.IsNullOrWhiteSpace(centroCosto))
            query = query.Where(g => g.CentroCosto == centroCosto);

        if (sucursalIds != null && sucursalIds.Any())
            query = query.Where(g => sucursalIds.Contains(g.CompraExterna.SucursalId));
        else if (sucursalId.HasValue)
            query = query.Where(g => g.CompraExterna.SucursalId == sucursalId.Value);

        if (!string.IsNullOrEmpty(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(g =>
                (g.Descripcion != null && g.Descripcion.ToLower().Contains(searchLower)) ||
                (g.TipoGasto != null && g.TipoGasto.Nombre.ToLower().Contains(searchLower)) ||
                (g.CentroCosto != null && g.CentroCosto.ToLower().Contains(searchLower)) ||
                (g.CompraExterna.Proveedor != null && g.CompraExterna.Proveedor.Nombre.ToLower().Contains(searchLower)));
        }

        var totalItems = await query.CountAsync();

        IOrderedQueryable<GastoAdministrativo> orderedQuery = sortBy?.ToLower() switch
        {
            "tipogasto" => sortDesc ? query.OrderByDescending(g => g.TipoGasto != null ? g.TipoGasto.Nombre : "") : query.OrderBy(g => g.TipoGasto != null ? g.TipoGasto.Nombre : ""),
            "monto" => sortDesc ? query.OrderByDescending(g => g.Monto) : query.OrderBy(g => g.Monto),
            "centrocosto" => sortDesc ? query.OrderByDescending(g => g.CentroCosto) : query.OrderBy(g => g.CentroCosto),
            "cuentacontable" => sortDesc ? query.OrderByDescending(g => g.CuentaContable) : query.OrderBy(g => g.CuentaContable),
            "descripcion" => sortDesc ? query.OrderByDescending(g => g.Descripcion) : query.OrderBy(g => g.Descripcion),
            "fechacreacion" => sortDesc ? query.OrderByDescending(g => g.FechaCreacion) : query.OrderBy(g => g.FechaCreacion),
            _ => sortDesc ? query.OrderByDescending(g => g.FechaCreacion) : query.OrderBy(g => g.FechaCreacion)
        };

        var items = await orderedQuery
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .Select(g => new GastoAdministrativoDto
            {
                Id = g.Id,
                CompraExternaId = g.CompraExternaId,
                CatTipoGastoId = g.CatTipoGastoId,
                TipoGastoNombre = g.TipoGasto != null ? g.TipoGasto.Nombre : null,
                Descripcion = g.Descripcion,
                Monto = g.Monto,
                CentroCosto = g.CentroCosto,
                CuentaContable = g.CuentaContable,
                FechaCreacion = g.FechaCreacion
            })
            .ToListAsync();

        return new PagedResult<GastoAdministrativoDto>
        {
            Items = items,
            TotalItems = totalItems,
            PageNumber = pagina,
            PageSize = tamanoPagina
        };
    }

    public async Task<decimal> ObtenerTotalGastosAsync(
        DateTime desde,
        DateTime hasta,
        int? tipoGastoId = null,
        int? sucursalId = null,
        List<int>? sucursalIds = null)
    {
        desde = FechaHelper.ToUtc(desde);
        hasta = FechaHelper.ToUtc(hasta);

        var query = _context.GastosAdministrativos
            .Include(g => g.CompraExterna)
            .Where(g => g.CompraExterna.Estado == "CONFIRMADA")
            .Where(g => g.CompraExterna.FechaEmision >= desde && g.CompraExterna.FechaEmision <= hasta);

        if (tipoGastoId.HasValue)
            query = query.Where(g => g.CatTipoGastoId == tipoGastoId.Value);

        if (sucursalIds != null && sucursalIds.Any())
            query = query.Where(g => sucursalIds.Contains(g.CompraExterna.SucursalId));
        else if (sucursalId.HasValue)
            query = query.Where(g => g.CompraExterna.SucursalId == sucursalId.Value);

        return await query.SumAsync(g => g.Monto);
    }

    public async Task<Dictionary<string, decimal>> ObtenerGastosPorTipoAsync(DateTime desde, DateTime hasta)
    {
        desde = FechaHelper.ToUtc(desde);
        hasta = FechaHelper.ToUtc(hasta);

        var gastos = await _context.GastosAdministrativos
            .Include(g => g.TipoGasto)
            .Include(g => g.CompraExterna)
            .Where(g => g.CompraExterna.Estado == "CONFIRMADA")
            .Where(g => g.CompraExterna.FechaEmision >= desde && g.CompraExterna.FechaEmision <= hasta)
            .ToListAsync();

        return gastos
            .GroupBy(g => g.TipoGasto?.Nombre ?? "Sin categoría")
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Monto));
    }

    public async Task<Dictionary<string, decimal>> ObtenerGastosPorCentroCostoAsync(DateTime desde, DateTime hasta)
    {
        desde = FechaHelper.ToUtc(desde);
        hasta = FechaHelper.ToUtc(hasta);

        var gastos = await _context.GastosAdministrativos
            .Include(g => g.CompraExterna)
            .Where(g => g.CompraExterna.Estado == "CONFIRMADA")
            .Where(g => g.CompraExterna.FechaEmision >= desde && g.CompraExterna.FechaEmision <= hasta)
            .ToListAsync();

        return gastos
            .GroupBy(g => g.CentroCosto ?? "Sin centro de costo")
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Monto));
    }
}
