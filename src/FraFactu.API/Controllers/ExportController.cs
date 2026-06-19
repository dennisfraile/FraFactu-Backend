using Microsoft.AspNetCore.Mvc;
using FraFactu.Application.Interfaces;
using FraFactu.API.Helpers;
using Microsoft.AspNetCore.Authorization;

namespace FraFactu.API.Controllers;

/// <summary>
/// Controller para exportación de datos
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "EmisorAdmin,GerenteSucursal,Contador,Auditor")]
public class ExportController : ControllerBase
{
    private readonly IExportService _exportService;

    public ExportController(IExportService exportService)
    {
        _exportService = exportService;
    }

    /// <summary>
    /// Exportar facturas a Excel
    /// </summary>
    [HttpGet("facturas/excel")]
    public async Task<IActionResult> ExportarFacturasExcel(
        [FromQuery] DateTime? fechaInicio,
        [FromQuery] DateTime? fechaFin)
    {
        var excelBytes = await _exportService.ExportarFacturasAExcelAsync(fechaInicio, fechaFin);

        var fileName = $"Facturas_{DateTime.Now:yyyyMMdd}.xlsx";
        return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    /// <summary>
    /// Exportar inventario a Excel
    /// </summary>
    [HttpGet("inventario/excel")]
    public async Task<IActionResult> ExportarInventarioExcel()
    {
        var excelBytes = await _exportService.ExportarInventarioAExcelAsync();

        var fileName = $"Inventario_{DateTime.Now:yyyyMMdd}.xlsx";
        return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    /// <summary>
    /// Exportar ventas a Excel
    /// </summary>
    [HttpGet("ventas/excel")]
    public async Task<IActionResult> ExportarVentasExcel(
        [FromQuery] DateTime fechaInicio,
        [FromQuery] DateTime fechaFin)
    {
        var excelBytes = await _exportService.ExportarVentasAExcelAsync(fechaInicio, fechaFin);

        var fileName = $"Ventas_{fechaInicio:yyyyMMdd}_{fechaFin:yyyyMMdd}.xlsx";
        return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    /// <summary>
    /// Descargar plantilla Excel para carga masiva de productos
    /// </summary>
    [HttpGet("plantilla/productos")]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal")]
    public async Task<IActionResult> DescargarPlantillaProductos()
    {
        var emisorId = GetEmisorId();
        var excelBytes = await _exportService.GenerarPlantillaProductosExcelAsync(emisorId);

        var fileName = $"Plantilla_Productos_{DateTime.Now:yyyyMMdd}.xlsx";
        return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    /// <summary>
    /// Descargar plantilla Excel para carga masiva de servicios
    /// </summary>
    [HttpGet("plantilla/servicios")]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal")]
    public async Task<IActionResult> DescargarPlantillaServicios()
    {
        var emisorId = GetEmisorId();
        var rol = ScopeHelper.GetRolFromClaims(User);
        List<int>? sucursalIds = null;
        if (ScopeHelper.RequiereRestriccionSucursal(rol) && !ScopeHelper.GetAccesoTodasSucursales(User))
            sucursalIds = ScopeHelper.GetSucursalIdsFromClaims(User);

        var excelBytes = await _exportService.GenerarPlantillaServiciosExcelAsync(emisorId, sucursalIds);

        var fileName = $"Plantilla_Servicios_{DateTime.Now:yyyyMMdd}.xlsx";
        return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    private int GetEmisorId()
    {
        var emisorIdClaim = User.FindFirst("EmisorId")?.Value;
        if (string.IsNullOrEmpty(emisorIdClaim) || !int.TryParse(emisorIdClaim, out var emisorId))
            throw new UnauthorizedAccessException("No se encontró el EmisorId en el token.");
        return emisorId;
    }
}
