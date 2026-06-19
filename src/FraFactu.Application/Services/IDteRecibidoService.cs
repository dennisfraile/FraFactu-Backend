using FraFactu.Application.DTOs.DtesRecibidos;

namespace FraFactu.Application.Services;

/// <summary>
/// Servicio para gestionar DTEs recibidos
/// </summary>
public interface IDteRecibidoService
{
    /// <summary>
    /// Lista DTEs recibidos con filtros y paginación
    /// </summary>
    Task<(List<DteRecibidoResumenDto> items, int total)> ListarAsync(
        int emisorId,
        int pagina = 1,
        int tamanoPagina = 20,
        string? estado = null,
        string? tipoDte = null,
        string? emisorNit = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        string? search = null,
        string? sortBy = null,
        bool sortDesc = false);

    /// <summary>
    /// Obtiene un DTE por ID con JSON completo
    /// </summary>
    Task<DteRecibidoDto> ObtenerPorIdAsync(int id, int emisorId);

    /// <summary>
    /// Descarta un DTE pendiente
    /// </summary>
    Task DescartarAsync(int id, DescartarDteDto dto, int emisorId);

    /// <summary>
    /// Obtiene estadísticas de DTEs recibidos. Acepta los mismos filtros
    /// opcionales que <see cref="ListarAsync"/> para que las tarjetas de la UI
    /// queden sincronizadas con el listado. Si se omiten, devuelve totales
    /// globales del emisor (compatibilidad con clientes anteriores).
    /// </summary>
    Task<DteRecibidoEstadisticasDto> ObtenerEstadisticasAsync(
        int emisorId,
        string? estado = null,
        string? tipoDte = null,
        string? emisorNit = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        string? search = null);

    /// <summary>
    /// Crea una compra externa pre-llenada desde un DTE recibido. Flujo legacy:
    /// todos los items del DTE se registran como GastoAdministrativo (no afectan
    /// stock). Para clasificar correctamente productos vs gastos use
    /// <see cref="MapearYCrearCompraAsync"/>.
    /// </summary>
    Task<int> CrearCompraDesdeAsync(int dteRecibidoId, int emisorId, int sucursalId);

    /// <summary>
    /// Vincula un DTE a una compra existente
    /// </summary>
    Task VincularACompraAsync(int dteRecibidoId, int compraId, int emisorId);

    /// <summary>
    /// F1 (Plan inventario desde DTE): crea la compra a partir del wizard de
    /// mapeo. Los items marcados como PRODUCTO_EXISTENTE / PRODUCTO_NUEVO
    /// quedan como <c>CompraExternaDetalle</c> con <c>EsParaInventario=true</c>
    /// y, al confirmar la compra, generaran movimientos de stock. Los items
    /// marcados como GASTO se registran como <c>GastoAdministrativo</c>.
    /// PRODUCTO_NUEVO crea el <c>ProductoServicio</c> dentro de la misma
    /// transaccion.
    /// </summary>
    /// <exception cref="KeyNotFoundException">DTE, sucursal, producto o bodega no encontrados (o cross-tenant).</exception>
    /// <exception cref="InvalidOperationException">Estado de DTE invalido, codigo de producto duplicado, o totales que no cuadran (tolerancia 0.01).</exception>
    Task<MapearDteCompraResponseDto> MapearYCrearCompraAsync(
        int dteRecibidoId, MapearDteCompraDto dto, int emisorId, int? cargadoPorUsuarioId = null);

    /// <summary>
    /// Carga manual (masiva) de archivos JSON/JWT de CCF. Por cada archivo
    /// delega en el pipeline único de ingesta y devuelve un reporte detallado.
    /// </summary>
    /// <param name="emisorId">Emisor receptor de los DTEs cargados.</param>
    /// <param name="archivos">Archivos a procesar (nombre + contenido).</param>
    /// <param name="cargadoPorUsuarioId">Usuario que sube los archivos (opcional, para auditoría).</param>
    Task<CargaMasivaDtesResponseDto> CargarDtesManualmenteAsync(
        int emisorId,
        IEnumerable<ArchivoAIngestar> archivos,
        int? cargadoPorUsuarioId = null,
        CancellationToken ct = default);
}
