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
/// Revocacion cross-app via TokenVersion: el middleware OnTokenValidated de
/// Smartix pega al Hub por cada request autenticada con un JWT que trae
/// usuario_hub_id + token_version. Estos tests cubren el contrato HTTP del
/// metodo cliente: URL, header X-Api-Key, cache TTL, fail-open ante errores.
/// </summary>
public class SmartHubApiServiceTokenVersionTests
{
    private static SmartHubApiService BuildService(HttpMessageHandler handler, IMemoryCache? cache = null, SmartHubSettings? settings = null)
    {
        var http = new HttpClient(handler);
        var opts = new Mock<IOptions<SmartHubSettings>>();
        opts.Setup(o => o.Value).Returns(settings ?? new SmartHubSettings
        {
            BaseUrl = "https://hub.test",
            ApiKey = "test-key"
        });
        return new SmartHubApiService(
            http,
            opts.Object,
            NullLogger<SmartHubApiService>.Instance,
            cache ?? new MemoryCache(new MemoryCacheOptions()));
    }

    private static Mock<HttpMessageHandler> Handler(HttpStatusCode status, string? body = null)
    {
        var response = new HttpResponseMessage(status);
        if (body != null) response.Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
        return handler;
    }

    [Fact]
    public async Task GetTokenVersion_OK_DevuelveValor_Y_PegaConApiKey()
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
                Content = new StringContent("""{"tokenVersion":7}""", System.Text.Encoding.UTF8, "application/json")
            });

        var sut = BuildService(handler.Object);
        var version = await sut.GetTokenVersionAsync(42);

        Assert.Equal(7, version);
        Assert.NotNull(captured);
        Assert.Equal(HttpMethod.Get, captured!.Method);
        Assert.Equal("https://hub.test/api/auth/token-version/42", captured.RequestUri!.ToString());
        Assert.Equal("test-key", captured.Headers.GetValues("X-Api-Key").Single());
    }

    [Fact]
    public async Task GetTokenVersion_Cachea_60s_NoRepiteHttp()
    {
        var handler = Handler(HttpStatusCode.OK, """{"tokenVersion":3}""");
        var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = BuildService(handler.Object, cache);

        var v1 = await sut.GetTokenVersionAsync(99);
        var v2 = await sut.GetTokenVersionAsync(99);

        Assert.Equal(3, v1);
        Assert.Equal(3, v2);
        handler.Protected().Verify("SendAsync", Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetTokenVersion_404_DevuelveNull_FailOpen()
    {
        var handler = Handler(HttpStatusCode.NotFound);
        var sut = BuildService(handler.Object);

        var version = await sut.GetTokenVersionAsync(123);

        Assert.Null(version);
    }

    [Fact]
    public async Task GetTokenVersion_500_DevuelveNull_FailOpen()
    {
        var handler = Handler(HttpStatusCode.InternalServerError);
        var sut = BuildService(handler.Object);

        var version = await sut.GetTokenVersionAsync(123);

        Assert.Null(version);
    }

    [Fact]
    public async Task GetTokenVersion_ConfigVacia_DevuelveNull_SinPegar()
    {
        var handler = new Mock<HttpMessageHandler>();
        var sut = BuildService(handler.Object, settings: new SmartHubSettings { BaseUrl = "", ApiKey = "" });

        var version = await sut.GetTokenVersionAsync(1);

        Assert.Null(version);
        handler.Protected().Verify("SendAsync", Times.Never(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetTokenVersion_ExcepcionDeRed_DevuelveNull_FailOpen()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("connection refused"));

        var sut = BuildService(handler.Object);
        var version = await sut.GetTokenVersionAsync(1);

        Assert.Null(version);
    }
}
