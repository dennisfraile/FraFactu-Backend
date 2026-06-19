using FraFactu.Application.DTOs.Common;
using FraFactu.Application.DTOs.Contingencia;

namespace FraFactu.Application.Interfaces;

/// <summary>
/// Interface para el servicio de Eventos de Contingencia
/// </summary>
public interface IEventoContingenciaService
{
    /// <summary>
    /// Crea un nuevo evento de contingencia
    /// </summary>
    /// <param name="dto">Datos del evento</param>
    /// <param name="emisorId">ID del emisor</param>
    /// <returns>DTO del evento creado</returns>
    Task<EventoContingenciaDto> CrearEventoAsync(CrearEventoContingenciaDto dto, int emisorId);

    /// <summary>
    /// Obtiene un evento por su ID
    /// </summary>
    /// <param name="id">ID del evento</param>
    /// <param name="emisorId">ID del emisor (para multi-tenancy)</param>
    /// <returns>DTO del evento</returns>
    Task<EventoContingenciaDto> ObtenerPorIdAsync(int id, int emisorId);

    /// <summary>
    /// Lista todos los eventos de un emisor
    /// </summary>
    /// <param name="emisorId">ID del emisor</param>
    /// <returns>Lista de eventos</returns>
    Task<PaginatedResponse<EventoContingenciaDto>> ListarPorEmisorAsync(
        PaginatedRequest request, int emisorId, List<int>? sucursalIds = null, int? usuarioId = null,
        string? search = null, int? tipoContingencia = null, string? estadoHacienda = null,
        DateTime? fechaDesde = null, DateTime? fechaHasta = null);

    /// <summary>
    /// Genera el JSON del evento para transmisión a MH
    /// </summary>
    /// <param name="eventoId">ID del evento</param>
    /// <param name="emisorId">ID del emisor</param>
    /// <returns>JSON formateado según schema v3</returns>
    Task<string> GenerarJsonEventoAsync(int eventoId, int emisorId);

    /// <summary>
    /// Obtiene o crea un evento de contingencia automático (tipo 1: MH no disponible)
    /// </summary>
    /// <param name="emisorId">ID del emisor</param>
    /// <param name="tipoContingencia">Tipo de contingencia (CAT-005)</param>
    /// <param name="motivo">Motivo descriptivo del fallo</param>
    /// <returns>ID del evento de contingencia</returns>
    Task<int> ObtenerOCrearEventoContingenciaAutomaticoAsync(int emisorId, int tipoContingencia, string motivo);

    /// <summary>
    /// Marca un DTE como diferido por contingencia manual (tipos 2-5)
    /// </summary>
    Task MarcarDteDiferidoAsync(MarcarDteDiferidoRequestDto request, int emisorId);

    /// <summary>
    /// Obtiene el tiempo restante del plazo de 24 horas para un evento de contingencia
    /// </summary>
    Task<TiempoRestanteContingenciaDto> ObtenerTiempoRestanteAsync(int eventoId, int emisorId);
}
