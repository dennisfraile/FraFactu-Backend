using FraFactu.Application.Services;
using FraFactu.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FraFactu.Infrastructure.Jobs;

/// <summary>
/// Background service que consulta automáticamente los estados individuales
/// de los DTEs en lotes que ya fueron enviados a Hacienda.
/// </summary>
public class ConsultaEstadosLotesBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ConsultaEstadosLotesBackgroundService> _logger;

    private static readonly TimeSpan IntervaloEjecucion = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan TiempoMinimoDesdeEnvio = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan TiempoMinimoEntreConsultas = TimeSpan.FromMinutes(5);

    public ConsultaEstadosLotesBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<ConsultaEstadosLotesBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("ConsultaEstadosLotesBackgroundService iniciado.");

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await ConsultarEstadosPendientesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ejecución de ConsultaEstadosLotesBackgroundService");
            }

            await Task.Delay(IntervaloEjecucion, ct);
        }
    }

    private async Task ConsultarEstadosPendientesAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var loteService = scope.ServiceProvider.GetRequiredService<ILoteService>();

        var ahora = DateTime.UtcNow;

        // Buscar lotes que:
        // 1. Estado = "Enviado" (enviados a MH, pendientes de consulta de estados individuales)
        // 2. FechaEnvio no es null y fue hace al menos 2 minutos (dar tiempo a MH para procesar)
        // 3. FechaUltimaConsulta es null (nunca consultado) o fue hace más de 5 minutos
        var lotesPendientes = await context.Lotes
            .Where(l => l.Estado == "Enviado"
                        && l.FechaEnvio != null
                        && l.FechaEnvio < ahora.Subtract(TiempoMinimoDesdeEnvio)
                        && (l.FechaUltimaConsulta == null
                            || l.FechaUltimaConsulta < ahora.Subtract(TiempoMinimoEntreConsultas)))
            .OrderBy(l => l.FechaEnvio)
            .Take(10) // Limitar a 10 lotes por ciclo para no saturar MH
            .Select(l => l.Id)
            .ToListAsync(ct);

        if (!lotesPendientes.Any()) return;

        _logger.LogInformation("ConsultaEstadosLotes: {Count} lotes pendientes de consulta de estados.", lotesPendientes.Count);

        foreach (var loteId in lotesPendientes)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                _logger.LogInformation("Consultando estados individuales del lote {LoteId}", loteId);
                await loteService.ConsultarEstadosIndividualesAsync(loteId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando estados del lote {LoteId}", loteId);
            }
        }
    }
}
