using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace FraFactu.API.HealthChecks;

/// <summary>
/// Readiness de los jobs en background. Los <see cref="BackgroundService"/> del
/// sistema son loops infinitos que atrapan sus excepciones internamente, así que en
/// operación normal su <c>ExecuteTask</c> nunca termina. Señal de fallo = la tarea
/// dejó de correr (RanToCompletion / Faulted / Canceled). Un servicio aún no
/// arrancado (ExecuteTask == null) se ignora para evitar flapping durante el boot.
///
/// No requiere tocar ninguna clase de job: <c>BackgroundService.ExecuteTask</c> es
/// público desde .NET 6.
///
/// Limitación conocida: los jobs cuyo trabajo real corre en un <c>Timer</c>
/// desacoplado (su <c>ExecuteAsync</c> solo hace <c>await Task.Delay(Infinite)</c>)
/// se reportan sanos mientras el servicio siga vivo, aunque el callback del timer
/// haya muerto en silencio. Este check confirma "servicio vivo", no "timer activo".
/// </summary>
public sealed class BackgroundServicesHealthCheck : IHealthCheck
{
    private readonly IEnumerable<IHostedService> _hostedServices;

    public BackgroundServicesHealthCheck(IEnumerable<IHostedService> hostedServices)
        => _hostedServices = hostedServices;

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var caidos = new Dictionary<string, object>();

        foreach (var svc in _hostedServices.OfType<BackgroundService>())
        {
            var task = svc.ExecuteTask;
            if (task is null) continue;      // aún arrancando -> se ignora
            if (!task.IsCompleted) continue; // corriendo -> OK

            var nombre = svc.GetType().Name;
            caidos[nombre] = task.IsFaulted
                ? $"Faulted: {task.Exception?.GetBaseException().Message}"
                : task.Status.ToString();
        }

        if (caidos.Count == 0)
            return Task.FromResult(
                HealthCheckResult.Healthy("Todos los background services están en ejecución."));

        return Task.FromResult(HealthCheckResult.Unhealthy(
            $"{caidos.Count} background service(s) caído(s): {string.Join(", ", caidos.Keys)}",
            data: caidos));
    }
}
