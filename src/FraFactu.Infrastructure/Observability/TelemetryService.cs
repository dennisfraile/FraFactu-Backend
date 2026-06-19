using FraFactu.Application.Common.Interfaces;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Http;

namespace FraFactu.Infrastructure.Observability;

/// <summary>
/// F7: implementacion de <see cref="ITelemetryService"/> que adjunta
/// automaticamente el correlationId resuelto desde HttpContext.Items
/// (puesto por <c>CorrelationIdMiddleware</c>) a cada customEvent.
/// </summary>
public class TelemetryService : ITelemetryService
{
    public const string CorrelationIdItemKey = "CorrelationId";

    private readonly TelemetryClient _client;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TelemetryService(TelemetryClient client, IHttpContextAccessor httpContextAccessor)
    {
        _client = client;
        _httpContextAccessor = httpContextAccessor;
    }

    public void TrackEvent(
        string name,
        IDictionary<string, string>? properties = null,
        IDictionary<string, double>? measurements = null)
    {
        var props = properties != null
            ? new Dictionary<string, string>(properties)
            : new Dictionary<string, string>();

        if (_httpContextAccessor.HttpContext?.Items[CorrelationIdItemKey] is string cid
            && !props.ContainsKey("correlationId"))
        {
            props["correlationId"] = cid;
        }

        _client.TrackEvent(name, props, measurements);
    }
}
