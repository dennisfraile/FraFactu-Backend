using FraFactu.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FraFactu.Infrastructure.Jobs;

/// <summary>
/// Background Service que corre diario a las 8:00 AM hora El Salvador:
/// marca cuotas/planes vencidos y envía recordatorios (por vencer / vencida).
/// </summary>
public class RecordatorioCuotasBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RecordatorioCuotasBackgroundService> _logger;
    private Timer? _timer;
    private DateTime _ultimaEjecucion = DateTime.MinValue;

    public RecordatorioCuotasBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<RecordatorioCuotasBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[RECORDATORIO-CUOTAS] Servicio iniciado");

        _timer = new Timer(
            callback: async _ => await VerificarYEjecutar(),
            state: null,
            dueTime: TimeSpan.FromMinutes(1),
            period: TimeSpan.FromMinutes(1));

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (TaskCanceledException)
        {
            _logger.LogInformation("[RECORDATORIO-CUOTAS] Servicio detenido");
        }
    }

    private async Task VerificarYEjecutar()
    {
        try
        {
            var nowUtc = DateTime.UtcNow;
            var zona = TimeZoneInfo.FindSystemTimeZoneById("Central America Standard Time");
            var nowSv = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, zona);

            var diferencia = Math.Abs((new TimeSpan(8, 0, 0) - nowSv.TimeOfDay).TotalMinutes);
            if (diferencia > 1.5) return;
            if (_ultimaEjecucion.Date == nowSv.Date) return;

            _ultimaEjecucion = nowSv;
            _logger.LogInformation("[RECORDATORIO-CUOTAS] Ejecutando - {Fecha}", nowSv.ToString("yyyy-MM-dd HH:mm:ss"));

            using var scope = _serviceProvider.CreateScope();
            var servicio = scope.ServiceProvider.GetRequiredService<IRecordatorioCuotasService>();
            await servicio.ProcesarAsync();

            _logger.LogInformation("[RECORDATORIO-CUOTAS] Procesado exitosamente");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RECORDATORIO-CUOTAS] Error en la ejecución");
        }
    }

    public override void Dispose()
    {
        _timer?.Dispose();
        base.Dispose();
    }
}
