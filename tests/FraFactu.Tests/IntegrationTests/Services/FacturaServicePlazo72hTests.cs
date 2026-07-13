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
/// Tests de P1-A.2 y P1-A.3 (Normativa DTE):
/// - Plazo de 72 h para transmitir DTE en contingencia, contado DESDE EL SELLO del Evento de
///   Contingencia (Cuadro 4 / regla 13.2.2). Solo marca/alerta.
/// - Informe Técnico de Contingencia cuando la contingencia del emisor persiste > 3 días (regla 13.2.1.1).
/// </summary>
public class FacturaServicePlazo72hTests
{
    private const int EmisorId = 10;

    private static ApplicationDbContext BuildContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"Plazo72_{Guid.NewGuid()}")
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
            new FacturaLoteSync(ctx, NullLogger<FacturaLoteSync>.Instance));

    /// <summary>Evento de contingencia con sello a las "selloHaceHoras" horas atrás (o sin sello si null).</summary>
    private static EventoContingencia Evento(int id, double? selloHaceHoras)
    {
        DateTime? sello = selloHaceHoras == null ? null : DateTime.UtcNow.AddHours(-selloHaceHoras.Value);
        return new EventoContingencia
        {
            Id = id,
            EmisorId = EmisorId,
            CodigoGeneracion = Guid.NewGuid().ToString(),
            FechaInicioContingencia = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(-1).Date, DateTimeKind.Utc),
            FechaFinContingencia = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc),
            FechaTransmisionMH = sello,
            SelloRecibido = sello == null ? null : "SELLO-DE-PRUEBA",
            EstadoHacienda = sello == null ? "PENDIENTE" : "RECIBIDO"
        };
    }

    private static FacturaElectronica Factura(int id, int? eventoId, string estado = "PENDIENTE_LOTE",
        double? errorHaceDias = null)
    {
        return new FacturaElectronica
        {
            Id = id,
            EmisorId = EmisorId,
            CatTipoDocumentoId = 1,
            NumeroControl = $"DTE-01-M001P001-00000000000000{id}",
            CodigoGeneracion = Guid.NewGuid().ToString(),
            EstadoHacienda = estado,
            EventoContingenciaId = eventoId,
            FechaErrorEnvio = errorHaceDias == null ? null : DateTime.UtcNow.AddDays(-errorHaceDias.Value),
            FechaEmision = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc),
            HoraEmision = new TimeSpan(10, 0, 0),
            TotalPagar = 100m,
            Activo = true
        };
    }

    // ─────────────────────────────── P1-A.2: plazo 72 h desde el sello del evento ───────────────────────────────

    [Fact]
    public async Task Plazo72h_EventoSelladoHace80h_FacturaVencidaSeMarca()
    {
        var ctx = BuildContext();
        ctx.EventosContingencia.Add(Evento(1, selloHaceHoras: 80)); // > 72h desde el sello
        ctx.Facturas.Add(Factura(1, eventoId: 1));
        ctx.SaveChanges();

        var resultado = await BuildService(ctx).MarcarDiferidosPorVencer72hAsync();

        resultado.Vencidas.Should().Be(1);
        resultado.NuevasVencidas.Should().Be(1);

        var factura = await ctx.Facturas.FindAsync(1);
        factura!.Observaciones.Should().Contain(FacturaService.MarcaPlazo72hVencido);
        factura.EstadoHacienda.Should().Be("PENDIENTE_LOTE"); // no se bloquea
    }

    [Fact]
    public async Task Plazo72h_VencidaYaMarcada_NoSeReMarca()
    {
        var ctx = BuildContext();
        ctx.EventosContingencia.Add(Evento(1, selloHaceHoras: 80));
        ctx.Facturas.Add(Factura(1, eventoId: 1));
        ctx.SaveChanges();
        var service = BuildService(ctx);

        await service.MarcarDiferidosPorVencer72hAsync();
        var segunda = await service.MarcarDiferidosPorVencer72hAsync();

        segunda.Vencidas.Should().Be(1);
        segunda.NuevasVencidas.Should().Be(0);
    }

    [Fact]
    public async Task Plazo72h_EventoSelladoHace65h_PorVencer()
    {
        var ctx = BuildContext();
        ctx.EventosContingencia.Add(Evento(1, selloHaceHoras: 65)); // restan ~7h (< 12h ventana)
        ctx.Facturas.Add(Factura(1, eventoId: 1));
        ctx.SaveChanges();

        var resultado = await BuildService(ctx).MarcarDiferidosPorVencer72hAsync();

        resultado.PorVencer.Should().Be(1);
        resultado.Vencidas.Should().Be(0);
    }

    [Fact]
    public async Task Plazo72h_EventoSelladoHace10h_DentroDePlazo()
    {
        var ctx = BuildContext();
        ctx.EventosContingencia.Add(Evento(1, selloHaceHoras: 10)); // restan ~62h
        ctx.Facturas.Add(Factura(1, eventoId: 1));
        ctx.SaveChanges();

        var resultado = await BuildService(ctx).MarcarDiferidosPorVencer72hAsync();

        resultado.Vencidas.Should().Be(0);
        resultado.PorVencer.Should().Be(0);
    }

    [Fact]
    public async Task Plazo72h_EventoSinSello_NoCorrePlazo()
    {
        var ctx = BuildContext();
        ctx.EventosContingencia.Add(Evento(1, selloHaceHoras: null)); // aún sin sello
        ctx.Facturas.Add(Factura(1, eventoId: 1));
        ctx.SaveChanges();

        var resultado = await BuildService(ctx).MarcarDiferidosPorVencer72hAsync();

        resultado.Vencidas.Should().Be(0);
        resultado.PorVencer.Should().Be(0);
    }

    [Fact]
    public async Task Plazo72h_FacturaSinEvento_NoSeEvalua()
    {
        var ctx = BuildContext();
        ctx.Facturas.Add(Factura(1, eventoId: null)); // PENDIENTE_LOTE sin evento asociado
        ctx.SaveChanges();

        var resultado = await BuildService(ctx).MarcarDiferidosPorVencer72hAsync();

        resultado.Vencidas.Should().Be(0);
        resultado.PorVencer.Should().Be(0);
    }

    // ─────────────────────────────── P1-A.3: Informe Técnico de Contingencia (>3 días) ───────────────────────────────

    [Fact]
    public async Task InformeTecnico_ContingenciaPersiste4Dias_RequiereInforme()
    {
        var ctx = BuildContext();
        ctx.Facturas.Add(Factura(1, eventoId: null, errorHaceDias: 4)); // contingencia hace 4 días
        ctx.SaveChanges();

        var resultado = await BuildService(ctx).DetectarContingenciasParaInformeTecnicoAsync();

        resultado.EmisoresQueRequierenInforme.Should().Be(1);
    }

    [Fact]
    public async Task InformeTecnico_ContingenciaReciente_NoRequiere()
    {
        var ctx = BuildContext();
        ctx.Facturas.Add(Factura(1, eventoId: null, errorHaceDias: 1)); // solo 1 día
        ctx.SaveChanges();

        var resultado = await BuildService(ctx).DetectarContingenciasParaInformeTecnicoAsync();

        resultado.EmisoresQueRequierenInforme.Should().Be(0);
    }

    [Fact]
    public async Task InformeTecnico_FacturasNoEnContingencia_NoCuentan()
    {
        var ctx = BuildContext();
        ctx.Facturas.Add(Factura(1, eventoId: null, estado: "PROCESADO", errorHaceDias: 10));
        ctx.Facturas.Add(Factura(2, eventoId: null, estado: "PENDIENTE_ENVIO", errorHaceDias: 10));
        ctx.SaveChanges();

        var resultado = await BuildService(ctx).DetectarContingenciasParaInformeTecnicoAsync();

        resultado.EmisoresQueRequierenInforme.Should().Be(0);
    }
}
