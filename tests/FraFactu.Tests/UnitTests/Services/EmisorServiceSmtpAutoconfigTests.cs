using AutoMapper;
using FraFactu.Application.DTOs.Emisores;
using FraFactu.Application.Interfaces;
using FraFactu.Application.Mapping;
using FraFactu.Infrastructure.Persistence;
using FraFactu.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FraFactu.Tests.UnitTests.Services;

/// <summary>
/// Cobertura de la autoconfiguración SMTP (<c>ConfigurarSmtpAutomaticoAsync</c>):
/// proveedores de consumo por dominio literal (ruta rápida sin red) y dominios
/// corporativos (Google Workspace / M365) deducidos por registros MX.
///
/// Bug que cubre: cuentas Google Workspace (p. ej. <c>@jdsmartcode.com</c>)
/// quedaban con SmtpHost=null porque el switch solo reconocía gmail.com literal,
/// y el envío fallaba mudo en ValidarSmtpEmisor.
/// </summary>
public class EmisorServiceSmtpAutoconfigTests
{
    private static ApplicationDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"EmisorSmtpTests_{Guid.NewGuid()}")
            .Options);

    private static IMapper CreateMapper() =>
        new MapperConfiguration(c => c.AddProfile<MappingProfile>()).CreateMapper();

    private static EmisorService BuildService(
        ApplicationDbContext ctx, IProveedorSmtpResolver? resolver)
    {
        var logger = new Mock<ILogger<EmisorService>>();
        var encryption = new Mock<IEncryptionService>();
        encryption.Setup(e => e.Encrypt(It.IsAny<string>())).Returns<string>(s => $"ENC:{s}");

        return new EmisorService(
            ctx,
            CreateMapper(),
            new HttpContextAccessor { HttpContext = new DefaultHttpContext() },
            encryption.Object,
            logger.Object,
            resolver);
    }

    private static CreateEmisorDto BaseDto(string smtpUser) => new()
    {
        Nit = $"NIT-{Guid.NewGuid():N}".Substring(0, 14),
        Nrc = "123456",
        NombreRazonSocial = "Empresa de Prueba",
        CodigoActividad = "62010",
        DescripcionActividad = "Software",
        CorreoElectronico = "info@empresa.com",
        Telefono = "22222222",
        Direccion = "San Salvador",
        SmtpUser = smtpUser,
        SmtpPassword = "app-password",
        // Sin SmtpHost: forzamos la autodetección
        SmtpHost = null,
    };

    // ---- Ruta rápida: dominio literal de consumo (no debe consultar MX) ----

    [Fact]
    public async Task CreateAsync_DominioGmail_AutoconfiguraSinConsultarMx()
    {
        using var ctx = CreateContext();
        var resolver = new Mock<IProveedorSmtpResolver>();
        var svc = BuildService(ctx, resolver.Object);

        await svc.CreateAsync(BaseDto("usuario@gmail.com"));

        var emisor = await ctx.Emisores.SingleAsync();
        Assert.Equal("smtp.gmail.com", emisor.SmtpHost);
        Assert.Equal(587, emisor.SmtpPort);
        Assert.True(emisor.EmailHabilitado);
        resolver.Verify(r => r.ResolverPorMxAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---- Google Workspace: dominio corporativo deducido por MX ----

    [Fact]
    public async Task CreateAsync_DominioWorkspace_AutoconfiguraPorMx()
    {
        using var ctx = CreateContext();
        var resolver = new Mock<IProveedorSmtpResolver>();
        resolver
            .Setup(r => r.ResolverPorMxAsync("jdsmartcode.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(("smtp.gmail.com", 587));
        var svc = BuildService(ctx, resolver.Object);

        await svc.CreateAsync(BaseDto("usuario@jdsmartcode.com"));

        var emisor = await ctx.Emisores.SingleAsync();
        Assert.Equal("smtp.gmail.com", emisor.SmtpHost);
        Assert.Equal(587, emisor.SmtpPort);
        Assert.True(emisor.EmailHabilitado);
        resolver.Verify(r => r.ResolverPorMxAsync("jdsmartcode.com", It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---- Dominio desconocido sin MX reconocible: no rompe, no auto-habilita ----

    [Fact]
    public async Task CreateAsync_DominioDesconocido_DejaHostNullYNoHabilita()
    {
        using var ctx = CreateContext();
        var resolver = new Mock<IProveedorSmtpResolver>();
        resolver
            .Setup(r => r.ResolverPorMxAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((((string, int)?)null));
        var svc = BuildService(ctx, resolver.Object);

        await svc.CreateAsync(BaseDto("usuario@dominio-raro.xyz"));

        var emisor = await ctx.Emisores.SingleAsync();
        Assert.Null(emisor.SmtpHost);
        Assert.False(emisor.EmailHabilitado);
    }

    // ---- Sin resolver inyectado (compatibilidad): dominio no literal se ignora ----

    [Fact]
    public async Task CreateAsync_SinResolver_DominioNoLiteralNoConfiguraHost()
    {
        using var ctx = CreateContext();
        var svc = BuildService(ctx, resolver: null);

        await svc.CreateAsync(BaseDto("usuario@jdsmartcode.com"));

        var emisor = await ctx.Emisores.SingleAsync();
        Assert.Null(emisor.SmtpHost);
    }
}
