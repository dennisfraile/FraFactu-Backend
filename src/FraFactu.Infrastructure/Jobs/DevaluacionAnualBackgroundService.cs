using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using FraFactu.Domain.Entities;
using FraFactu.Domain.Enums;
using FraFactu.Infrastructure.Persistence;

namespace FraFactu.Infrastructure.Jobs;

/// <summary>
/// F3 (G1). Devalúa mobiliario y equipo cada 1 de enero a las 03:00 UTC. Aplica el
/// porcentaje anual sobre ValorActual (decreciente), respetando ValorResidual como
/// piso. Cuando se cumplen AniosVidaUtil desde FechaAdquisicion deja de tocar el
/// item. Items sin datos completos (FechaAdquisicion/AniosVidaUtil/
/// PorcentajeDevaluacionAnual/ValorActual) se ignoran hasta completarlos.
///
/// Idempotente por año: FechaUltimaDevaluacion bloquea doble run dentro del mismo
/// año calendario, así que reiniciar el host no causa devaluación duplicada.
/// </summary>
public class DevaluacionAnualBackgroundService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<DevaluacionAnualBackgroundService> _logger;

    public DevaluacionAnualBackgroundService(
        IServiceProvider services,
        ILogger<DevaluacionAnualBackgroundService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Task.Delay solo acepta hasta ~24.8 días (int.MaxValue ms). Como el
        // próximo run puede estar a >365 días, dormimos en chunks de 1 hora.
        var maxChunk = TimeSpan.FromHours(1);

        while (!stoppingToken.IsCancellationRequested)
        {
            var next = NextRunUtc(DateTime.UtcNow);
            _logger.LogInformation(
                "[DEVALUACION] Próximo run programado en {Fecha:O} (en {Horas:F1}h)",
                next, (next - DateTime.UtcNow).TotalHours);

            try
            {
                var wait = next - DateTime.UtcNow;
                while (wait > TimeSpan.Zero && !stoppingToken.IsCancellationRequested)
                {
                    var chunk = wait > maxChunk ? maxChunk : wait;
                    await Task.Delay(chunk, stoppingToken);
                    wait = next - DateTime.UtcNow;
                }
            }
            catch (TaskCanceledException)
            {
                return;
            }

            if (stoppingToken.IsCancellationRequested) return;

            try
            {
                await EjecutarAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[DEVALUACION] Error ejecutando job anual. Reintentará en el próximo 1/ene 03:00 UTC.");
            }
        }
    }

    /// <summary>
    /// Calcula el próximo 1 de enero a las 03:00 UTC desde el momento dado.
    /// Si ya pasamos el 1/ene 03:00 de este año, devuelve el del año siguiente.
    /// </summary>
    public static DateTime NextRunUtc(DateTime nowUtc)
    {
        var thisYear = new DateTime(nowUtc.Year, 1, 1, 3, 0, 0, DateTimeKind.Utc);
        return nowUtc < thisYear
            ? thisYear
            : new DateTime(nowUtc.Year + 1, 1, 1, 3, 0, 0, DateTimeKind.Utc);
    }

    /// <summary>
    /// Recorre los items Mobiliario con datos completos y aplica una devaluación
    /// anual. Público para que un endpoint admin (futuro) pueda disparar un re-run
    /// manual sin esperar al cron natural. Devuelve cuántos items se modificaron.
    /// </summary>
    public async Task<int> EjecutarAsync(CancellationToken ct = default)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hoy = DateTime.UtcNow;

        var items = await db.Set<ProductoServicio>()
            .Where(p => p.TipoInventario == TipoInventario.MobiliarioEquipo
                && p.FechaAdquisicion != null
                && p.AniosVidaUtil != null && p.AniosVidaUtil > 0
                && p.PorcentajeDevaluacionAnual != null && p.PorcentajeDevaluacionAnual > 0
                && p.ValorActual != null)
            .ToListAsync(ct);

        var aplicados = 0;
        foreach (var p in items)
        {
            if (Devaluar(p, hoy))
                aplicados++;
        }

        if (aplicados > 0)
            await db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "[DEVALUACION] Job completado: {Aplicados}/{Total} items depreciados",
            aplicados, items.Count);

        return aplicados;
    }

    /// <summary>
    /// Aplica una devaluación anual al item si corresponde. Devuelve true si lo
    /// modificó. Reglas:
    /// - Solo MobiliarioEquipo con los 4 campos mínimos.
    /// - Skip si FechaUltimaDevaluacion es del mismo año que hoy (idempotente).
    /// - Skip si no ha pasado al menos un año calendario desde FechaAdquisicion.
    /// - Skip si pasaron más años que AniosVidaUtil.
    /// - Aplica ValorActual *= (1 - %/100), redondeado a 2 decimales. Piso = ValorResidual ?? 0.
    /// Método estático para que sea testeable sin DB ni DI.
    /// </summary>
    public static bool Devaluar(ProductoServicio p, DateTime hoy)
    {
        if (p.TipoInventario != TipoInventario.MobiliarioEquipo)
            return false;

        if (!p.FechaAdquisicion.HasValue
            || !p.AniosVidaUtil.HasValue
            || !p.PorcentajeDevaluacionAnual.HasValue
            || !p.ValorActual.HasValue)
            return false;

        // Idempotente por año calendario: si ya corrimos este año, skip.
        if (p.FechaUltimaDevaluacion.HasValue
            && p.FechaUltimaDevaluacion.Value.Year == hoy.Year)
            return false;

        var aniosTranscurridos = hoy.Year - p.FechaAdquisicion.Value.Year;
        if (aniosTranscurridos < 1) return false;
        if (aniosTranscurridos > p.AniosVidaUtil.Value) return false;

        var factor = 1m - (p.PorcentajeDevaluacionAnual.Value / 100m);
        var nuevo = p.ValorActual.Value * factor;
        var piso = p.ValorResidual ?? 0m;
        if (nuevo < piso) nuevo = piso;
        nuevo = Math.Round(nuevo, 2);

        if (nuevo == p.ValorActual.Value) return false;
        p.ValorActual = nuevo;
        p.FechaUltimaDevaluacion = hoy;
        return true;
    }
}
