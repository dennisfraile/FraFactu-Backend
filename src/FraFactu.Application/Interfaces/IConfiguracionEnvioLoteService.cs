using FraFactu.Application.DTOs.Configuracion;

namespace FraFactu.Application.Interfaces;

/// <summary>
/// Servicio para gestión de configuración de envío automático de lotes
/// </summary>
public interface IConfiguracionEnvioLoteService
{
    /// <summary>
    /// Obtiene la configuración de envío para un emisor
    /// </summary>
    Task<ConfiguracionEnvioLoteDto> ObtenerConfiguracionAsync(int emisorId);

    /// <summary>
    /// Actualiza la configuración de envío para un emisor
    /// </summary>
    Task<ConfiguracionEnvioLoteDto> ActualizarConfiguracionAsync(
        int emisorId,
        ActualizarConfiguracionEnvioDto dto
    );

    /// <summary>
    /// Obtiene o crea la configuración para un emisor (con valores por defecto)
    /// </summary>
    Task<ConfiguracionEnvioLoteDto> ObtenerOCrearConfiguracionAsync(int emisorId);
}
