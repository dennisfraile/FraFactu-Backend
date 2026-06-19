using FraFactu.Application.Common.Settings;
using FraFactu.Application.Interfaces;
using FraFactu.Domain.Entities;
using FraFactu.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FraFactu.Infrastructure.Jobs;

/// <summary>
/// F4 (Plan inventario desde DTE): BackgroundService que ejecuta el job
/// periodico de reconciliacion. Cada <c>IntervaloHoras</c> recorre los
/// emisores con <see cref="Emisor.TieneSmartInventoryActiva"/>=true y
/// llama a <see cref="ReconciliacionInventarioService.EjecutarParaEmisorAsync"/>
/// para cada uno. Si el cliente no esta configurado, el job queda inactivo
/// silenciosamente (mismo patron que <see cref="IntegracionInventarioConsumer"/>).
/// </summary>
public class ReconciliacionInventarioJob : BackgroundService
{
    private static readonly TimeSpan EsperaInicial = TimeSpan.FromMinutes(1);

    private readonly IServiceProvider _serviceProvider;
    private readonly ReconciliacionInventarioSettings _settings;
    private readonly SmartInventorySettings _smartInventorySettings;
    private readonly ILogger<ReconciliacionInventarioJob> _logger;

    public ReconciliacionInventarioJob(
        IServiceProvider serviceProvider,
        IOptions<ReconciliacionInventarioSettings> settings,
        IOptions<SmartInventorySettings> smartInventorySettings,
        ILogger<ReconciliacionInventarioJob> logger)
    {
        _serviceProvider = serviceProvider;
        _settings = settings.Value;
        _smartInventorySettings = smartInventorySettings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Habilitado)
        {
            _logger.LogInformation(
                "[ReconciliacionInventarioJob] Deshabilitado por configuracion; job inactivo.");
            return;
        }

        if (!_smartInventorySettings.EstaConfigurado)
        {
            _logger.LogInformation(
                "[ReconciliacionInventarioJob] SmartInventory no esta configurado; job inactivo.");
            return;
        }

        // Espera inicial para no chocar con el startup migration en docker-compose.
        await Task.Delay(EsperaInicial, stoppingToken);

        var intervalo = TimeSpan.FromHours(Math.Max(1, _settings.IntervaloHoras));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await EjecutarCorridaAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[ReconciliacionInventarioJob] Error inesperado en el bucle");
            }

            try { await Task.Delay(intervalo, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task EjecutarCorridaAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var client = scope.ServiceProvider.GetRequiredService<ISmartInventoryClient>();
        await EjecutarCorridaAsync(context, client, _logger, ct);
    }

    /// <summary>
    /// Cuerpo testeable de una corrida: recorre todos los emisores con
    /// SmartInventory activo y llama al servicio por cada uno. Aislado
    /// el bucle externo (Task.Delay) para que los tests puedan invocarlo
    /// con EF InMemory + ISmartInventoryClient mockeado.
    /// </summary>
    internal static async Task EjecutarCorridaAsync(
        ApplicationDbContext context,
        ISmartInventoryClient client,
        ILogger logger,
        CancellationToken ct)
    {
        var ejecucionId = Guid.NewGuid();

        var emisores = await context.Emisores
            .AsNoTracking()
            .Where(e => e.Activo && e.TieneSmartInventoryActiva && e.HubId != null)
            .ToListAsync(ct);

        if (emisores.Count == 0)
        {
            logger.LogInformation(
                "[ReconciliacionInventarioJob] Sin emisores con SmartInventory activo; ejec={Ejec}",
                ejecucionId);
            return;
        }

        logger.LogInformation(
            "[ReconciliacionInventarioJob] Iniciando corrida {Ejec} sobre {Count} emisores",
            ejecucionId, emisores.Count);

        foreach (var emisor in emisores)
        {
            if (ct.IsCancellationRequested) break;
            try
            {
                await ReconciliacionInventarioService.EjecutarParaEmisorAsync(
                    context, client, emisor, ejecucionId, logger, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // No interrumpe la corrida: un emisor que falle (red, datos)
                // no debe bloquear la reconciliacion del resto.
                logger.LogError(ex,
                    "[ReconciliacionInventarioJob] Emisor {EmisorId} fallo en ejec {Ejec}",
                    emisor.Id, ejecucionId);
            }
        }
    }
}
