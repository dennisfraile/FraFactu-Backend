using FraFactu.Infrastructure.Persistence;
using FraFactu.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FraFactu.Tests.Services;

public class NotificacionServiceTests
{
    private const int EmisorId = 7;
    private const int UsuarioA = 100;
    private const int UsuarioB = 200;

    private static ApplicationDbContext BuildContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"Notif_{Guid.NewGuid()}").Options);

    [Fact]
    public async Task Crear_DevuelveDtoNoLeida()
    {
        var ctx = BuildContext();
        var sut = new NotificacionService(ctx);

        var dto = await sut.CrearAsync(EmisorId, "CuotaVencida", "Título", "Mensaje",
            ruta: "/cuentas-por-cobrar", referenciaId: 55, nivel: "warning");

        dto.Id.Should().BeGreaterThan(0);
        dto.Leida.Should().BeFalse();
        dto.Nivel.Should().Be("warning");
        dto.ReferenciaId.Should().Be(55);
    }

    [Fact]
    public async Task Listar_MarcaLeidaPorUsuario()
    {
        var ctx = BuildContext();
        var sut = new NotificacionService(ctx);
        var n1 = await sut.CrearAsync(EmisorId, "t", "n1", "m");
        await sut.CrearAsync(EmisorId, "t", "n2", "m");
        await sut.MarcarLeidaAsync(n1.Id, UsuarioA);

        var paraA = await sut.ListarAsync(EmisorId, UsuarioA);
        var paraB = await sut.ListarAsync(EmisorId, UsuarioB);

        paraA.Single(n => n.Id == n1.Id).Leida.Should().BeTrue();
        paraA.Count(n => n.Leida).Should().Be(1);
        paraB.All(n => !n.Leida).Should().BeTrue();
    }

    [Fact]
    public async Task ContarNoLeidas_ExcluyeLeidasDeEseUsuario()
    {
        var ctx = BuildContext();
        var sut = new NotificacionService(ctx);
        var n1 = await sut.CrearAsync(EmisorId, "t", "n1", "m");
        await sut.CrearAsync(EmisorId, "t", "n2", "m");

        (await sut.ContarNoLeidasAsync(EmisorId, UsuarioA)).Should().Be(2);
        await sut.MarcarLeidaAsync(n1.Id, UsuarioA);
        (await sut.ContarNoLeidasAsync(EmisorId, UsuarioA)).Should().Be(1);
        (await sut.ContarNoLeidasAsync(EmisorId, UsuarioB)).Should().Be(2);
    }

    [Fact]
    public async Task MarcarLeida_EsIdempotente()
    {
        var ctx = BuildContext();
        var sut = new NotificacionService(ctx);
        var n1 = await sut.CrearAsync(EmisorId, "t", "n1", "m");

        await sut.MarcarLeidaAsync(n1.Id, UsuarioA);
        await sut.MarcarLeidaAsync(n1.Id, UsuarioA);

        (await ctx.Set<FraFactu.Domain.Entities.NotificacionLeida>()
            .CountAsync(l => l.NotificacionId == n1.Id && l.UsuarioId == UsuarioA)).Should().Be(1);
    }

    [Fact]
    public async Task MarcarTodas_DejaContadorEnCero()
    {
        var ctx = BuildContext();
        var sut = new NotificacionService(ctx);
        await sut.CrearAsync(EmisorId, "t", "n1", "m");
        await sut.CrearAsync(EmisorId, "t", "n2", "m");

        await sut.MarcarTodasLeidasAsync(EmisorId, UsuarioA);

        (await sut.ContarNoLeidasAsync(EmisorId, UsuarioA)).Should().Be(0);
    }
}
