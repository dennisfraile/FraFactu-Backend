using FraFactu.Application.Common.Settings;
using FraFactu.Application.Interfaces;
using FraFactu.Domain.Enums;
using FraFactu.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FraFactu.Infrastructure.Jobs;

/// <summary>
/// F3: dispara periódicamente la lectura automática del correo para cada
/// emisor con <c>LecturaCorreoHabilitada</c>, encolando un job que el
/// <see cref="LecturaCorreoConsumer"/> procesará. Por diseño solo lee el mes
/// en curso; para meses anteriores el usuario debe usar la lectura manual.
///
/// Reglas:
/// - Se ignora un emisor si ya tiene job ENCOLADO o EN_PROGRESO (idempotencia
///   reutilizada de <see cref="EmailReaderService"/>).
/// - Se respeta un cooldown (config) desde <c>UltimaLecturaCorreo</c> para no
///   re-leer al mismo emisor en cada barrido si el intervalo es corto.
/// - Si el toggle global <c>DtesRecibidos:RecepcionAutomatica:Habilitada</c>
///   está en false, el bucle no encola nada (pero sigue vivo para reaccionar
///   a cambios de configuración en caliente).
/// </summary>
public class LecturaCorreoAutomaticaScheduler : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptionsMonitor<DtesRecibidosSettings> _settings;
    private readonly ILogger<LecturaCorreoAutomaticaScheduler> _logger;

    public LecturaCorreoAutomaticaScheduler(
        IServiceProvider serviceProvider,
        IOptionsMonitor<DtesRecibidosSettings> settings,
        ILogger<LecturaCorreoAutomaticaScheduler> logger)
    {
        _serviceProvider = serviceProvider;
        _settings = settings;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var esperaInicial = TimeSpan.FromSeconds(
            Math.Max(1, _settings.CurrentValue.RecepcionAutomatica.EsperaInicialSegundos));
        try { await Task.Delay(esperaInicial, stoppingToken); }
        catch (OperationCanceledException) { return; }

        _logger.LogInformation(
            "[LecturaCorreoAutomaticaScheduler] Iniciando barrido cada {Intervalo} minutos (cooldown {Cooldown}h)",
            _settings.CurrentValue.RecepcionAutomatica.IntervaloMinutos,
            _settings.CurrentValue.RecepcionAutomatica.CooldownHoras);

        while (!stoppingToken.IsCancellationRequested)
        {
            var config = _settings.CurrentValue.RecepcionAutomatica;

            if (config.Habilitada)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var emailReader = scope.ServiceProvider.GetRequiredService<IEmailReaderService>();

                    await BarrerYEncolarAsync(context, emailReader, config.CooldownHoras, _logger, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[LecturaCorreoAutomaticaScheduler] Error en el barrido");
                }
            }

            var intervalo = TimeSpan.FromMinutes(Math.Max(1, config.IntervaloMinutos));
            try { await Task.Delay(intervalo, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    /// <summary>
    /// Núcleo del barrido. Public+static para que los tests lo invoquen
    /// directamente sin levantar todo el DI ni el bucle del hosted service.
    /// </summary>
    public static async Task BarrerYEncolarAsync(
        ApplicationDbContext context,
        IEmailReaderService emailReader,
        int cooldownHoras,
        ILogger logger,
        CancellationToken ct)
    {
        var corteCooldown = DateTime.UtcNow.AddHours(-Math.Max(0, cooldownHoras));

        // Emisores candidatos: tienen Gmail conectado, lectura habilitada, están
        // activos, y o nunca se leyó o pasó el cooldown desde la última lectura.
        var candidatos = await context.Emisores
            .Where(e => e.LecturaCorreoHabilitada
                && e.GmailConectado
                && e.Activo
                && e.GmailRefreshToken != null
                && (e.UltimaLecturaCorreo == null || e.UltimaLecturaCorreo <= corteCooldown))
            .Select(e => e.Id)
            .ToListAsync(ct);

        if (candidatos.Count == 0)
        {
            logger.LogDebug("[LecturaCorreoAutomaticaScheduler] No hay emisores candidatos en este barrido");
            return;
        }

        // Emisores con job activo: los saltamos sin pasar por EncolarLecturaAsync
        // (que igual los reusaría, pero así evitamos el roundtrip y logueamos claro).
        var conJobActivo = await context.LecturaCorreoJobs
            .Where(j => candidatos.Contains(j.EmisorId)
                && (j.Estado == EstadoLecturaCorreoJob.ENCOLADO
                    || j.Estado == EstadoLecturaCorreoJob.EN_PROGRESO))
            .Select(j => j.EmisorId)
            .Distinct()
            .ToListAsync(ct);

        var aEncolar = candidatos.Except(conJobActivo).ToList();

        if (aEncolar.Count == 0)
        {
            logger.LogDebug(
                "[LecturaCorreoAutomaticaScheduler] {Total} candidatos, todos con job activo",
                candidatos.Count);
            return;
        }

        logger.LogInformation(
            "[LecturaCorreoAutomaticaScheduler] Encolando lectura automatica para {Cantidad} emisores",
            aEncolar.Count);

        foreach (var emisorId in aEncolar)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                // Sin rango: EncolarLecturaAsync usa el mes en curso por default.
                await emailReader.EncolarLecturaAsync(
                    emisorId,
                    usuarioId: null,
                    rangoDesde: null,
                    rangoHasta: null,
                    esAutomatico: true,
                    ct: ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "[LecturaCorreoAutomaticaScheduler] No se pudo encolar lectura automatica para emisor {EmisorId}",
                    emisorId);
            }
        }
    }
}
