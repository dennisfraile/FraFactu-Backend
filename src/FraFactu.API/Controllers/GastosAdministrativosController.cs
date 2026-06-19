using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FraFactu.Application.Services;
using FraFactu.Application.DTOs.Compras;
using FraFactu.Application.Common;
using FraFactu.API.Helpers;

namespace FraFactu.API.Controllers;

/// <summary>
/// Controller para reportes de gastos administrativos
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "EmisorAdmin,GerenteSucursal,Contador,Auditor")]
public class GastosAdministrativosController : ControllerBase
{
    private readonly IGastoAdministrativoService _gastoService;

    public GastosAdministrativosController(IGastoAdministrativoService gastoService)
    {
        _gastoService = gastoService;
    }

    private (int? sucursalIdFinal, List<int>? sucursalIdsFinal, ActionResult? error) ResolverSucursal(int? sucursalId)
    {
        int? sucursalIdFinal = null;
        List<int>? sucursalIdsFinal = null;
        var rol = ScopeHelper.GetRolFromClaims(User);

        if (ScopeHelper.RequiereRestriccionSucursal(rol) && !ScopeHelper.GetAccesoTodasSucursales(User))
        {
            var userSucursalIds = ScopeHelper.GetSucursalIdsFromClaims(User);
            if (sucursalId.HasValue)
            {
                if (!userSucursalIds.Contains(sucursalId.Value))
                    return (null, null, StatusCode(403, new { error = "No tiene acceso a esta sucursal" }));
                sucursalIdFinal = sucursalId.Value;
            }
            else if (userSucursalIds.Count == 1)
            {
                sucursalIdFinal = userSucursalIds[0];
            }
            else
            {
                sucursalIdsFinal = userSucursalIds;
            }
        }
        else if (sucursalId.HasValue)
        {
            sucursalIdFinal = sucursalId.Value;
        }

        return (sucursalIdFinal, sucursalIdsFinal, null);
    }

    /// <summary>
    /// Obtener gastos por período con filtros
    /// </summary>
    [HttpGet("periodo")]
    public async Task<ActionResult<PagedResult<GastoAdministrativoDto>>> ObtenerPorPeriodo(
        [FromQuery] DateTime desde,
        [FromQuery] DateTime hasta,
        [FromQuery] int? tipoGastoId = null,
        [FromQuery] string? centroCosto = null,
        [FromQuery] int? sucursalId = null,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 10000,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDesc = false)
    {
        var (sucursalIdFinal, sucursalIdsFinal, error) = ResolverSucursal(sucursalId);
        if (error != null) return error;

        var resultado = await _gastoService.ObtenerPorPeriodoAsync(
            desde,
            hasta,
            tipoGastoId,
            centroCosto,
            sucursalIdFinal,
            pagina,
            tamanoPagina,
            search,
            sucursalIdsFinal,
            sortBy,
            sortDesc);

        return Ok(resultado);
    }

    /// <summary>
    /// Obtener total de gastos en un período
    /// </summary>
    [HttpGet("totales")]
    public async Task<ActionResult<decimal>> ObtenerTotales(
        [FromQuery] DateTime desde,
        [FromQuery] DateTime hasta,
        [FromQuery] int? tipoGastoId = null)
    {
        var total = await _gastoService.ObtenerTotalGastosAsync(desde, hasta, tipoGastoId);
        return Ok(new { desde, hasta, tipoGastoId, total });
    }

    /// <summary>
    /// Obtener gastos agrupados por tipo
    /// </summary>
    [HttpGet("por-tipo")]
    public async Task<ActionResult<Dictionary<string, decimal>>> ObtenerPorTipo(
        [FromQuery] DateTime desde,
        [FromQuery] DateTime hasta)
    {
        var resultado = await _gastoService.ObtenerGastosPorTipoAsync(desde, hasta);
        return Ok(resultado);
    }

    /// <summary>
    /// Obtener gastos agrupados por centro de costo
    /// </summary>
    [HttpGet("por-centro-costo")]
    public async Task<ActionResult<Dictionary<string, decimal>>> ObtenerPorCentroCosto(
        [FromQuery] DateTime desde,
        [FromQuery] DateTime hasta)
    {
        var resultado = await _gastoService.ObtenerGastosPorCentroCostoAsync(desde, hasta);
        return Ok(resultado);
    }
}
