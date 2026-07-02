using System.Linq;
using System.Threading.Tasks;
using FraFactu.Application.Interfaces;
using FraFactu.Domain.Enums;
using FraFactu.Infrastructure.Persistence;
using FraFactu.Infrastructure.Persistence.Seeding;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace FraFactu.Tests.UnitTests.Seeding;

public class BootstrapSeederTests
{
    private static ApplicationDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"bootstrap-{System.Guid.NewGuid()}")
            .Options;
        return new ApplicationDbContext(options);
    }

    private static IAuthService FakeAuth()
    {
        var mock = new Mock<IAuthService>();
        mock.Setup(a => a.HashPassword(It.IsAny<string>()))
            .Returns<string>(p => "hash:" + p);
        return mock.Object;
    }

    private static readonly string[] Canonicos =
        { "SuperAdmin", "EmisorAdmin", "GerenteSucursal", "Cajero", "Auditor", "Contador", "EncargadoInventario" };

    [Fact]
    public async Task Siembra_los_7_roles_canonicos()
    {
        using var db = NewDb();
        await BootstrapSeeder.SeedAsync(db, new BootstrapOptions(), FakeAuth(), NullLogger.Instance);

        var roles = await db.Roles.Select(r => r.Nombre).ToListAsync();
        roles.Should().BeEquivalentTo(Canonicos);
    }

    [Fact]
    public async Task Roles_son_idempotentes_en_re_run()
    {
        using var db = NewDb();
        await BootstrapSeeder.SeedAsync(db, new BootstrapOptions(), FakeAuth(), NullLogger.Instance);
        await BootstrapSeeder.SeedAsync(db, new BootstrapOptions(), FakeAuth(), NullLogger.Instance);

        (await db.Roles.CountAsync()).Should().Be(7);
    }

    [Fact]
    public async Task Sin_password_no_crea_usuario_SuperAdmin()
    {
        using var db = NewDb();
        await BootstrapSeeder.SeedAsync(db, new BootstrapOptions { AdminEmail = "a@b.com" }, FakeAuth(), NullLogger.Instance);

        (await db.Usuarios.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Con_credenciales_crea_SuperAdmin_forzando_cambio_pwd()
    {
        using var db = NewDb();
        var opts = new BootstrapOptions { AdminEmail = "admin@frafactu.local", AdminPassword = "S3cr#ta", AdminNombre = "Root" };

        await BootstrapSeeder.SeedAsync(db, opts, FakeAuth(), NullLogger.Instance);

        var user = await db.Usuarios.Include(u => u.Rol).SingleAsync();
        user.Email.Should().Be("admin@frafactu.local");
        user.NombreCompleto.Should().Be("Root");
        user.Rol.Nombre.Should().Be("SuperAdmin");
        user.RequiereCambioPwd.Should().BeTrue();
        user.AccesoTodasSucursales.Should().BeTrue();
        user.EmisorId.Should().BeNull();
        user.ProveedorAuth.Should().Be(ProveedorAutenticacion.Local);
        user.Estado.Should().Be(EstadoUsuario.Activo);
        user.PasswordHash.Should().Be("hash:S3cr#ta");
    }

    [Fact]
    public async Task SuperAdmin_es_idempotente_por_email()
    {
        using var db = NewDb();
        var opts = new BootstrapOptions { AdminEmail = "admin@frafactu.local", AdminPassword = "S3cr#ta" };

        await BootstrapSeeder.SeedAsync(db, opts, FakeAuth(), NullLogger.Instance);
        await BootstrapSeeder.SeedAsync(db, opts, FakeAuth(), NullLogger.Instance);

        (await db.Usuarios.CountAsync(u => u.Email == "admin@frafactu.local")).Should().Be(1);
    }
}
