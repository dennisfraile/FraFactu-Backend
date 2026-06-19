using FraFactu.API.Controllers;
using FraFactu.Application.Common.Settings;
using FraFactu.Domain.Entities;
using FraFactu.Domain.Entities.Catalogos;
using FraFactu.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace FraFactu.Tests.Controllers;

/// <summary>
/// F3 (Plan inventario desde DTE): tests del endpoint server-to-server que
/// recibe el toggle de la app SmartInventory desde SmartHub. Patron de auth
/// con X-Api-Key (compartido con SyncEmisorController).
/// </summary>
public class SyncInventoryAppControllerTests
{
    private const string ApiKey = "test-api-key";

    private static ApplicationDbContext BuildCtx()
    {
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"SyncInventoryApp_{Guid.NewGuid()}")
            .Options;
        return new ApplicationDbContext(opts);
    }

    private static void SeedEmisor(ApplicationDbContext db)
    {
        db.CatDepartamentos.Add(new CatDepartamento { Id = 1, Codigo = "06", Valor = "San Salvador" });
        db.CatMunicipios.Add(new CatMunicipio { Id = 1, Codigo = "23", Valor = "San Salvador" });
        db.CatTiposEstablecimiento.Add(new CatTipoEstablecimiento { Id = 1, Codigo = "01", Valor = "Casa Matriz" });

        db.Emisores.Add(new Emisor
        {
            Id = 1, Activo = true, HubId = 4,
            Nit = "13173006851011",
            Nrc = "2302820",
            NombreRazonSocial = "Sonia Dinarte",
            CodigoActividad = "86202",
            DescripcionActividad = "Odontología",
            CatTipoEstablecimientoId = 1,
            CatDepartamentoId = 1,
            CatMunicipioId = 1,
            Direccion = "x",
            CorreoElectronico = "x@x.com",
            Telefono = "11111111"
        });
        db.SaveChanges();
    }

    private static SyncInventoryAppController BuildController(ApplicationDbContext db, string? apiKeyHeader = ApiKey)
    {
        var settings = Options.Create(new SmartHubSettings { ApiKey = ApiKey });
        var controller = new SyncInventoryAppController(db, settings, NullLogger<SyncInventoryAppController>.Instance);
        var httpCtx = new DefaultHttpContext();
        if (apiKeyHeader is not null)
            httpCtx.Request.Headers["X-Api-Key"] = apiKeyHeader;
        controller.ControllerContext = new ControllerContext { HttpContext = httpCtx };
        return controller;
    }

    [Fact]
    public async Task Toggle_HubExistente_ActualizaFlag()
    {
        var db = BuildCtx();
        SeedEmisor(db);
        var ctrl = BuildController(db);

        var resp = await ctrl.Toggle(
            new InventoryAppToggleRequestDto { HubId = 4, Activa = true },
            CancellationToken.None);

        var dto = (resp.Result as OkObjectResult)!.Value as InventoryAppToggleResponseDto;
        dto.Should().NotBeNull();
        dto!.HubId.Should().Be(4);
        dto.EmisorId.Should().Be(1);
        dto.Encontrado.Should().BeTrue();
        dto.TieneSmartInventoryActiva.Should().BeTrue();

        var persistido = await db.Emisores.SingleAsync(e => e.Id == 1);
        persistido.TieneSmartInventoryActiva.Should().BeTrue();
    }

    [Fact]
    public async Task Toggle_Desactivar_BajaFlagAFalse()
    {
        var db = BuildCtx();
        SeedEmisor(db);
        var emisor = await db.Emisores.SingleAsync(e => e.Id == 1);
        emisor.TieneSmartInventoryActiva = true;
        await db.SaveChangesAsync();
        var ctrl = BuildController(db);

        await ctrl.Toggle(
            new InventoryAppToggleRequestDto { HubId = 4, Activa = false },
            CancellationToken.None);

        var persistido = await db.Emisores.SingleAsync(e => e.Id == 1);
        persistido.TieneSmartInventoryActiva.Should().BeFalse();
    }

    [Fact]
    public async Task Toggle_HubSinEmisor_DevuelveOkConEncontradoFalse()
    {
        // F3: si SmartHub envia el toggle antes de que Smartix tenga el
        // Emisor hidratado, no es error - respondemos OK noop.
        var db = BuildCtx();
        SeedEmisor(db);
        var ctrl = BuildController(db);

        var resp = await ctrl.Toggle(
            new InventoryAppToggleRequestDto { HubId = 999, Activa = true },
            CancellationToken.None);

        var dto = (resp.Result as OkObjectResult)!.Value as InventoryAppToggleResponseDto;
        dto!.Encontrado.Should().BeFalse();
        dto.EmisorId.Should().BeNull();
        dto.TieneSmartInventoryActiva.Should().BeTrue();  // hace eco del flag pedido
    }

    [Fact]
    public async Task Toggle_SinApiKey_Devuelve401()
    {
        var db = BuildCtx();
        SeedEmisor(db);
        var ctrl = BuildController(db, apiKeyHeader: null);

        var resp = await ctrl.Toggle(
            new InventoryAppToggleRequestDto { HubId = 4, Activa = true },
            CancellationToken.None);

        resp.Result.Should().BeOfType<UnauthorizedObjectResult>();
        var sinTocar = await db.Emisores.SingleAsync(e => e.Id == 1);
        sinTocar.TieneSmartInventoryActiva.Should().BeFalse();
    }

    [Fact]
    public async Task Toggle_ApiKeyInvalida_Devuelve401()
    {
        var db = BuildCtx();
        SeedEmisor(db);
        var ctrl = BuildController(db, apiKeyHeader: "otra-key");

        var resp = await ctrl.Toggle(
            new InventoryAppToggleRequestDto { HubId = 4, Activa = true },
            CancellationToken.None);

        resp.Result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Toggle_HubIdInvalido_Devuelve400()
    {
        var db = BuildCtx();
        SeedEmisor(db);
        var ctrl = BuildController(db);

        var resp = await ctrl.Toggle(
            new InventoryAppToggleRequestDto { HubId = 0, Activa = true },
            CancellationToken.None);

        resp.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Toggle_MismoValorDosVeces_EsIdempotente()
    {
        var db = BuildCtx();
        SeedEmisor(db);
        var ctrl = BuildController(db);

        await ctrl.Toggle(new InventoryAppToggleRequestDto { HubId = 4, Activa = true }, CancellationToken.None);
        var resp = await ctrl.Toggle(new InventoryAppToggleRequestDto { HubId = 4, Activa = true }, CancellationToken.None);

        var dto = (resp.Result as OkObjectResult)!.Value as InventoryAppToggleResponseDto;
        dto!.TieneSmartInventoryActiva.Should().BeTrue();
    }
}
