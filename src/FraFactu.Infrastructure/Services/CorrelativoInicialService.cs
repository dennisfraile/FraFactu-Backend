using FraFactu.Application.DTOs;
using FraFactu.Application.Interfaces;
using FraFactu.Domain.Entities;
using FraFactu.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FraFactu.Infrastructure.Services;

public class CorrelativoInicialService : ICorrelativoInicialService
{
    private readonly ApplicationDbContext _ctx;
    public CorrelativoInicialService(ApplicationDbContext ctx) { _ctx = ctx; }

    public async Task<int> ObtenerUltimoCorrelativoAsync(int emisorId, string tipoDte, int anio, string ambiente)
    {
        var reg = await _ctx.Set<CorrelativoInicial>().AsNoTracking()
            .FirstOrDefaultAsync(c => c.EmisorId == emisorId && c.TipoDte == tipoDte
                && c.Anio == anio && c.Ambiente == ambiente);
        return reg?.UltimoCorrelativo ?? 0;
    }

    public async Task<bool> ExistenFacturasAsync(int emisorId, string tipoDte, int anio, string ambiente)
    {
        var prefijo = $"DTE-{tipoDte}-";
        return await _ctx.Set<FacturaElectronica>().AsNoTracking()
            .AnyAsync(f => f.EmisorId == emisorId
                && f.NumeroControl.StartsWith(prefijo)
                && f.FechaEmision.Year == anio
                && f.Ambiente == ambiente);
    }

    public async Task<List<CorrelativoInicialDto>> ListarAsync(int emisorId, int anio, string ambiente)
    {
        var regs = await _ctx.Set<CorrelativoInicial>().AsNoTracking()
            .Where(c => c.EmisorId == emisorId && c.Anio == anio && c.Ambiente == ambiente)
            .OrderBy(c => c.TipoDte)
            .ToListAsync();

        var result = new List<CorrelativoInicialDto>();
        foreach (var c in regs)
        {
            result.Add(new CorrelativoInicialDto
            {
                Id = c.Id,
                TipoDte = c.TipoDte,
                Anio = c.Anio,
                Ambiente = c.Ambiente,
                UltimoCorrelativo = c.UltimoCorrelativo,
                Bloqueado = await ExistenFacturasAsync(emisorId, c.TipoDte, anio, ambiente)
            });
        }
        return result;
    }

    public async Task<CorrelativoInicialDto> UpsertAsync(int emisorId, CorrelativoInicialUpsertDto dto)
    {
        if (dto.UltimoCorrelativo < 0)
            throw new InvalidOperationException("El correlativo no puede ser negativo.");

        if (!TiposDteValidos.Contains(dto.TipoDte))
            throw new InvalidOperationException($"Tipo de DTE no soportado: {dto.TipoDte}.");
        if (!AmbientesValidos.Contains(dto.Ambiente))
            throw new InvalidOperationException($"Ambiente inválido: {dto.Ambiente}.");

        if (await ExistenFacturasAsync(emisorId, dto.TipoDte, dto.Anio, dto.Ambiente))
            throw new InvalidOperationException("Ya existen DTE emitidos para este tipo y año; no se puede modificar el correlativo inicial.");

        var reg = await _ctx.Set<CorrelativoInicial>()
            .FirstOrDefaultAsync(c => c.EmisorId == emisorId && c.TipoDte == dto.TipoDte
                && c.Anio == dto.Anio && c.Ambiente == dto.Ambiente);

        if (reg == null)
        {
            reg = new CorrelativoInicial
            {
                EmisorId = emisorId, TipoDte = dto.TipoDte, Anio = dto.Anio,
                Ambiente = dto.Ambiente, UltimoCorrelativo = dto.UltimoCorrelativo,
                FechaCreacion = DateTime.UtcNow
            };
            _ctx.Set<CorrelativoInicial>().Add(reg);
        }
        else
        {
            reg.UltimoCorrelativo = dto.UltimoCorrelativo;
            reg.FechaActualizacion = DateTime.UtcNow;
        }
        try
        {
            await _ctx.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            throw new InvalidOperationException("El correlativo inicial ya fue registrado por otra operación. Recargá e intentá de nuevo.");
        }

        // Bloqueado=false: el check previo garantiza que no hay facturas para esta combinación.
        return new CorrelativoInicialDto
        {
            Id = reg.Id, TipoDte = reg.TipoDte, Anio = reg.Anio, Ambiente = reg.Ambiente,
            UltimoCorrelativo = reg.UltimoCorrelativo, Bloqueado = false
        };
    }

    private static readonly string[] TiposDteValidos = { "01", "03", "05", "06", "14" };
    private static readonly string[] AmbientesValidos = { "00", "01" };
}
