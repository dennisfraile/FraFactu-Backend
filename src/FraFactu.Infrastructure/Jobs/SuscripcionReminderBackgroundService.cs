using FraFactu.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FraFactu.Infrastructure.Jobs;

/// <summary>
/// Background Service que ejecuta recordatorios de suscripción:
/// - Día 1 de cada mes: envía recordatorio a TODOS los emisores con suscripción activa
/// - 7 días antes del vencimiento: envía recordatorio de proximidad
/// Corre diario a las 8:00 AM hora El Salvador
/// </summary>
public class SuscripcionReminderBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SuscripcionReminderBackgroundService> _logger;
    private Timer? _timer;
    private DateTime _ultimaEjecucion = DateTime.MinValue;

    public SuscripcionReminderBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<SuscripcionReminderBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[SUSCRIPCION-REMINDER] Servicio de recordatorios de suscripción iniciado");

        // Verificar cada minuto si es hora de ejecutar
        _timer = new Timer(
            callback: async _ => await VerificarYEjecutar(),
            state: null,
            dueTime: TimeSpan.FromMinutes(1),
            period: TimeSpan.FromMinutes(1)
        );

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (TaskCanceledException)
        {
            // Shutdown graceful - esto es esperado
            _logger.LogInformation("[SUSCRIPCION-REMINDER] Servicio detenido");
        }
    }

    private async Task VerificarYEjecutar()
    {
        try
        {
            // Convertir a hora de El Salvador
            var nowUtc = DateTime.UtcNow;
            var zonaElSalvador = TimeZoneInfo.FindSystemTimeZoneById("Central America Standard Time");
            var nowElSalvador = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, zonaElSalvador);
            var horaActual = nowElSalvador.TimeOfDay;

            // Ejecutar a las 8:00 AM hora El Salvador (±1.5 minutos de tolerancia)
            var horaObjetivo = new TimeSpan(8, 0, 0);
            var diferencia = Math.Abs((horaObjetivo - horaActual).TotalMinutes);

            if (diferencia > 1.5)
                return;

            // Evitar doble ejecución en el mismo día
            if (_ultimaEjecucion.Date == nowElSalvador.Date)
                return;

            _ultimaEjecucion = nowElSalvador;
            _logger.LogInformation("[SUSCRIPCION-REMINDER] Ejecutando recordatorios - Fecha El Salvador: {Fecha}",
                nowElSalvador.ToString("yyyy-MM-dd HH:mm:ss"));

            using var scope = _serviceProvider.CreateScope();
            var suscripcionService = scope.ServiceProvider.GetRequiredService<ISuscripcionService>();

            // 1. Si es día 1 del mes → recordatorio mensual a todos
            if (nowElSalvador.Day == 1)
            {
                _logger.LogInformation("[SUSCRIPCION-REMINDER] Día 1 del mes - Enviando recordatorios mensuales");
                await suscripcionService.ProcesarRecordatoriosMensualesAsync();
            }

            // 2. Siempre verificar si hay suscripciones que vencen en 7 días
            _logger.LogInformation("[SUSCRIPCION-REMINDER] Verificando suscripciones próximas a vencer");
            await suscripcionService.ProcesarRecordatoriosProximidadAsync();

            _logger.LogInformation("[SUSCRIPCION-REMINDER] Recordatorios procesados exitosamente");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SUSCRIPCION-REMINDER] Error en la verificación de recordatorios");
        }
    }

    public override void Dispose()
    {
        _timer?.Dispose();
        base.Dispose();
    }
}
