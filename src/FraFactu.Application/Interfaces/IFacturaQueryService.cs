using FraFactu.Application.DTOs.Common;
using FraFactu.Application.DTOs.Facturas;

namespace FraFactu.Application.Interfaces
{
    /// <summary>
    /// Lectura/consulta de Facturas Electrónicas (extraído de FacturaService, Fase 1).
    /// </summary>
    public interface IFacturaQueryService
    {
        /// <summary>
        /// Obtener factura por ID
        /// </summary>
        /// <param name="id">ID de la factura</param>
        /// <param name="emisorId">ID del emisor (multi-tenancy)</param>
        /// <returns>Factura o null si no existe</returns>
        Task<FacturaElectronicaResponseDto?> GetByIdAsync(int id, int emisorId);

        /// <summary>
        /// Obtener factura por Código de Generación
        /// </summary>
        /// <param name="codigoGeneracion">GUID de la factura</param>
        /// <param name="emisorId">ID del emisor</param>
        /// <returns>Factura o null</returns>
        Task<FacturaElectronicaResponseDto?> GetByCodigoGeneracionAsync(string codigoGeneracion, int emisorId);

        /// <summary>
        /// Listar facturas con paginación
        /// </summary>
        /// <param name="request">Parámetros de paginación</param>
        /// <param name="emisorId">ID del emisor</param>
        /// <param name="sucursalId">ID de sucursal (opcional, para filtrado por sucursal)</param>
        /// <returns>Lista paginada de facturas</returns>
        Task<PaginatedResponse<FacturaListDto>> GetAllAsync(PaginatedRequest request, int emisorId, int? sucursalId = null, string? search = null, DateTime? fechaDesde = null, DateTime? fechaHasta = null, int? vendedorId = null, int? usuarioId = null, string? tipoDte = null, string? estadoHacienda = null, List<int>? sucursalIds = null, int? catTipoTransmisionId = null, string? ambiente = null);

        /// <summary>
        /// Buscar facturas por criterios
        /// </summary>
        /// <param name="searchTerm">Término de búsqueda (número control, código, receptor)</param>
        /// <param name="emisorId">ID del emisor</param>
        /// <returns>Lista de facturas que coinciden</returns>
        Task<List<FacturaListDto>> SearchAsync(string searchTerm, int emisorId, string? ambiente = null);

        /// <summary>
        /// Obtiene facturas con EstadoHacienda = 'PENDIENTE_ENVIO'
        /// Soporta filtros por fecha
        /// </summary>
        Task<PaginatedResponse<FacturaListDto>> ObtenerFacturasPendientesAsync(
            int emisorId,
            DateTime? fechaDesde = null,
            DateTime? fechaHasta = null,
            int pageNumber = 1,
            int pageSize = 10,
            int? sucursalId = null,
            string? search = null,
            string? ambiente = null
        );

        /// <summary>
        /// Obtiene facturas diferidas con EstadoHacienda = 'PENDIENTE_LOTE'
        /// Soporta filtros por sucursal y fecha
        /// </summary>
        Task<PaginatedResponse<FacturaListDto>> ObtenerFacturasDiferidasAsync(
            PaginatedRequest request,
            int emisorId,
            List<int>? sucursalIds = null,
            DateTime? fechaDesde = null,
            DateTime? fechaHasta = null,
            int? usuarioId = null,
            string? ambiente = null
        );

        /// <summary>
        /// Calcula el tiempo restante antes de que venza el período de envío
        /// (24h normal, 30min último día del mes)
        /// </summary>
        Task<TiempoRestanteDto> ObtenerTiempoRestanteAsync(int facturaId);

        /// <summary>
        /// Busca DTEs procesados (03, 07) para referenciar en una Nota de Crédito
        /// </summary>
        Task<List<BuscarParaNcResultDto>> BuscarParaNotaCreditoAsync(string searchTerm, int emisorId);

        /// <summary>
        /// Obtiene detalle completo de un DTE para pre-cargar en una Nota de Crédito
        /// </summary>
        Task<DetalleParaNcDto?> ObtenerDetalleParaNotaCreditoAsync(int facturaId, int emisorId);

        /// <summary>
        /// Busca DTEs procesados (03, 07) para referenciar en una Nota de Débito
        /// </summary>
        Task<List<BuscarParaNcResultDto>> BuscarParaNotaDebitoAsync(string searchTerm, int emisorId);

        /// <summary>
        /// Obtiene detalle completo de un DTE para referenciar en una Nota de Débito
        /// </summary>
        Task<DetalleParaNcDto?> ObtenerDetalleParaNotaDebitoAsync(int facturaId, int emisorId);
    }
}
