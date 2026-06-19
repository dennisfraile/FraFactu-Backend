using System.Text.Json;
using FraFactu.Application.Common.Settings;
using FraFactu.Application.DTOs.Integraciones;
using FraFactu.Application.Interfaces;
using FraFactu.Domain.Entities;
using FraFactu.Domain.Enums;
using FraFactu.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FraFactu.Infrastructure.Jobs;

/// <summary>
/// F3 (Plan inventario desde DTE): BackgroundService que drena el outbox
/// <c>IntegracionInventarioPendiente</c>. Por cada job ENCOLADO (o FALLIDO
/// listo para reintento) lee el payload, llama a SmartInventory y actualiza
/// el estado. Si la app no esta configurada (sin URL/ApiKey) el servicio
/// no hace nada — util para entornos dev sin SmartInventory levantado.
///
/// Patron alineado con <c>LecturaCorreoConsumer</c> (F2 DTEs Recibidos):
/// metodo estatico testeable + bucle wrapper.
/// </summary>
public class IntegracionInventarioConsumer : BackgroundService
{
    private static readonly TimeSpan EsperaInicial = TimeSpan.FromSeconds(10);

    private readonly IServiceProvider _serviceProvider;
    private readonly SmartInventorySettings _settings;
    private readonly ILogger<IntegracionInventarioConsumer> _logger;

    public IntegracionInventarioConsumer(
        IServiceProvider serviceProvider,
        IOptions<SmartInventorySettings> settings,
        ILogger<IntegracionInventarioConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.EstaConfigurado)
        {
            _logger.LogInformation(
                "[IntegracionInventarioConsumer] SmartInventory no esta configurado; consumer inactivo.");
            return;
        }

        await Task.Delay(EsperaInicial, stoppingToken);

        var intervalo = TimeSpan.FromSeconds(Math.Max(1, _settings.PollingSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcesarSiguienteJobAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[IntegracionInventarioConsumer] Error inesperado en el bucle");
            }

            try { await Task.Delay(intervalo, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task ProcesarSiguienteJobAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var client = scope.ServiceProvider.GetRequiredService<ISmartInventoryClient>();

        await ProcesarSiguienteJobAsync(context, client, _settings, _logger, ct);
    }

    /// <summary>
    /// Pickup testeable: toma el job mas antiguo elegible para procesar,
    /// llama a SmartInventory y aplica la transicion correspondiente. Se
    /// expone como internal static para validar el comportamiento con
    /// EF InMemory + ISmartInventoryClient fake.
    /// </summary>
    internal static async Task ProcesarSiguienteJobAsync(
        ApplicationDbContext context,
        ISmartInventoryClient client,
        SmartInventorySettings settings,
        ILogger logger,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        var job = await context.IntegracionInventarioPendientes
            .Where(j =>
                (j.Estado == EstadoIntegracionInventario.ENCOLADO ||
                 j.Estado == EstadoIntegracionInventario.FALLIDO)
                && (j.FechaProximoIntento == null || j.FechaProximoIntento <= now)
                && j.Intentos < settings.MaxIntentos)
            .OrderBy(j => j.FechaCreacion)
            .FirstOrDefaultAsync(ct);

        if (job == null) return;

        job.Estado = EstadoIntegracionInventario.EN_PROCESO;
        job.Intentos++;
        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "[IntegracionInventarioConsumer] Procesando job {JobId} (intento {Intento}/{Max}, evento {Tipo}, mov {Mov})",
            job.Id, job.Intentos, settings.MaxIntentos, job.TipoEvento, job.MovimientoIdExterno);

        SmartInventoryMovimientoRequestDto? payload;
        try
        {
            payload = JsonSerializer.Deserialize<SmartInventoryMovimientoRequestDto>(job.PayloadJson);
            if (payload == null) throw new InvalidOperationException("Payload deserializado a null.");
        }
        catch (Exception ex)
        {
            // Payload corrupto: error permanente, no tiene sentido reintentar.
            MarcarFallidoPermanente(job, $"Payload invalido: {ex.Message}");
            await context.SaveChangesAsync(CancellationToken.None);
            logger.LogError(ex,
                "[IntegracionInventarioConsumer] Job {JobId} payload invalido", job.Id);
            return;
        }

        try
        {
            var resultado = await client.RegistrarEntradaAsync(payload, ct);

            if (resultado.EsExito)
            {
                job.Estado = EstadoIntegracionInventario.COMPLETADO;
                job.FechaProcesado = DateTime.UtcNow;
                job.UltimoError = resultado.EsDuplicado
                    ? "Idempotente (movimiento ya procesado en SmartInventory)"
                    : null;
                await context.SaveChangesAsync(CancellationToken.None);
                return;
            }

            if (resultado.EsErrorPermanente)
            {
                MarcarFallidoPermanente(job, resultado.Mensaje ?? "Error permanente");
                await context.SaveChangesAsync(CancellationToken.None);
                return;
            }

            // Caso defensivo: cliente devolvio resultado raro.
            ReencolarConBackoff(job, "Resultado inesperado del cliente", settings);
            await context.SaveChangesAsync(CancellationToken.None);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Shutdown: dejamos el job EN_PROCESO; al reanudar el consumidor lo
            // retomara (lo elige porque EN_PROCESO no es excluyente del filtro
            // si lo agregamos en el futuro - hoy queda ahi para inspeccion manual).
            // El admin puede re-encolarlo manualmente.
            throw;
        }
        catch (Exception ex)
        {
            // Transitorio: reintentar con backoff. Si superamos MaxIntentos en el
            // proximo ciclo, ProcesarSiguienteJobAsync no lo va a tomar y queda
            // marcado FALLIDO en este mismo SaveChanges.
            if (job.Intentos >= settings.MaxIntentos)
            {
                MarcarFallidoPermanente(job, ex.Message);
            }
            else
            {
                ReencolarConBackoff(job, ex.Message, settings);
            }

            try { await context.SaveChangesAsync(CancellationToken.None); }
            catch (Exception saveEx)
            {
                logger.LogError(saveEx,
                    "[IntegracionInventarioConsumer] No se pudo persistir el reencolado del job {JobId}",
                    job.Id);
            }

            logger.LogWarning(ex,
                "[IntegracionInventarioConsumer] Job {JobId} fallo en intento {Intento}",
                job.Id, job.Intentos);
        }
    }

    private static void ReencolarConBackoff(
        IntegracionInventarioPendiente job, string mensaje, SmartInventorySettings settings)
    {
        // Backoff exponencial: 1, 2, 4, 8... minutos, tope ~6h.
        var minutos = (int)Math.Min(360, Math.Pow(2, job.Intentos - 1));
        job.Estado = EstadoIntegracionInventario.FALLIDO;
        job.FechaProximoIntento = DateTime.UtcNow.AddMinutes(minutos);
        job.UltimoError = Truncar(mensaje, 1000);
    }

    private static void MarcarFallidoPermanente(IntegracionInventarioPendiente job, string mensaje)
    {
        job.Estado = EstadoIntegracionInventario.FALLIDO;
        job.FechaProcesado = DateTime.UtcNow;
        job.FechaProximoIntento = null;  // no se reencola
        job.UltimoError = Truncar(mensaje, 1000);
    }

    private static string Truncar(string s, int max) =>
        string.IsNullOrEmpty(s) ? "" : (s.Length <= max ? s : s[..max]);
}
