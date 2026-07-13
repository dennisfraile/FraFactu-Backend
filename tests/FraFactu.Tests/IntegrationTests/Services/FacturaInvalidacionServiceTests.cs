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
/// Caracterización de la anulación/actualización de estado de facturas (Fase 3 del refactor
/// de la God class FacturaService). Cubre los flujos credential-independent (sin transmisión a MH).
/// </summary>
public class FacturaInvalidacionServiceTests
{
    private const int EmisorId = 10;

    private static ApplicationDbContext BuildContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"Invalidacion_{Guid.NewGuid()}")
            .Options);

    // Replica el constructor ACTUAL de FacturaService (18 args). Task 2 le añadió el
    // IFacturaLoteSync y Task 3 el IFacturaInvalidacionService (ambos como últimos args).
    // Aquí se cablea la implementación REAL de FacturaInvalidacionService (no un mock) para
    // que estos tests de caracterización prueben la delegación de la fachada, no solo que
    // exista un método que reenvíe la llamada.
    private static FacturaService BuildFacturaService(ApplicationDbContext ctx)
    {
        var loteSync = new FacturaLoteSync(ctx, NullLogger<FacturaLoteSync>.Instance);
        var invalidacion = new FacturaInvalidacionService(
            ctx,
            Mock.Of<IHaciendaApiService>(),
            Mock.Of<IInventarioIntegrationService>(),
            Mock.Of<ISaldoDteService>(),
            Mock.Of<IEmailService>(),
            NullLogger<FacturaInvalidacionService>.Instance,
            loteSync);

        return new FacturaService(
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
            loteSync,
            invalidacion);
    }

    private static FacturaElectronica Factura(int id, string estado)
    {
        return new FacturaElectronica
        {
            Id = id,
            EmisorId = EmisorId,
            CatTipoDocumentoId = 1,
            TipoDocumento = new CatTipoDocumento { Id = 1, Codigo = "01", Valor = "Factura" },
            NumeroControl = $"DTE-01-M001P001-00000000000000{id}",
            CodigoGeneracion = Guid.NewGuid().ToString(),
            EstadoHacienda = estado,
            Ambiente = "01",
            FechaEmision = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc),
            HoraEmision = new TimeSpan(10, 0, 0),
            FechaCreacion = DateTime.UtcNow,
            CatTipoTransmisionId = 1,
            TotalPagar = 100m,
            Activo = true
        };
    }

    [Fact]
    public async Task ActualizarEstadoHaciendaAsync_ActualizaEstadoYSello()
    {
        var ctx = BuildContext();
        ctx.Facturas.Add(Factura(1, "PENDIENTE_ENVIO"));
        ctx.SaveChanges();

        var ok = await BuildFacturaService(ctx)
            .ActualizarEstadoHaciendaAsync(1, EmisorId, "PROCESADO", "SELLO-123");

        ok.Should().BeTrue();
        var f = await ctx.Facturas.FindAsync(1);
        f!.EstadoHacienda.Should().Be("PROCESADO");
        f.SelloRecibido.Should().Be("SELLO-123");
    }

    [Fact]
    public async Task ActualizarEstadoHaciendaAsync_FacturaOtroEmisor_RetornaFalse()
    {
        var ctx = BuildContext();
        var otra = Factura(1, "PENDIENTE_ENVIO");
        otra.EmisorId = 999;
        ctx.Facturas.Add(otra);
        ctx.SaveChanges();

        var ok = await BuildFacturaService(ctx)
            .ActualizarEstadoHaciendaAsync(1, EmisorId, "PROCESADO", "SELLO-123");

        ok.Should().BeFalse();
    }
}
