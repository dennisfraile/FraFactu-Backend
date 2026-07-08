using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FraFactu.API.HealthChecks;

/// <summary>
/// Escribe la respuesta de readiness como JSON diagnosticable (estado global +
/// una entrada por check) en vez del texto plano por defecto.
/// </summary>
public static class HealthCheckResponse
{
    public static async Task WriteJsonAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var payload = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description
            })
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
