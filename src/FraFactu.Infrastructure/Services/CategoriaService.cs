using FraFactu.Application.DTOs.Common;
using FraFactu.Application.DTOs.Inventario;
using FraFactu.Application.Interfaces;
using FraFactu.Infrastructure.Helpers.Pagination;
using FraFactu.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FraFactu.Infrastructure.Services;

public class CategoriaService : ICategoriaService
{
    private readonly ApplicationDbContext _context;

    public CategoriaService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResponse<CategoriaDto>> GetAllAsync(PaginatedRequest request, int emisorId, bool? soloActivos = null, bool soloRaices = false)
    {
        var query = _context.Categorias
            .Include(c => c.CategoriaPadre)
            .Where(c => c.EmisorId == emisorId)
            .AsQueryable();

        if (soloActivos == true)
            query = query.Where(c => c.Activo);

        if (soloRaices)
            query = query.Where(c => c.CategoriaPadreId == null);

        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLower();
            query = query.Where(c =>
                c.Nombre.ToLower().Contains(term) ||
                c.Codigo.ToLower().Contains(term));
        }

        IOrderedQueryable<Domain.Entities.Categoria> orderedQuery = request.OrderBy?.ToLower() switch
        {
            "codigo" => request.OrderDirection == "desc" ? query.OrderByDescending(c => c.Codigo) : query.OrderBy(c => c.Codigo),
            "fechacreacion" => request.OrderDirection == "desc" ? query.OrderByDescending(c => c.FechaCreacion) : query.OrderBy(c => c.FechaCreacion),
            _ => request.OrderDirection == "desc" ? query.OrderByDescending(c => c.Nombre) : query.OrderBy(c => c.Nombre)
        };

        var pagedItems = await orderedQuery
            .Select(c => new CategoriaDto
            {
                Id = c.Id,
                Codigo = c.Codigo,
                Nombre = c.Nombre,
                Descripcion = c.Descripcion,
                CategoriaPadreId = c.CategoriaPadreId,
                CategoriaPadreNombre = c.CategoriaPadre != null ? c.CategoriaPadre.Nombre : null,
                Activo = c.Activo,
                FechaCreacion = c.FechaCreacion
            })
            .ToPaginatedListAsync(request.PageNumber, request.PageSize);

        return new PaginatedResponse<CategoriaDto>
        {
            Items = pagedItems.ToList(),
            CurrentPage = pagedItems.CurrentPage,
            PageSize = pagedItems.PageSize,
            TotalCount = pagedItems.TotalCount,
            TotalPages = pagedItems.TotalPages
        };
    }
}
