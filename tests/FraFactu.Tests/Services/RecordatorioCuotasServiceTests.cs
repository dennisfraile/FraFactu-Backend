using FraFactu.Application.DTOs.Notificaciones;
using FraFactu.Application.Interfaces;
using FraFactu.Domain.Entities;
using FraFactu.Domain.Enums;
using FraFactu.Infrastructure.Persistence;
using FraFactu.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace FraFactu.Tests.Services;

public class RecordatorioCuotasServiceTests
{
    private const int EmisorId = 7;

    private static ApplicationDbContext BuildContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"Record_{Guid.NewGuid()}").Options);

    private static (Mock<IEmailService> email, Mock<INotificacionService> notif) Mocks()
    {
        var email = new Mock<IEmailService>();
        email.Setup(s => s.EnviarNotificacionGenericaAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
            .Returns(Task.CompletedTask);
        var notif = new Mock<INotificacionService>();
        notif.Setup(s => s.CrearAsync(
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<string>()))
            .ReturnsAsync(new NotificacionDto());
        return (email, notif);
    }

    private static RecordatorioCuotasService BuildSut(
        ApplicationDbContext ctx, Mock<IEmailService> email, Mock<INotificacionService> notif) =>
        new(ctx, email.Object, notif.Object, NullLogger<RecordatorioCuotasService>.Instance);

    private static PlanCuotas SeedPlan(ApplicationDbContext ctx, DateTime fechaPactada, string correo = "cliente@x.com")
    {
        var receptor = new Receptor
        {
            EmisorId = EmisorId,
            NombreRazonSocial = "Cliente X",
            NumeroDocumento = "0614-1",
            CorreoElectronico = correo
        };
        ctx.Set<Receptor>().Add(receptor);
        ctx.SaveChanges();

        var plan = new PlanCuotas
        {
            EmisorId = EmisorId,
            ReceptorId = receptor.Id,
            CondicionOperacion = 2,
            MontoTotal = 100m,
            MontoPagado = 0m,
            SaldoAdeudado = 100m,
            EstadoCobro = EstadoCobroPlan.Pendiente,
            VentaSnapshotJson = "{}",
            FechaCreacion = DateTime.UtcNow
        };
        plan.Cuotas.Add(new Cuota
        {
            Numero = 1,
            Monto = 100m,
            FechaPactada = fechaPactada,
            Estado = EstadoCuota.Pendiente,
            EsCuotaFinal = true
        });
        ctx.Set<PlanCuotas>().Add(plan);
        ctx.SaveChanges();
        return plan;
    }

    [Fact]
    public async Task CuotaVencida_SeMarcaYNotifica()
    {
        var ctx = BuildContext();
        var (email, notif) = Mocks();
        var plan = SeedPlan(ctx, DateTime.UtcNow.Date.AddDays(-5));
        var sut = BuildSut(ctx, email, notif);

        await sut.ProcesarAsync();

        var cuota = await ctx.Set<Cuota>().FirstAsync(c => c.PlanCuotasId == plan.Id);
        cuota.Estado.Should().Be(EstadoCuota.Vencida);
        cuota.RecordatorioVencidaEnviado.Should().BeTrue();
        (await ctx.Set<PlanCuotas>().FirstAsync(p => p.Id == plan.Id)).EstadoCobro
            .Should().Be(EstadoCobroPlan.Vencida);
        notif.Verify(s => s.CrearAsync(EmisorId, "CuotaVencida", It.IsAny<string>(), It.IsAny<string>(),
            "/cuentas-por-cobrar", plan.Id, "warning"), Times.Once);
    }

    [Fact]
    public async Task PorVencer_SeEnviaUnaSolaVez()
    {
        var ctx = BuildContext();
        var (email, notif) = Mocks();
        var plan = SeedPlan(ctx, DateTime.UtcNow.Date.AddDays(2));
        var sut = BuildSut(ctx, email, notif);

        await sut.ProcesarAsync();
        await sut.ProcesarAsync();

        var cuota = await ctx.Set<Cuota>().FirstAsync(c => c.PlanCuotasId == plan.Id);
        cuota.RecordatorioPorVencerEnviado.Should().BeTrue();
        cuota.Estado.Should().Be(EstadoCuota.Pendiente);
        notif.Verify(s => s.CrearAsync(EmisorId, "CuotaPorVencer", It.IsAny<string>(), It.IsAny<string>(),
            "/cuentas-por-cobrar", plan.Id, "info"), Times.Once);
    }

    [Fact]
    public async Task Vencida_SeEnviaUnaSolaVez()
    {
        var ctx = BuildContext();
        var (email, notif) = Mocks();
        var plan = SeedPlan(ctx, DateTime.UtcNow.Date.AddDays(-1));
        var sut = BuildSut(ctx, email, notif);

        await sut.ProcesarAsync();
        await sut.ProcesarAsync();

        notif.Verify(s => s.CrearAsync(EmisorId, "CuotaVencida", It.IsAny<string>(), It.IsAny<string>(),
            "/cuentas-por-cobrar", plan.Id, "warning"), Times.Once);
    }

    [Fact]
    public async Task RecordatoriosDeshabilitados_NoNotifica_PeroMarcaVencida()
    {
        var ctx = BuildContext();
        var (email, notif) = Mocks();
        ctx.Set<ConfiguracionCuotas>().Add(new ConfiguracionCuotas
        {
            EmisorId = EmisorId, RecordatoriosHabilitados = false, FechaCreacion = DateTime.UtcNow
        });
        ctx.SaveChanges();
        var plan = SeedPlan(ctx, DateTime.UtcNow.Date.AddDays(-3));
        var sut = BuildSut(ctx, email, notif);

        await sut.ProcesarAsync();

        (await ctx.Set<Cuota>().FirstAsync(c => c.PlanCuotasId == plan.Id)).Estado
            .Should().Be(EstadoCuota.Vencida);
        notif.Verify(s => s.CrearAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<string>()), Times.Never);
        email.Verify(s => s.EnviarNotificacionGenericaAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task ReceptorSinEmail_NoEnviaEmail_PeroCreaNotificacion()
    {
        var ctx = BuildContext();
        var (email, notif) = Mocks();
        var plan = SeedPlan(ctx, DateTime.UtcNow.Date.AddDays(2), correo: "");
        var sut = BuildSut(ctx, email, notif);

        await sut.ProcesarAsync();

        email.Verify(s => s.EnviarNotificacionGenericaAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        notif.Verify(s => s.CrearAsync(EmisorId, "CuotaPorVencer", It.IsAny<string>(), It.IsAny<string>(),
            "/cuentas-por-cobrar", plan.Id, "info"), Times.Once);
    }
}
