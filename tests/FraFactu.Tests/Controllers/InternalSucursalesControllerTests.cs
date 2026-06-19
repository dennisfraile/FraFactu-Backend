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
/// Plan B Hub-as-Emisor — Tests unitarios del InternalSucursalesController.
/// Valida (a) listado de sucursales filtrado por emisorId y soloLibres, (b) scope
/// AdminOrg → solo Hubs accesibles, (c) conflict 409 si HubSucursalId tomado,
/// (d) NotFound si sucursal no existe.
/// </summary>
public class InternalSucursalesControllerTests
{
    private static ApplicationDbContext BuildCtx()
    {
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"InternalSucursales_{Guid.NewGuid()}")
            .Options;
        return new ApplicationDbContext(opts);
    }

    private static void Seed(ApplicationDbContext db)
    {
        // Catalogos MH (FK NOT NULL desde Emisor y Sucursal; sin estos rows el .Include
        // INNER JOIN del controller deja a EF InMemory descartando los rows).
        db.CatDepartamentos.Add(new CatDepartamento { Id = 1, Codigo = "06", Valor = "San Salvador" });
        db.CatMunicipios.Add(new CatMunicipio { Id = 1, Codigo = "23", Valor = "San Salvador" });
        db.CatTiposEstablecimiento.Add(new CatTipoEstablecimiento { Id = 1, Codigo = "01", Valor = "Casa Matriz" });

        db.Emisores.Add(new Emisor
        {
            Id = 1, Activo = true, HubId = 4,
            Nit = "13173006851011", Nrc = "2302820",
            NombreRazonSocial = "Sonia", CodigoActividad = "86202",
            DescripcionActividad = "Odontología",
            CatTipoEstablecimientoId = 1,
            CatDepartamentoId = 1, CatMunicipioId = 1,
            Direccion = "x", CorreoElectronico = "x@x.com", Telefono = "11111111",
        });
        db.Sucursales.Add(new Sucursal
        {
            Id = 10, EmisorId = 1, Activo = true,
            Codigo = "SUC01", Nombre = "Casa Matriz",
            CatDepartamentoId = 1, CatMunicipioId = 1, CatTipoEstablecimientoId = 1,
            CodigoEstablecimiento = "M001",
            HubSucursalId = 5, // ya vinculada
        });
        db.Sucursales.Add(new Sucursal
        {
            Id = 11, EmisorId = 1, Activo = true,
            Codigo = "SUC02", Nombre = "Sucursal Norte",
            CatDepartamentoId = 1, CatMunicipioId = 1, CatTipoEstablecimientoId = 1,
            CodigoEstablecimiento = "M002",
            HubSucursalId = null, // libre
        });
        db.SaveChanges();
    }

    private static InternalSucursalesController BuildController(
        ApplicationDbContext db, string rol, IEnumerable<int>? hubsAccesibles = null)
    {
        var claims = new List<Claim> { new("rol", rol) };
        if (hubsAccesibles is not null)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(hubsAccesibles);
            claims.Add(new Claim("hubs_accesibles", json));
        }
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ctrl = new InternalSucursalesController(db);
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
        return ctrl;
    }

    [Fact]
    public async Task Listar_SuperAdmin_SoloLibres_FiltraVinculadas()
    {
        var db = BuildCtx();
        Seed(db);
        var ctrl = BuildController(db, "SuperAdmin");

        var actionResult = await ctrl.Listar(emisorId: 1);
        var ok = actionResult.Result as OkObjectResult;

        var items = (ok!.Value as IEnumerable<SucursalParaVincularDto>)!.ToList();
        items.Should().HaveCount(1);
        items.Single().Id.Should().Be(11);
    }

    [Fact]
    public async Task Listar_UsuarioCompartidoConHubAccesible_OK()
    {
        // 2026-05-21: el rol cross-Hub-en-Grupo es UsuarioCompartido (antes AdminOrg).
        var db = BuildCtx();
        Seed(db);
        // Hub 4 (al que pertenece el Emisor 1) está en hubs_accesibles → autorizado.
        var ctrl = BuildController(db, "UsuarioCompartido", hubsAccesibles: new[] { 4 });

        var actionResult = await ctrl.Listar(emisorId: 1);
        actionResult.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Listar_UsuarioCompartidoSinHubAccesible_Forbid()
    {
        var db = BuildCtx();
        Seed(db);
        // Hubs accesibles vacío → UsuarioCompartido no puede listar Sucursales del Emisor 1.
        var ctrl = BuildController(db, "UsuarioCompartido", hubsAccesibles: Array.Empty<int>());

        var actionResult = await ctrl.Listar(emisorId: 1);
        actionResult.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task Listar_AdminOrg_Forbid()
    {
        // 2026-05-21: AdminOrg paso a Hub-unico. Ya NO califica para listar
        // Sucursales cross-Hub (queda exclusivo para SuperAdmin y UsuarioCompartido).
        var db = BuildCtx();
        Seed(db);
        var ctrl = BuildController(db, "AdminOrg", hubsAccesibles: new[] { 4 });

        var actionResult = await ctrl.Listar(emisorId: 1);
        actionResult.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task Listar_EmisorIdInvalido_BadRequest()
    {
        var db = BuildCtx();
        Seed(db);
        var ctrl = BuildController(db, "SuperAdmin");

        var actionResult = await ctrl.Listar(emisorId: 0);
        actionResult.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task SetHubLink_SuperAdmin_VinculaLibre_RetornaOkConSnapshotYPersiste()
    {
        var db = BuildCtx();
        Seed(db);
        var ctrl = BuildController(db, "SuperAdmin");

        var result = await ctrl.SetHubLink(11, new PatchSucursalHubLinkRequest(99), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var snapshot = ok.Value.Should().BeOfType<SucursalFiscalSnapshotDto>().Subject;
        snapshot.Id.Should().Be(11);
        snapshot.EmisorId.Should().Be(1);
        snapshot.Nombre.Should().Be("Sucursal Norte");
        snapshot.CodigoEstablecimientoMH.Should().Be("M002");

        var s = await db.Sucursales.FindAsync(11);
        s!.HubSucursalId.Should().Be(99);
    }

    [Fact]
    public async Task SetHubLink_HubSucursalIdTomado_Conflict()
    {
        var db = BuildCtx();
        Seed(db);
        var ctrl = BuildController(db, "SuperAdmin");

        // HubSucursal 5 ya pertenece a Sucursal 10. Intentar vincular Sucursal 11 ahí.
        var result = await ctrl.SetHubLink(11, new PatchSucursalHubLinkRequest(5), CancellationToken.None);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task SetHubLink_UsuarioCompartidoSinHubAccesible_Forbid()
    {
        // 2026-05-21: cross-Hub-en-Grupo es UsuarioCompartido (antes AdminOrg).
        var db = BuildCtx();
        Seed(db);
        var ctrl = BuildController(db, "UsuarioCompartido", hubsAccesibles: Array.Empty<int>());

        var result = await ctrl.SetHubLink(11, new PatchSucursalHubLinkRequest(99), CancellationToken.None);

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task SetHubLink_AdminOrg_Forbid()
    {
        // 2026-05-21: AdminOrg paso a Hub-unico. SetHubLink queda exclusivo para
        // SuperAdmin (cross-Grupo) y UsuarioCompartido (cross-Hub-en-Grupo).
        var db = BuildCtx();
        Seed(db);
        var ctrl = BuildController(db, "AdminOrg", hubsAccesibles: new[] { 4 });

        var result = await ctrl.SetHubLink(11, new PatchSucursalHubLinkRequest(99), CancellationToken.None);

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task SetHubLink_Desvincular_Success()
    {
        var db = BuildCtx();
        Seed(db);
        var ctrl = BuildController(db, "SuperAdmin");

        var result = await ctrl.SetHubLink(10, new PatchSucursalHubLinkRequest(null), CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        var s = await db.Sucursales.FindAsync(10);
        s!.HubSucursalId.Should().BeNull();
    }

    [Fact]
    public async Task SetHubLink_SucursalNoExiste_NotFound()
    {
        var db = BuildCtx();
        Seed(db);
        var ctrl = BuildController(db, "SuperAdmin");

        var result = await ctrl.SetHubLink(999, new PatchSucursalHubLinkRequest(99), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task SetHubLink_SucursalInactiva_NotFound()
    {
        var db = BuildCtx();
        Seed(db);
        var s = await db.Sucursales.FindAsync(11);
        s!.Activo = false;
        await db.SaveChangesAsync();

        var ctrl = BuildController(db, "SuperAdmin");
        var result = await ctrl.SetHubLink(11, new PatchSucursalHubLinkRequest(99), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task SetHubLink_AdminOrg_EmisorSinHubId_Forbid()
    {
        // N2 review fix: si el Emisor existe pero no tiene HubId, AdminOrg no puede operar.
        var db = BuildCtx();
        Seed(db);
        var emisor = await db.Emisores.FindAsync(1);
        emisor!.HubId = null; // desvinculamos
        await db.SaveChangesAsync();

        var ctrl = BuildController(db, "AdminOrg", hubsAccesibles: new[] { 4 });
        var result = await ctrl.SetHubLink(11, new PatchSucursalHubLinkRequest(99), CancellationToken.None);

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task SetHubLink_SucursalConDistrito_SnapshotIncluyeCodDistritoMH()
    {
        // B2: CodDistritoMH debe popularse desde el Include(s => s.Distrito).
        var db = BuildCtx();
        db.CatDepartamentos.Add(new CatDepartamento { Id = 1, Codigo = "06", Valor = "San Salvador" });
        db.CatMunicipios.Add(new CatMunicipio { Id = 1, Codigo = "23", Valor = "San Salvador" });
        db.CatTiposEstablecimiento.Add(new CatTipoEstablecimiento { Id = 1, Codigo = "01", Valor = "Casa Matriz" });
        db.CatDistritos.Add(new CatDistrito { Id = 1, Codigo = "14", Valor = "San Salvador Centro", CodigoDepartamento = "06", CodigoMunicipio = "23" });
        db.Emisores.Add(new Emisor
        {
            Id = 1, Activo = true, HubId = 4,
            Nit = "13173006851011", Nrc = "2302820",
            NombreRazonSocial = "Sonia", CodigoActividad = "86202",
            DescripcionActividad = "Odontología",
            CatTipoEstablecimientoId = 1,
            CatDepartamentoId = 1, CatMunicipioId = 1,
            Direccion = "x", CorreoElectronico = "x@x.com", Telefono = "11111111",
        });
        db.Sucursales.Add(new Sucursal
        {
            Id = 20, EmisorId = 1, Activo = true,
            Codigo = "SUC20", Nombre = "Sucursal Con Distrito",
            CatDepartamentoId = 1, CatMunicipioId = 1, CatTipoEstablecimientoId = 1,
            CatDistritoId = 1,
            CodigoEstablecimiento = "M020",
            HubSucursalId = null,
        });
        db.SaveChanges();

        var ctrl = BuildController(db, "SuperAdmin");
        var result = await ctrl.SetHubLink(20, new PatchSucursalHubLinkRequest(77), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var snapshot = ok.Value.Should().BeOfType<SucursalFiscalSnapshotDto>().Subject;
        snapshot.CodDistritoMH.Should().Be("14");
    }

    [Fact]
    public async Task SetHubLink_SucursalSinDistrito_SnapshotCodDistritoMHEsNull()
    {
        // B2: si la sucursal no tiene CatDistritoId, CodDistritoMH debe ser null.
        var db = BuildCtx();
        Seed(db); // Sucursal 11 no tiene CatDistritoId
        var ctrl = BuildController(db, "SuperAdmin");

        var result = await ctrl.SetHubLink(11, new PatchSucursalHubLinkRequest(99), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var snapshot = ok.Value.Should().BeOfType<SucursalFiscalSnapshotDto>().Subject;
        snapshot.CodDistritoMH.Should().BeNull();
    }
}
