using FraFactu.Application.DTOs;
using FraFactu.Domain.Entities;
using FraFactu.Infrastructure.Persistence;
using FraFactu.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FraFactu.Tests.Services;

public class CorrelativoInicialServiceTests
{
    private const int EmisorId = 7;

    private static ApplicationDbContext BuildContext()
    {
        var ctx = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"CorrelativoInicial_{Guid.NewGuid()}").Options);
        ctx.Set<Emisor>().Add(new Emisor { Id = EmisorId, NombreRazonSocial = "Mi Empresa" });
        ctx.SaveChanges();
        return ctx;
    }

    [Fact]
    public async Task ObtenerUltimo_SinRegistro_DevuelveCero()
    {
        var ctx = BuildContext();
        var sut = new CorrelativoInicialService(ctx);
        (await sut.ObtenerUltimoCorrelativoAsync(EmisorId, "01", 2026, "01")).Should().Be(0);
    }

    [Fact]
    public async Task Upsert_Crea_Y_ObtenerUltimo_LoDevuelve()
    {
        var ctx = BuildContext();
        var sut = new CorrelativoInicialService(ctx);

        var dto = await sut.UpsertAsync(EmisorId, new CorrelativoInicialUpsertDto
        { TipoDte = "01", Anio = 2026, Ambiente = "01", UltimoCorrelativo = 400 });

        dto.UltimoCorrelativo.Should().Be(400);
        dto.Bloqueado.Should().BeFalse();
        (await sut.ObtenerUltimoCorrelativoAsync(EmisorId, "01", 2026, "01")).Should().Be(400);
    }

    [Fact]
    public async Task Upsert_Existente_Actualiza_SinDuplicar()
    {
        var ctx = BuildContext();
        var sut = new CorrelativoInicialService(ctx);
        await sut.UpsertAsync(EmisorId, new CorrelativoInicialUpsertDto { TipoDte = "01", Anio = 2026, Ambiente = "01", UltimoCorrelativo = 400 });

        await sut.UpsertAsync(EmisorId, new CorrelativoInicialUpsertDto { TipoDte = "01", Anio = 2026, Ambiente = "01", UltimoCorrelativo = 450 });

        (await sut.ObtenerUltimoCorrelativoAsync(EmisorId, "01", 2026, "01")).Should().Be(450);
        ctx.Set<CorrelativoInicial>().Count(c => c.EmisorId == EmisorId && c.TipoDte == "01" && c.Anio == 2026 && c.Ambiente == "01").Should().Be(1);
    }

    [Fact]
    public async Task Upsert_ConFacturasExistentes_Lanza()
    {
        var ctx = BuildContext();
        ctx.Set<FacturaElectronica>().Add(new FacturaElectronica
        {
            Id = 1, EmisorId = EmisorId, NumeroControl = "DTE-01-M001P001-000000000000005",
            FechaEmision = new DateTime(2026, 3, 1), Ambiente = "01"
        });
        ctx.SaveChanges();
        var sut = new CorrelativoInicialService(ctx);

        var act = async () => await sut.UpsertAsync(EmisorId, new CorrelativoInicialUpsertDto
        { TipoDte = "01", Anio = 2026, Ambiente = "01", UltimoCorrelativo = 400 });

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*DTE emitidos*");
    }

    [Fact]
    public async Task Listar_MarcaBloqueado_SegunFacturas()
    {
        var ctx = BuildContext();
        ctx.Set<FacturaElectronica>().Add(new FacturaElectronica
        { Id = 1, EmisorId = EmisorId, NumeroControl = "DTE-01-M001P001-000000000000001", FechaEmision = new DateTime(2026, 2, 1), Ambiente = "01" });
        ctx.SaveChanges();
        var sut = new CorrelativoInicialService(ctx);
        await ctx.Set<CorrelativoInicial>().AddAsync(new CorrelativoInicial { EmisorId = EmisorId, TipoDte = "01", Anio = 2026, Ambiente = "01", UltimoCorrelativo = 1 });
        await ctx.Set<CorrelativoInicial>().AddAsync(new CorrelativoInicial { EmisorId = EmisorId, TipoDte = "03", Anio = 2026, Ambiente = "01", UltimoCorrelativo = 80 });
        await ctx.SaveChangesAsync();

        var lista = await sut.ListarAsync(EmisorId, 2026, "01");

        lista.Should().HaveCount(2);
        lista.Single(x => x.TipoDte == "01").Bloqueado.Should().BeTrue();
        lista.Single(x => x.TipoDte == "03").Bloqueado.Should().BeFalse();
    }

    [Fact]
    public async Task Upsert_CorrelativoNegativo_Lanza()
    {
        var ctx = BuildContext();
        var sut = new CorrelativoInicialService(ctx);
        var act = async () => await sut.UpsertAsync(EmisorId, new CorrelativoInicialUpsertDto
        { TipoDte = "01", Anio = 2026, Ambiente = "01", UltimoCorrelativo = -1 });
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Upsert_TipoDteInvalido_Lanza()
    {
        var ctx = BuildContext();
        var sut = new CorrelativoInicialService(ctx);
        var act = async () => await sut.UpsertAsync(EmisorId, new CorrelativoInicialUpsertDto
        { TipoDte = "99", Anio = 2026, Ambiente = "01", UltimoCorrelativo = 10 });
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Upsert_AmbienteInvalido_Lanza()
    {
        var ctx = BuildContext();
        var sut = new CorrelativoInicialService(ctx);
        var act = async () => await sut.UpsertAsync(EmisorId, new CorrelativoInicialUpsertDto
        { TipoDte = "01", Anio = 2026, Ambiente = "99", UltimoCorrelativo = 10 });
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
