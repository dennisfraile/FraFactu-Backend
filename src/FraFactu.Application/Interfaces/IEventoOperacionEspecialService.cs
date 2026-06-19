using FraFactu.Application.DTOs.OperacionesEspeciales;

namespace FraFactu.Application.Interfaces;

/// <summary>
/// Servicio de Eventos de Operaciones Especiales (tipoEvento "17", esquema fe-eop-v1.json).
/// </summary>
public interface IEventoOperacionEspecialService
{
    /// <summary>
    /// Crea un evento de operaciones especiales, calcula el resumen, persiste y lo transmite al MH.
    /// </summary>
    Task<EventoOperacionEspecialResultDto> CrearEventoAsync(CrearEventoOperacionEspecialDto dto, int emisorId);

    /// <summary>Obtiene un evento por ID validando que pertenezca al emisor.</summary>
    Task<EventoOperacionEspecialResultDto> ObtenerPorIdAsync(int id, int emisorId);

    /// <summary>Genera el JSON del evento según el esquema del MH.</summary>
    Task<string> GenerarJsonEventoAsync(int id, int emisorId);
}
