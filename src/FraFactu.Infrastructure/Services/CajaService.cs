using FraFactu.Domain.Entities;
using FraFactu.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FraFactu.Infrastructure.Services.Helpers;

namespace FraFactu.Infrastructure.Services;

public interface ICajaService
{
    Task<List<Caja>> GetAllAsync(int emisorId, int? sucursalId = null);
    Task<Caja?> GetByIdAsync(int id);
    Task<Caja> CreateAsync(Caja caja, int emisorId);
    Task<Caja> UpdateAsync(int id, Caja caja);
    Task DeleteAsync(int id);
    Task<bool> ToggleActiveAsync(int id);
    Task AsignarCajaAUsuarioAsync(int usuarioId, int cajaId, int usuarioAsignadorId);
    Task DesasignarCajaDeUsuarioAsync(int usuarioId, int usuarioAsignadorId);
}

public class CajaService : ICajaService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CajaService> _logger;

    public CajaService(ApplicationDbContext context, ILogger<CajaService> logger)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<List<Caja>> GetAllAsync(int emisorId, int? sucursalId = null)
    {
        var query = _context.Cajas
            .Include(c => c.Sucursal)
            .Where(c => c.Sucursal.EmisorId == emisorId)
            .AsNoTracking();

        if (sucursalId.HasValue)
        {
            query = query.Where(c => c.SucursalId == sucursalId.Value);
        }

        return await query.OrderBy(c => c.Nombre).ToListAsync();
    }

    public async Task<Caja?> GetByIdAsync(int id)
    {
        return await _context.Cajas
            .Include(c => c.Sucursal)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Caja> CreateAsync(Caja caja, int emisorId)
    {
        // Validar que sucursal exista
        var sucursal = await _context.Sucursales.FindAsync(caja.SucursalId);
        if (sucursal == null) throw new KeyNotFoundException("Sucursal no encontrada");

        // Validar que la sucursal pertenezca al emisor (SECURITY CHECK)
        if (sucursal.EmisorId != emisorId)
        {
            throw new UnauthorizedAccessException("La sucursal no pertenece al emisor actual");
        }



        // Generación automática de código si no se proporciona
        if (string.IsNullOrWhiteSpace(caja.Codigo))
        {
            var lastEntity = await _context.Cajas
                .Where(x => x.SucursalId == caja.SucursalId)
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();

            var totalCount = await _context.Cajas
                .CountAsync(x => x.SucursalId == caja.SucursalId);

            caja.Codigo = CodeGeneratorHelper.GenerateNextCode("CAJA", lastEntity?.Codigo, totalCount);
        }

        // Auto-generación de CodPuntoVenta con formato P### si no se proporciona
        if (string.IsNullOrWhiteSpace(caja.CodPuntoVenta))
        {
            var lastCaja = await _context.Cajas
                .Where(x => x.SucursalId == caja.SucursalId && x.CodPuntoVenta.StartsWith("P"))
                .OrderByDescending(x => x.CodPuntoVenta)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastCaja != null && lastCaja.CodPuntoVenta.Length == 4
                && int.TryParse(lastCaja.CodPuntoVenta.Substring(1), out int lastNum))
            {
                nextNumber = lastNum + 1;
            }

            caja.CodPuntoVenta = $"P{nextNumber:D3}";
        }

        // Sincronizar: Codigo = CodPuntoVenta si fue auto-generado
        if (string.IsNullOrWhiteSpace(caja.Codigo) || caja.Codigo.StartsWith("CAJA"))
        {
            caja.Codigo = caja.CodPuntoVenta;
        }

        // Validar unicidad de código en la sucursal
        if (await _context.Cajas.AnyAsync(c => c.SucursalId == caja.SucursalId && c.Codigo == caja.Codigo && c.Activo && !string.IsNullOrEmpty(caja.Codigo)))
        {
            throw new InvalidOperationException($"Ya existe una caja con el código {caja.Codigo} en esta sucursal");
        }

        // Validar unicidad de CodPuntoVenta en la sucursal
        if (await _context.Cajas.AnyAsync(c => c.SucursalId == caja.SucursalId && c.CodPuntoVenta == caja.CodPuntoVenta && c.Activo && !string.IsNullOrEmpty(caja.CodPuntoVenta)))
        {
            throw new InvalidOperationException($"Ya existe una caja con el código de punto de venta {caja.CodPuntoVenta} en esta sucursal");
        }

        // Validar unicidad de CodPuntoVentaMH en la sucursal
        if (!string.IsNullOrEmpty(caja.CodPuntoVentaMH) &&
            await _context.Cajas.AnyAsync(c => c.SucursalId == caja.SucursalId && c.CodPuntoVentaMH == caja.CodPuntoVentaMH && c.Activo))
        {
            throw new InvalidOperationException($"Ya existe una caja con el código MH de punto de venta {caja.CodPuntoVentaMH} en esta sucursal");
        }

        _context.Cajas.Add(caja);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            var innerMessage = ex.InnerException?.Message ?? ex.Message;
            _logger.LogError(ex, "[CAJA] Error al guardar caja. Inner: {InnerMessage}", innerMessage);
            throw new InvalidOperationException($"Error al guardar la caja: {innerMessage}", ex);
        }
        return caja;
    }

    public async Task<Caja> UpdateAsync(int id, Caja caja)
    {
        var existing = await _context.Cajas.FindAsync(id);
        if (existing == null) throw new KeyNotFoundException($"Caja {id} no encontrada");

        // Validar unicidad de código si cambió
        if (existing.Codigo != caja.Codigo &&
            await _context.Cajas.AnyAsync(c => c.SucursalId == existing.SucursalId && c.Codigo == caja.Codigo && c.Activo && c.Id != id))
        {
            throw new InvalidOperationException($"Ya existe una caja activa con el código {caja.Codigo} en esta sucursal");
        }

        // Validar unicidad de CodPuntoVenta si cambió
        if (!string.IsNullOrEmpty(caja.CodPuntoVenta) && existing.CodPuntoVenta != caja.CodPuntoVenta &&
            await _context.Cajas.AnyAsync(c => c.SucursalId == existing.SucursalId && c.CodPuntoVenta == caja.CodPuntoVenta && c.Activo && c.Id != id))
        {
            throw new InvalidOperationException($"Ya existe una caja con el código de punto de venta {caja.CodPuntoVenta} en esta sucursal");
        }

        // Validar unicidad de CodPuntoVentaMH si cambió
        if (!string.IsNullOrEmpty(caja.CodPuntoVentaMH) && existing.CodPuntoVentaMH != caja.CodPuntoVentaMH &&
            await _context.Cajas.AnyAsync(c => c.SucursalId == existing.SucursalId && c.CodPuntoVentaMH == caja.CodPuntoVentaMH && c.Activo && c.Id != id))
        {
            throw new InvalidOperationException($"Ya existe una caja con el código MH de punto de venta {caja.CodPuntoVentaMH} en esta sucursal");
        }

        existing.Nombre = caja.Nombre;
        existing.Codigo = caja.Codigo;
        existing.CodPuntoVenta = caja.CodPuntoVenta;
        existing.CodPuntoVentaMH = caja.CodPuntoVentaMH;

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task DeleteAsync(int id)
    {
        var existing = await _context.Cajas.FindAsync(id);
        if (existing == null) throw new KeyNotFoundException($"Caja {id} no encontrada");

        // Validar si tiene facturas
        if (await _context.Facturas.AnyAsync(f => f.CajaId == id))
        {
            throw new InvalidOperationException("No se puede eliminar la caja porque tiene facturas asociadas.");
        }

        // Validar si tiene usuarios asignados
        if (await _context.Set<UsuarioCaja>().AnyAsync(uc => uc.CajaId == id))
        {
            throw new InvalidOperationException("No se puede eliminar la caja porque tiene usuarios asignados. Desasigne los usuarios primero.");
        }

        _context.Cajas.Remove(existing);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ToggleActiveAsync(int id)
    {
        var existing = await _context.Cajas.FindAsync(id);
        if (existing == null) throw new KeyNotFoundException($"Caja {id} no encontrada");

        // Al reactivar, validar unicidad de campos únicos
        if (!existing.Activo)
        {
            if (!string.IsNullOrEmpty(existing.CodPuntoVentaMH) && await _context.Cajas.AnyAsync(c =>
                c.SucursalId == existing.SucursalId && c.CodPuntoVentaMH == existing.CodPuntoVentaMH && c.Activo && c.Id != id))
                throw new InvalidOperationException($"No se puede reactivar: ya existe una caja activa con el código MH de punto de venta {existing.CodPuntoVentaMH}");

            if (!string.IsNullOrEmpty(existing.CodPuntoVenta) && await _context.Cajas.AnyAsync(c =>
                c.SucursalId == existing.SucursalId && c.CodPuntoVenta == existing.CodPuntoVenta && c.Activo && c.Id != id))
                throw new InvalidOperationException($"No se puede reactivar: ya existe una caja activa con el código de punto de venta {existing.CodPuntoVenta}");

            if (!string.IsNullOrEmpty(existing.Codigo) && await _context.Cajas.AnyAsync(c =>
                c.SucursalId == existing.SucursalId && c.Codigo == existing.Codigo && c.Activo && c.Id != id))
                throw new InvalidOperationException($"No se puede reactivar: ya existe una caja activa con el código {existing.Codigo}");
        }

        existing.Activo = !existing.Activo;
        await _context.SaveChangesAsync();
        return existing.Activo;
    }

    public async Task AsignarCajaAUsuarioAsync(int usuarioId, int cajaId, int usuarioAsignadorId)
    {
        var usuario = await _context.Usuarios
            .Include(u => u.UsuarioCajas)
            .FirstOrDefaultAsync(u => u.Id == usuarioId);
        if (usuario == null) throw new KeyNotFoundException($"Usuario {usuarioId} no encontrado");

        // Validar que la caja exista
        if (!await _context.Cajas.AnyAsync(c => c.Id == cajaId))
        {
            throw new KeyNotFoundException($"Caja {cajaId} no encontrada");
        }

        // Cerrar historial abierto de esta caja si existe
        var historialAnterior = await _context.HistorialUsuariosCajas
            .Where(h => h.UsuarioId == usuarioId && h.CajaId == cajaId && h.FechaFin == null)
            .OrderByDescending(h => h.FechaInicio)
            .FirstOrDefaultAsync();

        if (historialAnterior != null)
        {
            historialAnterior.FechaFin = DateTime.UtcNow;
        }

        var historial = new HistorialUsuarioCaja
        {
            UsuarioId = usuarioId,
            CajaId = cajaId,
            UsuarioAsignadorId = usuarioAsignadorId,
            FechaInicio = DateTime.UtcNow,
            FechaCreacion = DateTime.UtcNow
        };

        _context.HistorialUsuariosCajas.Add(historial);

        // Agregar relación si no existe
        if (!usuario.UsuarioCajas.Any(uc => uc.CajaId == cajaId))
        {
            _context.UsuariosCajas.Add(new UsuarioCaja
            {
                UsuarioId = usuarioId,
                CajaId = cajaId
            });
        }

        await _context.SaveChangesAsync();
    }

    public async Task DesasignarCajaDeUsuarioAsync(int usuarioId, int usuarioAsignadorId)
    {
        var usuario = await _context.Usuarios
            .Include(u => u.UsuarioCajas)
            .FirstOrDefaultAsync(u => u.Id == usuarioId);
        if (usuario == null) throw new KeyNotFoundException($"Usuario {usuarioId} no encontrado");

        if (!usuario.UsuarioCajas.Any())
        {
            return; // Nada que desasignar
        }

        // Cerrar todos los historiales abiertos
        var historialesAbiertos = await _context.HistorialUsuariosCajas
            .Where(h => h.UsuarioId == usuarioId && h.FechaFin == null)
            .ToListAsync();

        foreach (var historial in historialesAbiertos)
        {
            historial.FechaFin = DateTime.UtcNow;
        }

        // Remover todas las asignaciones de cajas
        _context.UsuariosCajas.RemoveRange(usuario.UsuarioCajas);

        await _context.SaveChangesAsync();
    }
}
