using AutoMapper;
using FraFactu.Application.DTOs.Configuracion;
using FraFactu.Application.Interfaces;
using FraFactu.Domain.Entities;
using FraFactu.Infrastructure.Persistence;
using FraFactu.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FraFactu.Infrastructure.Services;

/// <summary>
/// Servicio para gestión de configuración de envío automático de lotes
/// </summary>
public class ConfiguracionEnvioLoteService : IConfiguracionEnvioLoteService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<ConfiguracionEnvioLoteService> _logger;

    public ConfiguracionEnvioLoteService(
        ApplicationDbContext context,
        IMapper mapper,
        ILogger<ConfiguracionEnvioLoteService> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ConfiguracionEnvioLoteDto> ObtenerConfiguracionAsync(int emisorId)
    {
        var config = await _context.ConfiguracionEnvioLotes
            .FirstOrDefaultAsync(c => c.EmisorId == emisorId);

        if (config == null)
            throw new InvalidOperationException("Configuración no encontrada para el emisor");

        return MapToDto(config);
    }

    public async Task<ConfiguracionEnvioLoteDto> ActualizarConfiguracionAsync(
        int emisorId,
        ActualizarConfiguracionEnvioDto dto)
    {
        var config = await _context.ConfiguracionEnvioLotes
            .FirstOrDefaultAsync(c => c.EmisorId == emisorId);

        if (config == null)
        {
            // Crear nueva configuración si no existe
            config = new ConfiguracionEnvioLote
            {
                EmisorId = emisorId,
                FechaCreacion = DateTime.UtcNow,
                Activo = true
            };
            _context.ConfiguracionEnvioLotes.Add(config);
        }

        // Parsear hora (formato "HH:mm")
        if (TimeSpan.TryParse(dto.HoraEnvioAutomatico, out TimeSpan nuevaHora))
        {
            config.HoraEnvioAutomatico = nuevaHora;
        }

        config.EnvioAutomaticoHabilitado = dto.EnvioAutomaticoHabilitado;
        config.EnviarRecordatorio = dto.EnviarRecordatorio;
        config.MinutosAnticipacionRecordatorio = dto.MinutosAnticipacionRecordatorio;

        await _context.SaveChangesAsync();

        _logger.LogInformation("[CONFIG-LOTE] Configuración actualizada para emisor {EmisorId}. Envío automático: {Habilitado}, Hora: {Hora}",
            emisorId, config.EnvioAutomaticoHabilitado, config.HoraEnvioAutomatico);

        return MapToDto(config);
    }

    public async Task<ConfiguracionEnvioLoteDto> ObtenerOCrearConfiguracionAsync(int emisorId)
    {
        var config = await _context.ConfiguracionEnvioLotes
            .FirstOrDefaultAsync(c => c.EmisorId == emisorId);

        if (config == null)
        {
            // Crear configuración con valores por defecto
            config = new ConfiguracionEnvioLote
            {
                EmisorId = emisorId,
                HoraEnvioAutomatico = new TimeSpan(17, 0, 0), // 5 PM por defecto
                EnvioAutomaticoHabilitado = true,
                ZonaHoraria = "America/El_Salvador",
                EnviarRecordatorio = true,
                MinutosAnticipacionRecordatorio = 60,
                FechaCreacion = DateTime.UtcNow,
                Activo = true
            };

            _context.ConfiguracionEnvioLotes.Add(config);
            await _context.SaveChangesAsync();

            _logger.LogInformation("[CONFIG-LOTE] Configuración creada con valores por defecto para emisor {EmisorId}", emisorId);
        }

        return MapToDto(config);
    }

    private ConfiguracionEnvioLoteDto MapToDto(ConfiguracionEnvioLote config)
    {
        return new ConfiguracionEnvioLoteDto
        {
            Id = config.Id,
            EmisorId = config.EmisorId,
            HoraEnvioAutomatico = config.HoraEnvioAutomatico.ToString(@"hh\:mm"),
            EnvioAutomaticoHabilitado = config.EnvioAutomaticoHabilitado,
            ZonaHoraria = config.ZonaHoraria,
            EnviarRecordatorio = config.EnviarRecordatorio,
            MinutosAnticipacionRecordatorio = config.MinutosAnticipacionRecordatorio
        };
    }
}
