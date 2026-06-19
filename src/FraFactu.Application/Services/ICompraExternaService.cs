using FraFactu.Application.DTOs.Compras;
using FraFactu.Application.Common;

namespace FraFactu.Application.Services;

/// <summary>
/// Servicio para gestión de compras externas
/// </summary>
public interface ICompraExternaService
{
    /// <summary>
    /// Crear una nueva compra externa en estado BORRADOR
    /// </summary>
    /// <summary>
    /// Crear una nueva compra externa en estado BORRADOR
    /// </summary>
    Task<CompraExternaDto> CrearAsync(CrearCompraExternaDto dto, int emisorId);

    /// <summary>
    /// Actualizar una compra externa en estado BORRADOR
    /// </summary>
    /// <summary>
    /// Actualizar una compra externa en estado BORRADOR
    /// </summary>
    Task<CompraExternaDto> ActualizarAsync(int id, ActualizarCompraExternaDto dto, int emisorId);

    /// <summary>
    /// Confirmar una compra (BORRADOR → CONFIRMADA) y afectar inventario
    /// </summary>
    /// <summary>
    /// Confirmar una compra (BORRADOR → CONFIRMADA) y afectar inventario
    /// </summary>
    Task<CompraExternaDto> ConfirmarAsync(int id, int emisorId, ConfirmarCompraDto? dto = null);

    /// <summary>
    /// Anular una compra (CONFIRMADA → ANULADA) y revertir movimientos
    /// </summary>
    /// <summary>
    /// Anular una compra (CONFIRMADA → ANULADA) y revertir movimientos
    /// </summary>
    Task<CompraExternaDto> AnularAsync(int id, AnularCompraDto dto, int emisorId);

    /// <summary>
    /// Editar una compra en estado CONFIRMADA (solo origen MANUAL).
    /// Si los items no cambian, solo actualiza cabecera. Si cambian, revierte
    /// y reaplica inventario atómicamente. Notifica corrección a SmartInventory
    /// best-effort. Las compras de origen DTE no son editables.
    /// </summary>
    Task<CompraExternaDto> EditarConfirmadaAsync(int id, ActualizarCompraExternaDto dto, int emisorId);

    /// <summary>
    /// Obtener una compra por ID
    /// </summary>
    /// <summary>
    /// Obtener una compra por ID
    /// </summary>
    Task<CompraExternaDto> ObtenerPorIdAsync(int id, int emisorId);

    /// <summary>
    /// Listar compras con filtros
    /// </summary>
    /// <summary>
    /// Listar compras con filtros
    /// </summary>
    Task<PagedResult<CompraExternaDto>> ListarAsync(
        int emisorId,
        int pagina = 1,
        int tamanoPagina = 20,
        int? proveedorId = null,
        int? bodegaId = null,
        int? sucursalId = null,
        string? estado = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        DateTime? fechaRegistroDesde = null,
        DateTime? fechaRegistroHasta = null,
        string? search = null,
        List<int>? sucursalIds = null,
        string? sortBy = null,
        bool sortDesc = true);

    /// <summary>
    /// Obtener compras por proveedor
    /// </summary>
    /// <summary>
    /// Obtener compras por proveedor
    /// </summary>
    Task<List<CompraExternaDto>> ObtenerPorProveedorAsync(int proveedorId, int emisorId, int limite = 10);

    /// <summary>
    /// Obtener totales de compra por período
    /// </summary>
    /// <summary>
    /// Obtener totales de compra por período
    /// </summary>
    Task<decimal> ObtenerTotalComprasAsync(DateTime desde, DateTime hasta, int emisorId, int? proveedorId = null, int? sucursalId = null);
}
