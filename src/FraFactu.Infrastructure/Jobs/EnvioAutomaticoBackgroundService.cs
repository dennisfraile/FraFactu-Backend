using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace FraFactu.Infrastructure.Jobs;

/// <summary>
/// Background Service que ejecuta el envío automático de lotes
/// según la configuración de cada emisor
/// </summary>
public class EnvioAutomaticoBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EnvioAutomaticoBackgroundService> _logger;
    private Timer? _timer;

    public EnvioAutomaticoBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<EnvioAutomaticoBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[BACKGROUND-SERVICE] Servicio de envío automático iniciado");

        // Ejecutar cada minuto para verificar si hay que enviar lotes
        _timer = new Timer(
            callback: async _ => await VerificarYEjecutarEnvios(),
            state: null,
            dueTime: TimeSpan.FromMinutes(1), // Esperar 1 minuto antes de la primera ejecución
            period: TimeSpan.FromMinutes(1)   // Verificar cada minuto
        );

        // Mantener el servicio corriendo
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task VerificarYEjecutarEnvios()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<FraFactu.Infrastructure.Persistence.ApplicationDbContext>();

            var nowUtc = DateTime.UtcNow;
            var zonaElSalvador = TimeZoneInfo.FindSystemTimeZoneById("Central America Standard Time");
            var nowElSalvador = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, zonaElSalvador);
            var horaActual = nowElSalvador.TimeOfDay;

            // Buscar configuraciones donde la hora actual coincida con la hora programada
            // (con tolerancia de ±1 minuto)
            var configuraciones = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
                context.ConfiguracionEnvioLotes
                    .Where(c => c.EnvioAutomaticoHabilitado && c.Activo));

            foreach (var config in configuraciones)
            {
                // Verificar si estamos en la hora programada (±1 minuto de tolerancia)
                var diferencia = Math.Abs((config.HoraEnvioAutomatico - horaActual).TotalMinutes);

                if (diferencia <= 1.5) // Tolerancia de 1.5 minutos
                {
                    _logger.LogInformation("[BACKGROUND-SERVICE] Hora de envío detectada para emisor {EmisorId}. " +
                        "Hora programada: {HoraProgramada}, Hora actual: {HoraActual}",
                        config.EmisorId,
                        config.HoraEnvioAutomatico,
                        horaActual);

                    // Ejecutar el job
                    var job = scope.ServiceProvider.GetRequiredService<EnvioAutomaticoLotesJob>();
                    await job.EjecutarEnvioAutomaticoAsync();
                }

                // Verificar si hay que enviar recordatorio
                if (config.EnviarRecordatorio)
                {
                    var horaRecordatorio = config.HoraEnvioAutomatico
                        .Subtract(TimeSpan.FromMinutes(config.MinutosAnticipacionRecordatorio));

                    var diferenciaRecordatorio = Math.Abs((horaRecordatorio - horaActual).TotalMinutes);

                    if (diferenciaRecordatorio <= 1.5)
                    {
                        _logger.LogInformation("[BACKGROUND-SERVICE] Hora de recordatorio para emisor {EmisorId}",
                            config.EmisorId);

                        var job = scope.ServiceProvider.GetRequiredService<EnvioAutomaticoLotesJob>();
                        await job.EnviarRecordatorioAsync();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BACKGROUND-SERVICE] Error en verificación de envíos");
        }
    }

    public override void Dispose()
    {
        _timer?.Dispose();
        base.Dispose();
    }
}
