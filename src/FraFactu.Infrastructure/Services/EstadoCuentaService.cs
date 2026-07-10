using FraFactu.Application.DTOs.Cuotas;
using FraFactu.Application.Interfaces;
using FraFactu.Domain.Entities;
using FraFactu.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FraFactu.Infrastructure.Services;

public class EstadoCuentaService : IEstadoCuentaService
{
    private readonly ApplicationDbContext _ctx;
    public EstadoCuentaService(ApplicationDbContext ctx) { _ctx = ctx; }

    public async Task<EstadoCuentaPlanDto?> GenerarPlanAsync(int planId, int emisorId)
    {
        var plan = await _ctx.Set<PlanCuotas>()
            .AsNoTracking()
            .Include(p => p.Cuotas).Include(p => p.Receptor).Include(p => p.Emisor)
            .FirstOrDefaultAsync(p => p.Id == planId && p.EmisorId == emisorId);
        if (plan == null) return null;
        var codigos = await CodigosPorCuotaAsync(plan);
        return MapPlan(plan, codigos);
    }

    public async Task<EstadoCuentaClienteDto?> GenerarClienteAsync(int receptorId, int emisorId)
    {
        var planes = await _ctx.Set<PlanCuotas>()
            .AsNoTracking()
            .Include(p => p.Cuotas).Include(p => p.Receptor).Include(p => p.Emisor)
            .Where(p => p.EmisorId == emisorId && p.ReceptorId == receptorId && p.Activo)
            .OrderBy(p => p.Id)
            .ToListAsync();
        if (planes.Count == 0) return null;

        var planDtos = new List<EstadoCuentaPlanDto>();
        foreach (var plan in planes)
            planDtos.Add(MapPlan(plan, await CodigosPorCuotaAsync(plan)));

        var primero = planes[0];
        return new EstadoCuentaClienteDto
        {
            EmisorNombre = NombreEmisor(primero.Emisor),
            ClienteNombre = primero.Receptor?.NombreRazonSocial ?? "Sin cliente",
            ClienteDocumento = primero.Receptor?.NumeroDocumento ?? "",
            FechaReporte = DateTime.UtcNow,
            MontoTotal = planDtos.Sum(p => p.MontoTotal),
            MontoPagado = planDtos.Sum(p => p.MontoPagado),
            SaldoAdeudado = planDtos.Sum(p => p.SaldoAdeudado),
            Planes = planDtos
        };
    }

    private async Task<Dictionary<int, string>> CodigosPorCuotaAsync(PlanCuotas plan)
    {
        var ids = plan.Cuotas.Where(c => c.FacturaId.HasValue).Select(c => c.FacturaId!.Value).Distinct().ToList();
        if (ids.Count == 0) return new();
        return await _ctx.Set<FacturaElectronica>()
            .AsNoTracking()
            .Where(f => ids.Contains(f.Id))
            .ToDictionaryAsync(f => f.Id, f => f.CodigoGeneracion);
    }

    private static EstadoCuentaPlanDto MapPlan(PlanCuotas plan, Dictionary<int, string> codigos) => new()
    {
        EmisorNombre = NombreEmisor(plan.Emisor),
        ClienteNombre = plan.Receptor?.NombreRazonSocial ?? "Sin cliente",
        ClienteDocumento = plan.Receptor?.NumeroDocumento ?? "",
        FechaReporte = DateTime.UtcNow,
        PlanId = plan.Id,
        Condicion = plan.CondicionOperacion == 2 ? "Crédito"
                  : plan.CondicionOperacion == 3 ? "Mixto"
                  : plan.CondicionOperacion.ToString(),
        MontoTotal = plan.MontoTotal,
        MontoPagado = plan.MontoPagado,
        SaldoAdeudado = plan.SaldoAdeudado,
        EstadoCobro = plan.EstadoCobro.ToString(),
        Cuotas = plan.Cuotas.Where(c => c.Activo).OrderBy(c => c.Numero).Select(c => new EstadoCuentaCuotaDto
        {
            Numero = c.Numero,
            FechaPactada = c.FechaPactada,
            Monto = c.Monto,
            Estado = c.Estado.ToString(),
            FechaPago = c.FechaPago,
            CodigoGeneracion = c.FacturaId.HasValue && codigos.TryGetValue(c.FacturaId.Value, out var cod) ? cod : null,
            InteresMora = c.InteresMora
        }).ToList()
    };

    private static string NombreEmisor(Emisor? e) => e?.NombreComercial ?? e?.NombreRazonSocial ?? "";
}
