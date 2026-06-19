using System.Net;
using System.Text.Json;
using FraFactu.Application.Common.Settings;
using FraFactu.Domain.Entities;
using FraFactu.Infrastructure.Jobs;
using FraFactu.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace FraFactu.Tests.UnitTests.Jobs;

/// <summary>
/// F1 centralización de usuarios: <see cref="HubRoleRegistrationService"/> declara
/// el catálogo de roles de Smartix al Hub al arrancar. Debe enviar payload
/// camelCase con header X-Api-Key, omitir si la config falta, y no romper si el
/// Hub responde con error.
/// </summary>
public class HubRoleRegistrationServiceTests
{
    private static (ApplicationDbContext context, IServiceProvider sp) BuildContextScope()
    {
        var dbName = $"HubRoleRegistration_{Guid.NewGuid()}";
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(opts => opts.UseInMemoryDatabase(dbName));
        var sp = services.BuildServiceProvider();
        var context = sp.GetRequiredService<ApplicationDbContext>();
        return (context, sp);
    }

    private static SmartHubSettings BuildSettings(
        string baseUrl = "https://hub.test",
        string apiKey = "test-key",
        string selfUrl = "https://smartix.test")
        => new() { BaseUrl = baseUrl, ApiKey = apiKey, SelfUrl = selfUrl };

    private static IHttpClientFactory BuildFactory(HttpMessageHandler handler)
    {
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>()))
               .Returns(() => new HttpClient(handler, disposeHandler: false));
        return factory.Object;
    }

    private static void SeedRolesEstandar(ApplicationDbContext ctx)
    {
        ctx.Roles.AddRange(
            new Rol { Id = 1, Nombre = "SuperAdmin" },
            new Rol { Id = 2, Nombre = "EmisorAdmin" },
            new Rol { Id = 3, Nombre = "GerenteSucursal" },
            new Rol { Id = 4, Nombre = "Cajero" },
            new Rol { Id = 5, Nombre = "Contador" },
            new Rol { Id = 6, Nombre = "Vendedor" }
        );
        ctx.SaveChanges();
    }

    [Fact]
    public async Task EnviaPayloadCamelCaseConHeaderApiKey()
    {
        var (ctx, sp) = BuildContextScope();
        SeedRolesEstandar(ctx);

        var capturing = new CapturingHandler(HttpStatusCode.OK);
        var service = new HubRoleRegistrationService(
            sp,
            Options.Create(BuildSettings()),
            BuildFactory(capturing),
            NullLogger<HubRoleRegistrationService>.Instance);

        await service.RegistrarRolesEnHubAsync(CancellationToken.None);

        capturing.LastRequest.Should().NotBeNull();
        capturing.LastRequest!.Method.Should().Be(HttpMethod.Post);
        capturing.LastRequest.RequestUri!.ToString()
            .Should().Be("https://hub.test/api/aplicaciones/facturacion/roles");
        capturing.LastRequest.Headers.GetValues("X-Api-Key").Single().Should().Be("test-key");

        capturing.LastBody.Should().NotBeNull();
        var json = JsonDocument.Parse(capturing.LastBody!).RootElement;
        json.GetProperty("rolAdminGeneral").GetString().Should().Be("EmisorAdmin");
        json.GetProperty("notifyUrl").GetString().Should().Be("https://smartix.test");
        var roles = json.GetProperty("roles").EnumerateArray()
            .Select(r => r.GetProperty("nombre").GetString())
            .ToArray();
        roles.Should().BeEquivalentTo(new[]
        {
            "SuperAdmin", "EmisorAdmin", "GerenteSucursal", "Cajero", "Contador", "Vendedor"
        });
    }

    [Fact]
    public async Task QuitaTrailingSlashDeBaseUrlYSelfUrl()
    {
        var (ctx, sp) = BuildContextScope();
        SeedRolesEstandar(ctx);

        var capturing = new CapturingHandler(HttpStatusCode.OK);
        var service = new HubRoleRegistrationService(
            sp,
            Options.Create(BuildSettings(
                baseUrl: "https://hub.test/",
                selfUrl: "https://smartix.test/")),
            BuildFactory(capturing),
            NullLogger<HubRoleRegistrationService>.Instance);

        await service.RegistrarRolesEnHubAsync(CancellationToken.None);

        capturing.LastRequest!.RequestUri!.ToString()
            .Should().Be("https://hub.test/api/aplicaciones/facturacion/roles");

        var json = JsonDocument.Parse(capturing.LastBody!).RootElement;
        json.GetProperty("notifyUrl").GetString().Should().Be("https://smartix.test");
    }

    [Fact]
    public async Task OmiteRegistroSiBaseUrlVacia()
    {
        var (ctx, sp) = BuildContextScope();
        SeedRolesEstandar(ctx);

        var capturing = new CapturingHandler(HttpStatusCode.OK);
        var service = new HubRoleRegistrationService(
            sp,
            Options.Create(BuildSettings(baseUrl: string.Empty)),
            BuildFactory(capturing),
            NullLogger<HubRoleRegistrationService>.Instance);

        await service.RegistrarRolesEnHubAsync(CancellationToken.None);

        capturing.LastRequest.Should().BeNull("no debe llegar al Hub si falta BaseUrl");
    }

    [Fact]
    public async Task OmiteRegistroSiApiKeyVacia()
    {
        var (ctx, sp) = BuildContextScope();
        SeedRolesEstandar(ctx);

        var capturing = new CapturingHandler(HttpStatusCode.OK);
        var service = new HubRoleRegistrationService(
            sp,
            Options.Create(BuildSettings(apiKey: string.Empty)),
            BuildFactory(capturing),
            NullLogger<HubRoleRegistrationService>.Instance);

        await service.RegistrarRolesEnHubAsync(CancellationToken.None);

        capturing.LastRequest.Should().BeNull("no debe llegar al Hub si falta ApiKey");
    }

    [Fact]
    public async Task OmiteRegistroSiNoHayRolesEnLaBD()
    {
        var (_, sp) = BuildContextScope();

        var capturing = new CapturingHandler(HttpStatusCode.OK);
        var service = new HubRoleRegistrationService(
            sp,
            Options.Create(BuildSettings()),
            BuildFactory(capturing),
            NullLogger<HubRoleRegistrationService>.Instance);

        await service.RegistrarRolesEnHubAsync(CancellationToken.None);

        capturing.LastRequest.Should().BeNull("no tiene sentido registrar un catálogo vacío");
    }

    [Fact]
    public async Task NoLanzaExcepcionSiHubResponde500()
    {
        var (ctx, sp) = BuildContextScope();
        SeedRolesEstandar(ctx);

        var capturing = new CapturingHandler(HttpStatusCode.InternalServerError);
        var service = new HubRoleRegistrationService(
            sp,
            Options.Create(BuildSettings()),
            BuildFactory(capturing),
            NullLogger<HubRoleRegistrationService>.Instance);

        var act = async () => await service.RegistrarRolesEnHubAsync(CancellationToken.None);

        await act.Should().NotThrowAsync(
            "una caída del Hub no debe tumbar el arranque de Smartix");
        capturing.LastRequest.Should().NotBeNull();
    }

    [Fact]
    public async Task EnviaNotifyUrlVacioSiSelfUrlNoEstaConfigurado()
    {
        var (ctx, sp) = BuildContextScope();
        SeedRolesEstandar(ctx);

        var capturing = new CapturingHandler(HttpStatusCode.OK);
        var service = new HubRoleRegistrationService(
            sp,
            Options.Create(BuildSettings(selfUrl: string.Empty)),
            BuildFactory(capturing),
            NullLogger<HubRoleRegistrationService>.Instance);

        await service.RegistrarRolesEnHubAsync(CancellationToken.None);

        var json = JsonDocument.Parse(capturing.LastBody!).RootElement;
        json.GetProperty("notifyUrl").GetString().Should().BeEmpty();
    }

    /// <summary>
    /// Captura cada request que pasa por el HttpClient mockeado y permite
    /// inspeccionar URL, headers y body en los asserts.
    /// </summary>
    private sealed class CapturingHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastBody { get; private set; }

        public CapturingHandler(HttpStatusCode status) => _status = status;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            LastBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(_status) { Content = new StringContent("{}") };
        }
    }
}
