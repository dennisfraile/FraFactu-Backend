using FraFactu.Application.DTOs.Common;
using FraFactu.Application.DTOs.Facturas;

namespace FraFactu.Application.Interfaces
{
    /// <summary>
    /// Interfaz de servicio para gestión de Facturas Electrónicas
    /// </summary>
    public interface IFacturaService
    {
        /// <summary>
        /// Crear una nueva Factura Electrónica
        /// </summary>
        /// <param name="dto">Datos de la factura</param>
        /// <param name="emisorId">ID del emisor (del JWT)</param>
        /// <returns>Factura creada con código de generación y número de control</returns>
        Task<FacturaElectronicaResponseDto> CreateAsync(CreateFacturaElectronicaDto dto, int emisorId);

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
        /// Generar JSON del DTE según estándar de Hacienda
        /// </summary>
        /// <param name="facturaId">ID de la factura</param>
        /// <param name="emisorId">ID del emisor</param>
        /// <returns>JSON del DTE</returns>
        Task<string> GenerateJsonDteAsync(int facturaId, int emisorId);

        /// <summary>
        /// Anular una factura (cambiar estado)
        /// </summary>
        /// <param name="facturaId">ID de la factura</param>
        /// <param name="emisorId">ID del emisor</param>
        /// <param name="motivo">Motivo de anulación</param>
        /// <returns>True si se anuló correctamente</returns>
        Task<bool> AnularAsync(int facturaId, int emisorId, string motivo);

        /// <summary>
        /// Actualizar estado de Hacienda
        /// </summary>
        /// <param name="facturaId">ID de la factura</param>
        /// <param name="emisorId">ID del emisor</param>
        /// <param name="estado">Nuevo estado</param>
        /// <param name="selloRecepcion">Sello de recepción de Hacienda</param>
        /// <returns>True si se actualizó</returns>
        Task<bool> ActualizarEstadoHaciendaAsync(int facturaId, int emisorId, string estado, string? selloRecepcion = null);

        /// <summary>
        /// Invalidar factura mediante evento oficial de MH
        /// </summary>
        /// <param name="facturaId">ID de la factura a invalidar</param>
        /// <param name="dto">Datos del evento de invalidación</param>
        /// <param name="emisorId">ID del emisor</param>
        /// <returns>Resultado con sello de recepción del evento</returns>
        Task<FraFactu.Application.DTOs.Invalidacion.InvalidacionDto> InvalidarFacturaAsync(int facturaId, FraFactu.Application.DTOs.Invalidacion.AnularFacturaDto dto, int emisorId);

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
        /// Reenvía el correo con el DTE al receptor
        /// </summary>
        Task<bool> ReenviarCorreoAsync(int facturaId);

        /// <summary>
        /// Guarda una factura sin firmar en estado PENDIENTE_ENVIO
        /// (Botón "Guardar como Pendiente")
        /// </summary>
        Task<FacturaElectronicaResponseDto> GuardarComoPendienteAsync(
            int emisorId,
            CreateFacturaElectronicaDto facturaDto
        );

        /// <summary>
        /// Envía una factura individual (firma y transmite a Hacienda)
        /// Usado desde la vista de facturas pendientes
        /// </summary>
        Task<FacturaElectronicaResponseDto> EnviarFacturaIndividualAsync(int facturaId);

        /// <summary>
        /// Reintenta automáticamente la transmisión a Hacienda de las facturas que quedaron
        /// en estado ERROR (regla 13.2.1: reintentos espaciados ≥ 15 min). Reintenta hasta un
        /// tope de intentos; al agotarlo, escala la factura a contingencia (PENDIENTE_LOTE).
        /// Pensado para ejecutarse desde un background service.
        /// </summary>
        Task<ReintentoErrorResultadoDto> ReintentarEnviosEnErrorAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Controla el plazo de 72 h para transmitir DTE diferidos/contingencia desde su
        /// FechaEmision. Marca (con una observación) y alerta las facturas en PENDIENTE_LOTE
        /// que ya vencieron el plazo o están por vencer. No cambia el estado ni bloquea.
        /// Pensado para ejecutarse desde un background service.
        /// </summary>
        Task<ControlPlazo72hResultadoDto> MarcarDiferidosPorVencer72hAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Detecta las contingencias en curso (por emisor) que persisten más de 3 días consecutivos
        /// y que, por tanto, obligan a presentar el "Informe Técnico de Contingencia" a Hacienda antes
        /// de transmitir el Evento de Contingencia (regla 13.2.1.1). Solo detecta y alerta (log/telemetría);
        /// el informe se presenta por los medios que disponga la Administración Tributaria, fuera de la API.
        /// </summary>
        Task<InformeTecnicoContingenciaResultadoDto> DetectarContingenciasParaInformeTecnicoAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Descarta una factura rechazada/error, revierte stock y elimina el registro
        /// </summary>
        Task DescartarFacturaRechazadaAsync(int facturaId, int emisorId);

        /// <summary>
        /// Regenera NumeroControl y CodigoGeneracion si el rechazo MH es por NumeroControl duplicado (código 004).
        /// Retorna true si se regeneró, false si el error no aplica.
        /// </summary>
        Task<bool> RegenerarSiNumeroControlDuplicadoAsync(int facturaId, string? codigoMsg, string? descripcionMsg);

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
