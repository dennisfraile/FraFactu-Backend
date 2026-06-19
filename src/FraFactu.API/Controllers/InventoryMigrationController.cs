using FraFactu.Application.Common.Settings;
using FraFactu.Application.DTOs;
using FraFactu.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FraFactu.API.Controllers;

[ApiController]
[Route("api/inventory")]
[AllowAnonymous]
public class InventoryMigrationController : ControllerBase
{
    private readonly IInventoryMigrationService _service;
    private readonly SmartHubSettings _settings;

    public InventoryMigrationController(IInventoryMigrationService service, IOptions<SmartHubSettings> settings)
    {
        _service = service;
        _settings = settings.Value;
    }

    /// <summary>
    /// Exporta el catálogo de productos activos del emisor correspondiente al HubId.
    /// Llamado por SmartHub durante migración SmartixToSmartInventory.
    /// </summary>
    [HttpPost("export-to-smartinventory")]
    public async Task<IActionResult> Export([FromBody] InventoryExportRequestDto request)
    {
        if (!ValidarApiKey()) return Unauthorized(new { error = "API key inválida." });
        if (request.HubId <= 0) return BadRequest(new { error = "HubId requerido." });

        try
        {
            var result = await _service.ExportarInventarioAsync(request.HubId, request.MigrationId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Importa (upsert) el catálogo recibido desde SmartInventory al inventario interno del emisor.
    /// Idempotente por MigrationId. Llamado por SmartHub durante migración SmartInventoryToSmartix.
    /// </summary>
    [HttpPost("import-from-smartinventory")]
    public async Task<IActionResult> Import([FromBody] InventoryImportRequestDto request)
    {
        if (!ValidarApiKey()) return Unauthorized(new { error = "API key inválida." });
        if (request.HubId <= 0) return BadRequest(new { error = "HubId requerido." });

        try
        {
            await _service.ImportarDesdeSmartInventoryAsync(request);
            return Ok(new { mensaje = "Importación completada." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    private bool ValidarApiKey()
    {
        var apiKey = Request.Headers["X-Api-Key"].FirstOrDefault();
        return !string.IsNullOrEmpty(_settings.ApiKey) && apiKey == _settings.ApiKey;
    }
}
