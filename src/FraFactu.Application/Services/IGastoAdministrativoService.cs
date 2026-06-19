using FraFactu.Application.DTOs.Compras;
using FraFactu.Application.Common;

namespace FraFactu.Application.Services;

/// <summary>
/// Servicio para gestión de gastos administrativos
/// </summary>
public interface IGastoAdministrativoService
{
    /// <summary>
    /// Obtener gastos por período
    /// </summary>
    Task<PagedResult<GastoAdministrativoDto>> ObtenerPorPeriodoAsync(
        DateTime desde,
        DateTime hasta,
        int? tipoGastoId = null,
        string? centroCosto = null,
        int? sucursalId = null,
        int pagina = 1,
        int tamanoPagina = 10000,
        string? search = null,
        List<int>? sucursalIds = null,
        string? sortBy = null,
        bool sortDesc = false);

    /// <summary>
    /// Obtener total de gastos por período
    /// </summary>
    Task<decimal> ObtenerTotalGastosAsync(
        DateTime desde,
        DateTime hasta,
        int? tipoGastoId = null,
        int? sucursalId = null,
        List<int>? sucursalIds = null);

    /// <summary>
    /// Obtener gastos agrupados por tipo
    /// </summary>
    Task<Dictionary<string, decimal>> ObtenerGastosPorTipoAsync(DateTime desde, DateTime hasta);

    /// <summary>
    /// Obtener gastos agrupados por centro de costo
    /// </summary>
    Task<Dictionary<string, decimal>> ObtenerGastosPorCentroCostoAsync(DateTime desde, DateTime hasta);
}
