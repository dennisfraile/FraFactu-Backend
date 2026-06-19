using FraFactu.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FraFactu.Infrastructure.Jobs;

/// <summary>
/// Reintenta automáticamente la transmisión a Hacienda de las facturas que quedaron en
/// estado ERROR (fallo de comunicación con MH), espaciando los reintentos ≥ 15 min
/// (Normativa DTE, regla 13.2.1). Tras agotar el tope de intentos, FacturaService las
/// escala a contingencia diferida (PENDIENTE_LOTE).
/// </summary>
public class ReintentoEnvioErrorBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReintentoEnvioErrorBackgroundService> _logger;

    // Cadencia de la pasada. El espaciado real ≥ 15 min por factura lo garantiza
    // FacturaService.ReintentarEnviosEnErrorAsync con FechaErrorEnvio.
    private static readonly TimeSpan Intervalo = TimeSpan.FromMinutes(15);

    public ReintentoEnvioErrorBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<ReintentoEnvioErrorBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "[REINTENTO-ERROR] Servicio de reintento de envíos en ERROR iniciado (cada {Min} min)",
            Intervalo.TotalMinutes);

        // Espera inicial para no competir con el arranque de la aplicación.
        try { await Task.Delay(Intervalo, stoppingToken); }
        catch (TaskCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var facturaService = scope.ServiceProvider.GetRequiredService<IFacturaService>();
                await facturaService.ReintentarEnviosEnErrorAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[REINTENTO-ERROR] Error en la pasada de reintento de envíos en ERROR");
            }

            try { await Task.Delay(Intervalo, stoppingToken); }
            catch (TaskCanceledException) { break; }
        }
    }
}
