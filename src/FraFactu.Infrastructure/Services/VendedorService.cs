using FraFactu.Application.Interfaces;
using FraFactu.Domain.Entities;
using FraFactu.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using FraFactu.Infrastructure.Services.Helpers;

namespace FraFactu.Infrastructure.Services;

public class VendedorService : IVendedorService
{
    private readonly ApplicationDbContext _context;

    public VendedorService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Vendedor>> GetAllAsync(int emisorId, bool activeOnly = true, int? sucursalId = null, List<int>? sucursalIds = null)
    {
        var query = _context.Vendedores
            .Include(v => v.VendedorSucursales)
                .ThenInclude(vs => vs.Sucursal)
            .Where(v => v.EmisorId == emisorId)
            .AsNoTracking();

        if (activeOnly)
        {
            query = query.Where(v => v.Activo);
        }

        if (sucursalIds != null && sucursalIds.Count > 0)
        {
            query = query.Where(v => v.AccesoTodasSucursales
                || v.VendedorSucursales.Any(vs => sucursalIds.Contains(vs.SucursalId)));
        }
        else if (sucursalId.HasValue)
        {
            query = query.Where(v => v.AccesoTodasSucursales
                || v.VendedorSucursales.Any(vs => vs.SucursalId == sucursalId.Value));
        }

        return await query.OrderBy(v => v.Nombre).ToListAsync();
    }

    public async Task<Vendedor?> GetByIdAsync(int id)
    {
        return await _context.Vendedores
            .Include(v => v.VendedorSucursales)
                .ThenInclude(vs => vs.Sucursal)
            .FirstOrDefaultAsync(v => v.Id == id);
    }

    public async Task<Vendedor> CreateAsync(Vendedor vendedor, bool accesoTodasSucursales, List<int> sucursalIds)
    {
        // Generación automática de código si no se proporciona
        if (string.IsNullOrWhiteSpace(vendedor.Codigo))
        {
            var lastEntity = await _context.Vendedores
                .Where(x => x.EmisorId == vendedor.EmisorId)
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();

            var totalCount = await _context.Vendedores
                .CountAsync(x => x.EmisorId == vendedor.EmisorId);

            vendedor.Codigo = CodeGeneratorHelper.GenerateNextCode("VEND", lastEntity?.Codigo, totalCount);
        }

        // Validar unicidad de código (solo entre activos)
        if (await _context.Vendedores.AnyAsync(v => v.EmisorId == vendedor.EmisorId && v.Codigo == vendedor.Codigo && v.Activo))
        {
            throw new InvalidOperationException($"Ya existe un vendedor con el código {vendedor.Codigo}");
        }

        vendedor.AccesoTodasSucursales = accesoTodasSucursales;

        // Validar sucursales pertenecen al emisor
        if (!accesoTodasSucursales && sucursalIds.Count > 0)
        {
            var sucursalesValidas = await _context.Sucursales
                .Where(s => sucursalIds.Contains(s.Id) && s.EmisorId == vendedor.EmisorId)
                .CountAsync();

            if (sucursalesValidas != sucursalIds.Count)
                throw new InvalidOperationException("Una o más sucursales no pertenecen al emisor.");
        }

        _context.Vendedores.Add(vendedor);
        await _context.SaveChangesAsync();

        // Guardar sucursales asignadas
        if (!accesoTodasSucursales && sucursalIds.Count > 0)
        {
            foreach (var sid in sucursalIds)
            {
                _context.VendedoresSucursales.Add(new VendedorSucursal
                {
                    VendedorId = vendedor.Id,
                    SucursalId = sid
                });
            }
            await _context.SaveChangesAsync();
        }

        // Recargar relaciones
        await _context.Entry(vendedor).Collection(v => v.VendedorSucursales).LoadAsync();
        foreach (var vs in vendedor.VendedorSucursales)
        {
            await _context.Entry(vs).Reference(x => x.Sucursal).LoadAsync();
        }

        return vendedor;
    }

    public async Task<Vendedor> UpdateAsync(int id, Vendedor vendedor, bool accesoTodasSucursales, List<int> sucursalIds)
    {
        var existing = await _context.Vendedores
            .Include(v => v.VendedorSucursales)
            .FirstOrDefaultAsync(v => v.Id == id);
        if (existing == null) throw new KeyNotFoundException($"Vendedor con ID {id} no encontrado");

        // Validar unicidad si cambia el código
        if (existing.Codigo != vendedor.Codigo &&
            await _context.Vendedores.AnyAsync(v => v.EmisorId == existing.EmisorId && v.Codigo == vendedor.Codigo && v.Activo))
        {
            throw new InvalidOperationException($"Ya existe un vendedor con el código {vendedor.Codigo}");
        }

        existing.Nombre = vendedor.Nombre;
        existing.Codigo = vendedor.Codigo;
        existing.PorcentajeComision = vendedor.PorcentajeComision;
        existing.AccesoTodasSucursales = accesoTodasSucursales;

        // Validar sucursales pertenecen al emisor
        if (!accesoTodasSucursales && sucursalIds.Count > 0)
        {
            var sucursalesValidas = await _context.Sucursales
                .Where(s => sucursalIds.Contains(s.Id) && s.EmisorId == existing.EmisorId)
                .CountAsync();

            if (sucursalesValidas != sucursalIds.Count)
                throw new InvalidOperationException("Una o más sucursales no pertenecen al emisor.");
        }

        // Reemplazar sucursales: remove-all + re-add
        _context.VendedoresSucursales.RemoveRange(existing.VendedorSucursales);

        if (!accesoTodasSucursales && sucursalIds.Count > 0)
        {
            foreach (var sid in sucursalIds)
            {
                _context.VendedoresSucursales.Add(new VendedorSucursal
                {
                    VendedorId = existing.Id,
                    SucursalId = sid
                });
            }
        }

        await _context.SaveChangesAsync();

        // Recargar relaciones
        await _context.Entry(existing).Collection(v => v.VendedorSucursales).LoadAsync();
        foreach (var vs in existing.VendedorSucursales)
        {
            await _context.Entry(vs).Reference(x => x.Sucursal).LoadAsync();
        }

        return existing;
    }

    public async Task ToggleActiveAsync(int id)
    {
        var existing = await _context.Vendedores.FindAsync(id);
        if (existing == null) throw new KeyNotFoundException($"Vendedor con ID {id} no encontrado");

        existing.Activo = !existing.Activo;
        await _context.SaveChangesAsync();
    }
}
