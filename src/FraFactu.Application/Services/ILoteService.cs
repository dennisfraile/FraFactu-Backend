using FraFactu.Application.DTOs.Lotes;
using FraFactu.Application.DTOs.Common;

namespace FraFactu.Application.Services;

/// <summary>
/// Servicio para gestión de lotes de envío de DTEs
/// </summary>
public interface ILoteService
{
    /// <summary>
    /// Crea un nuevo lote con las facturas especificadas
    /// </summary>
    Task<LoteDto> CrearLoteAsync(int emisorId, CrearLoteDto dto);

    /// <summary>
    /// Envía un lote al Ministerio de Hacienda
    /// </summary>
    Task<LoteDto> EnviarLoteAsync(int loteId);

    /// <summary>
    /// Consulta el estado individual de cada DTE en el lote
    /// </summary>
    Task<LoteDto> ConsultarEstadosIndividualesAsync(int loteId);

    /// <summary>
    /// Obtiene un lote por ID con todos sus detalles
    /// </summary>
    Task<LoteDto> ObtenerLoteAsync(int loteId);

    /// <summary>
    /// Obtiene todos los lotes de un emisor paginados
    /// </summary>
    Task<PaginatedResponse<LoteDto>> ObtenerLotesPorEmisorAsync(PaginatedRequest request, int emisorId, List<int>? sucursalIds = null, bool? esContingencia = null, int? usuarioId = null, string? estado = null, string? ambiente = null, DateTime? fechaDesde = null, DateTime? fechaHasta = null, string? search = null);

    /// <summary>
    /// Crea y envía un lote manual con las facturas especificadas
    /// </summary>
    Task<LoteDto> EnviarLoteManualAsync(int emisorId, EnviarLoteManualDto dto);
}
