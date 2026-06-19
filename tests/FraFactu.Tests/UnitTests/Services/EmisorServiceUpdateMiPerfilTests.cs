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
/// Tests del guard write-only en UpdateMiPerfilAsync (Plan B Fase 3 / Task 19):
/// si el emisor está vinculado a un Hub, los campos fiscales identitarios se
/// descartan; los campos Smartix-only (Mh*, Smtp*, Ambiente, Gmail*) se aplican.
/// Emisores legacy sin HubId conservan el comportamiento original.
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

    private static (EmisorService svc, ApplicationDbContext ctx, Mock<ILogger<EmisorService>> logger)
        BuildService(IHttpContextAccessor? accessor = null)
    {
        var ctx = CreateContext();
        var logger = new Mock<ILogger<EmisorService>>();
        var encryption = new Mock<IEncryptionService>();
        encryption.Setup(e => e.Encrypt(It.IsAny<string>())).Returns<string>(s => $"ENC:{s}");
        var svc = new EmisorService(
            ctx,
            CreateMapper(),
            accessor ?? CreateAdminAccessor(),
            encryption.Object,
            logger.Object);
        return (svc, ctx, logger);
    }

    private static Emisor SeedEmisorVinculadoAHub(ApplicationDbContext ctx, int id = 10, int hubId = 100)
    {
        var emisor = new Emisor
        {
            Id = id,
            HubId = hubId,
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

    private static Emisor SeedEmisorLegacy(ApplicationDbContext ctx, int id = 20)
    {
        var emisor = new Emisor
        {
            Id = id,
            HubId = null, // legacy
            Nit = "06140506141022",
            Nrc = "LEGACY-456",
            NombreRazonSocial = "Razon Legacy SA",
            NombreComercial = "Comercial Legacy",
            CodigoActividad = "47999",
            DescripcionActividad = "Legacy",
            CorreoElectronico = "legacy@empresa.com",
            Telefono = "22229999",
            Direccion = "Direccion legacy",
            CatDepartamentoId = 2,
            CatMunicipioId = 2,
            CatTipoEstablecimientoId = 2
        };
        ctx.Emisores.Add(emisor);
        ctx.SaveChanges();
        return emisor;
    }

    [Fact]
    public async Task UpdateMiPerfil_DescartaCamposFiscales_CuandoEmisorTieneHubId()
    {
        var (svc, ctx, _) = BuildService();
        SeedEmisorVinculadoAHub(ctx);

        var dto = new UpdatePerfilEmisorDto
        {
            NombreComercial = "Intento Editar",
            CorreoElectronico = "intento@hack.com",
            Telefono = "99999999",
            Direccion = "Direccion FALSA",
            Nrc = "FALSO-999",
            CodigoActividad = "00000",
            DescripcionActividad = "Falsa actividad",
            CatDepartamentoId = 99,
            CatMunicipioId = 99,
            CatTipoEstablecimientoId = 99
        };

        await svc.UpdateMiPerfilAsync(10, dto);

        var emisor = await ctx.Emisores.FindAsync(10);
        // Valores originales del seed preservados.
        Assert.Equal("Comercial Original", emisor!.NombreComercial);
        Assert.Equal("original@empresa.com", emisor.CorreoElectronico);
        Assert.Equal("22220000", emisor.Telefono);
        Assert.Equal("Direccion original", emisor.Direccion);
        Assert.Equal("ORIGINAL-123", emisor.Nrc);
        Assert.Equal("47111", emisor.CodigoActividad);
        Assert.Equal("Comercio original", emisor.DescripcionActividad);
        Assert.Equal(1, emisor.CatDepartamentoId);
        Assert.Equal(1, emisor.CatMunicipioId);
        Assert.Equal(1, emisor.CatTipoEstablecimientoId);
    }

    [Fact]
    public async Task UpdateMiPerfil_AplicaCamposSmartixOnly_CuandoEmisorTieneHubId()
    {
        var (svc, ctx, _) = BuildService();
        SeedEmisorVinculadoAHub(ctx);

        var dto = new UpdatePerfilEmisorDto
        {
            // Fiscales descartados — usamos valores que matchean para no generar warnings.
            CorreoElectronico = "original@empresa.com",
            Telefono = "22220000",
            Direccion = "Direccion original",
            // Smartix-only: deben aplicarse.
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

    [Fact]
    public async Task UpdateMiPerfil_LogueaWarning_CuandoSeIntentaEditarCampoFiscal()
    {
        var (svc, ctx, logger) = BuildService();
        SeedEmisorVinculadoAHub(ctx);

        var dto = new UpdatePerfilEmisorDto
        {
            Telefono = "OTRO-NUMERO",
            CorreoElectronico = "otro@empresa.com",
            Direccion = "Direccion original"
        };

        await svc.UpdateMiPerfilAsync(10, dto);

        logger.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("intentó editar campos fiscales")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task UpdateMiPerfil_NoLoguea_CuandoDtoTraeValoresIguales()
    {
        var (svc, ctx, logger) = BuildService();
        SeedEmisorVinculadoAHub(ctx);

        // dto trae exactamente los mismos valores: no es "intento de edición".
        var dto = new UpdatePerfilEmisorDto
        {
            NombreComercial = "Comercial Original",
            CorreoElectronico = "original@empresa.com",
            Telefono = "22220000",
            Direccion = "Direccion original",
            Nrc = "ORIGINAL-123",
            CodigoActividad = "47111",
            DescripcionActividad = "Comercio original",
            CatDepartamentoId = 1,
            CatMunicipioId = 1,
            CatTipoEstablecimientoId = 1
        };

        await svc.UpdateMiPerfilAsync(10, dto);

        logger.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateMiPerfil_AplicaCamposFiscales_CuandoEmisorLegacySinHubId()
    {
        // Compat: emisores pre-Plan-B (HubId=null) conservan comportamiento original.
        var (svc, ctx, _) = BuildService();
        SeedEmisorLegacy(ctx);

        var dto = new UpdatePerfilEmisorDto
        {
            NombreComercial = "Comercial Nuevo",
            CorreoElectronico = "nuevo@empresa.com",
            Telefono = "77777777",
            Direccion = "Direccion nueva",
            Nrc = "LEGACY-789",
            CodigoActividad = "47111",
            DescripcionActividad = "Actividad nueva",
            CatDepartamentoId = 5,
            CatMunicipioId = 5,
            CatTipoEstablecimientoId = 5
        };

        await svc.UpdateMiPerfilAsync(20, dto);

        var emisor = await ctx.Emisores.FindAsync(20);
        Assert.Equal("Comercial Nuevo", emisor!.NombreComercial);
        Assert.Equal("nuevo@empresa.com", emisor.CorreoElectronico);
        Assert.Equal("77777777", emisor.Telefono);
        Assert.Equal("Direccion nueva", emisor.Direccion);
        Assert.Equal("LEGACY-789", emisor.Nrc);
        Assert.Equal("47111", emisor.CodigoActividad);
        Assert.Equal(5, emisor.CatDepartamentoId);
        Assert.Equal(5, emisor.CatMunicipioId);
        Assert.Equal(5, emisor.CatTipoEstablecimientoId);
    }
}
