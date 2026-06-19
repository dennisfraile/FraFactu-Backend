using System.Net;
using Google;
using Polly;
using Polly.Extensions.Http;

namespace FraFactu.Infrastructure.Http;

/// <summary>
/// F7: politica unificada de reintentos HTTP cross-app. 3 reintentos con
/// exponential backoff (base 1s) + jitter aleatorio 0-1000ms. Reintenta
/// 5xx, 408 (timeout) y 429 (rate limit). Peor caso ~14s antes de fallar.
/// Misma formula en SmartHub, Smartix y SmartInventory para que el
/// comportamiento sea predecible cross-app.
/// </summary>
public static class HttpPolicies
{
    public static IAsyncPolicy<HttpResponseMessage> CrossAppRetry()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(r => (int)r.StatusCode == 429)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, attempt - 1))
                    + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 1000)));
    }

    /// <summary>
    /// F4: politica de reintentos para llamadas Gmail API. Mismo perfil
    /// (3 reintentos + exponential backoff + jitter) que <see cref="CrossAppRetry"/>,
    /// pero adaptado a las excepciones que lanza la SDK de Google
    /// (<see cref="GoogleApiException"/> con codigos 5xx/429, y errores
    /// de red genericos como <see cref="HttpRequestException"/> / sockets cerrados).
    /// No reintenta 401/403 (auth) ni 404 (recurso ausente): no son transitorios.
    /// </summary>
    public static IAsyncPolicy GmailRetry()
    {
        return Policy
            .Handle<GoogleApiException>(ex => EsCodigoTransitorio(ex.HttpStatusCode))
            .Or<HttpRequestException>()
            .Or<TaskCanceledException>(ex => ex.InnerException is TimeoutException
                                            || ex.InnerException is IOException)
            .Or<IOException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, attempt - 1))
                    + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 1000)));
    }

    private static bool EsCodigoTransitorio(HttpStatusCode code)
    {
        var n = (int)code;
        return n == 0 || n == 408 || n == 429 || n >= 500;
    }
}
