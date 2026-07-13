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

namespace FraFactu.Tests.IntegrationTests.Services;

/// <summary>
/// Tests de P0-B (Normativa DTE V2.0): resolución del distrito del receptor por la triada
/// departamento→municipio→distrito (CAT-008) y validación dura del distrito según el tipo
/// de DTE (obligatorio en CCF/NC/ND/FSE, exento en Factura).
/// </summary>
public class FacturaServiceDistritoTests
{
    private const int EmisorId = 10;

    private static ApplicationDbContext BuildContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"Distrito_{Guid.NewGuid()}")
            .Options);

    private static FacturaService BuildService(ApplicationDbContext ctx) =>
        new(
            ctx,
            Mock.Of<IMapper>(),
            Mock.Of<IInventarioIntegrationService>(),
            Mock.Of<IHaciendaApiService>(),
            Mock.Of<IHaciendaRetryService>(),
            Mock.Of<IEventoContingenciaService>(),
            Mock.Of<ICurrentUserService>(),
            Mock.Of<IHttpContextAccessor>(),
            NullLogger<FacturaService>.Instance,
            Mock.Of<IEmailService>(),
            Mock.Of<ICrossDbCorrelativoService>(),
            Mock.Of<ICorrelativoInicialService>(),
            Mock.Of<ISaldoDteService>(),
            Mock.Of<ITelemetryService>(),
            Mock.Of<IFacturaQueryService>(),
            new DteJsonBuilder(ctx, NullLogger<DteJsonBuilder>.Instance),
            new FacturaLoteSync(ctx, NullLogger<FacturaLoteSync>.Instance),
            Mock.Of<IFacturaInvalidacionService>());

    private static void SeedDistritos(ApplicationDbContext ctx)
    {
        // Misma forma que el catálogo real (códigos de 2 dígitos por triada).
        ctx.CatDistritos.AddRange(
            new CatDistrito { Id = 1, Codigo = "13", CodigoDepartamento = "06", CodigoMunicipio = "14", Valor = "San Salvador Centro" },
            new CatDistrito { Id = 2, Codigo = "13", CodigoDepartamento = "01", CodigoMunicipio = "13", Valor = "Atiquizaya" } // mismo código de distrito, otra triada
        );
        ctx.SaveChanges();
    }

    private static Receptor SeedReceptor(ApplicationDbContext ctx, int id, int? catDistritoId)
    {
        var receptor = new Receptor
        {
            Id = id,
            EmisorId = EmisorId,
            CatTipoDocumentoIdentificacionReceptorId = 1,
            NumeroDocumento = $"0614180494102{id}",
            NombreRazonSocial = "CLIENTE DE PRUEBA",
            // Asegurar tetrada parcial: depto/muni/dir poblados, solo varía distrito.
            // (El validador post-2026-06-11 chequea los 4 campos; estos tests siguen
            //  enfocados en la semántica original "falta solo el distrito".)
            CatDepartamentoId = 1,
            CatMunicipioId    = 1,
            Direccion         = "Calle Test",
            CatDistritoId     = catDistritoId
        };
        ctx.Receptores.Add(receptor);
        ctx.SaveChanges();
        return receptor;
    }

    // ─────────────────────────────── Resolver por triada ───────────────────────────────

    [Fact]
    public async Task ConvertirDistrito_TriadaValida_ResuelveIdCorrecto()
    {
        var ctx = BuildContext();
        SeedDistritos(ctx);

        var id = await BuildService(ctx).ConvertirDistritoACatalogoId("06", "14", "13");

        id.Should().Be(1);
    }

    [Fact]
    public async Task ConvertirDistrito_MismoCodigoOtraTriada_ResuelvePorElMunicipioCorrecto()
    {
        // El código "13" existe en dos triadas distintas: debe desambiguar por dep+muni.
        var ctx = BuildContext();
        SeedDistritos(ctx);

        var id = await BuildService(ctx).ConvertirDistritoACatalogoId("01", "13", "13");

        id.Should().Be(2);
    }

    [Fact]
    public async Task ConvertirDistrito_TriadaInexistente_DevuelveNull()
    {
        var ctx = BuildContext();
        SeedDistritos(ctx);

        var id = await BuildService(ctx).ConvertirDistritoACatalogoId("06", "14", "99");

        id.Should().BeNull();
    }

    [Theory]
    [InlineData(null, "14", "13")]
    [InlineData("06", null, "13")]
    [InlineData("06", "14", null)]
    public async Task ConvertirDistrito_CodigoFaltante_DevuelveNull(string? dep, string? mun, string? dis)
    {
        var ctx = BuildContext();
        SeedDistritos(ctx);

        var id = await BuildService(ctx).ConvertirDistritoACatalogoId(dep, mun, dis);

        id.Should().BeNull();
    }

    // ─────────────────────────────── Validación dura por tipo ───────────────────────────────

    [Theory]
    [InlineData("03")] // CCF
    [InlineData("05")] // Nota de Crédito
    [InlineData("06")] // Nota de Débito
    [InlineData("14")] // Factura de Sujeto Excluido
    public async Task ValidarDistrito_DocumentoQueExigeDistrito_SinDistrito_Lanza(string tipoDte)
    {
        var ctx = BuildContext();
        var receptor = SeedReceptor(ctx, id: 100, catDistritoId: null);
        var service = BuildService(ctx);

        var act = async () => await service.ValidarDireccionReceptorAsync(tipoDte, receptor.Id);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*distrito*");
    }

    [Theory]
    [InlineData("03")]
    [InlineData("05")]
    [InlineData("06")]
    [InlineData("14")]
    public async Task ValidarDistrito_DocumentoQueExigeDistrito_ConDistrito_NoLanza(string tipoDte)
    {
        var ctx = BuildContext();
        var receptor = SeedReceptor(ctx, id: 101, catDistritoId: 1);
        var service = BuildService(ctx);

        var act = async () => await service.ValidarDireccionReceptorAsync(tipoDte, receptor.Id);

        await act.Should().NotThrowAsync();
    }

    [Theory]
    [InlineData("03")]
    [InlineData("14")]
    public async Task ValidarDistrito_DocumentoQueExigeDistrito_SinReceptor_Lanza(string tipoDte)
    {
        var ctx = BuildContext();
        var service = BuildService(ctx);

        var act = async () => await service.ValidarDireccionReceptorAsync(tipoDte, null);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ValidarDistrito_Factura01_SinDistrito_NoLanza()
    {
        // La Factura (01) está exenta: admite consumidor final sin dirección.
        var ctx = BuildContext();
        var receptor = SeedReceptor(ctx, id: 102, catDistritoId: null);
        var service = BuildService(ctx);

        var act = async () => await service.ValidarDireccionReceptorAsync("01", receptor.Id);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ValidarDistrito_Factura01_SinReceptor_NoLanza()
    {
        var ctx = BuildContext();
        var service = BuildService(ctx);

        var act = async () => await service.ValidarDireccionReceptorAsync("01", null);

        await act.Should().NotThrowAsync();
    }
}
