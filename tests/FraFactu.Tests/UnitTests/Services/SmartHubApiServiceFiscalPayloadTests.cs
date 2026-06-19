using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FraFactu.Application.Common.Settings;
using FraFactu.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Xunit;

namespace FraFactu.Tests.UnitTests.Services;

/// <summary>
/// Tests del cliente HTTP outgoing hacia SmartHub para refrescar el cache
/// fiscal del Emisor durante el SSO hub-login (Bloque D / Task 16).
/// Casos: happy path con header X-Api-Key, fallback null en 4xx/5xx/timeout/config.
/// </summary>
public class SmartHubApiServiceFiscalPayloadTests
{
    private static SmartHubApiService BuildService(HttpMessageHandler handler, SmartHubSettings? settings = null)
    {
        var http = new HttpClient(handler);
        var opts = new Mock<IOptions<SmartHubSettings>>();
        opts.Setup(o => o.Value).Returns(settings ?? new SmartHubSettings
        {
            BaseUrl = "https://hub.test",
            ApiKey = "test-key"
        });
        return new SmartHubApiService(http, opts.Object, NullLogger<SmartHubApiService>.Instance, new MemoryCache(new MemoryCacheOptions()));
    }

    private static Mock<HttpMessageHandler> HandlerReturning(HttpResponseMessage response)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
        return handler;
    }

    [Fact]
    public async Task GetFiscalPayload_HaceGetConApiKey_YDeserializa()
    {
        HttpRequestMessage? captured = null;
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => captured = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"emisor":{"hubId":42,"nit":"X","nrc":"Y","nombreRazonSocial":"RS","codActividadEconomica":"47190","descActividadEconomica":"Desc","codTipoEstablecimiento":"01","codDepartamento":"06","codMunicipio":"23","direccionComplemento":"Calle"},"sucursal":null}""",
                    System.Text.Encoding.UTF8, "application/json")
            });

        var svc = BuildService(handler.Object);
        var result = await svc.GetFiscalPayloadForHubAsync(42);

        Assert.NotNull(result);
        Assert.Equal(42, result!.Emisor.HubId);
        Assert.Equal("RS", result.Emisor.NombreRazonSocial);
        Assert.Null(result.Sucursal);

        Assert.NotNull(captured);
        Assert.Equal(HttpMethod.Get, captured!.Method);
        Assert.Equal("https://hub.test/api/internal/hubs/42/fiscal-payload", captured.RequestUri!.ToString());
        Assert.Equal("test-key", captured.Headers.GetValues("X-Api-Key").Single());
    }

    [Fact]
    public async Task GetFiscalPayload_RetornaNull_CuandoStatus400()
    {
        var handler = HandlerReturning(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("""{"error":"Hub no tiene datos fiscales completos."}""")
        });
        var svc = BuildService(handler.Object);

        var result = await svc.GetFiscalPayloadForHubAsync(42);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetFiscalPayload_RetornaNull_CuandoStatus404()
    {
        var handler = HandlerReturning(new HttpResponseMessage(HttpStatusCode.NotFound));
        var svc = BuildService(handler.Object);

        var result = await svc.GetFiscalPayloadForHubAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetFiscalPayload_RetornaNull_CuandoBaseUrlVacio()
    {
        var handler = new Mock<HttpMessageHandler>();
        var svc = BuildService(handler.Object, new SmartHubSettings { BaseUrl = "", ApiKey = "k" });

        var result = await svc.GetFiscalPayloadForHubAsync(42);

        Assert.Null(result);
        // No debe hacer ninguna llamada HTTP si no hay BaseUrl configurada.
        handler.Protected().Verify("SendAsync", Times.Never(),
            ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetFiscalPayload_RetornaNull_CuandoExcepcionRed()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("connection refused"));
        var svc = BuildService(handler.Object);

        var result = await svc.GetFiscalPayloadForHubAsync(42);

        Assert.Null(result);
    }
}
