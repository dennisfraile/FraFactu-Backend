using AutoMapper;
using FraFactu.Application.Common.Interfaces;
using FraFactu.Application.DTOs.Common;
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
/// Caracterización de los métodos de lectura de facturas (Fase 1 del refactor de la
/// God class FacturaService). Fija GetAllAsync (filtro por emisor + exclusión de
/// PENDIENTE_ENVIO/PENDIENTE_LOTE por defecto) y SearchAsync (por número de control).
/// </summary>
public class FacturaQueryServiceTests
{
    private const int EmisorId = 10;

    private static ApplicationDbContext BuildContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"FacturaQuery_{Guid.NewGuid()}")
            .Options);

    // Construye el FacturaService ACTUAL (fachada). En Task 2 se le añade el
    // Mock.Of<IFacturaQueryService>() como último argumento.
    private static FacturaService BuildFacturaService(ApplicationDbContext ctx) =>
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
            Mock.Of<ITelemetryService>());

    // Nota de seeding (Task 1): el InMemory provider de EF Core lanza un conflicto de
    // identidad si se agregan varias instancias de CatTipoDocumento con el mismo Id al
    // mismo contexto (una por cada Factura()). Se comparte una única instancia por test
    // para simular el catálogo real (una sola fila Id=1) sin tocar producción ni asserts.
    private static FacturaElectronica Factura(int id, string numeroControl, string estado, CatTipoDocumento tipoDocumento, string ambiente = "01")
    {
        return new FacturaElectronica
        {
            Id = id,
            EmisorId = EmisorId,
            CatTipoDocumentoId = tipoDocumento.Id,
            TipoDocumento = tipoDocumento,
            NumeroControl = numeroControl,
            CodigoGeneracion = Guid.NewGuid().ToString(),
            EstadoHacienda = estado,
            Ambiente = ambiente,
            FechaEmision = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc),
            HoraEmision = new TimeSpan(10, 0, 0),
            FechaCreacion = DateTime.UtcNow,
            CatTipoTransmisionId = 1,
            TotalPagar = 100m,
            Activo = true
        };
    }

    [Fact]
    public async Task GetAllAsync_ExcluyePendientesPorDefecto_YFiltraPorEmisor()
    {
        var ctx = BuildContext();
        var tipoDocumento = new CatTipoDocumento { Id = 1, Codigo = "01", Valor = "Factura" };
        ctx.Facturas.Add(Factura(1, "DTE-01-M001P001-000000000000001", "PROCESADO", tipoDocumento));
        ctx.Facturas.Add(Factura(2, "DTE-01-M001P001-000000000000002", "PENDIENTE_ENVIO", tipoDocumento));
        ctx.Facturas.Add(Factura(3, "DTE-01-M001P001-000000000000003", "PENDIENTE_LOTE", tipoDocumento));
        // Factura de OTRO emisor: no debe aparecer.
        var otra = Factura(4, "DTE-01-M001P001-000000000000004", "PROCESADO", tipoDocumento);
        otra.EmisorId = 999;
        ctx.Facturas.Add(otra);
        ctx.SaveChanges();

        var result = await BuildFacturaService(ctx)
            .GetAllAsync(new PaginatedRequest { PageNumber = 1, PageSize = 10 }, EmisorId);

        result.Items.Should().ContainSingle();
        result.Items[0].NumeroControl.Should().Be("DTE-01-M001P001-000000000000001");
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task SearchAsync_EncuentraPorNumeroControl()
    {
        var ctx = BuildContext();
        var tipoDocumento = new CatTipoDocumento { Id = 1, Codigo = "01", Valor = "Factura" };
        ctx.Facturas.Add(Factura(1, "DTE-01-M001P001-000000000000777", "PROCESADO", tipoDocumento));
        ctx.Facturas.Add(Factura(2, "DTE-01-M001P001-000000000000888", "PROCESADO", tipoDocumento));
        ctx.SaveChanges();

        var result = await BuildFacturaService(ctx).SearchAsync("000000777", EmisorId);

        result.Should().ContainSingle();
        result[0].NumeroControl.Should().Be("DTE-01-M001P001-000000000000777");
    }
}
