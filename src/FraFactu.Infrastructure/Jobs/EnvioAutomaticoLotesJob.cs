using FraFactu.Application.Interfaces;
using FraFactu.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace FraFactu.Infrastructure.Jobs;

/// <summary>
/// Servicio para ejecutar envío automático de facturas pendientes por lotes
/// </summary>
public class EnvioAutomaticoLotesJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EnvioAutomaticoLotesJob> _logger;

    public EnvioAutomaticoLotesJob(
        IServiceProvider serviceProvider,
        ILogger<EnvioAutomaticoLotesJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Ejecuta el envío automático de facturas pendientes para todos los emisores habilitados
    /// </summary>
    public async Task EjecutarEnvioAutomaticoAsync()
    {
        _logger.LogInformation("[JOB-ENVIO-AUTO] ========================================");
        _logger.LogInformation("[JOB-ENVIO-AUTO] Iniciando envío automático de lotes");
        _logger.LogInformation("[JOB-ENVIO-AUTO] Hora: {Hora}", DateTime.UtcNow);
        _logger.LogInformation("[JOB-ENVIO-AUTO] ========================================");

        using var scope = _serviceProvider.CreateScope();
        var facturaService = scope.ServiceProvider.GetRequiredService<IFacturaService>();
        var configService = scope.ServiceProvider.GetRequiredService<IConfiguracionEnvioLoteService>();

        try
        {
            // Obtener todos los emisores con envío automático habilitado
            var context = scope.ServiceProvider.GetRequiredService<FraFactu.Infrastructure.Persistence.ApplicationDbContext>();

            var configuraciones = await context.ConfiguracionEnvioLotes
                .Where(c => c.EnvioAutomaticoHabilitado && c.Activo)
                .ToListAsync();

            _logger.LogInformation("[JOB-ENVIO-AUTO] Encontradas {Count} configuraciones habilitadas", configuraciones.Count);

            foreach (var config in configuraciones)
            {
                try
                {
                    await ProcesarEmisorAsync(config, facturaService);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[JOB-ENVIO-AUTO] Error procesando emisor {EmisorId}", config.EmisorId);
                    // Continuar con el siguiente emisor
                }
            }

            _logger.LogInformation("[JOB-ENVIO-AUTO] Envío automático completado exitosamente");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[JOB-ENVIO-AUTO] Error crítico en el job de envío automático");
            throw;
        }
    }

    private async Task ProcesarEmisorAsync(ConfiguracionEnvioLote config, IFacturaService facturaService)
    {
        _logger.LogInformation("[JOB-ENVIO-AUTO] Procesando emisor {EmisorId}", config.EmisorId);

        // Obtener facturas pendientes del emisor (sin límite de paginación para el job)
        var facturasPendientes = await facturaService.ObtenerFacturasPendientesAsync(
            config.EmisorId,
            fechaDesde: null,
            fechaHasta: null,
            pageNumber: 1,
            pageSize: 1000 // Procesar hasta 1000 facturas por vez
        );

        if (facturasPendientes.TotalCount == 0)
        {
            _logger.LogInformation("[JOB-ENVIO-AUTO] Emisor {EmisorId}: No hay facturas pendientes", config.EmisorId);
            return;
        }

        _logger.LogInformation("[JOB-ENVIO-AUTO] Emisor {EmisorId}: {Count} facturas pendientes encontradas",
            config.EmisorId, facturasPendientes.TotalCount);

        int exitosas = 0;
        int fallidas = 0;

        foreach (var factura in facturasPendientes.Items)
        {
            try
            {
                _logger.LogInformation("[JOB-ENVIO-AUTO] Enviando factura {NumeroControl} (ID: {FacturaId})",
                    factura.NumeroControl, factura.Id);

                await facturaService.EnviarFacturaIndividualAsync(factura.Id);
                exitosas++;

                _logger.LogInformation("[JOB-ENVIO-AUTO] ✅ Factura {NumeroControl} enviada exitosamente",
                    factura.NumeroControl);
            }
            catch (Exception ex)
            {
                fallidas++;
                _logger.LogError(ex, "[JOB-ENVIO-AUTO] ❌ Error enviando factura {NumeroControl} (ID: {FacturaId})",
                    factura.NumeroControl, factura.Id);
                // Continuar con la siguiente factura
            }
        }

        _logger.LogInformation("[JOB-ENVIO-AUTO] Emisor {EmisorId}: Resumen - Exitosas: {Exitosas}, Fallidas: {Fallidas}",
            config.EmisorId, exitosas, fallidas);
    }

    /// <summary>
    /// Envía recordatorio por email antes del envío automático (opcional)
    /// </summary>
    public async Task EnviarRecordatorioAsync()
    {
        _logger.LogInformation("[JOB-RECORDATORIO] Iniciando envío de recordatorios");

        using var scope = _serviceProvider.CreateScope();

        try
        {
            var context = scope.ServiceProvider.GetRequiredService<FraFactu.Infrastructure.Persistence.ApplicationDbContext>();

            var configuraciones = await context.ConfiguracionEnvioLotes
                .Where(c => c.EnvioAutomaticoHabilitado && c.EnviarRecordatorio && c.Activo)
                .ToListAsync();

            _logger.LogInformation("[JOB-RECORDATORIO] {Count} emisores configurados para recibir recordatorio",
                configuraciones.Count);

            // TODO: Implementar cuando IEmailService esté disponible
            // Por ahora solo registramos en logs
            foreach (var config in configuraciones)
            {
                _logger.LogInformation("[JOB-RECORDATORIO] Recordatorio pendiente para emisor {EmisorId}. " +
                    "Envío programado en {Minutos} minutos a las {Hora}",
                    config.EmisorId,
                    config.MinutosAnticipacionRecordatorio,
                    config.HoraEnvioAutomatico);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[JOB-RECORDATORIO] Error enviando recordatorios");
        }
    }
}
