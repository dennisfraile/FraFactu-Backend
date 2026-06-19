using Microsoft.AspNetCore.Http;

namespace FraFactu.Infrastructure.Http;

/// <summary>
/// F7: <see cref="DelegatingHandler"/> que propaga el header X-Correlation-Id
/// desde el HttpContext del request entrante hacia los HttpClient salientes.
/// Permite que el correlationId atraviese los 4 backends sin codigo manual
/// en cada llamada cross-app.
/// </summary>
public class CorrelationIdHandler : DelegatingHandler
{
    public const string HeaderName = "X-Correlation-Id";
    public const string CorrelationIdItemKey = "CorrelationId";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public CorrelationIdHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!request.Headers.Contains(HeaderName)
            && _httpContextAccessor.HttpContext?.Items[CorrelationIdItemKey] is string cid
            && !string.IsNullOrWhiteSpace(cid))
        {
            request.Headers.Add(HeaderName, cid);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
