using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FraFactu.Application.Common;
using FraFactu.Application.Services;
using FraFactu.Application.DTOs.Reportes;
using FraFactu.API.Helpers;

namespace FraFactu.API.Controllers;

/// <summary>
/// Controller para reportes de inventario
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InventarioReportesController : ControllerBase
{
    private readonly IInventarioReporteService _reporteService;

    public InventarioReportesController(IInventarioReporteService reporteService)
    {
        _reporteService = reporteService;
    }

    private int GetEmisorId()
    {
        var emisorIdClaim = User.FindFirst("EmisorId")?.Value;
        if (string.IsNullOrEmpty(emisorIdClaim) || !int.TryParse(emisorIdClaim, out var emisorId))
        {
            throw new UnauthorizedAccessException("EmisorId no encontrado en el token");
        }
        return emisorId;
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
    /// Obtener stock actual por bodega y/o producto con paginación y búsqueda
    /// </summary>
    [HttpGet("stock")]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Contador,Auditor,Cajero")]
    public async Task<ActionResult<PagedResult<StockPorBodegaDto>>> GetStockPorBodega(
        [FromQuery] int? bodegaId = null,
        [FromQuery] int? productoId = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] int? sucursalId = null,
        [FromQuery] int? categoriaId = null,
        [FromQuery] int? marcaId = null,
        [FromQuery] bool soloBajoMinimo = false,
        [FromQuery] bool soloSinStock = false,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDesc = false)
    {
        var emisorId = GetEmisorId();

        // Determinar filtro de sucursal
        // Si el usuario selecciona una sucursal explícitamente, se permite sin restricción de rol
        int? sucursalIdFinal = null;
        List<int>? sucursalIdsFinal = null;

        if (sucursalId.HasValue)
        {
            sucursalIdFinal = sucursalId.Value;
        }
        else
        {
            var rol = ScopeHelper.GetRolFromClaims(User);
            if (ScopeHelper.RequiereRestriccionSucursal(rol) && !ScopeHelper.GetAccesoTodasSucursales(User))
            {
                var userSucursalIds = ScopeHelper.GetSucursalIdsFromClaims(User);
                if (userSucursalIds.Count == 1)
                {
                    sucursalIdFinal = userSucursalIds[0];
                }
                else
                {
                    sucursalIdsFinal = userSucursalIds;
                }
            }
        }

        var resultado = await _reporteService.ObtenerStockPorBodegaAsync(
            emisorId, bodegaId, productoId, search, page, pageSize, sucursalIdFinal, sucursalIdsFinal,
            categoriaId, marcaId, soloBajoMinimo, soloSinStock, sortBy, sortDesc);
        return Ok(resultado);
    }

    /// <summary>
    /// Productos con stock por debajo del mínimo configurado
    /// </summary>
    [HttpGet("stock/bajo-minimo")]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Contador,Auditor")]
    public async Task<ActionResult> GetProductosBajoMinimo(
        [FromQuery] int? sucursalId = null)
    {
        var emisorId = GetEmisorId();
        var (sucursalIdFinal, _, error) = ResolverSucursal(sucursalId);
        if (error != null) return error;

        var resultado = await _reporteService.ObtenerProductosBajoMinimoAsync(emisorId, sucursalIdFinal);
        return Ok(resultado);
    }

    /// <summary>
    /// Productos sin movimientos en los últimos X días
    /// </summary>
    [HttpGet("stock/sin-movimiento")]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Contador,Auditor")]
    public async Task<ActionResult> GetProductosSinMovimiento(
        [FromQuery] int dias = 30,
        [FromQuery] int? sucursalId = null)
    {
        var emisorId = GetEmisorId();
        var (sucursalIdFinal, _, error) = ResolverSucursal(sucursalId);
        if (error != null) return error;

        var resultado = await _reporteService.ObtenerProductosSinMovimientoAsync(emisorId, dias, sucursalIdFinal);
        return Ok(resultado);
    }

    /// <summary>
    /// Movimientos de inventario por período con paginación y búsqueda
    /// </summary>
    [HttpGet("movimientos")]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Contador,Auditor")]
    public async Task<ActionResult<PagedResult<MovimientoInventarioDto>>> ObtenerMovimientos(
        [FromQuery] DateTime desde,
        [FromQuery] DateTime hasta,
        [FromQuery] int? productoId = null,
        [FromQuery] int? bodegaId = null,
        [FromQuery] string? tipoMovimiento = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] int? sucursalId = null,
        [FromQuery] bool ocultarAnulaciones = false,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDesc = false)
    {
        var emisorId = GetEmisorId();

        // Determinar filtro de sucursal
        int? sucursalIdFinal = null;
        List<int>? sucursalIdsFinal = null;
        var rol = ScopeHelper.GetRolFromClaims(User);

        if (ScopeHelper.RequiereRestriccionSucursal(rol) && !ScopeHelper.GetAccesoTodasSucursales(User))
        {
            var userSucursalIds = ScopeHelper.GetSucursalIdsFromClaims(User);
            if (sucursalId.HasValue)
            {
                if (!userSucursalIds.Contains(sucursalId.Value))
                    return StatusCode(403, new { error = "No tiene acceso a esta sucursal" });
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

        var resultado = await _reporteService.ObtenerMovimientosPorPeriodoAsync(
            emisorId, desde, hasta, productoId, bodegaId, tipoMovimiento, search, page, pageSize, sucursalIdFinal, sucursalIdsFinal,
            ocultarAnulaciones, sortBy, sortDesc);
        return Ok(resultado);
    }

    /// <summary>
    /// Kardex de un producto (historial detallado de movimientos) con paginación y búsqueda
    /// </summary>
    [HttpGet("kardex/{productoId}")]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Contador,Auditor")]
    public async Task<ActionResult<KardexProductoDto>> ObtenerKardex(
        int productoId,
        [FromQuery] int? bodegaId = null,
        [FromQuery] DateTime? desde = null,
        [FromQuery] DateTime? hasta = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] int? sucursalId = null,
        [FromQuery] string? tipoMovimiento = null,
        [FromQuery] bool ocultarAnulaciones = false,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDesc = false)
    {
        try
        {
            var emisorId = GetEmisorId();

            // Determinar filtro de sucursal
            int? sucursalIdFinal = null;
            List<int>? sucursalIdsFinal = null;
            var rol = ScopeHelper.GetRolFromClaims(User);

            if (ScopeHelper.RequiereRestriccionSucursal(rol) && !ScopeHelper.GetAccesoTodasSucursales(User))
            {
                var userSucursalIds = ScopeHelper.GetSucursalIdsFromClaims(User);
                if (sucursalId.HasValue)
                {
                    if (!userSucursalIds.Contains(sucursalId.Value))
                        return StatusCode(403, new { error = "No tiene acceso a esta sucursal" });
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

            var resultado = await _reporteService.ObtenerKardexProductoAsync(productoId, emisorId, bodegaId, desde, hasta, search, page, pageSize, sucursalIdFinal, sucursalIdsFinal,
                tipoMovimiento, ocultarAnulaciones, sortBy, sortDesc);
            return Ok(resultado);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Resumen de movimientos agrupados por tipo
    /// </summary>
    [HttpGet("movimientos/resumen-por-tipo")]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Contador,Auditor")]
    public async Task<ActionResult> GetResumenMovimientos(
        [FromQuery] DateTime desde,
        [FromQuery] DateTime hasta,
        [FromQuery] int? sucursalId = null,
        [FromQuery] int? bodegaId = null)
    {
        var emisorId = GetEmisorId();
        var (sucursalIdFinal, _, error) = ResolverSucursal(sucursalId);
        if (error != null) return error;

        var resultado = await _reporteService.ObtenerResumenMovimientosPorTipoAsync(emisorId, desde, hasta, sucursalIdFinal, bodegaId);
        return Ok(resultado);
    }

    /// <summary>
    /// Valoración total del inventario
    /// </summary>
    [HttpGet("valoracion")]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Contador,Auditor")]
    public async Task<ActionResult> GetValoracion(
        [FromQuery] int? bodegaId = null,
        [FromQuery] int? sucursalId = null)
    {
        var emisorId = GetEmisorId();
        var (sucursalIdFinal, _, error) = ResolverSucursal(sucursalId);
        if (error != null) return error;

        var resultado = await _reporteService.ObtenerValoracionInventarioAsync(emisorId, bodegaId, sucursalIdFinal);
        return Ok(resultado);
    }

    /// <summary>
    /// Costo de Mercancía Vendida (CMV) en un período
    /// </summary>
    [HttpGet("cmv")]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Contador,Auditor")]
    public async Task<ActionResult> GetCostoMercanciaVendida(
        [FromQuery] DateTime desde,
        [FromQuery] DateTime hasta,
        [FromQuery] int? sucursalId = null,
        [FromQuery] int? bodegaId = null)
    {
        var emisorId = GetEmisorId();
        var (sucursalIdFinal, _, error) = ResolverSucursal(sucursalId);
        if (error != null) return error;

        var resultado = await _reporteService.ObtenerCostoMercanciaVendidaAsync(
            emisorId, desde, hasta, sucursalIdFinal, bodegaId);
        return Ok(new { desde, hasta, costoMercanciaVendida = resultado, sucursalId = sucursalIdFinal, bodegaId });
    }

    /// <summary>
    /// Análisis de rotación de inventario (clasificación ABC)
    /// </summary>
    [HttpGet("rotacion")]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Contador,Auditor")]
    public async Task<ActionResult> GetRotacion(
        [FromQuery] int meses = 12,
        [FromQuery] int? sucursalId = null,
        [FromQuery] int? bodegaId = null)
    {
        var emisorId = GetEmisorId();
        var (sucursalIdFinal, _, error) = ResolverSucursal(sucursalId);
        if (error != null) return error;

        var resultado = await _reporteService.ObtenerRotacionInventarioAsync(
            emisorId, meses, sucursalIdFinal, bodegaId);
        return Ok(resultado);
    }

    /// <summary>
    /// KPIs principales del inventario (dashboard)
    /// </summary>
    [HttpGet("kpis")]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Contador,Auditor")]
    public async Task<ActionResult> GetKPIs(
        [FromQuery] int? sucursalId = null)
    {
        var emisorId = GetEmisorId();
        var (sucursalIdFinal, _, error) = ResolverSucursal(sucursalId);
        if (error != null) return error;

        var resultado = await _reporteService.ObtenerKPIsAsync(emisorId, sucursalIdFinal);
        return Ok(resultado);
    }
}
