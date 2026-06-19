namespace FraFactu.API.Middleware;

/// <summary>
/// F7: lee el header <c>X-Correlation-Id</c> del request entrante (o genera
/// uno nuevo si no viene), lo guarda en <c>HttpContext.Items["CorrelationId"]</c>
/// para que <c>TelemetryService</c> y <c>CorrelationIdHandler</c> lo recuperen,
/// y lo expone en el header de respuesta para que el cliente pueda correlacionar
/// su llamada con los eventos del backend.
/// </summary>
public class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";
    public const string ItemKey = "CorrelationId";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var incoming = context.Request.Headers[HeaderName].FirstOrDefault();
        var correlationId = !string.IsNullOrWhiteSpace(incoming)
            ? incoming!
            : Guid.NewGuid().ToString();

        context.Items[ItemKey] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        await _next(context);
    }
}
