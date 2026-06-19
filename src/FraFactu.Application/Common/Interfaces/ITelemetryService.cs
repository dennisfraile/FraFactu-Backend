namespace FraFactu.Application.Common.Interfaces;

/// <summary>
/// F7: emite customEvents a Application Insights con correlationId resuelto
/// automaticamente desde HttpContext. Si no hay AppInsights configurado
/// (entorno local sin connection string), las llamadas son no-op.
/// </summary>
public interface ITelemetryService
{
    void TrackEvent(
        string name,
        IDictionary<string, string>? properties = null,
        IDictionary<string, double>? measurements = null);
}
