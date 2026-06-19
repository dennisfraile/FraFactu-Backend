using System.Security.Claims;
using FraFactu.API.Controllers;
using FraFactu.Application.DTOs;
using FraFactu.Domain.Entities;
using FraFactu.Domain.Entities.Catalogos;
using FraFactu.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FraFactu.Tests.Controllers;

/// <summary>
/// Plan B Hub-as-Emisor — Tests unitarios del InternalEmisoresController.
/// Valida (a) filtrado libres/tomados, (b) búsqueda por NIT/nombre, (c) rol SuperAdmin
/// obligatorio, (d) conflict 409 si HubId ya está tomado.
/// </summary>
public class InternalEmisoresControllerTests
{
    private static ApplicationDbContext BuildCtx()
    {
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"InternalEmisores_{Guid.NewGuid()}")
            .Options;
        return new ApplicationDbContext(opts);
    }

    private static void SeedEmisores(ApplicationDbContext db)
    {
        // Catalogos MH (FK NOT NULL desde Emisor; sin estos rows el .Include
        // INNER JOIN del controller deja a EF InMemory descartando el row).
        db.CatDepartamentos.Add(new CatDepartamento { Id = 1, Codigo = "06", Valor = "San Salvador" });
        db.CatMunicipios.Add(new CatMunicipio { Id = 1, Codigo = "23", Valor = "San Salvador" });
        db.CatTiposEstablecimiento.Add(new CatTipoEstablecimiento { Id = 1, Codigo = "01", Valor = "Casa Matriz" });

        // Emisor 1 vinculado a Hub 4 (caso "tomado")
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
            Telefono = "11111111",
        });
        // Emisor 2 libre
        db.Emisores.Add(new Emisor
        {
            Id = 2, Activo = true, HubId = null,
            Nit = "06140123456789",
            Nrc = "1234567",
            NombreRazonSocial = "Otro Emisor SA",
            CodigoActividad = "12345",
            DescripcionActividad = "Otro rubro",
            CatTipoEstablecimientoId = 1,
            CatDepartamentoId = 1,
            CatMunicipioId = 1,
            Direccion = "y",
            CorreoElectronico = "y@y.com",
            Telefono = "22222222",
        });
        db.SaveChanges();
    }

    private static InternalEmisoresController BuildController(ApplicationDbContext db, string rol)
    {
        var identity = new ClaimsIdentity(new[] { new Claim("rol", rol) }, "Test");
        var principal = new ClaimsPrincipal(identity);
        var controller = new InternalEmisoresController(db);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
        return controller;
    }

    [Fact]
    public async Task Listar_SoloLibres_Default_FiltraVinculados()
    {
        var db = BuildCtx();
        SeedEmisores(db);
        var ctrl = BuildController(db, "SuperAdmin");

        var actionResult = await ctrl.Listar();
        var ok = actionResult.Result as OkObjectResult;

        ok.Should().NotBeNull();
        var items = ok!.Value as IEnumerable<EmisorParaVincularDto>;
        items.Should().NotBeNull();
        items!.Should().HaveCount(1);
        items.Single().Id.Should().Be(2);
    }

    [Fact]
    public async Task Listar_SoloLibresFalse_DevuelveTodos()
    {
        var db = BuildCtx();
        SeedEmisores(db);
        var ctrl = BuildController(db, "SuperAdmin");

        var actionResult = await ctrl.Listar(soloLibres: false);
        var ok = actionResult.Result as OkObjectResult;

        var items = (ok!.Value as IEnumerable<EmisorParaVincularDto>)!.ToList();
        items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Listar_AdminOrg_DevuelveForbid()
    {
        var db = BuildCtx();
        SeedEmisores(db);
        var ctrl = BuildController(db, "AdminOrg");

        var result = await ctrl.Listar();

        result.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task SetHubLink_VincularEmisorLibre_RetornaOkConSnapshotYPersiste()
    {
        var db = BuildCtx();
        SeedEmisores(db);
        var ctrl = BuildController(db, "SuperAdmin");

        var result = await ctrl.SetHubLink(2, new PatchEmisorHubLinkRequest(7), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var snapshot = ok.Value.Should().BeOfType<EmisorFiscalSnapshotDto>().Subject;
        snapshot.Id.Should().Be(2);
        snapshot.Nit.Should().Be("06140123456789");
        snapshot.Nrc.Should().Be("1234567");
        snapshot.NombreRazonSocial.Should().Be("Otro Emisor SA");
        snapshot.CodigoActividad.Should().Be("12345");
        // Codigos MH resueltos desde catalogos
        snapshot.CodTipoEstablecimientoMH.Should().Be("01");
        snapshot.CodDepartamentoMH.Should().Be("06");
        snapshot.CodMunicipioMH.Should().Be("23");

        var emisor = await db.Emisores.FindAsync(2);
        emisor!.HubId.Should().Be(7);
    }

    [Fact]
    public async Task SetHubLink_HubYaTomadoPorOtroEmisor_Conflict()
    {
        var db = BuildCtx();
        SeedEmisores(db);
        var ctrl = BuildController(db, "SuperAdmin");

        // Hub 4 ya pertenece al Emisor 1. Intentar vincular Emisor 2 al mismo Hub.
        var result = await ctrl.SetHubLink(2, new PatchEmisorHubLinkRequest(4), CancellationToken.None);

        result.Should().BeOfType<ConflictObjectResult>();
        var emisor = await db.Emisores.FindAsync(2);
        emisor!.HubId.Should().BeNull();
    }

    [Fact]
    public async Task SetHubLink_Desvincular_Success()
    {
        var db = BuildCtx();
        SeedEmisores(db);
        var ctrl = BuildController(db, "SuperAdmin");

        var result = await ctrl.SetHubLink(1, new PatchEmisorHubLinkRequest(null), CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        var emisor = await db.Emisores.FindAsync(1);
        emisor!.HubId.Should().BeNull();
    }

    [Fact]
    public async Task SetHubLink_AdminOrg_Forbid()
    {
        var db = BuildCtx();
        SeedEmisores(db);
        var ctrl = BuildController(db, "AdminOrg");

        var result = await ctrl.SetHubLink(2, new PatchEmisorHubLinkRequest(7), CancellationToken.None);

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task SetHubLink_EmisorNoExiste_NotFound()
    {
        var db = BuildCtx();
        SeedEmisores(db);
        var ctrl = BuildController(db, "SuperAdmin");

        var result = await ctrl.SetHubLink(999, new PatchEmisorHubLinkRequest(7), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task SetHubLink_EmisorInactivo_NotFound()
    {
        // I3 review fix: SetHubLink filtra Activo, no debe operar sobre inactivos.
        var db = BuildCtx();
        SeedEmisores(db);
        var emisor = await db.Emisores.FindAsync(2);
        emisor!.Activo = false;
        await db.SaveChangesAsync();

        var ctrl = BuildController(db, "SuperAdmin");
        var result = await ctrl.SetHubLink(2, new PatchEmisorHubLinkRequest(7), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Listar_JwtLocalSmartix_NoAutoriza()
    {
        // I1 review fix: un JWT emitido por AuthService de Smartix usa ClaimTypes.Role
        // (no el claim "rol" del Hub). IsHubSuperAdmin debe devolver false aun cuando
        // ClaimTypes.Role sea "SuperAdmin", para que un user de Smartix con rol parecido
        // no gane acceso a los endpoints internos cross-app.
        var db = BuildCtx();
        SeedEmisores(db);

        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Role, "SuperAdmin"), new Claim("EmisorId", "1") },
            "Test");
        var principal = new ClaimsPrincipal(identity);
        var ctrl = new InternalEmisoresController(db);
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        var actionResult = await ctrl.Listar();

        actionResult.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task SetHubLink_EmisorConDistrito_SnapshotIncluyeCodDistritoMH()
    {
        // B2: CodDistritoMH debe popularse desde el Include(e => e.Distrito).
        var db = BuildCtx();
        db.CatDepartamentos.Add(new CatDepartamento { Id = 1, Codigo = "06", Valor = "San Salvador" });
        db.CatMunicipios.Add(new CatMunicipio { Id = 1, Codigo = "23", Valor = "San Salvador" });
        db.CatTiposEstablecimiento.Add(new CatTipoEstablecimiento { Id = 1, Codigo = "01", Valor = "Casa Matriz" });
        db.CatDistritos.Add(new CatDistrito { Id = 1, Codigo = "14", Valor = "San Salvador Centro", CodigoDepartamento = "06", CodigoMunicipio = "23" });
        db.Emisores.Add(new Emisor
        {
            Id = 3, Activo = true, HubId = null,
            Nit = "09999999999999",
            Nrc = "9999999",
            NombreRazonSocial = "Emisor Con Distrito SA",
            CodigoActividad = "11111",
            DescripcionActividad = "Rubro Test",
            CatTipoEstablecimientoId = 1,
            CatDepartamentoId = 1,
            CatMunicipioId = 1,
            CatDistritoId = 1,
            Direccion = "Calle Falsa 123",
            CorreoElectronico = "test@test.com",
            Telefono = "33333333",
        });
        db.SaveChanges();

        var ctrl = BuildController(db, "SuperAdmin");
        var result = await ctrl.SetHubLink(3, new PatchEmisorHubLinkRequest(8), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var snapshot = ok.Value.Should().BeOfType<EmisorFiscalSnapshotDto>().Subject;
        snapshot.CodDistritoMH.Should().Be("14");
    }

    [Fact]
    public async Task SetHubLink_EmisorSinDistrito_SnapshotCodDistritoMHEsNull()
    {
        // B2: si el emisor no tiene CatDistritoId, CodDistritoMH debe ser null (no exception).
        var db = BuildCtx();
        SeedEmisores(db); // Emisor 2 no tiene CatDistritoId
        var ctrl = BuildController(db, "SuperAdmin");

        var result = await ctrl.SetHubLink(2, new PatchEmisorHubLinkRequest(7), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var snapshot = ok.Value.Should().BeOfType<EmisorFiscalSnapshotDto>().Subject;
        snapshot.CodDistritoMH.Should().BeNull();
    }

    // F3: los tests del toggle de SmartInventory se movieron a
    // SyncInventoryAppControllerTests porque el endpoint cambio de
    // patron (JWT/SuperAdmin -> X-Api-Key server-to-server) durante
    // la implementacion del commit 4 de F3.
}
