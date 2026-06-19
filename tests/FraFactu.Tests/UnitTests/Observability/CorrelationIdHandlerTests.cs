using FraFactu.Infrastructure.Http;
using Microsoft.AspNetCore.Http;

namespace FraFactu.Tests.UnitTests.Observability;

/// <summary>
/// F7: <see cref="CorrelationIdHandler"/> debe propagar el correlationId
/// desde HttpContext.Items hacia los HttpClient salientes via header
/// X-Correlation-Id. Si no hay HttpContext (background tasks) o el item no
/// esta seteado, NO debe agregar el header.
/// </summary>
public class CorrelationIdHandlerTests
{
    private static (HttpClient client, CapturingInner inner) BuildClient(HttpContext? ctx)
    {
        var accessor = new HttpContextAccessor { HttpContext = ctx };
        var inner = new CapturingInner();
        var handler = new CorrelationIdHandler(accessor) { InnerHandler = inner };
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://test.local") };
        return (client, inner);
    }

    [Fact]
    public async Task PropagaHeaderCuandoHayCorrelationIdEnHttpContext()
    {
        var ctx = new DefaultHttpContext();
        ctx.Items[CorrelationIdHandler.CorrelationIdItemKey] = "cid-from-request";
        var (client, inner) = BuildClient(ctx);

        await client.GetAsync("/ping");

        Assert.True(inner.LastRequest!.Headers.Contains(CorrelationIdHandler.HeaderName));
        Assert.Equal("cid-from-request",
            inner.LastRequest!.Headers.GetValues(CorrelationIdHandler.HeaderName).Single());
    }

    [Fact]
    public async Task NoAgregaHeaderSiNoHayHttpContext()
    {
        var (client, inner) = BuildClient(ctx: null);

        await client.GetAsync("/ping");

        Assert.False(inner.LastRequest!.Headers.Contains(CorrelationIdHandler.HeaderName));
    }

    [Fact]
    public async Task NoAgregaHeaderSiItemNoEstaSeteado()
    {
        var ctx = new DefaultHttpContext();
        var (client, inner) = BuildClient(ctx);

        await client.GetAsync("/ping");

        Assert.False(inner.LastRequest!.Headers.Contains(CorrelationIdHandler.HeaderName));
    }

    [Fact]
    public async Task RespectaHeaderExistenteSiYaFueAgregadoManualmente()
    {
        var ctx = new DefaultHttpContext();
        ctx.Items[CorrelationIdHandler.CorrelationIdItemKey] = "cid-from-request";
        var (client, inner) = BuildClient(ctx);

        var req = new HttpRequestMessage(HttpMethod.Get, "/ping");
        req.Headers.Add(CorrelationIdHandler.HeaderName, "cid-manual-override");
        await client.SendAsync(req);

        Assert.Equal("cid-manual-override",
            inner.LastRequest!.Headers.GetValues(CorrelationIdHandler.HeaderName).Single());
    }

    private class CapturingInner : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }
}
