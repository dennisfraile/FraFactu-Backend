using FraFactu.Application.DTOs.Notificaciones;
using FraFactu.Application.Interfaces;
using FraFactu.Domain.Entities;
using FraFactu.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FraFactu.Infrastructure.Services;

public class NotificacionService : INotificacionService
{
    private readonly ApplicationDbContext _ctx;
    public NotificacionService(ApplicationDbContext ctx) { _ctx = ctx; }

    public async Task<NotificacionDto> CrearAsync(int emisorId, string tipo, string titulo, string mensaje,
        string? ruta = null, int? referenciaId = null, string nivel = "info")
    {
        var n = new Notificacion
        {
            EmisorId = emisorId,
            Tipo = tipo,
            Nivel = nivel,
            Titulo = titulo,
            Mensaje = mensaje,
            Ruta = ruta,
            ReferenciaId = referenciaId,
            FechaCreacion = DateTime.UtcNow
        };
        _ctx.Set<Notificacion>().Add(n);
        await _ctx.SaveChangesAsync();
        return Map(n, leida: false);
    }

    public async Task<List<NotificacionDto>> ListarAsync(int emisorId, int usuarioId, int max = 30)
    {
        var notifs = await _ctx.Set<Notificacion>()
            .Where(n => n.EmisorId == emisorId && n.Activo)
            .OrderByDescending(n => n.FechaCreacion)
            .Take(max)
            .ToListAsync();

        var ids = notifs.Select(n => n.Id).ToList();
        var leidas = await _ctx.Set<NotificacionLeida>()
            .Where(l => l.UsuarioId == usuarioId && ids.Contains(l.NotificacionId))
            .Select(l => l.NotificacionId)
            .ToListAsync();
        var leidasSet = leidas.ToHashSet();

        return notifs.Select(n => Map(n, leidasSet.Contains(n.Id))).ToList();
    }

    public async Task<int> ContarNoLeidasAsync(int emisorId, int usuarioId)
    {
        var leidasIds = _ctx.Set<NotificacionLeida>()
            .Where(l => l.UsuarioId == usuarioId)
            .Select(l => l.NotificacionId);

        return await _ctx.Set<Notificacion>()
            .Where(n => n.EmisorId == emisorId && n.Activo && !leidasIds.Contains(n.Id))
            .CountAsync();
    }

    public async Task MarcarLeidaAsync(int notificacionId, int usuarioId)
    {
        var yaLeida = await _ctx.Set<NotificacionLeida>()
            .AnyAsync(l => l.NotificacionId == notificacionId && l.UsuarioId == usuarioId);
        if (yaLeida) return;

        var existe = await _ctx.Set<Notificacion>().AnyAsync(n => n.Id == notificacionId);
        if (!existe) throw new InvalidOperationException("Notificación no encontrada.");

        _ctx.Set<NotificacionLeida>().Add(new NotificacionLeida
        {
            NotificacionId = notificacionId,
            UsuarioId = usuarioId,
            FechaLeida = DateTime.UtcNow,
            FechaCreacion = DateTime.UtcNow
        });
        await _ctx.SaveChangesAsync();
    }

    public async Task MarcarTodasLeidasAsync(int emisorId, int usuarioId)
    {
        var leidasIds = (await _ctx.Set<NotificacionLeida>()
            .Where(l => l.UsuarioId == usuarioId)
            .Select(l => l.NotificacionId)
            .ToListAsync()).ToHashSet();

        var noLeidas = await _ctx.Set<Notificacion>()
            .Where(n => n.EmisorId == emisorId && n.Activo && !leidasIds.Contains(n.Id))
            .Select(n => n.Id)
            .ToListAsync();

        foreach (var id in noLeidas)
        {
            _ctx.Set<NotificacionLeida>().Add(new NotificacionLeida
            {
                NotificacionId = id,
                UsuarioId = usuarioId,
                FechaLeida = DateTime.UtcNow,
                FechaCreacion = DateTime.UtcNow
            });
        }
        if (noLeidas.Count > 0) await _ctx.SaveChangesAsync();
    }

    private static NotificacionDto Map(Notificacion n, bool leida) => new()
    {
        Id = n.Id,
        Tipo = n.Tipo,
        Nivel = n.Nivel,
        Titulo = n.Titulo,
        Mensaje = n.Mensaje,
        Ruta = n.Ruta,
        ReferenciaId = n.ReferenciaId,
        FechaCreacion = n.FechaCreacion,
        Leida = leida
    };
}
