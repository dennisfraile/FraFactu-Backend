using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FraFactu.Application.Interfaces;

namespace FraFactu.API.Controllers;

/// <summary>
/// Controller para reportes de compras (Libro de Compras, Resumen, Cruce DTEs)
/// </summary>
[ApiController]
[Route("api/reportes/compras")]
[Authorize(Roles = "EmisorAdmin,GerenteSucursal,Contador,Auditor")]
public class ReportesComprasController : ControllerBase
{
    private readonly IReporteComprasService _reporteService;

    public ReportesComprasController(IReporteComprasService reporteService)
    {
        _reporteService = reporteService;
    }

    private int GetEmisorId()
    {
        var emisorIdClaim = User.FindFirst("EmisorId")?.Value;
        if (string.IsNullOrEmpty(emisorIdClaim) || !int.TryParse(emisorIdClaim, out var emisorId))
            throw new UnauthorizedAccessException("EmisorId no encontrado en el token");
        return emisorId;
    }

    /// <summary>
    /// Genera el Libro de Compras en Excel
    /// </summary>
    [HttpGet("libro")]
    public async Task<IActionResult> GenerarLibroCompras(
        [FromQuery] DateTime desde,
        [FromQuery] DateTime hasta)
    {
        try
        {
            var emisorId = GetEmisorId();
            var bytes = await _reporteService.GenerarLibroComprasExcelAsync(emisorId, desde, hasta);
            var fileName = $"LibroCompras_{desde:yyyyMMdd}_{hasta:yyyyMMdd}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Genera el Libro de Compras en CSV
    /// </summary>
    [HttpGet("libro/csv")]
    public async Task<IActionResult> GenerarLibroComprasCsv(
        [FromQuery] DateTime desde,
        [FromQuery] DateTime hasta)
    {
        try
        {
            var emisorId = GetEmisorId();
            var bytes = await _reporteService.GenerarLibroComprasCsvAsync(emisorId, desde, hasta);
            var fileName = $"LibroCompras_{desde:yyyyMMdd}_{hasta:yyyyMMdd}.csv";
            return File(bytes, "text/csv", fileName);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Genera Resumen Mensual de Compras en Excel
    /// </summary>
    [HttpGet("resumen-mensual")]
    public async Task<IActionResult> GenerarResumenMensual(
        [FromQuery] int mes,
        [FromQuery] int anio)
    {
        try
        {
            var emisorId = GetEmisorId();
            var bytes = await _reporteService.GenerarResumenMensualExcelAsync(emisorId, mes, anio);
            var fileName = $"ResumenCompras_{anio}_{mes:D2}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Genera Detalle de Compras por Proveedor en Excel
    /// </summary>
    [HttpGet("por-proveedor")]
    public async Task<IActionResult> GenerarDetallePorProveedor(
        [FromQuery] DateTime desde,
        [FromQuery] DateTime hasta,
        [FromQuery] int? proveedorId = null)
    {
        try
        {
            var emisorId = GetEmisorId();
            var bytes = await _reporteService.GenerarDetallePorProveedorExcelAsync(emisorId, desde, hasta, proveedorId);
            var fileName = $"ComprasPorProveedor_{desde:yyyyMMdd}_{hasta:yyyyMMdd}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Genera Cruce de Compras vs DTEs Recibidos en Excel
    /// </summary>
    [HttpGet("cruce-dtes")]
    public async Task<IActionResult> GenerarCruceDtes(
        [FromQuery] DateTime desde,
        [FromQuery] DateTime hasta)
    {
        try
        {
            var emisorId = GetEmisorId();
            var bytes = await _reporteService.GenerarCruceComprasVsDtesExcelAsync(emisorId, desde, hasta);
            var fileName = $"CruceComprasDTEs_{desde:yyyyMMdd}_{hasta:yyyyMMdd}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
