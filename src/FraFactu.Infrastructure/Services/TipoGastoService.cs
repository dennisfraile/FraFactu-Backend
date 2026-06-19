using FraFactu.Application.DTOs.Common;
using FraFactu.Application.DTOs.Inventario;
using FraFactu.Application.Interfaces;
using FraFactu.Infrastructure.Helpers.Pagination;
using FraFactu.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FraFactu.Infrastructure.Services;

public class TipoGastoService : ITipoGastoService
{
    private readonly ApplicationDbContext _context;

    public TipoGastoService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResponse<TipoGastoDto>> GetAllAsync(PaginatedRequest request, int emisorId, bool? soloActivos = null)
    {
        var query = _context.CatTiposGasto
            .Where(t => t.EmisorId == emisorId)
            .AsQueryable();

        if (soloActivos == true)
            query = query.Where(t => t.Activo);

        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLower();
            query = query.Where(t =>
                t.Nombre.ToLower().Contains(term) ||
                t.Codigo.ToLower().Contains(term));
        }

        IOrderedQueryable<Domain.Entities.Catalogos.CatTipoGasto> orderedQuery = request.OrderBy?.ToLower() switch
        {
            "codigo" => request.OrderDirection == "desc" ? query.OrderByDescending(t => t.Codigo) : query.OrderBy(t => t.Codigo),
            "fechacreacion" => request.OrderDirection == "desc" ? query.OrderByDescending(t => t.FechaCreacion) : query.OrderBy(t => t.FechaCreacion),
            _ => request.OrderDirection == "desc" ? query.OrderByDescending(t => t.Nombre) : query.OrderBy(t => t.Nombre)
        };

        var pagedItems = await orderedQuery
            .Select(t => new TipoGastoDto
            {
                Id = t.Id,
                Codigo = t.Codigo,
                Nombre = t.Nombre,
                Descripcion = t.Descripcion,
                Activo = t.Activo,
                FechaCreacion = t.FechaCreacion
            })
            .ToPaginatedListAsync(request.PageNumber, request.PageSize);

        return new PaginatedResponse<TipoGastoDto>
        {
            Items = pagedItems.ToList(),
            CurrentPage = pagedItems.CurrentPage,
            PageSize = pagedItems.PageSize,
            TotalCount = pagedItems.TotalCount,
            TotalPages = pagedItems.TotalPages
        };
    }
}
