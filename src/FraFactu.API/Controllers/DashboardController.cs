using Microsoft.AspNetCore.Mvc;
using FraFactu.Application.Common;
using FraFactu.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using FraFactu.API.Helpers;

namespace FraFactu.API.Controllers;

/// <summary>
/// Controller para Dashboard y Analytics
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin,EmisorAdmin,GerenteSucursal,Contador,Cajero,Auditor")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
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
    /// Obtener KPIs principales del dashboard
    /// </summary>
    [HttpGet("kpis")]
    public async Task<ActionResult<DashboardKPIs>> ObtenerKPIs(
        [FromQuery] DateTime? fechaInicio,
        [FromQuery] DateTime? fechaFin,
        [FromQuery] int? sucursalId,
        [FromQuery] DateTime? fechaAnteriorInicio = null,
        [FromQuery] DateTime? fechaAnteriorFin = null,
        [FromQuery] string? ambiente = null)
    {
        var emisorIdClaim = User.FindFirst("EmisorId");
        if (emisorIdClaim == null || !int.TryParse(emisorIdClaim.Value, out int emisorId))
            return Unauthorized("No se pudo identificar al emisor");

        var (sucursalIdFinal, sucursalIdsFinal, error) = ResolverSucursal(sucursalId);
        if (error != null) return error;

        var kpis = await _dashboardService.ObtenerKPIsAsync(emisorId, fechaInicio, fechaFin, sucursalIdFinal, sucursalIdsFinal, fechaAnteriorInicio, fechaAnteriorFin, ambiente);
        return Ok(kpis);
    }

    /// <summary>
    /// Obtener ventas por día (gráfica de tendencia)
    /// </summary>
    [HttpGet("ventas-por-dia")]
    public async Task<ActionResult<List<VentasPorDia>>> ObtenerVentasPorDia(
        [FromQuery] int dias = 30,
        [FromQuery] int? sucursalId = null,
        [FromQuery] string? ambiente = null)
    {
        var emisorIdClaim = User.FindFirst("EmisorId");
        if (emisorIdClaim == null || !int.TryParse(emisorIdClaim.Value, out int emisorId))
            return Unauthorized("No se pudo identificar al emisor");

        var (sucursalIdFinal, sucursalIdsFinal, error) = ResolverSucursal(sucursalId);
        if (error != null) return error;

        var ventas = await _dashboardService.ObtenerVentasPorDiaAsync(emisorId, dias, sucursalIdFinal, sucursalIdsFinal, ambiente);
        return Ok(ventas);
    }

    /// <summary>
    /// Obtener productos más vendidos (top 10)
    /// </summary>
    [HttpGet("productos-mas-vendidos")]
    public async Task<ActionResult<List<ProductoMasVendido>>> ObtenerProductosMasVendidos(
        [FromQuery] int top = 10,
        [FromQuery] DateTime? fechaInicio = null,
        [FromQuery] DateTime? fechaFin = null,
        [FromQuery] int? sucursalId = null,
        [FromQuery] string? ambiente = null)
    {
        var emisorIdClaim = User.FindFirst("EmisorId");
        if (emisorIdClaim == null || !int.TryParse(emisorIdClaim.Value, out int emisorId))
            return Unauthorized("No se pudo identificar al emisor");

        var (sucursalIdFinal, sucursalIdsFinal, error) = ResolverSucursal(sucursalId);
        if (error != null) return error;

        var productos = await _dashboardService.ObtenerProductosMasVendidosAsync(emisorId, top, fechaInicio, fechaFin, sucursalIdFinal, sucursalIdsFinal, ambiente);
        return Ok(productos);
    }

    /// <summary>
    /// Obtener ventas por categoría
    /// </summary>
    [HttpGet("ventas-por-categoria")]
    public async Task<ActionResult<List<VentasPorCategoria>>> ObtenerVentasPorCategoria(
        [FromQuery] DateTime? fechaInicio,
        [FromQuery] DateTime? fechaFin,
        [FromQuery] int? sucursalId = null,
        [FromQuery] string? ambiente = null)
    {
        var emisorIdClaim = User.FindFirst("EmisorId");
        if (emisorIdClaim == null || !int.TryParse(emisorIdClaim.Value, out int emisorId))
            return Unauthorized("No se pudo identificar al emisor");

        var (sucursalIdFinal, sucursalIdsFinal, error) = ResolverSucursal(sucursalId);
        if (error != null) return error;

        var ventas = await _dashboardService.ObtenerVentasPorCategoriaAsync(emisorId, sucursalIdFinal, fechaInicio, fechaFin, sucursalIdsFinal, ambiente);
        return Ok(ventas);
    }

    /// <summary>
    /// Obtener ventas por vendedor
    /// </summary>
    [HttpGet("ventas-por-vendedor")]
    public async Task<ActionResult<List<VentasPorVendedor>>> ObtenerVentasPorVendedor(
        [FromQuery] DateTime? fechaInicio,
        [FromQuery] DateTime? fechaFin,
        [FromQuery] int? sucursalId = null,
        [FromQuery] string? ambiente = null)
    {
        var emisorIdClaim = User.FindFirst("EmisorId");
        if (emisorIdClaim == null || !int.TryParse(emisorIdClaim.Value, out int emisorId))
            return Unauthorized("No se pudo identificar al emisor");

        var (sucursalIdFinal, sucursalIdsFinal, error) = ResolverSucursal(sucursalId);
        if (error != null) return error;

        var ventas = await _dashboardService.ObtenerVentasPorVendedorAsync(emisorId, fechaInicio, fechaFin, sucursalIdFinal, sucursalIdsFinal, ambiente);
        return Ok(ventas);
    }

    /// <summary>
    /// Detalle de ventas por vendedor con paginación y búsqueda
    /// </summary>
    [HttpGet("ventas-por-vendedor/detalle")]
    public async Task<ActionResult<PagedResult<VentaVendedorDetalleDto>>> ObtenerVentasPorVendedorDetalle(
        [FromQuery] DateTime? fechaInicio,
        [FromQuery] DateTime? fechaFin,
        [FromQuery] int? sucursalId = null,
        [FromQuery] int? vendedorId = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? ambiente = null)
    {
        var emisorIdClaim = User.FindFirst("EmisorId");
        if (emisorIdClaim == null || !int.TryParse(emisorIdClaim.Value, out int emisorId))
            return Unauthorized("No se pudo identificar al emisor");

        var (sucursalIdFinal, sucursalIdsFinal, error) = ResolverSucursal(sucursalId);
        if (error != null) return error;

        var resultado = await _dashboardService.ObtenerVentasPorVendedorDetalleAsync(
            emisorId, fechaInicio, fechaFin, sucursalIdFinal, vendedorId, search, page, pageSize, sucursalIdsFinal, ambiente);
        return Ok(resultado);
    }

    /// <summary>
    /// Detalle de ventas por categoría con paginación y búsqueda
    /// </summary>
    [HttpGet("ventas-por-categoria/detalle")]
    public async Task<ActionResult<PagedResult<VentaCategoriaDetalleDto>>> ObtenerVentasPorCategoriaDetalle(
        [FromQuery] DateTime? fechaInicio,
        [FromQuery] DateTime? fechaFin,
        [FromQuery] int? sucursalId = null,
        [FromQuery] int? categoriaId = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? ambiente = null)
    {
        var emisorIdClaim = User.FindFirst("EmisorId");
        if (emisorIdClaim == null || !int.TryParse(emisorIdClaim.Value, out int emisorId))
            return Unauthorized("No se pudo identificar al emisor");

        var (sucursalIdFinal, sucursalIdsFinal, error) = ResolverSucursal(sucursalId);
        if (error != null) return error;

        var resultado = await _dashboardService.ObtenerVentasPorCategoriaDetalleAsync(
            emisorId, fechaInicio, fechaFin, sucursalIdFinal, categoriaId, search, page, pageSize, sucursalIdsFinal, ambiente);
        return Ok(resultado);
    }

    /// <summary>
    /// Comparativo de ventas por sucursal con paginación y búsqueda
    /// </summary>
    [HttpGet("comparativo-sucursales")]
    public async Task<ActionResult<PagedResult<ComparativoSucursalDto>>> ObtenerComparativoSucursales(
        [FromQuery] DateTime desde,
        [FromQuery] DateTime hasta,
        [FromQuery] int? sucursalId = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? ambiente = null)
    {
        var emisorIdClaim = User.FindFirst("EmisorId");
        if (emisorIdClaim == null || !int.TryParse(emisorIdClaim.Value, out int emisorId))
            return Unauthorized("No se pudo identificar al emisor");

        var (sucursalIdFinal, sucursalIdsFinal, error) = ResolverSucursal(sucursalId);
        if (error != null) return error;

        var resultado = await _dashboardService.ObtenerComparativoSucursalesAsync(
            emisorId, desde, hasta, search, page, pageSize, sucursalIdFinal, sucursalIdsFinal, ambiente);
        return Ok(resultado);
    }

    /// <summary>
    /// Obtener dashboard del cajero (ventas por usuario)
    /// </summary>
    [HttpGet("cajero")]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Contador,Cajero")]
    public async Task<ActionResult<Application.DTOs.Dashboard.DashboardCajeroDto>> ObtenerDashboardCajero(
        [FromQuery] DateTime? fechaDesde,
        [FromQuery] DateTime? fechaHasta,
        [FromQuery] string? ambiente = null)
    {
        // Obtener UsuarioId del token JWT (se guarda como "sub" / ClaimTypes.NameIdentifier)
        var usuarioIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)
                             ?? User.FindFirst(ClaimTypes.NameIdentifier);
        if (usuarioIdClaim == null || !int.TryParse(usuarioIdClaim.Value, out int usuarioId))
        {
            return Unauthorized("No se pudo identificar al usuario");
        }

        var emisorIdClaim = User.FindFirst("EmisorId");
        if (emisorIdClaim == null || !int.TryParse(emisorIdClaim.Value, out int emisorId))
            return Unauthorized("No se pudo identificar al emisor");

        var dashboard = await _dashboardService.ObtenerDashboardCajeroAsync(emisorId, usuarioId, fechaDesde, fechaHasta, ambiente);
        return Ok(dashboard);
    }
}
