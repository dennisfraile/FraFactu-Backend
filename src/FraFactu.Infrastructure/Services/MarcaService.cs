using FraFactu.Application.DTOs.Common;
using FraFactu.Application.DTOs.Inventario;
using FraFactu.Application.Interfaces;
using FraFactu.Infrastructure.Helpers.Pagination;
using FraFactu.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FraFactu.Infrastructure.Services;

public class MarcaService : IMarcaService
{
    private readonly ApplicationDbContext _context;

    public MarcaService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResponse<MarcaDto>> GetAllAsync(PaginatedRequest request, int emisorId, bool? soloActivas = null)
    {
        var query = _context.Marcas
            .Where(m => m.EmisorId == emisorId)
            .AsQueryable();

        if (soloActivas == true)
            query = query.Where(m => m.Activa);

        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLower();
            query = query.Where(m => m.Nombre.ToLower().Contains(term));
        }

        IOrderedQueryable<Domain.Entities.Marca> orderedQuery = request.OrderBy?.ToLower() switch
        {
            "fechacreacion" => request.OrderDirection == "desc" ? query.OrderByDescending(m => m.FechaCreacion) : query.OrderBy(m => m.FechaCreacion),
            _ => request.OrderDirection == "desc" ? query.OrderByDescending(m => m.Nombre) : query.OrderBy(m => m.Nombre)
        };

        var pagedItems = await orderedQuery
            .Select(m => new MarcaDto
            {
                Id = m.Id,
                Nombre = m.Nombre,
                Descripcion = m.Descripcion,
                Activa = m.Activa,
                FechaCreacion = m.FechaCreacion
            })
            .ToPaginatedListAsync(request.PageNumber, request.PageSize);

        return new PaginatedResponse<MarcaDto>
        {
            Items = pagedItems.ToList(),
            CurrentPage = pagedItems.CurrentPage,
            PageSize = pagedItems.PageSize,
            TotalCount = pagedItems.TotalCount,
            TotalPages = pagedItems.TotalPages
        };
    }
}
