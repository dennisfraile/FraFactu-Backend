using FraFactu.Application.DTOs.Cuotas;
using FraFactu.Domain.Entities;
using FraFactu.Domain.Enums;
using FraFactu.Infrastructure.Persistence;
using FraFactu.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FraFactu.Tests.Services;

public class AgingServiceTests
{
    private const int EmisorId = 7;

    private static ApplicationDbContext BuildContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"Aging_{Guid.NewGuid()}").Options);

    private static PlanCuotas SeedPlan(ApplicationDbContext ctx, int receptorId, string nombre,
        params (DateTime fecha, decimal monto, EstadoCuota estado)[] cuotas)
    {
        if (!ctx.Set<Receptor>().Any(r => r.Id == receptorId))
        {
            ctx.Set<Receptor>().Add(new Receptor
            {
                Id = receptorId, EmisorId = EmisorId,
                NombreRazonSocial = nombre, NumeroDocumento = $"DOC-{receptorId}", CorreoElectronico = ""
            });
            ctx.SaveChanges();
        }
        decimal saldo = cuotas.Where(c => c.estado != EstadoCuota.Pagada).Sum(c => c.monto);
        var plan = new PlanCuotas
        {
            EmisorId = EmisorId, ReceptorId = receptorId, CondicionOperacion = 2,
            MontoTotal = cuotas.Sum(c => c.monto), MontoPagado = 0m, SaldoAdeudado = saldo,
            EstadoCobro = EstadoCobroPlan.Pendiente, VentaSnapshotJson = "{}", FechaCreacion = DateTime.UtcNow
        };
        int n = 1;
        foreach (var (fecha, monto, estado) in cuotas)
            plan.Cuotas.Add(new Cuota { Numero = n++, Monto = monto, FechaPactada = fecha, Estado = estado });
        ctx.Set<PlanCuotas>().Add(plan);
        ctx.SaveChanges();
        return plan;
    }

    [Fact]
    public async Task AsignaCadaCuotaASuTramo()
    {
        var ctx = BuildContext();
        var hoy = DateTime.UtcNow.Date;
        SeedPlan(ctx, 1, "Cliente A",
            (hoy.AddDays(5), 10m, EstadoCuota.Pendiente),
            (hoy.AddDays(-15), 10m, EstadoCuota.Vencida),
            (hoy.AddDays(-45), 10m, EstadoCuota.Vencida),
            (hoy.AddDays(-75), 10m, EstadoCuota.Vencida),
            (hoy.AddDays(-120), 10m, EstadoCuota.Vencida));
        var sut = new AgingService(ctx);

        var rep = await sut.GenerarAsync(EmisorId);

        rep.Totales.PorVencer.Should().Be(10m);
        rep.Totales.D1a30.Should().Be(10m);
        rep.Totales.D31a60.Should().Be(10m);
        rep.Totales.D61a90.Should().Be(10m);
        rep.Totales.Mas90.Should().Be(10m);
        rep.Totales.Total.Should().Be(50m);
        rep.Clientes.Should().HaveCount(1);
        rep.Clientes[0].Planes.Should().HaveCount(1);
    }

    [Fact]
    public async Task CuotaPagada_NoCuenta()
    {
        var ctx = BuildContext();
        var hoy = DateTime.UtcNow.Date;
        SeedPlan(ctx, 1, "Cliente A",
            (hoy.AddDays(-10), 10m, EstadoCuota.Pagada),
            (hoy.AddDays(-10), 20m, EstadoCuota.Vencida));
        var sut = new AgingService(ctx);

        var rep = await sut.GenerarAsync(EmisorId);

        rep.Totales.Total.Should().Be(20m);
        rep.Totales.D1a30.Should().Be(20m);
    }

    [Fact]
    public async Task AgrupaPorCliente_ConSubtotalesYTotal()
    {
        var ctx = BuildContext();
        var hoy = DateTime.UtcNow.Date;
        SeedPlan(ctx, 1, "Cliente A", (hoy.AddDays(-15), 30m, EstadoCuota.Vencida));
        SeedPlan(ctx, 1, "Cliente A", (hoy.AddDays(5), 40m, EstadoCuota.Pendiente));
        SeedPlan(ctx, 2, "Cliente B", (hoy.AddDays(-100), 50m, EstadoCuota.Vencida));
        var sut = new AgingService(ctx);

        var rep = await sut.GenerarAsync(EmisorId);

        rep.Clientes.Should().HaveCount(2);
        var a = rep.Clientes.First(c => c.ClienteNombre == "Cliente A");
        a.Planes.Should().HaveCount(2);
        a.Subtotal.Total.Should().Be(70m);
        rep.Totales.Total.Should().Be(120m);
        rep.Totales.Mas90.Should().Be(50m);
    }

    [Fact]
    public void ExcelExporter_DevuelveBytesNoVacios()
    {
        var rep = new AgingReporteDto
        {
            FechaCorte = new DateTime(2026, 6, 10),
            Clientes = new()
            {
                new AgingClienteDto
                {
                    ReceptorId = 1, ClienteNombre = "Cliente A",
                    Planes = new() { new AgingPlanDto { PlanId = 1, D1a30 = 30m, Total = 30m } },
                    Subtotal = new AgingTotalesDto { D1a30 = 30m, Total = 30m }
                }
            },
            Totales = new AgingTotalesDto { D1a30 = 30m, Total = 30m }
        };
        var bytes = new AgingExcelExporter().Generar(rep);
        bytes.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void PdfExporter_DevuelveBytesNoVacios()
    {
        var rep = new AgingReporteDto
        {
            FechaCorte = new DateTime(2026, 6, 10),
            Clientes = new()
            {
                new AgingClienteDto
                {
                    ReceptorId = 1, ClienteNombre = "Cliente A",
                    Planes = new() { new AgingPlanDto { PlanId = 1, D1a30 = 30m, Total = 30m } },
                    Subtotal = new AgingTotalesDto { D1a30 = 30m, Total = 30m }
                }
            },
            Totales = new AgingTotalesDto { D1a30 = 30m, Total = 30m }
        };
        var bytes = new AgingPdfExporter().Generar(rep);
        bytes.Should().NotBeNullOrEmpty();
    }
}
