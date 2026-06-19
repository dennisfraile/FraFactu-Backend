using FraFactu.Application.DTOs.Cuotas;
using FraFactu.Application.Interfaces;
using FraFactu.Domain.Entities;
using FraFactu.Domain.Enums;
using FraFactu.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FraFactu.Infrastructure.Services;

public class AgingService : IAgingService
{
    private readonly ApplicationDbContext _ctx;
    public AgingService(ApplicationDbContext ctx) { _ctx = ctx; }

    public async Task<AgingReporteDto> GenerarAsync(int emisorId)
    {
        var hoy = DateTime.UtcNow.Date;

        var planes = await _ctx.Set<PlanCuotas>()
            .Include(p => p.Cuotas)
            .Include(p => p.Receptor)
            .Where(p => p.Activo && p.EmisorId == emisorId && p.SaldoAdeudado > 0)
            .ToListAsync();

        var filas = new List<(int? receptorId, string cliente, AgingPlanDto plan)>();

        foreach (var plan in planes)
        {
            decimal porVencer = 0, d1 = 0, d2 = 0, d3 = 0, d4 = 0;
            foreach (var cuota in plan.Cuotas.Where(c => c.Activo && c.Estado != EstadoCuota.Pagada))
            {
                int dias = (hoy - cuota.FechaPactada.Date).Days;
                if (dias <= 0) porVencer += cuota.Monto;
                else if (dias <= 30) d1 += cuota.Monto;
                else if (dias <= 60) d2 += cuota.Monto;
                else if (dias <= 90) d3 += cuota.Monto;
                else d4 += cuota.Monto;
            }

            var planDto = new AgingPlanDto
            {
                PlanId = plan.Id,
                PorVencer = decimal.Round(porVencer, 2),
                D1a30 = decimal.Round(d1, 2),
                D31a60 = decimal.Round(d2, 2),
                D61a90 = decimal.Round(d3, 2),
                Mas90 = decimal.Round(d4, 2),
                Total = decimal.Round(porVencer + d1 + d2 + d3 + d4, 2)
            };
            if (planDto.Total <= 0) continue;
            filas.Add((plan.ReceptorId, plan.Receptor?.NombreRazonSocial ?? "Sin cliente", planDto));
        }

        var clientes = filas
            .GroupBy(x => new { x.receptorId, x.cliente })
            .Select(g => new AgingClienteDto
            {
                ReceptorId = g.Key.receptorId,
                ClienteNombre = g.Key.cliente,
                Planes = g.Select(x => x.plan).OrderBy(p => p.PlanId).ToList(),
                Subtotal = Sumar(g.Select(x => x.plan))
            })
            .OrderBy(c => c.ClienteNombre)
            .ToList();

        return new AgingReporteDto
        {
            FechaCorte = hoy,
            Clientes = clientes,
            Totales = Sumar(filas.Select(f => f.plan))
        };
    }

    private static AgingTotalesDto Sumar(IEnumerable<AgingPlanDto> planes) => new()
    {
        PorVencer = decimal.Round(planes.Sum(p => p.PorVencer), 2),
        D1a30 = decimal.Round(planes.Sum(p => p.D1a30), 2),
        D31a60 = decimal.Round(planes.Sum(p => p.D31a60), 2),
        D61a90 = decimal.Round(planes.Sum(p => p.D61a90), 2),
        Mas90 = decimal.Round(planes.Sum(p => p.Mas90), 2),
        Total = decimal.Round(planes.Sum(p => p.Total), 2)
    };
}
