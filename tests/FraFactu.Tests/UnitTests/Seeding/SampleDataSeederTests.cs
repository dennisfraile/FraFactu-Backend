using System.Linq;
using System.Threading.Tasks;
using FraFactu.Application.Interfaces;
using FraFactu.Domain.Entities;
using FraFactu.Domain.Entities.Catalogos;
using FraFactu.Infrastructure.Persistence;
using FraFactu.Infrastructure.Persistence.Seeding;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace FraFactu.Tests.UnitTests.Seeding;

public class SampleDataSeederTests
{
    private static ApplicationDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"sample-{System.Guid.NewGuid()}")
            .Options;
        return new ApplicationDbContext(options);
    }

    private static IAuthService FakeAuth()
    {
        var mock = new Mock<IAuthService>();
        mock.Setup(a => a.HashPassword(It.IsAny<string>())).Returns<string>(p => "hash:" + p);
        return mock.Object;
    }

    private static async Task ArrangePrereqs(ApplicationDbContext db)
    {
        db.Roles.Add(new Rol { Nombre = "EmisorAdmin" });
        db.Roles.Add(new Rol { Nombre = "Cajero" });
        // Los catálogos heredan de CatalogoBase → propiedades Codigo/Valor (NO Nombre).
        db.CatDepartamentos.Add(new CatDepartamento { Codigo = "00", Valor = "Depto Demo" });
        db.CatMunicipios.Add(new CatMunicipio { Codigo = "00", Valor = "Muni Demo", CodigoDepartamento = "00" });
        db.CatTiposEstablecimiento.Add(new CatTipoEstablecimiento { Codigo = "01", Valor = "Sucursal" });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Siembra_el_tenant_demo_completo()
    {
        using var db = NewDb();
        await ArrangePrereqs(db);

        await SampleDataSeeder.SeedAsync(db, FakeAuth(), NullLogger.Instance);

        (await db.Emisores.CountAsync(e => e.Nit == SampleDataSeeder.DemoNit)).Should().Be(1);
        (await db.Sucursales.CountAsync()).Should().Be(1);
        (await db.Cajas.CountAsync()).Should().Be(1);
        var usuarios = await db.Usuarios.Include(u => u.Rol).ToListAsync();
        usuarios.Select(u => u.Rol.Nombre).Should().BeEquivalentTo(new[] { "EmisorAdmin", "Cajero" });
        usuarios.Should().OnlyContain(u => u.RequiereCambioPwd);
    }

    [Fact]
    public async Task Es_idempotente_por_Nit_del_emisor()
    {
        using var db = NewDb();
        await ArrangePrereqs(db);

        await SampleDataSeeder.SeedAsync(db, FakeAuth(), NullLogger.Instance);
        await SampleDataSeeder.SeedAsync(db, FakeAuth(), NullLogger.Instance);

        (await db.Emisores.CountAsync()).Should().Be(1);
        (await db.Usuarios.CountAsync()).Should().Be(2);
    }
}
