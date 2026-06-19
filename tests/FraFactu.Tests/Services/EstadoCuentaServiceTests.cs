using FraFactu.Application.DTOs.Cuotas;
using FraFactu.Domain.Entities;
using FraFactu.Domain.Enums;
using FraFactu.Infrastructure.Persistence;
using FraFactu.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FraFactu.Tests.Services;

public class EstadoCuentaServiceTests
{
    private const int EmisorId = 7;

    private static ApplicationDbContext BuildContext()
    {
        var ctx = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"EstadoCuenta_{Guid.NewGuid()}").Options);
        ctx.Set<Emisor>().Add(new Emisor { Id = EmisorId, NombreRazonSocial = "Mi Empresa", NombreComercial = "Mi Empresa SA" });
        ctx.SaveChanges();
        return ctx;
    }

    private static Receptor SeedReceptor(ApplicationDbContext ctx, int id, string nombre)
    {
        var r = new Receptor { Id = id, EmisorId = EmisorId, NombreRazonSocial = nombre, NumeroDocumento = $"DOC-{id}", CorreoElectronico = "" };
        ctx.Set<Receptor>().Add(r);
        ctx.SaveChanges();
        return r;
    }

    private static PlanCuotas SeedPlan(ApplicationDbContext ctx, int receptorId,
        decimal total, decimal pagado, params Cuota[] cuotas)
    {
        var plan = new PlanCuotas
        {
            EmisorId = EmisorId, ReceptorId = receptorId, CondicionOperacion = 2,
            MontoTotal = total, MontoPagado = pagado, SaldoAdeudado = total - pagado,
            EstadoCobro = EstadoCobroPlan.Parcial, VentaSnapshotJson = "{}", FechaCreacion = DateTime.UtcNow
        };
        foreach (var c in cuotas) plan.Cuotas.Add(c);
        ctx.Set<PlanCuotas>().Add(plan);
        ctx.SaveChanges();
        return plan;
    }

    [Fact]
    public async Task GenerarPlan_ArmaCuotasYResuelveCodigoDte()
    {
        var ctx = BuildContext();
        SeedReceptor(ctx, 1, "Cliente A");
        ctx.Set<FacturaElectronica>().Add(new FacturaElectronica { Id = 500, CodigoGeneracion = "COD-500" });
        ctx.SaveChanges();
        var plan = SeedPlan(ctx, 1, total: 100m, pagado: 50m,
            new Cuota { Numero = 1, Monto = 50m, FechaPactada = new DateTime(2026, 6, 1), Estado = EstadoCuota.Pagada, FechaPago = new DateTime(2026, 6, 1), FacturaId = 500, InteresMora = 1.13m },
            new Cuota { Numero = 2, Monto = 50m, FechaPactada = new DateTime(2026, 7, 1), Estado = EstadoCuota.Pendiente });
        var sut = new EstadoCuentaService(ctx);

        var dto = await sut.GenerarPlanAsync(plan.Id, EmisorId);

        dto.Should().NotBeNull();
        dto!.EmisorNombre.Should().Be("Mi Empresa SA");
        dto.ClienteNombre.Should().Be("Cliente A");
        dto.Condicion.Should().Be("Crédito");
        dto.MontoTotal.Should().Be(100m);
        dto.SaldoAdeudado.Should().Be(50m);
        dto.Cuotas.Should().HaveCount(2);
        dto.Cuotas[0].CodigoGeneracion.Should().Be("COD-500");
        dto.Cuotas[0].InteresMora.Should().Be(1.13m);
        dto.Cuotas[1].CodigoGeneracion.Should().BeNull();
    }

    [Fact]
    public async Task GenerarPlan_Inexistente_DevuelveNull()
    {
        var ctx = BuildContext();
        var sut = new EstadoCuentaService(ctx);
        (await sut.GenerarPlanAsync(999, EmisorId)).Should().BeNull();
    }

    [Fact]
    public async Task GenerarCliente_ConsolidaPlanes()
    {
        var ctx = BuildContext();
        SeedReceptor(ctx, 1, "Cliente A");
        SeedPlan(ctx, 1, total: 100m, pagado: 40m,
            new Cuota { Numero = 1, Monto = 100m, FechaPactada = new DateTime(2026, 6, 1), Estado = EstadoCuota.Pendiente });
        SeedPlan(ctx, 1, total: 200m, pagado: 0m,
            new Cuota { Numero = 1, Monto = 200m, FechaPactada = new DateTime(2026, 8, 1), Estado = EstadoCuota.Pendiente });
        var sut = new EstadoCuentaService(ctx);

        var dto = await sut.GenerarClienteAsync(1, EmisorId);

        dto.Should().NotBeNull();
        dto!.Planes.Should().HaveCount(2);
        dto.MontoTotal.Should().Be(300m);
        dto.MontoPagado.Should().Be(40m);
        dto.SaldoAdeudado.Should().Be(260m);
        dto.ClienteNombre.Should().Be("Cliente A");
    }

    [Fact]
    public async Task GenerarCliente_SinPlanes_DevuelveNull()
    {
        var ctx = BuildContext();
        SeedReceptor(ctx, 1, "Cliente A");
        var sut = new EstadoCuentaService(ctx);
        (await sut.GenerarClienteAsync(1, EmisorId)).Should().BeNull();
    }

    private static EstadoCuentaPlanDto PlanDtoEjemplo() => new()
    {
        EmisorNombre = "Mi Empresa", ClienteNombre = "Cliente A", ClienteDocumento = "DOC-1",
        FechaReporte = new DateTime(2026, 6, 10), PlanId = 1, Condicion = "Crédito",
        MontoTotal = 100m, MontoPagado = 50m, SaldoAdeudado = 50m, EstadoCobro = "Parcial",
        Cuotas = new()
        {
            new EstadoCuentaCuotaDto { Numero = 1, FechaPactada = new DateTime(2026,6,1), Monto = 50m, Estado = "Pagada", FechaPago = new DateTime(2026,6,1), CodigoGeneracion = "COD-500", InteresMora = 0m },
            new EstadoCuentaCuotaDto { Numero = 2, FechaPactada = new DateTime(2026,7,1), Monto = 50m, Estado = "Pendiente", InteresMora = 0m }
        }
    };

    [Fact]
    public void ExcelExporter_Plan_DevuelveBytes()
    {
        new EstadoCuentaExcelExporter().GenerarPlan(PlanDtoEjemplo()).Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ExcelExporter_Cliente_DevuelveBytes()
    {
        var cliente = new EstadoCuentaClienteDto
        {
            EmisorNombre = "Mi Empresa", ClienteNombre = "Cliente A", ClienteDocumento = "DOC-1",
            FechaReporte = new DateTime(2026, 6, 10), MontoTotal = 100m, MontoPagado = 50m, SaldoAdeudado = 50m,
            Planes = new() { PlanDtoEjemplo() }
        };
        new EstadoCuentaExcelExporter().GenerarCliente(cliente).Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void PdfExporter_Plan_DevuelveBytes()
    {
        new EstadoCuentaPdfExporter().GenerarPlan(PlanDtoEjemplo()).Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void PdfExporter_Cliente_DevuelveBytes()
    {
        var cliente = new EstadoCuentaClienteDto
        {
            EmisorNombre = "Mi Empresa", ClienteNombre = "Cliente A", ClienteDocumento = "DOC-1",
            FechaReporte = new DateTime(2026, 6, 10), MontoTotal = 100m, MontoPagado = 50m, SaldoAdeudado = 50m,
            Planes = new() { PlanDtoEjemplo() }
        };
        new EstadoCuentaPdfExporter().GenerarCliente(cliente).Should().NotBeNullOrEmpty();
    }
}
