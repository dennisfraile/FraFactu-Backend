using FraFactu.API.Middleware;
using Microsoft.AspNetCore.Http;

namespace FraFactu.Tests.UnitTests.Observability;

/// <summary>
/// F7: <see cref="CorrelationIdMiddleware"/> debe leer el header X-Correlation-Id
/// si viene (cross-app), o generar un UUID v4 si no. Siempre debe exponer el id
/// en HttpContext.Items[ItemKey] y en el header de la respuesta.
/// </summary>
public class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task UsaHeaderEntranteSiViene()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers[CorrelationIdMiddleware.HeaderName] = "abcd-1234-dead-beef";
        var nextCalled = false;

        var mw = new CorrelationIdMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        await mw.InvokeAsync(ctx);

        Assert.True(nextCalled);
        Assert.Equal("abcd-1234-dead-beef", ctx.Items[CorrelationIdMiddleware.ItemKey]);
        Assert.Equal("abcd-1234-dead-beef", ctx.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString());
    }

    [Fact]
    public async Task GeneraUuidSiNoVieneHeader()
    {
        var ctx = new DefaultHttpContext();

        var mw = new CorrelationIdMiddleware(_ => Task.CompletedTask);
        await mw.InvokeAsync(ctx);

        var generado = ctx.Items[CorrelationIdMiddleware.ItemKey] as string;
        Assert.NotNull(generado);
        Assert.True(Guid.TryParse(generado, out _), $"Esperaba UUID v4, recibido: {generado}");
        Assert.Equal(generado, ctx.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString());
    }

    [Fact]
    public async Task GeneraUuidSiHeaderVieneVacio()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers[CorrelationIdMiddleware.HeaderName] = "   ";

        var mw = new CorrelationIdMiddleware(_ => Task.CompletedTask);
        await mw.InvokeAsync(ctx);

        var generado = ctx.Items[CorrelationIdMiddleware.ItemKey] as string;
        Assert.NotNull(generado);
        Assert.True(Guid.TryParse(generado, out _));
    }

    [Fact]
    public async Task ExponerEnResponseHeaderEsCondicionRequerida()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers[CorrelationIdMiddleware.HeaderName] = "fixed-id-123";

        var mw = new CorrelationIdMiddleware(_ => Task.CompletedTask);
        await mw.InvokeAsync(ctx);

        Assert.Equal("fixed-id-123", ctx.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString());
    }
}
