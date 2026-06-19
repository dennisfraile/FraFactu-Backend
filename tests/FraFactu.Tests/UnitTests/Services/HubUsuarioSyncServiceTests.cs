using FraFactu.Application.DTOs.Hub;
using FraFactu.Application.Interfaces;
using FraFactu.Domain.Entities;
using FraFactu.Domain.Enums;
using FraFactu.Infrastructure.Persistence;
using FraFactu.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace FraFactu.Tests.UnitTests.Services;

/// <summary>
/// F2 plan centralización: tests para <see cref="HubUsuarioSyncService"/>. Cubren
/// los 5 webhooks de gestión de usuarios + GET usuarios-roles. Casos: validación
/// de inputs, idempotency, auto-create de usuario, vinculación por Email a cuentas
/// pre-SSO, manejo de errores (Emisor/Rol/Usuario ausentes).
/// </summary>
public class HubUsuarioSyncServiceTests
{
    private const int HubIdOrg = 100;
    private const int EmisorIdSmartix = 10;

    private static ApplicationDbContext BuildContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"HubUsuarioSync_{Guid.NewGuid()}")
            .Options;
        var ctx = new ApplicationDbContext(options);
        Seed(ctx);
        return ctx;
    }

    private static void Seed(ApplicationDbContext ctx)
    {
        ctx.Roles.AddRange(
            new Rol { Id = 1, Nombre = "SuperAdmin" },
            new Rol { Id = 2, Nombre = "EmisorAdmin" },
            new Rol { Id = 3, Nombre = "GerenteSucursal" },
            new Rol { Id = 4, Nombre = "Cajero" },
            new Rol { Id = 5, Nombre = "Contador" },
            new Rol { Id = 6, Nombre = "Vendedor" }
        );
        ctx.Emisores.Add(new Emisor
        {
            Id = EmisorIdSmartix,
            HubId = HubIdOrg,
            NombreRazonSocial = "Empresa Test",
            Nit = "06140506141011",
            Nrc = "123456-7",
            CodigoActividad = "47111",
            DescripcionActividad = "Comercio",
            CorreoElectronico = "test@empresa.com",
            Telefono = "22223333",
            CatDepartamentoId = 1,
            CatMunicipioId = 1
        });
        ctx.SaveChanges();
    }

    private static HubUsuarioSyncService BuildService(ApplicationDbContext ctx) =>
        new(ctx, NullLogger<HubUsuarioSyncService>.Instance);

    private static HubUsuarioWebhookDto AsignarDto(int hubUsuarioId = 500, string email = "nuevo@test.com",
        string rol = "Cajero", int orgId = HubIdOrg) =>
        new()
        {
            HubUsuarioId = hubUsuarioId,
            Email = email,
            NombreCompleto = "Usuario Nuevo",
            RolEnApp = rol,
            OrganizacionId = orgId,
            Accion = "asignar"
        };

    // =========================================================================
    // Asignar
    // =========================================================================

    [Theory]
    [InlineData(0, "u@t.com", "Cajero", HubIdOrg)]
    [InlineData(500, "", "Cajero", HubIdOrg)]
    [InlineData(500, "u@t.com", "", HubIdOrg)]
    [InlineData(500, "u@t.com", "Cajero", 0)]
    public async Task Asignar_BadRequest_SiCampoRequeridoFaltaOEsInvalido(
        int hubId, string email, string rol, int orgId)
    {
        var ctx = BuildContext();
        var svc = BuildService(ctx);

        var result = await svc.AsignarAsync(AsignarDto(hubId, email, rol, orgId));

        result.Status.Should().Be(HubSyncStatus.BadRequest);
    }

    [Fact]
    public async Task Asignar_Conflict_SiHubIdNoTieneEmisorEnSmartix()
    {
        var ctx = BuildContext();
        var svc = BuildService(ctx);

        var result = await svc.AsignarAsync(AsignarDto(orgId: 999));

        result.Status.Should().Be(HubSyncStatus.Conflict);
        result.Mensaje.Should().Contain("999");
    }

    [Fact]
    public async Task Asignar_BadRequest_SiRolNoEstaEnCatalogo()
    {
        var ctx = BuildContext();
        var svc = BuildService(ctx);

        var result = await svc.AsignarAsync(AsignarDto(rol: "RolInexistente"));

        result.Status.Should().Be(HubSyncStatus.BadRequest);
        result.Mensaje.Should().Contain("RolInexistente");
    }

    [Fact]
    public async Task Asignar_CreaUsuarioNuevoCuandoNoExiste()
    {
        var ctx = BuildContext();
        var svc = BuildService(ctx);

        var result = await svc.AsignarAsync(AsignarDto());

        result.Status.Should().Be(HubSyncStatus.Ok);
        result.Cambio.Should().BeTrue();

        var usuario = await ctx.Usuarios.SingleAsync(u => u.HubUsuarioId == 500);
        usuario.Email.Should().Be("nuevo@test.com");
        usuario.EmisorId.Should().Be(EmisorIdSmartix);
        usuario.RolId.Should().Be(4); // Cajero
        usuario.Estado.Should().Be(EstadoUsuario.Activo);
        usuario.ProveedorAuth.Should().Be(ProveedorAutenticacion.SmartHub);
    }

    [Fact]
    public async Task Asignar_EmisorAdmin_RecibeAccesoTodasSucursales()
    {
        var ctx = BuildContext();
        var svc = BuildService(ctx);

        await svc.AsignarAsync(AsignarDto(rol: "EmisorAdmin"));

        var usuario = await ctx.Usuarios.SingleAsync(u => u.HubUsuarioId == 500);
        usuario.AccesoTodasSucursales.Should().BeTrue();
    }

    [Fact]
    public async Task Asignar_Cajero_NoRecibeAccesoTodasSucursales()
    {
        var ctx = BuildContext();
        var svc = BuildService(ctx);

        await svc.AsignarAsync(AsignarDto(rol: "Cajero"));

        var usuario = await ctx.Usuarios.SingleAsync(u => u.HubUsuarioId == 500);
        usuario.AccesoTodasSucursales.Should().BeFalse();
    }

    [Fact]
    public async Task Asignar_VinculaPorEmailCuandoHubUsuarioIdNoMatchea()
    {
        var ctx = BuildContext();
        ctx.Usuarios.Add(new Usuario
        {
            Id = 50,
            Email = "preexiste@test.com",
            NombreCompleto = "Pre SSO",
            HubUsuarioId = null, // cuenta pre-SSO
            EmisorId = EmisorIdSmartix,
            RolId = 4,
            Estado = EstadoUsuario.Activo
        });
        ctx.SaveChanges();
        var svc = BuildService(ctx);

        var result = await svc.AsignarAsync(AsignarDto(hubUsuarioId: 777, email: "preexiste@test.com"));

        result.Status.Should().Be(HubSyncStatus.Ok);
        result.Cambio.Should().BeFalse("ya existía y rol/emisor/estado coinciden; solo se enlazó por email");
        var usuario = await ctx.Usuarios.SingleAsync(u => u.Id == 50);
        usuario.HubUsuarioId.Should().Be(777);
    }

    [Fact]
    public async Task Asignar_VinculaPorEmail_CaseInsensitive_NoCreaDuplicado()
    {
        // E: el match por email es case-insensitive. El user pre-SSO esta en
        // minusculas; el Hub manda mayusculas. Debe enlazarse al existente, no
        // crear uno nuevo.
        var ctx = BuildContext();
        ctx.Usuarios.Add(new Usuario
        {
            Id = 60,
            Email = "preexiste@test.com",
            NombreCompleto = "Pre SSO",
            HubUsuarioId = null,
            EmisorId = EmisorIdSmartix,
            RolId = 4,
            Estado = EstadoUsuario.Activo
        });
        ctx.SaveChanges();
        var svc = BuildService(ctx);

        var result = await svc.AsignarAsync(AsignarDto(hubUsuarioId: 888, email: "PreExiste@Test.com"));

        result.Status.Should().Be(HubSyncStatus.Ok);
        ctx.Usuarios.Count(u => u.Email.ToLower() == "preexiste@test.com").Should().Be(1, "no debe crear duplicado por diferencia de mayusculas");
        var usuario = await ctx.Usuarios.SingleAsync(u => u.Id == 60);
        usuario.HubUsuarioId.Should().Be(888);
    }

    [Fact]
    public async Task Asignar_Idempotente_SiUsuarioYaExisteConMismoRol()
    {
        var ctx = BuildContext();
        ctx.Usuarios.Add(new Usuario
        {
            Id = 51,
            Email = "ya@test.com",
            NombreCompleto = "Ya",
            HubUsuarioId = 600,
            EmisorId = EmisorIdSmartix,
            RolId = 4,
            Estado = EstadoUsuario.Activo
        });
        ctx.SaveChanges();
        var svc = BuildService(ctx);

        var result = await svc.AsignarAsync(AsignarDto(hubUsuarioId: 600, email: "ya@test.com", rol: "Cajero"));

        result.Status.Should().Be(HubSyncStatus.Ok);
        result.Cambio.Should().BeFalse();
    }

    [Fact]
    public async Task Asignar_CambiaRol_SiUsuarioExistenteConRolDistinto()
    {
        var ctx = BuildContext();
        ctx.Usuarios.Add(new Usuario
        {
            Id = 52,
            Email = "rot@test.com",
            NombreCompleto = "Rotacion",
            HubUsuarioId = 610,
            EmisorId = EmisorIdSmartix,
            RolId = 4, // Cajero
            Estado = EstadoUsuario.Activo
        });
        ctx.SaveChanges();
        var svc = BuildService(ctx);

        var result = await svc.AsignarAsync(AsignarDto(hubUsuarioId: 610, email: "rot@test.com", rol: "GerenteSucursal"));

        result.Cambio.Should().BeTrue();
        var u = await ctx.Usuarios.SingleAsync(x => x.Id == 52);
        u.RolId.Should().Be(3);
    }

    [Fact]
    public async Task Asignar_ReactivaUsuarioInactivo()
    {
        var ctx = BuildContext();
        ctx.Usuarios.Add(new Usuario
        {
            Id = 53,
            Email = "inactivo@test.com",
            NombreCompleto = "Inactivo",
            HubUsuarioId = 620,
            EmisorId = EmisorIdSmartix,
            RolId = 4,
            Estado = EstadoUsuario.Inactivo
        });
        ctx.SaveChanges();
        var svc = BuildService(ctx);

        var result = await svc.AsignarAsync(AsignarDto(hubUsuarioId: 620, email: "inactivo@test.com", rol: "Cajero"));

        result.Cambio.Should().BeTrue();
        var u = await ctx.Usuarios.SingleAsync(x => x.Id == 53);
        u.Estado.Should().Be(EstadoUsuario.Activo);
    }

    // =========================================================================
    // CambiarRol
    // =========================================================================

    [Fact]
    public async Task CambiarRol_NotFound_SiUsuarioNoExiste()
    {
        var ctx = BuildContext();
        var svc = BuildService(ctx);

        var result = await svc.CambiarRolAsync(new HubUsuarioWebhookDto
        {
            HubUsuarioId = 9999,
            RolEnApp = "Cajero"
        });

        result.Status.Should().Be(HubSyncStatus.NotFound);
    }

    [Fact]
    public async Task CambiarRol_BadRequest_SiRolNoExiste()
    {
        var ctx = BuildContext();
        ctx.Usuarios.Add(new Usuario
        {
            Email = "u@t.com", NombreCompleto = "U", HubUsuarioId = 700,
            EmisorId = EmisorIdSmartix, RolId = 4, Estado = EstadoUsuario.Activo
        });
        ctx.SaveChanges();
        var svc = BuildService(ctx);

        var result = await svc.CambiarRolAsync(new HubUsuarioWebhookDto
        {
            HubUsuarioId = 700,
            RolEnApp = "NoExiste"
        });

        result.Status.Should().Be(HubSyncStatus.BadRequest);
    }

    [Fact]
    public async Task CambiarRol_NoOp_SiYaTieneEseRol()
    {
        var ctx = BuildContext();
        ctx.Usuarios.Add(new Usuario
        {
            Email = "u@t.com", NombreCompleto = "U", HubUsuarioId = 710,
            EmisorId = EmisorIdSmartix, RolId = 4, Estado = EstadoUsuario.Activo
        });
        ctx.SaveChanges();
        var svc = BuildService(ctx);

        var result = await svc.CambiarRolAsync(new HubUsuarioWebhookDto
        {
            HubUsuarioId = 710,
            RolEnApp = "Cajero"
        });

        result.Status.Should().Be(HubSyncStatus.Ok);
        result.Cambio.Should().BeFalse();
    }

    [Fact]
    public async Task CambiarRol_CambiaCuandoEsDistinto()
    {
        var ctx = BuildContext();
        ctx.Usuarios.Add(new Usuario
        {
            Email = "u@t.com", NombreCompleto = "U", HubUsuarioId = 720,
            EmisorId = EmisorIdSmartix, RolId = 4, Estado = EstadoUsuario.Activo
        });
        ctx.SaveChanges();
        var svc = BuildService(ctx);

        var result = await svc.CambiarRolAsync(new HubUsuarioWebhookDto
        {
            HubUsuarioId = 720,
            RolEnApp = "Contador"
        });

        result.Cambio.Should().BeTrue();
        var u = await ctx.Usuarios.SingleAsync(x => x.HubUsuarioId == 720);
        u.RolId.Should().Be(5);
    }

    // =========================================================================
    // Deshabilitar / Habilitar
    // =========================================================================

    [Fact]
    public async Task Deshabilitar_CambiaEstadoAInactivo()
    {
        var ctx = BuildContext();
        ctx.Usuarios.Add(new Usuario
        {
            Email = "u@t.com", NombreCompleto = "U", HubUsuarioId = 800,
            EmisorId = EmisorIdSmartix, RolId = 4, Estado = EstadoUsuario.Activo
        });
        ctx.SaveChanges();
        var svc = BuildService(ctx);

        var result = await svc.DeshabilitarAsync(800);

        result.Cambio.Should().BeTrue();
        var u = await ctx.Usuarios.SingleAsync(x => x.HubUsuarioId == 800);
        u.Estado.Should().Be(EstadoUsuario.Inactivo);
    }

    [Fact]
    public async Task Deshabilitar_NoOp_SiYaInactivo()
    {
        var ctx = BuildContext();
        ctx.Usuarios.Add(new Usuario
        {
            Email = "u@t.com", NombreCompleto = "U", HubUsuarioId = 810,
            EmisorId = EmisorIdSmartix, RolId = 4, Estado = EstadoUsuario.Inactivo
        });
        ctx.SaveChanges();
        var svc = BuildService(ctx);

        var result = await svc.DeshabilitarAsync(810);

        result.Status.Should().Be(HubSyncStatus.Ok);
        result.Cambio.Should().BeFalse();
    }

    [Fact]
    public async Task Deshabilitar_NotFound_SiUsuarioNoExiste()
    {
        var ctx = BuildContext();
        var svc = BuildService(ctx);

        var result = await svc.DeshabilitarAsync(9999);

        result.Status.Should().Be(HubSyncStatus.NotFound);
    }

    [Fact]
    public async Task Habilitar_CambiaEstadoAActivo()
    {
        var ctx = BuildContext();
        ctx.Usuarios.Add(new Usuario
        {
            Email = "u@t.com", NombreCompleto = "U", HubUsuarioId = 820,
            EmisorId = EmisorIdSmartix, RolId = 4, Estado = EstadoUsuario.Inactivo
        });
        ctx.SaveChanges();
        var svc = BuildService(ctx);

        var result = await svc.HabilitarAsync(820);

        result.Cambio.Should().BeTrue();
        var u = await ctx.Usuarios.SingleAsync(x => x.HubUsuarioId == 820);
        u.Estado.Should().Be(EstadoUsuario.Activo);
    }

    // =========================================================================
    // CambiarAsignacionSucursal
    // =========================================================================

    [Fact]
    public async Task CambiarSucursal_BadRequest_SiAccionInvalida()
    {
        var ctx = BuildContext();
        var svc = BuildService(ctx);

        var result = await svc.CambiarAsignacionSucursalAsync(new HubSucursalAsignacionDto
        {
            Email = "u@t.com", SucursalId = 1, Accion = "otro", HubId = HubIdOrg
        });

        result.Status.Should().Be(HubSyncStatus.BadRequest);
    }

    [Fact]
    public async Task CambiarSucursal_NotFound_SiEmailNoExiste()
    {
        var ctx = BuildContext();
        var svc = BuildService(ctx);

        var result = await svc.CambiarAsignacionSucursalAsync(new HubSucursalAsignacionDto
        {
            Email = "no@existe.com", SucursalId = 1, Accion = "asignar", HubId = HubIdOrg
        });

        result.Status.Should().Be(HubSyncStatus.NotFound);
    }

    [Fact]
    public async Task CambiarSucursal_Asignar_AgregaUsuarioSucursal()
    {
        var ctx = BuildContext();
        ctx.Usuarios.Add(new Usuario
        {
            Email = "u@t.com", NombreCompleto = "U", HubUsuarioId = 900,
            EmisorId = EmisorIdSmartix, RolId = 4, Estado = EstadoUsuario.Activo
        });
        ctx.SaveChanges();
        var svc = BuildService(ctx);

        var result = await svc.CambiarAsignacionSucursalAsync(new HubSucursalAsignacionDto
        {
            Email = "u@t.com", SucursalId = 7, Accion = "asignar", HubId = HubIdOrg
        });

        result.Cambio.Should().BeTrue();
        var u = await ctx.Usuarios.Include(x => x.UsuarioSucursales)
            .SingleAsync(x => x.Email == "u@t.com");
        u.UsuarioSucursales.Should().ContainSingle(us => us.SucursalId == 7);
    }

    [Fact]
    public async Task CambiarSucursal_Asignar_IdempotenteSiYaAsignada()
    {
        var ctx = BuildContext();
        var user = new Usuario
        {
            Email = "u@t.com", NombreCompleto = "U", HubUsuarioId = 910,
            EmisorId = EmisorIdSmartix, RolId = 4, Estado = EstadoUsuario.Activo
        };
        user.UsuarioSucursales.Add(new UsuarioSucursal { SucursalId = 5 });
        ctx.Usuarios.Add(user);
        ctx.SaveChanges();
        var svc = BuildService(ctx);

        var result = await svc.CambiarAsignacionSucursalAsync(new HubSucursalAsignacionDto
        {
            Email = "u@t.com", SucursalId = 5, Accion = "asignar", HubId = HubIdOrg
        });

        result.Status.Should().Be(HubSyncStatus.Ok);
        result.Cambio.Should().BeFalse();
    }

    [Fact]
    public async Task CambiarSucursal_Remover_BorraUsuarioSucursal()
    {
        var ctx = BuildContext();
        var user = new Usuario
        {
            Email = "u@t.com", NombreCompleto = "U", HubUsuarioId = 920,
            EmisorId = EmisorIdSmartix, RolId = 4, Estado = EstadoUsuario.Activo
        };
        user.UsuarioSucursales.Add(new UsuarioSucursal { SucursalId = 8 });
        ctx.Usuarios.Add(user);
        ctx.SaveChanges();
        var svc = BuildService(ctx);

        var result = await svc.CambiarAsignacionSucursalAsync(new HubSucursalAsignacionDto
        {
            Email = "u@t.com", SucursalId = 8, Accion = "remover", HubId = HubIdOrg
        });

        result.Cambio.Should().BeTrue();
        var u = await ctx.Usuarios.Include(x => x.UsuarioSucursales)
            .SingleAsync(x => x.Email == "u@t.com");
        u.UsuarioSucursales.Should().BeEmpty();
    }

    [Fact]
    public async Task CambiarSucursal_Remover_IdempotenteSiNoEstaAsignada()
    {
        var ctx = BuildContext();
        ctx.Usuarios.Add(new Usuario
        {
            Email = "u@t.com", NombreCompleto = "U", HubUsuarioId = 930,
            EmisorId = EmisorIdSmartix, RolId = 4, Estado = EstadoUsuario.Activo
        });
        ctx.SaveChanges();
        var svc = BuildService(ctx);

        var result = await svc.CambiarAsignacionSucursalAsync(new HubSucursalAsignacionDto
        {
            Email = "u@t.com", SucursalId = 99, Accion = "remover", HubId = HubIdOrg
        });

        result.Status.Should().Be(HubSyncStatus.Ok);
        result.Cambio.Should().BeFalse();
    }

    // =========================================================================
    // ListarUsuariosRoles
    // =========================================================================

    [Fact]
    public async Task ListarUsuariosRoles_IncluyeVinculadosYHuerfanosConEmail()
    {
        var ctx = BuildContext();
        ctx.Usuarios.AddRange(
            new Usuario { Email = "linked@t.com", NombreCompleto = "L", HubUsuarioId = 1000,
                          EmisorId = EmisorIdSmartix, RolId = 4, Estado = EstadoUsuario.Activo },
            new Usuario { Email = "orphan@t.com", NombreCompleto = "O", HubUsuarioId = null,
                          EmisorId = EmisorIdSmartix, RolId = 4, Estado = EstadoUsuario.Activo }
        );
        ctx.SaveChanges();
        var svc = BuildService(ctx);

        var result = await svc.ListarUsuariosRolesAsync();

        result.Should().HaveCount(2, "ahora /reconciliar necesita ver huérfanos para matchear por email");

        var linked = result.Single(r => r.Email == "linked@t.com");
        linked.HubUsuarioId.Should().Be(1000);
        linked.RolEnApp.Should().Be("Cajero");
        linked.Estado.Should().Be("Activo");

        var orphan = result.Single(r => r.Email == "orphan@t.com");
        orphan.HubUsuarioId.Should().BeNull("usuarios pre-SSO sin vincular se exponen para reconciliación");
        orphan.RolEnApp.Should().Be("Cajero");
    }
}
