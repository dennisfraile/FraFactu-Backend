using AutoMapper;
using FraFactu.Application.Common.Interfaces;
using FraFactu.Application.Interfaces;
using FraFactu.Application.Interfaces.Hacienda;
using FraFactu.Application.Services;
using FraFactu.Domain.Entities;
using FraFactu.Infrastructure.Persistence;
using FraFactu.Infrastructure.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FraFactu.Tests.IntegrationTests.Services;

/// <summary>
/// Tests de P1-A.1: reintento automático de facturas en estado ERROR (regla 13.2.1).
/// Cubren la lógica nueva: selección con ventana ≥ 15 min, escalado a contingencia al
/// agotar el tope (3) e incremento del contador de intentos.
/// </summary>
public class FacturaServiceReintentoErrorTests
{
    private const int EmisorId = 10;

    private static ApplicationDbContext BuildContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"Reintento_{Guid.NewGuid()}")
            .Options);

    private static FacturaService BuildService(ApplicationDbContext ctx,
        Mock<IEventoContingenciaService>? contingencia = null) =>
        new(
            ctx,
            Mock.Of<IMapper>(),
            Mock.Of<IInventarioIntegrationService>(),
            Mock.Of<IHaciendaApiService>(),
            Mock.Of<IHaciendaRetryService>(),
            (contingencia ?? new Mock<IEventoContingenciaService>()).Object,
            Mock.Of<ICurrentUserService>(),
            Mock.Of<IHttpContextAccessor>(),
            NullLogger<FacturaService>.Instance,
            Mock.Of<IEmailService>(),
            Mock.Of<ICrossDbCorrelativoService>(),
            Mock.Of<ICorrelativoInicialService>(),
            Mock.Of<ISaldoDteService>(),
            Mock.Of<ITelemetryService>(),
            Mock.Of<IFacturaQueryService>());

    private static FacturaElectronica NuevaFactura(int id, string estado, int intentos,
        DateTime? fechaError)
    {
        return new FacturaElectronica
        {
            Id = id,
            EmisorId = EmisorId,
            CatTipoDocumentoId = 1,
            CatModeloFacturacionId = 1,
            CatTipoTransmisionId = 1,
            NumeroControl = $"DTE-01-M001P001-00000000000000{id}",
            CodigoGeneracion = Guid.NewGuid().ToString(),
            EstadoHacienda = estado,
            IntentosEnvio = intentos,
            FechaErrorEnvio = fechaError,
            FechaEmision = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            HoraEmision = new TimeSpan(10, 0, 0),
            TotalPagar = 100m,
            Activo = true
        };
    }

    [Fact]
    public async Task Reintento_ErrorConToteAgotado_EscalaAContingencia()
    {
        var ctx = BuildContext();
        // IntentosEnvio = 3 (tope) → debe escalar, no reintentar.
        ctx.Facturas.Add(NuevaFactura(1, "ERROR", intentos: 3, fechaError: DateTime.UtcNow.AddHours(-1)));
        ctx.SaveChanges();

        var contingencia = new Mock<IEventoContingenciaService>();
        contingencia
            .Setup(s => s.ObtenerOCrearEventoContingenciaAutomaticoAsync(EmisorId, It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(99);

        var resultado = await BuildService(ctx, contingencia).ReintentarEnviosEnErrorAsync();

        resultado.Escaladas.Should().Be(1);
        resultado.Reintentadas.Should().Be(0);

        var factura = await ctx.Facturas.FindAsync(1);
        factura!.EstadoHacienda.Should().Be("PENDIENTE_LOTE");
        factura.CatTipoTransmisionId.Should().Be(2);
        factura.CatModeloFacturacionId.Should().Be(2);
        factura.EventoContingenciaId.Should().Be(99);
    }

    [Fact]
    public async Task Reintento_ErrorRecienteDentroDe15Min_NoSeToca()
    {
        var ctx = BuildContext();
        // Error hace 5 min → todavía dentro de la ventana, no debe reintentarse.
        ctx.Facturas.Add(NuevaFactura(1, "ERROR", intentos: 0, fechaError: DateTime.UtcNow.AddMinutes(-5)));
        ctx.SaveChanges();

        var resultado = await BuildService(ctx).ReintentarEnviosEnErrorAsync();

        resultado.Candidatas.Should().Be(0);

        var factura = await ctx.Facturas.FindAsync(1);
        factura!.EstadoHacienda.Should().Be("ERROR");
        factura.IntentosEnvio.Should().Be(0);
    }

    [Fact]
    public async Task Reintento_ErrorElegible_IncrementaContadorYReintenta()
    {
        var ctx = BuildContext();
        // Sin grafo de emisor → el reenvío real fallará, pero debe quedar registrado el intento.
        ctx.Facturas.Add(NuevaFactura(1, "ERROR", intentos: 0, fechaError: null));
        ctx.SaveChanges();

        var resultado = await BuildService(ctx).ReintentarEnviosEnErrorAsync();

        resultado.Candidatas.Should().Be(1);
        resultado.Fallidas.Should().Be(1);

        var factura = await ctx.Facturas.FindAsync(1);
        factura!.IntentosEnvio.Should().Be(1);            // contador incrementado
        factura.FechaErrorEnvio.Should().NotBeNull();     // ventana de 15 min reiniciada
        factura.EstadoHacienda.Should().NotBe("PROCESADO"); // el reenvío no tuvo éxito
    }

    [Fact]
    public async Task Reintento_FacturasEnOtrosEstados_NoSeTocan()
    {
        var ctx = BuildContext();
        ctx.Facturas.Add(NuevaFactura(1, "PROCESADO", intentos: 0, fechaError: null));
        ctx.Facturas.Add(NuevaFactura(2, "PENDIENTE_ENVIO", intentos: 0, fechaError: null));
        ctx.Facturas.Add(NuevaFactura(3, "RECHAZADO", intentos: 0, fechaError: null));
        ctx.SaveChanges();

        var resultado = await BuildService(ctx).ReintentarEnviosEnErrorAsync();

        resultado.Candidatas.Should().Be(0);
        (await ctx.Facturas.FindAsync(1))!.EstadoHacienda.Should().Be("PROCESADO");
        (await ctx.Facturas.FindAsync(2))!.EstadoHacienda.Should().Be("PENDIENTE_ENVIO");
        (await ctx.Facturas.FindAsync(3))!.EstadoHacienda.Should().Be("RECHAZADO");
    }
}
