using AutoMapper;
using FraFactu.Application.Common.Interfaces;
using FraFactu.Application.Interfaces;
using FraFactu.Application.Interfaces.Hacienda;
using FraFactu.Application.Services;
using FraFactu.Domain.Entities;
using FraFactu.Domain.Entities.Catalogos;
using FraFactu.Infrastructure.Persistence;
using FraFactu.Infrastructure.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace FraFactu.Tests.IntegrationTests.Services;

public class ValidarDireccionReceptorAsyncTests
{
    private static ApplicationDbContext BuildContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"Validar_{System.Guid.NewGuid()}").Options);

    private static FacturaService BuildService(ApplicationDbContext ctx) =>
        new(ctx, Mock.Of<IMapper>(), Mock.Of<IInventarioIntegrationService>(),
            Mock.Of<IHaciendaApiService>(), Mock.Of<IHaciendaRetryService>(),
            Mock.Of<IEventoContingenciaService>(), Mock.Of<ICurrentUserService>(),
            Mock.Of<IHttpContextAccessor>(), NullLogger<FacturaService>.Instance,
            Mock.Of<IEmailService>(), Mock.Of<ICrossDbCorrelativoService>(),
            Mock.Of<ICorrelativoInicialService>(),
            Mock.Of<ISaldoDteService>(), Mock.Of<ISmartCareWebhookService>(),
            Mock.Of<ITelemetryService>());

    private static void SeedReceptor(ApplicationDbContext ctx, int id, int? depId, int? muniId, int? disId, string? direccion)
    {
        ctx.Receptores.Add(new Receptor
        {
            Id = id, EmisorId = 1, NombreRazonSocial = "Test",
            NumeroDocumento = "00000000000000", CatTipoDocumentoIdentificacionReceptorId = 1,
            Direccion = direccion ?? "",
            CatDepartamentoId = depId, CatMunicipioId = muniId, CatDistritoId = disId
        });
        ctx.SaveChanges();
    }

    [Fact]
    public async System.Threading.Tasks.Task FC01_ReceptorIncompleto_NoLanza()
    {
        var ctx = BuildContext();
        SeedReceptor(ctx, 100, null, null, null, null);
        var svc = BuildService(ctx);
        var act = async () => await svc.ValidarDireccionReceptorAsync("01", 100);
        await act.Should().NotThrowAsync();
    }

    [Theory]
    [InlineData("03")] [InlineData("04")] [InlineData("05")] [InlineData("06")] [InlineData("14")]
    public async System.Threading.Tasks.Task TiposQueRequierenDireccion_TetradaVacia_LanzaConCamposCitados(string tipoDte)
    {
        var ctx = BuildContext();
        SeedReceptor(ctx, 200, null, null, null, "");
        var svc = BuildService(ctx);
        var act = async () => await svc.ValidarDireccionReceptorAsync(tipoDte, 200);
        var ex = await act.Should().ThrowAsync<System.InvalidOperationException>();
        ex.Which.Message.Should().Contain("departamento")
                        .And.Contain("municipio")
                        .And.Contain("distrito")
                        .And.Contain("complemento");
    }

    [Fact]
    public async System.Threading.Tasks.Task CCF03_SoloComplemento_LanzaConTriadaCitada()
    {
        var ctx = BuildContext();
        SeedReceptor(ctx, 300, null, null, null, "Calle X");
        var svc = BuildService(ctx);
        var act = async () => await svc.ValidarDireccionReceptorAsync("03", 300);
        var ex = await act.Should().ThrowAsync<System.InvalidOperationException>();
        ex.Which.Message.Should().Contain("departamento")
                        .And.Contain("municipio")
                        .And.Contain("distrito");
        ex.Which.Message.Should().NotContain("complemento");
    }

    [Fact]
    public async System.Threading.Tasks.Task CCF03_SoloDistritoFalta_MensajeCitaSoloDistrito()
    {
        var ctx = BuildContext();
        SeedReceptor(ctx, 400, 7, 24, null, "Calle X");
        var svc = BuildService(ctx);
        var act = async () => await svc.ValidarDireccionReceptorAsync("03", 400);
        var ex = await act.Should().ThrowAsync<System.InvalidOperationException>();
        ex.Which.Message.Should().Contain("distrito");
        ex.Which.Message.Should().NotContain("departamento, ").And.NotContain("municipio, ");
    }

    [Fact]
    public async System.Threading.Tasks.Task CCF03_TetradaCompleta_NoLanza()
    {
        var ctx = BuildContext();
        SeedReceptor(ctx, 500, 7, 24, 111, "Calle X");
        var svc = BuildService(ctx);
        var act = async () => await svc.ValidarDireccionReceptorAsync("03", 500);
        await act.Should().NotThrowAsync();
    }
}
