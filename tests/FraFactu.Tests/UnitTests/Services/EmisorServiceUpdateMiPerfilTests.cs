using System.Security.Claims;
using AutoMapper;
using FraFactu.Application.DTOs.Emisores;
using FraFactu.Application.Interfaces;
using FraFactu.Application.Mapping;
using FraFactu.Domain.Entities;
using FraFactu.Infrastructure.Persistence;
using FraFactu.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FraFactu.Tests.UnitTests.Services;

/// <summary>
/// Tests de UpdateMiPerfilAsync tras F1 (identidad propia, sin Hub): FraFactu es
/// dueño de los datos fiscales, así que siempre se aplican. Los campos
/// Smartix-only (Mh*, Smtp*, Ambiente) se aplican solo para roles admin.
/// </summary>
public class EmisorServiceUpdateMiPerfilTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"EmisorPerfilTests_{Guid.NewGuid()}")
            .Options;
        return new ApplicationDbContext(options);
    }

    private static IMapper CreateMapper()
    {
        var cfg = new MapperConfiguration(c => c.AddProfile<MappingProfile>());
        return cfg.CreateMapper();
    }

    private static IHttpContextAccessor CreateAdminAccessor()
    {
        var ctx = new DefaultHttpContext();
        ctx.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, "EmisorAdmin")
        }, "Test"));
        return new HttpContextAccessor { HttpContext = ctx };
    }

    private static EmisorService BuildService(ApplicationDbContext ctx, IHttpContextAccessor? accessor = null)
    {
        var encryption = new Mock<IEncryptionService>();
        encryption.Setup(e => e.Encrypt(It.IsAny<string>())).Returns<string>(s => $"ENC:{s}");
        return new EmisorService(
            ctx,
            CreateMapper(),
            accessor ?? CreateAdminAccessor(),
            encryption.Object,
            new Mock<ILogger<EmisorService>>().Object);
    }

    private static Emisor SeedEmisor(ApplicationDbContext ctx, int id = 10)
    {
        var emisor = new Emisor
        {
            Id = id,
            Nit = "06140506141011",
            Nrc = "ORIGINAL-123",
            NombreRazonSocial = "Razon Original SA",
            NombreComercial = "Comercial Original",
            CodigoActividad = "47111",
            DescripcionActividad = "Comercio original",
            CorreoElectronico = "original@empresa.com",
            Telefono = "22220000",
            Direccion = "Direccion original",
            CatDepartamentoId = 1,
            CatMunicipioId = 1,
            CatTipoEstablecimientoId = 1
        };
        ctx.Emisores.Add(emisor);
        ctx.SaveChanges();
        return emisor;
    }

    [Fact]
    public async Task UpdateMiPerfil_AplicaCamposFiscales()
    {
        var ctx = CreateContext();
        var svc = BuildService(ctx);
        SeedEmisor(ctx);

        var dto = new UpdatePerfilEmisorDto
        {
            NombreComercial = "Comercial Nuevo",
            CorreoElectronico = "nuevo@empresa.com",
            Telefono = "77777777",
            Direccion = "Direccion nueva",
            Nrc = "NUEVO-789",
            CodigoActividad = "47999",
            DescripcionActividad = "Actividad nueva",
            CatDepartamentoId = 5,
            CatMunicipioId = 5,
            CatTipoEstablecimientoId = 5
        };

        await svc.UpdateMiPerfilAsync(10, dto);

        var emisor = await ctx.Emisores.FindAsync(10);
        Assert.Equal("Comercial Nuevo", emisor!.NombreComercial);
        Assert.Equal("nuevo@empresa.com", emisor.CorreoElectronico);
        Assert.Equal("77777777", emisor.Telefono);
        Assert.Equal("Direccion nueva", emisor.Direccion);
        Assert.Equal("NUEVO-789", emisor.Nrc);
        Assert.Equal("47999", emisor.CodigoActividad);
        Assert.Equal(5, emisor.CatDepartamentoId);
        Assert.Equal(5, emisor.CatMunicipioId);
        Assert.Equal(5, emisor.CatTipoEstablecimientoId);
    }

    [Fact]
    public async Task UpdateMiPerfil_AplicaCamposSmartixOnly_ParaAdmin()
    {
        var ctx = CreateContext();
        var svc = BuildService(ctx);
        SeedEmisor(ctx);

        var dto = new UpdatePerfilEmisorDto
        {
            CatAmbienteDestinoId = 2,
            MhUsuario = "mh-user-nuevo",
            MhClaveApi = "clave-api-en-claro",
            SmtpHost = "smtp.midominio.com",
            SmtpPort = 587,
            EmailHabilitado = true
        };

        await svc.UpdateMiPerfilAsync(10, dto);

        var emisor = await ctx.Emisores.FindAsync(10);
        Assert.Equal(2, emisor!.CatAmbienteDestinoId);
        Assert.Equal("mh-user-nuevo", emisor.MhUsuario);
        Assert.Equal("ENC:clave-api-en-claro", emisor.MhClaveApi); // encriptada
        Assert.Equal("smtp.midominio.com", emisor.SmtpHost);
        Assert.Equal(587, emisor.SmtpPort);
        Assert.True(emisor.EmailHabilitado);
    }
}
