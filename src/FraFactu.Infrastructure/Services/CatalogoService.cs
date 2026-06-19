using FraFactu.Application.Common.Interfaces;
using FraFactu.Domain.Entities.Catalogos;
using FraFactu.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FraFactu.Infrastructure.Services;

public class CatalogoService : ICatalogoService
{
    private readonly ApplicationDbContext _context;

    public CatalogoService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<T>> GetCatalogoAsync<T>() where T : CatalogoBase
    {
        // AsNoTracking() es vital para lectura rápida (no guarda caché de cambios)
        return await _context.Set<T>()
                             .AsNoTracking()
                             .OrderBy(x => x.Id)
                             .ToListAsync();
    }
}