using Microsoft.EntityFrameworkCore;
using FraFactu.Application.DTOs.Compras;
using FraFactu.Application.Services;
using FraFactu.Application.Common;
using FraFactu.Domain.Entities;
using FraFactu.Infrastructure.Persistence;

namespace FraFactu.Infrastructure.Services;

public class ProveedorService : IProveedorService
{
    private readonly ApplicationDbContext _context;

    public ProveedorService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ProveedorDto> CrearAsync(CrearProveedorDto dto, int emisorId)
    {
        // Validar que el NIT no exista para este emisor
        var existente = await _context.Proveedores
            .AnyAsync(p => p.EmisorId == emisorId && p.NIT == dto.NIT && p.Activo);

        if (existente)
            throw new InvalidOperationException($"Ya existe un proveedor con el NIT {dto.NIT} para este emisor");

        var proveedor = new Proveedor
        {
            EmisorId = emisorId,
            NIT = dto.NIT,
            Nombre = dto.Nombre,
            NombreComercial = dto.NombreComercial,
            Direccion = dto.Direccion,
            Telefono = dto.Telefono,
            Email = dto.Email,
            Contacto = dto.Contacto,
            SitioWeb = dto.SitioWeb,
            Notas = dto.Notas,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        _context.Proveedores.Add(proveedor);
        await _context.SaveChangesAsync();

        return await MapToDtoAsync(proveedor);
    }

    public async Task<ProveedorDto> ActualizarAsync(int id, ActualizarProveedorDto dto)
    {
        var proveedor = await _context.Proveedores.FindAsync(id);
        if (proveedor == null)
            throw new KeyNotFoundException($"Proveedor con ID {id} no encontrado");

        // Si se está reactivando, validar que no exista otro activo con el mismo NIT
        if (dto.Activo && !proveedor.Activo)
        {
            var existeNit = await _context.Proveedores
                .AnyAsync(p => p.EmisorId == proveedor.EmisorId && p.NIT == proveedor.NIT && p.Activo && p.Id != id);
            if (existeNit)
                throw new InvalidOperationException($"No se puede reactivar: ya existe un proveedor activo con el NIT {proveedor.NIT}");
        }

        // Actualizar campos
        proveedor.Nombre = dto.Nombre;
        proveedor.NombreComercial = dto.NombreComercial;
        proveedor.Direccion = dto.Direccion;
        proveedor.Telefono = dto.Telefono;
        proveedor.Email = dto.Email;
        proveedor.Contacto = dto.Contacto;
        proveedor.SitioWeb = dto.SitioWeb;
        proveedor.Notas = dto.Notas;
        proveedor.Activo = dto.Activo;

        await _context.SaveChangesAsync();

        return await MapToDtoAsync(proveedor);
    }

    public async Task<ProveedorDto> ObtenerPorIdAsync(int id, int emisorId)
    {
        var proveedor = await _context.Proveedores
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id && p.EmisorId == emisorId);

        if (proveedor == null)
            throw new KeyNotFoundException($"Proveedor con ID {id} no encontrado");

        return await MapToDtoAsync(proveedor);
    }

    public async Task<ProveedorDto?> ObtenerPorNITAsync(string nit, int emisorId)
    {
        var proveedor = await _context.Proveedores
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.NIT == nit && p.EmisorId == emisorId);

        return proveedor != null ? await MapToDtoAsync(proveedor) : null;
    }

    public async Task<PagedResult<ProveedorDto>> ListarAsync(
        int emisorId,
        int pagina = 1,
        int tamanoPagina = 20,
        string? filtro = null,
        bool? soloActivos = true)
    {
        var query = _context.Proveedores
            .AsNoTracking()
            .Where(p => p.EmisorId == emisorId)
            .AsQueryable();

        // Filtro por estado
        if (soloActivos.HasValue)
            query = query.Where(p => p.Activo == soloActivos.Value);

        // Filtro por texto
        if (!string.IsNullOrWhiteSpace(filtro))
        {
            filtro = filtro.ToLower();
            query = query.Where(p =>
                p.NIT.ToLower().Contains(filtro) ||
                p.Nombre.ToLower().Contains(filtro) ||
                (p.NombreComercial != null && p.NombreComercial.ToLower().Contains(filtro)));
        }

        // Total
        var total = await query.CountAsync();

        // Paginación
        var proveedores = await query
            .OrderBy(p => p.Nombre)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToListAsync();

        var items = new List<ProveedorDto>();
        foreach (var p in proveedores)
        {
            items.Add(await MapToDtoAsync(p));
        }

        return new PagedResult<ProveedorDto>
        {
            Items = items,
            TotalItems = total,
            PageNumber = pagina,
            PageSize = tamanoPagina
        };
    }

    public async Task<bool> CambiarEstadoAsync(int id, bool activo)
    {
        var proveedor = await _context.Proveedores.FindAsync(id);
        if (proveedor == null)
            return false;

        proveedor.Activo = activo;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> EliminarAsync(int id)
    {
        var proveedor = await _context.Proveedores.FindAsync(id);
        if (proveedor == null)
            return false;

        // Verificar si tiene compras asociadas
        var tieneCompras = await _context.ComprasExternas
            .AnyAsync(c => c.ProveedorId == id);

        if (tieneCompras)
            throw new InvalidOperationException("No se puede eliminar el proveedor porque tiene compras asociadas");

        _context.Proveedores.Remove(proveedor);
        await _context.SaveChangesAsync();

        return true;
    }

    private async Task<ProveedorDto> MapToDtoAsync(Proveedor proveedor)
    {
        // Calcular estadísticas
        var compras = await _context.ComprasExternas
            .Where(c => c.ProveedorId == proveedor.Id && c.Estado == "CONFIRMADA")
            .ToListAsync();

        return new ProveedorDto
        {
            Id = proveedor.Id,
            NIT = proveedor.NIT,
            Nombre = proveedor.Nombre,
            NombreComercial = proveedor.NombreComercial,
            Direccion = proveedor.Direccion,
            Telefono = proveedor.Telefono,
            Email = proveedor.Email,
            Contacto = proveedor.Contacto,
            SitioWeb = proveedor.SitioWeb,
            Notas = proveedor.Notas,
            Activo = proveedor.Activo,
            TotalCompras = compras.Count,
            MontoTotalCompras = compras.Sum(c => c.Total),
            UltimaCompra = compras.Any() ? compras.Max(c => c.FechaEmision) : null,
            FechaCreacion = proveedor.FechaCreacion
        };
    }
}
