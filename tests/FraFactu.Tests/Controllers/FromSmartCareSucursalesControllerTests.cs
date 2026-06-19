using FraFactu.API.Controllers;
using FraFactu.Application.Common.Settings;
using FraFactu.Application.DTOs;
using FraFactu.Domain.Entities;
using FraFactu.Domain.Enums;
using FraFactu.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace FraFactu.Tests.Controllers;

/// <summary>
/// Tests del endpoint catalogo-only que SmartCare consume para construir el dropdown
/// de servicios disponibles en el modal "Agregar mapeo" del modulo Facturacion
/// electronica por clinica.
/// </summary>
public class FromSmartCareSucursalesControllerTests
{
    private const string ApiKey = "test-smartcare-key";

    private static ApplicationDbContext BuildCtx()
    {
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"FromSmartCareSuc_{Guid.NewGuid()}")
            .Options;
        return new ApplicationDbContext(opts);
    }

    private static FromSmartCareSucursalesController BuildController(ApplicationDbContext db, string? apiKey = ApiKey)
    {
        var settings = Options.Create(new SmartCareSettings { ApiKey = ApiKey });
        var ctrl = new FromSmartCareSucursalesController(db, settings);
        ctrl.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        if (apiKey is not null)
            ctrl.ControllerContext.HttpContext.Request.Headers["X-Api-Key"] = apiKey;
        return ctrl;
    }

    private static async Task SeedAsync(ApplicationDbContext db)
    {
        db.Sucursales.Add(new Sucursal
        {
            Id = 1, EmisorId = 10, Activo = true,
            Codigo = "SUC01", Nombre = "Casa Matriz",
            CatDepartamentoId = 1, CatMunicipioId = 1, CatTipoEstablecimientoId = 1,
            HubSucursalId = 500, // vinculada a Hub-sucursal — habilita servir catalogo a SmartCare
        });
        db.Sucursales.Add(new Sucursal
        {
            Id = 2, EmisorId = 10, Activo = false, // inactiva
            Codigo = "SUC02", Nombre = "Sucursal Vieja",
            CatDepartamentoId = 1, CatMunicipioId = 1, CatTipoEstablecimientoId = 1,
            HubSucursalId = 501,
        });
        // Sucursal activa pero SIN vinculo Hub (cadena cross-app rota): defensive 404.
        db.Sucursales.Add(new Sucursal
        {
            Id = 3, EmisorId = 10, Activo = true,
            Codigo = "SUC03", Nombre = "Huerfana sin Hub",
            CatDepartamentoId = 1, CatMunicipioId = 1, CatTipoEstablecimientoId = 1,
            HubSucursalId = null,
        });

        // Producto asignado explicito a sucursal 1.
        db.ProductosServicios.Add(new ProductoServicio
        {
            Id = 100, EmisorId = 10, Codigo = "SERV-001", Nombre = "Consulta general",
            PrecioVenta = 22.12m, Activo = true, AccesoTodasSucursales = false,
            CatTipoItemId = 2,
        });
        db.ProductosServiciosSucursales.Add(new ProductoServicioSucursal
        {
            ProductoServicioId = 100, SucursalId = 1
        });

        // Producto con AccesoTodasSucursales=true (sin row puente).
        db.ProductosServicios.Add(new ProductoServicio
        {
            Id = 101, EmisorId = 10, Codigo = "PROD-002", Nombre = "Gasa esteril",
            PrecioVenta = 1.13m, Activo = true, AccesoTodasSucursales = true,
            CatTipoItemId = 1,
        });

        // Producto inactivo (NO debe aparecer).
        db.ProductosServicios.Add(new ProductoServicio
        {
            Id = 102, EmisorId = 10, Codigo = "OLD", Nombre = "Servicio descontinuado",
            PrecioVenta = 10m, Activo = false, AccesoTodasSucursales = true,
            CatTipoItemId = 2,
        });

        // Producto de OTRO emisor (NO debe aparecer).
        db.ProductosServicios.Add(new ProductoServicio
        {
            Id = 200, EmisorId = 99, Codigo = "OTRO-001", Nombre = "De otro emisor",
            PrecioVenta = 50m, Activo = true, AccesoTodasSucursales = true,
            CatTipoItemId = 2,
        });

        // Producto NO asignado a sucursal 1 (sin row puente, AccesoTodasSucursales=false).
        db.ProductosServicios.Add(new ProductoServicio
        {
            Id = 103, EmisorId = 10, Codigo = "RESTRINGIDO", Nombre = "Solo otra sucursal",
            PrecioVenta = 5m, Activo = true, AccesoTodasSucursales = false,
            CatTipoItemId = 1,
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task ListarServicios_SinApiKey_Unauthorized()
    {
        var db = BuildCtx();
        await SeedAsync(db);
        var ctrl = BuildController(db, apiKey: null);

        var result = await ctrl.ListarServicios(1, CancellationToken.None);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task ListarServicios_ApiKeyInvalida_Unauthorized()
    {
        var db = BuildCtx();
        await SeedAsync(db);
        var ctrl = BuildController(db, apiKey: "wrong-key");

        var result = await ctrl.ListarServicios(1, CancellationToken.None);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task ListarServicios_SucursalNoExiste_NotFound()
    {
        var db = BuildCtx();
        await SeedAsync(db);
        var ctrl = BuildController(db);

        var result = await ctrl.ListarServicios(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ListarServicios_SucursalInactiva_NotFound()
    {
        var db = BuildCtx();
        await SeedAsync(db);
        var ctrl = BuildController(db);

        var result = await ctrl.ListarServicios(2, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ListarServicios_SucursalSinHubSucursalId_NotFound()
    {
        // Defensive: si la sucursal Smartix ya no esta vinculada a un Hub-sucursal
        // (cadena cross-app rota / race con unlink Emisor<->Hub / cache stale en
        // smartHubClient de SmartCare), no debemos servir catalogo. 404 + mensaje
        // especifico "no esta vinculada".
        var db = BuildCtx();
        await SeedAsync(db);
        var ctrl = BuildController(db);

        var result = await ctrl.ListarServicios(3, CancellationToken.None);

        var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFound.Value.Should().NotBeNull();
        notFound.Value!.ToString().Should().Contain("no esta vinculada");
    }

    [Fact]
    public async Task ListarServicios_HappyPath_DevuelveAsignadosYAccesoTodas_FiltraInactivosYOtroEmisor()
    {
        var db = BuildCtx();
        await SeedAsync(db);
        var ctrl = BuildController(db);

        var result = await ctrl.ListarServicios(1, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var items = ok.Value.Should().BeAssignableTo<IEnumerable<ServicioParaMapeoDto>>().Subject.ToList();
        items.Should().HaveCount(2);
        // Bug #1 IVA (2026-05-29): ahora devolvemos precio CON IVA cuando Gravado.
        // El default del entity ProductoServicio es TipoImpuesto.Gravado + PorcentajeIVA=13.
        // Id 100: 22.12 * 1.13 = 24.9956 → round HALF_AWAY_FROM_ZERO 2 dec = 25.00.
        // Id 101: 1.13  * 1.13 = 1.2769  → round HALF_AWAY_FROM_ZERO 2 dec = 1.28.
        items.Should().ContainSingle(i => i.Id == 100 && i.Codigo == "SERV-001" && i.PrecioUnitario == 25.00m && i.CatTipoItemId == 2);
        items.Should().ContainSingle(i => i.Id == 101 && i.Codigo == "PROD-002" && i.PrecioUnitario == 1.28m && i.CatTipoItemId == 1);
        // No aparece: inactivo (102), de otro emisor (200), restringido a otra sucursal (103).
        items.Should().NotContain(i => i.Id == 102);
        items.Should().NotContain(i => i.Id == 200);
        items.Should().NotContain(i => i.Id == 103);
    }

    [Fact]
    public async Task ListarServicios_Exento_NoSuma_IVA()
    {
        // Bug #1 IVA — Exento y NoSujeto deben devolver PrecioVenta tal cual.
        var db = BuildCtx();
        await SeedAsync(db);
        db.ProductosServicios.Add(new ProductoServicio
        {
            Id = 110, EmisorId = 10, Codigo = "EXENTO-01", Nombre = "Servicio exento",
            PrecioVenta = 100m, Activo = true, AccesoTodasSucursales = true,
            CatTipoItemId = 2,
            TipoImpuesto = TipoImpuesto.Exento,
            PorcentajeIVA = null,
        });
        db.ProductosServicios.Add(new ProductoServicio
        {
            Id = 111, EmisorId = 10, Codigo = "NOSUJ-01", Nombre = "Servicio no sujeto",
            PrecioVenta = 75m, Activo = true, AccesoTodasSucursales = true,
            CatTipoItemId = 2,
            TipoImpuesto = TipoImpuesto.NoSujeto,
            PorcentajeIVA = null,
        });
        await db.SaveChangesAsync();

        var ctrl = BuildController(db);
        var result = await ctrl.ListarServicios(1, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var items = ok.Value.Should().BeAssignableTo<IEnumerable<ServicioParaMapeoDto>>().Subject.ToList();
        items.Should().ContainSingle(i => i.Id == 110 && i.PrecioUnitario == 100m);
        items.Should().ContainSingle(i => i.Id == 111 && i.PrecioUnitario == 75m);
    }
}
