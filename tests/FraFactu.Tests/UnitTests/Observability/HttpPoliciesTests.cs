using System.Net;
using FraFactu.Infrastructure.Http;

namespace FraFactu.Tests.UnitTests.Observability;

/// <summary>
/// F7 fase 4: <see cref="HttpPolicies.CrossAppRetry"/> debe reintentar 3 veces
/// los errores transitorios (5xx, 408, 429) y NO reintentar 4xx ni 2xx. El test
/// del 5xx ejecuta la politica completa con backoff real (~7s) — eso es
/// intencional para validar la formula end-to-end.
/// </summary>
public class HttpPoliciesTests
{
    [Fact]
    public async Task NoReintentaParaResponse2xx()
    {
        var policy = HttpPolicies.CrossAppRetry();
        var calls = 0;

        var result = await policy.ExecuteAsync(() =>
        {
            calls++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        Assert.Equal(1, calls);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task NoReintentaParaResponse4xx()
    {
        var policy = HttpPolicies.CrossAppRetry();
        var calls = 0;

        var result = await policy.ExecuteAsync(() =>
        {
            calls++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest));
        });

        Assert.Equal(1, calls);
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task Reintenta3VecesParaResponse5xx()
    {
        var policy = HttpPolicies.CrossAppRetry();
        var calls = 0;

        var result = await policy.ExecuteAsync(() =>
        {
            calls++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        });

        Assert.Equal(4, calls); // 1 inicial + 3 retries
        Assert.Equal(HttpStatusCode.InternalServerError, result.StatusCode);
    }

    [Fact]
    public async Task Reintenta3VecesParaResponse429()
    {
        var policy = HttpPolicies.CrossAppRetry();
        var calls = 0;

        var result = await policy.ExecuteAsync(() =>
        {
            calls++;
            return Task.FromResult(new HttpResponseMessage((HttpStatusCode)429));
        });

        Assert.Equal(4, calls);
        Assert.Equal(429, (int)result.StatusCode);
    }
}
