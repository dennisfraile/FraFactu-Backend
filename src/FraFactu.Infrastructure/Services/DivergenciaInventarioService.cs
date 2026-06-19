using FraFactu.Application.DTOs.Integraciones;
using FraFactu.Application.Services;
using FraFactu.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FraFactu.Infrastructure.Services;

/// <summary>
/// F4 (Plan inventario desde DTE): consulta de divergencias persistidas por
/// el job de reconciliacion. Lee y filtra; no modifica nada.
/// </summary>
public class DivergenciaInventarioService : IDivergenciaInventarioService
{
    private readonly ApplicationDbContext _context;

    public DivergenciaInventarioService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DivergenciasListadoDto> ListarAsync(int emisorId, DivergenciasFiltroDto filtro)
    {
        var pagina = Math.Max(1, filtro.Pagina);
        var tamano = Math.Clamp(filtro.TamanoPagina, 1, 200);

        var query = _context.DivergenciasInventario
            .AsNoTracking()
            .Where(d => d.EmisorId == emisorId);

        if (filtro.DesdeFecha.HasValue)
            query = query.Where(d => d.UltimaDeteccion >= filtro.DesdeFecha.Value);
        if (filtro.HastaFecha.HasValue)
            query = query.Where(d => d.UltimaDeteccion <= filtro.HastaFecha.Value);
        if (filtro.Estado.HasValue)
            query = query.Where(d => d.Estado == filtro.Estado.Value);
        if (filtro.EjecucionId.HasValue)
            query = query.Where(d => d.EjecucionId == filtro.EjecucionId.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(d => d.UltimaDeteccion)
            .ThenBy(d => d.Id)
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .Select(d => new DivergenciaInventarioDto
            {
                Id = d.Id,
                EjecucionId = d.EjecucionId,
                EmisorId = d.EmisorId,
                CodigoProducto = d.CodigoProducto,
                NombreBodega = d.NombreBodega,
                StockSmartix = d.StockSmartix,
                StockSmartInventory = d.StockSmartInventory,
                Diff = d.Diff,
                Estado = d.Estado.ToString(),
                UltimaDeteccion = d.UltimaDeteccion,
                FechaResolucion = d.FechaResolucion,
                FechaCreacion = d.FechaCreacion
            })
            .ToListAsync();

        return new DivergenciasListadoDto
        {
            Items = items,
            Total = total,
            Pagina = pagina,
            TamanoPagina = tamano
        };
    }
}
