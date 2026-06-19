using FraFactu.Application.Interfaces;
using FraFactu.Domain.Enums;
using FraFactu.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FraFactu.Infrastructure.Jobs;

/// <summary>
/// F2: servicio en background que recoge solicitudes de lectura de correo
/// (<c>LecturaCorreoJob</c>) en estado ENCOLADO y las ejecuta secuencialmente
/// vía <see cref="IEmailLectorWorker"/>. Se procesa un job por iteración para
/// mantener la carga sobre Gmail acotada (la paginación interna del worker
/// barre toda la bandeja del emisor).
/// </summary>
public class LecturaCorreoConsumer : BackgroundService
{
    private static readonly TimeSpan EsperaInicial = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan IntervaloPolling = TimeSpan.FromSeconds(3);

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LecturaCorreoConsumer> _logger;

    public LecturaCorreoConsumer(
        IServiceProvider serviceProvider,
        ILogger<LecturaCorreoConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(EsperaInicial, stoppingToken);

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
                _logger.LogError(ex, "[LecturaCorreoConsumer] Error inesperado en el bucle de procesamiento");
            }

            try { await Task.Delay(IntervaloPolling, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task ProcesarSiguienteJobAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var worker = scope.ServiceProvider.GetRequiredService<IEmailLectorWorker>();

        await ProcesarSiguienteJobAsync(context, worker, _logger, ct);
    }

    /// <summary>
    /// Versión testeable del pickup: toma el job ENCOLADO más antiguo,
    /// lo marca EN_PROGRESO, delega al worker, y cierra el job en
    /// COMPLETADO o FALLIDO según el resultado. Se expone como internal
    /// static para poder validar transiciones sin levantar el BackgroundService.
    /// </summary>
    internal static async Task ProcesarSiguienteJobAsync(
        ApplicationDbContext context,
        IEmailLectorWorker worker,
        ILogger logger,
        CancellationToken ct)
    {
        var job = await context.LecturaCorreoJobs
            .Where(j => j.Estado == EstadoLecturaCorreoJob.ENCOLADO)
            .OrderBy(j => j.FechaCreacion)
            .FirstOrDefaultAsync(ct);

        if (job == null) return;

        // Marca el inicio antes de invocar al worker para que la UI ya vea EN_PROGRESO.
        job.Estado = EstadoLecturaCorreoJob.EN_PROGRESO;
        job.FechaInicio = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "[LecturaCorreoConsumer] Iniciando job {JobId} para emisor {EmisorId}",
            job.Id, job.EmisorId);

        try
        {
            await worker.EjecutarAsync(job.Id, ct);

            job.Estado = EstadoLecturaCorreoJob.COMPLETADO;
            job.FechaFin = DateTime.UtcNow;
            await context.SaveChangesAsync(ct);

            logger.LogInformation(
                "[LecturaCorreoConsumer] Job {JobId} completado: {Nuevos} nuevos, {Duplicados} duplicados, {Errores} errores",
                job.Id, job.DtesNuevos, job.DtesDuplicados, job.Errores);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // La app se está apagando: dejar el job en EN_PROGRESO para que
            // se note al reanudar; otra opción sería marcarlo ENCOLADO para
            // reintento, pero eso podría duplicar trabajo si ya procesó parte.
            throw;
        }
        catch (Exception ex)
        {
            job.Estado = EstadoLecturaCorreoJob.FALLIDO;
            job.FechaFin = DateTime.UtcNow;
            job.MensajeError = ex.Message.Length > 1000 ? ex.Message[..1000] : ex.Message;

            try { await context.SaveChangesAsync(CancellationToken.None); }
            catch (Exception saveEx)
            {
                logger.LogError(saveEx,
                    "[LecturaCorreoConsumer] No se pudo persistir el estado FALLIDO del job {JobId}",
                    job.Id);
            }

            logger.LogError(ex,
                "[LecturaCorreoConsumer] Job {JobId} fallido para emisor {EmisorId}",
                job.Id, job.EmisorId);
        }
    }
}
