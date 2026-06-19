using FraFactu.Application.DTOs.Retorno;

namespace FraFactu.Application.Interfaces;

/// <summary>
/// Servicio de Eventos de Retorno (tipoEvento "18", esquema fe-eret-v1.json).
/// </summary>
public interface IEventoRetornoService
{
    /// <summary>
    /// Crea un evento de retorno, calcula el resumen, persiste y lo transmite al MH.
    /// </summary>
    Task<EventoRetornoResultDto> CrearEventoAsync(CrearEventoRetornoDto dto, int emisorId);

    /// <summary>Obtiene un evento por ID validando que pertenezca al emisor.</summary>
    Task<EventoRetornoResultDto> ObtenerPorIdAsync(int id, int emisorId);

    /// <summary>Genera el JSON del evento según el esquema del MH.</summary>
    Task<string> GenerarJsonEventoAsync(int id, int emisorId);
}
