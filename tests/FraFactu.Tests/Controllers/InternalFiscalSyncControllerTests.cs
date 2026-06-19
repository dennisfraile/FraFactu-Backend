using FraFactu.API.Controllers;
using FraFactu.Application.Common.Settings;
using FraFactu.Domain.Entities;
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
/// Plan B Hub-as-Emisor — Fase 2 Task 15 (Smartix lado receiver).
/// Verifica auth, lookup, sync de Emisor / Sucursal, preservación de campos
/// Smartix-only.
/// </summary>
public class InternalFiscalSyncControllerTests
{
    private const string ApiKey = "test-fiscal-sync-key";
    private const int HubId = 444;
    private const int HubSucursalId = 555;
    private const int EmisorId = 10;
    private const int SucursalId = 100;

    private static (InternalFiscalSyncController ctrl, ApplicationDbContext db)
        Build(bool seedEmisor = true, bool seedSucursal = true)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"InternalFiscalSync_{Guid.NewGuid()}")
            .Options;
        var db = new ApplicationDbContext(options);

        // Catalogos
        db.CatDepartamentos.Add(new FraFactu.Domain.Entities.Catalogos.CatDepartamento
        { Id = 6, Codigo = "06", Valor = "San Salvador" });
        db.CatMunicipios.Add(new FraFactu.Domain.Entities.Catalogos.CatMunicipio
        { Id = 23, Codigo = "23", Valor = "San Salvador (capital)", CodigoDepartamento = "06" });
        db.CatTiposEstablecimiento.Add(new FraFactu.Domain.Entities.Catalogos.CatTipoEstablecimiento
        { Id = 1, Codigo = "01", Valor = "Casa Matriz" });
        db.SaveChanges();

        if (seedEmisor)
        {
            db.Emisores.Add(new Emisor
            {
                Id = EmisorId,
                HubId = HubId,
                Nit = "OLD_NIT",
                Nrc = "OLD_NRC",
                NombreRazonSocial = "Razon Vieja",
                CodigoActividad = "10001",
                DescripcionActividad = "Empleados",
                CatDepartamentoId = 6,
                CatMunicipioId = 23,
                Direccion = "Direccion vieja",
                CatTipoEstablecimientoId = 1,
                CorreoElectronico = "viejo@test.com",
                Telefono = "11112222",
                CatAmbienteDestinoId = 1,
                MhUsuario = "mh-user-secret",
                SmtpHost = "smtp.gmail.com",
                LogoUrl = "https://logo.png"
            });
            db.SaveChanges();
        }

        if (seedSucursal)
        {
            db.Sucursales.Add(new Sucursal
            {
                Id = SucursalId,
                EmisorId = EmisorId,
                HubSucursalId = HubSucursalId,
                Codigo = $"HUB-{HubSucursalId}",
                Nombre = "Sucursal Vieja",
                CodigoEstablecimiento = "OLD",
                CatDepartamentoId = 6,
                CatMunicipioId = 23,
                CatTipoEstablecimientoId = 1
            });
            db.SaveChanges();
        }

        // Fix Bug 1 (2026-06-08): el receiver fiscal valida con SmartHubSettings (la
        // misma key que el resto de endpoints internos llamados por SmartHub) y NO con
        // SmartCareSettings. Antes el controller inyectaba la setting equivocada, lo
        // que provocaba 401 silencioso en Demo y dejaba a Smartix desincronizado.
        var settings = Options.Create(new SmartHubSettings { ApiKey = ApiKey });
        var ctrl = new InternalFiscalSyncController(db, settings, NullLogger<InternalFiscalSyncController>.Instance);
        ctrl.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return (ctrl, db);
    }

    private static void SetApiKey(InternalFiscalSyncController ctrl, string? key)
    {
        if (key != null)
            ctrl.ControllerContext.HttpContext.Request.Headers["X-Api-Key"] = key;
    }

    private static HubFiscalSyncRequest BuildHubRequest() => new()
    {
        HubId = HubId,
        Nit = "NEW_NIT",
        Nrc = "NEW_NRC",
        NombreRazonSocial = "Razon NUEVA SA",
        NombreComercial = "Comercial Nuevo",
        CodActividadEconomica = "10001",
        DescActividadEconomica = "Empleados",
        CodTipoEstablecimiento = "01",
        CodDepartamento = "06",
        CodMunicipio = "23",
        DireccionComplemento = "Direccion Nueva",
        TelefonoFiscal = "99998888",
        CorreoFiscal = "nuevo@test.com"
    };

    private static SucursalFiscalSyncRequest BuildSucursalRequest() => new()
    {
        HubSucursalId = HubSucursalId,
        Nombre = "Casa Matriz Hub",
        CodigoEstablecimientoMH = "M001P001",
        CodTipoEstablecimiento = "01",
        CodDepartamento = "06",
        CodMunicipio = "23",
        DireccionComplemento = "Sucursal addr",
        TelefonoSucursal = "77770000",
        CorreoSucursal = "suc@test.com",
        ContingenciaNombreResponsable = "Responsable",
        ContingenciaTipoDocResponsable = "36",
        ContingenciaNumeroDocResponsable = "1317-300685-101-1"
    };

    // ===========================================================================
    // POST /api/internal/sync-emisor
    // ===========================================================================

    [Fact]
    public async Task SyncEmisor_SinApiKey_Unauthorized()
    {
        var (ctrl, _) = Build();
        var resp = await ctrl.SyncEmisor(BuildHubRequest(), CancellationToken.None);
        resp.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task SyncEmisor_ApiKeyInvalida_Unauthorized()
    {
        var (ctrl, _) = Build();
        SetApiKey(ctrl, "wrong");
        var resp = await ctrl.SyncEmisor(BuildHubRequest(), CancellationToken.None);
        resp.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task SyncEmisor_HappyPath_RefrescaCamposIdentitarios()
    {
        var (ctrl, db) = Build();
        SetApiKey(ctrl, ApiKey);

        var resp = await ctrl.SyncEmisor(BuildHubRequest(), CancellationToken.None);

        resp.Should().BeOfType<NoContentResult>();
        var emisor = await db.Emisores.SingleAsync();
        emisor.Nit.Should().Be("NEW_NIT");
        emisor.Nrc.Should().Be("NEW_NRC");
        emisor.NombreRazonSocial.Should().Be("Razon NUEVA SA");
        emisor.Direccion.Should().Be("Direccion Nueva");
        emisor.Telefono.Should().Be("99998888");
        emisor.CorreoElectronico.Should().Be("nuevo@test.com");
    }

    [Fact]
    public async Task SyncEmisor_PreservaCamposSmartixOnly()
    {
        var (ctrl, db) = Build();
        SetApiKey(ctrl, ApiKey);

        await ctrl.SyncEmisor(BuildHubRequest(), CancellationToken.None);

        var emisor = await db.Emisores.SingleAsync();
        emisor.MhUsuario.Should().Be("mh-user-secret");
        emisor.SmtpHost.Should().Be("smtp.gmail.com");
        emisor.LogoUrl.Should().Be("https://logo.png");
        emisor.HubId.Should().Be(HubId); // intacto
    }

    [Fact]
    public async Task SyncEmisor_HubSinEmisorVinculado_NotFound()
    {
        var (ctrl, _) = Build(seedEmisor: false);
        SetApiKey(ctrl, ApiKey);

        var resp = await ctrl.SyncEmisor(BuildHubRequest(), CancellationToken.None);

        resp.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task SyncEmisor_CodigoMHInvalido_LanzaValidationException()
    {
        var (ctrl, _) = Build();
        SetApiKey(ctrl, ApiKey);
        var req = BuildHubRequest();
        req.CodDepartamento = "99";

        var act = async () => await ctrl.SyncEmisor(req, CancellationToken.None);

        await act.Should().ThrowAsync<FraFactu.Domain.Exceptions.ValidationException>();
    }

    // ===========================================================================
    // POST /api/internal/sync-sucursal
    // ===========================================================================

    [Fact]
    public async Task SyncSucursal_HappyPath_RefrescaCampos()
    {
        var (ctrl, db) = Build();
        SetApiKey(ctrl, ApiKey);

        var resp = await ctrl.SyncSucursal(BuildSucursalRequest(), CancellationToken.None);

        resp.Should().BeOfType<NoContentResult>();
        var sucursal = await db.Sucursales.SingleAsync();
        sucursal.Nombre.Should().Be("Casa Matriz Hub");
        sucursal.CodigoEstablecimiento.Should().Be("M001P001");
        sucursal.ContingenciaTipoDocResponsable.Should().Be("36");
    }

    [Fact]
    public async Task SyncSucursal_NoVinculada_NotFound()
    {
        var (ctrl, _) = Build(seedSucursal: false);
        SetApiKey(ctrl, ApiKey);

        var resp = await ctrl.SyncSucursal(BuildSucursalRequest(), CancellationToken.None);

        resp.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task SyncSucursal_SinApiKey_Unauthorized()
    {
        var (ctrl, _) = Build();
        var resp = await ctrl.SyncSucursal(BuildSucursalRequest(), CancellationToken.None);
        resp.Should().BeOfType<UnauthorizedObjectResult>();
    }

    // ===========================================================================
    // CatDistrito — 3 tests (Task B1)
    // ===========================================================================

    /// <summary>
    /// Test 1: CodDistrito válido → CatDistritoId se persiste en el Emisor.
    /// Distrito "14" = "San Salvador" para depto "06" munic "23" (Id = 111 en seed).
    /// </summary>
    [Fact]
    public async Task SyncEmisor_CodDistritoValido_PersisteCatDistritoId()
    {
        var (ctrl, db) = Build();
        SetApiKey(ctrl, ApiKey);

        // Agregar el CatDistrito necesario al in-memory DB
        db.CatDistritos.Add(new FraFactu.Domain.Entities.Catalogos.CatDistrito
        { Id = 111, Codigo = "14", Valor = "San Salvador", CodigoDepartamento = "06", CodigoMunicipio = "23" });
        db.SaveChanges();

        var req = BuildHubRequest();
        req.CodDistrito = "14";

        var resp = await ctrl.SyncEmisor(req, CancellationToken.None);

        resp.Should().BeOfType<NoContentResult>();
        var emisor = await db.Emisores.SingleAsync();
        emisor.CatDistritoId.Should().Be(111);
    }

    /// <summary>
    /// Test 2: CodDistrito inválido (código que no existe para ese depto+munic) →
    /// ResolverCatalogosAsync lanza ValidationException (la misma ruta que CodDepartamento inválido).
    /// </summary>
    [Fact]
    public async Task SyncEmisor_CodDistritoInvalido_LanzaValidationException()
    {
        var (ctrl, db) = Build();
        SetApiKey(ctrl, ApiKey);

        // Sembrar un distrito para depto 06 munic 23, pero el request usará código "99" que no existe
        db.CatDistritos.Add(new FraFactu.Domain.Entities.Catalogos.CatDistrito
        { Id = 111, Codigo = "14", Valor = "San Salvador", CodigoDepartamento = "06", CodigoMunicipio = "23" });
        db.SaveChanges();

        var req = BuildHubRequest();
        req.CodDistrito = "99"; // no existe para depto 06 munic 23

        var act = async () => await ctrl.SyncEmisor(req, CancellationToken.None);

        await act.Should().ThrowAsync<FraFactu.Domain.Exceptions.ValidationException>()
            .WithMessage("*distrito*");
    }

    /// <summary>
    /// Test 3: CodDistrito vacío (backwards-compat: SmartHub antiguo no lo envía) →
    /// CatDistritoId permanece null, respuesta NoContent.
    /// </summary>
    [Fact]
    public async Task SyncEmisor_CodDistritoVacio_PersisteCatDistritoIdComoNull()
    {
        var (ctrl, db) = Build();
        SetApiKey(ctrl, ApiKey);

        var req = BuildHubRequest();
        req.CodDistrito = string.Empty; // no envía distrito

        var resp = await ctrl.SyncEmisor(req, CancellationToken.None);

        resp.Should().BeOfType<NoContentResult>();
        var emisor = await db.Emisores.SingleAsync();
        emisor.CatDistritoId.Should().BeNull();
    }

    // ===========================================================================
    // CodTipoEstablecimiento opcional — Fix Bug 3 (2026-06-08 PM)
    //
    // SmartHub-BE HubFiscalDataValidator trata CodTipoEstablecimiento como OPCIONAL
    // en el Hub (el DTE toma tipoEstablecimiento de la Sucursal en lugar del Emisor;
    // ese comentario explicativo vive en HubFiscalDataValidator.cs:46-51). Smartix
    // Emisor.CatTipoEstablecimientoId tambien es nullable, alineado.
    //
    // Antes del fix el receiver throw "Codigo de tipo de establecimiento '' no
    // existe en catalogo MH." cuando SH enviaba el campo vacio → 500 → dispatcher
    // SH-BE silenciaba el warning best-effort → sync silenciosamente fallaba para
    // Hubs sin tipoEstablecimiento.
    // ===========================================================================

    /// <summary>
    /// SyncEmisor con CodTipoEstablecimiento vacío → CatTipoEstablecimientoId
    /// queda en null (la column es nullable en Emisor) y la respuesta es NoContent.
    /// </summary>
    [Fact]
    public async Task SyncEmisor_CodTipoEstablecimientoVacio_PersisteCatTipoComoNull()
    {
        var (ctrl, db) = Build();
        SetApiKey(ctrl, ApiKey);

        var req = BuildHubRequest();
        req.CodTipoEstablecimiento = string.Empty;

        var resp = await ctrl.SyncEmisor(req, CancellationToken.None);

        resp.Should().BeOfType<NoContentResult>();
        var emisor = await db.Emisores.SingleAsync();
        emisor.CatTipoEstablecimientoId.Should().BeNull();
    }

    /// <summary>
    /// SyncSucursal con CodTipoEstablecimiento vacío → CatTipoEstablecimientoId
    /// PRESERVA el valor previo (la column en Sucursal es non-nullable, asignar 0
    /// rompería FK; la política es no tocar si el caller no provee).
    /// </summary>
    [Fact]
    public async Task SyncSucursal_CodTipoEstablecimientoVacio_PreservaValorPrevio()
    {
        var (ctrl, db) = Build();
        SetApiKey(ctrl, ApiKey);

        var req = BuildSucursalRequest();
        req.CodTipoEstablecimiento = string.Empty;

        var resp = await ctrl.SyncSucursal(req, CancellationToken.None);

        resp.Should().BeOfType<NoContentResult>();
        var sucursal = await db.Sucursales.SingleAsync();
        sucursal.CatTipoEstablecimientoId.Should().Be(1); // valor previo del seed (Casa Matriz)
    }
}
