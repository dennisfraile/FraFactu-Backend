using FraFactu.Application.DTOs.Cuotas;
using FraFactu.Application.Interfaces;
using FraFactu.Domain.Entities;
using FraFactu.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FraFactu.Infrastructure.Services;

public class ConfiguracionCuotasService : IConfiguracionCuotasService
{
    private readonly ApplicationDbContext _ctx;

    public ConfiguracionCuotasService(ApplicationDbContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<ConfiguracionCuotasDto> ObtenerOCrearAsync(int emisorId)
    {
        var cfg = await _ctx.Set<ConfiguracionCuotas>().FirstOrDefaultAsync(c => c.EmisorId == emisorId);
        if (cfg == null)
        {
            cfg = new ConfiguracionCuotas { EmisorId = emisorId, FechaCreacion = DateTime.UtcNow };
            _ctx.Set<ConfiguracionCuotas>().Add(cfg);
            await _ctx.SaveChangesAsync();
        }
        return Map(cfg);
    }

    public async Task<ConfiguracionCuotasDto> ActualizarAsync(int emisorId, ActualizarConfiguracionCuotasDto dto)
    {
        var cfg = await _ctx.Set<ConfiguracionCuotas>().FirstOrDefaultAsync(c => c.EmisorId == emisorId);
        if (cfg == null)
        {
            cfg = new ConfiguracionCuotas { EmisorId = emisorId, FechaCreacion = DateTime.UtcNow };
            _ctx.Set<ConfiguracionCuotas>().Add(cfg);
        }
        cfg.TasaMoraMensual = dto.TasaMoraMensual;
        cfg.DiasGracia = dto.DiasGracia;
        cfg.MoraHabilitada = dto.MoraHabilitada;
        cfg.RecordatoriosHabilitados = dto.RecordatoriosHabilitados;
        cfg.DiasAntesRecordatorio = dto.DiasAntesRecordatorio;
        await _ctx.SaveChangesAsync();
        return Map(cfg);
    }

    private static ConfiguracionCuotasDto Map(ConfiguracionCuotas c) => new()
    {
        TasaMoraMensual = c.TasaMoraMensual,
        DiasGracia = c.DiasGracia,
        MoraHabilitada = c.MoraHabilitada,
        RecordatoriosHabilitados = c.RecordatoriosHabilitados,
        DiasAntesRecordatorio = c.DiasAntesRecordatorio
    };
}
