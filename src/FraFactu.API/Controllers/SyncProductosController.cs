using FraFactu.Application.Common.Settings;
using FraFactu.Application.DTOs.ProductosServicios;
using FraFactu.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FraFactu.API.Controllers;

/// <summary>
/// Endpoints server-to-server (X-Api-Key con SmartHubSettings.ApiKey) para que
/// SmartHub propague cambios desde SmartInventory hacia el catalogo de Smartix.
/// Idempotente: el caller puede reintentar sin efectos colaterales.
/// </summary>
[ApiController]
[Route("api/sync/productos")]
[AllowAnonymous]
public class SyncProductosController : ControllerBase
{
    private readonly IProductoServicioService _service;
    private readonly SmartHubSettings _settings;
    private readonly ILogger<SyncProductosController> _logger;

    public SyncProductosController(
        IProductoServicioService service,
        IOptions<SmartHubSettings> settings,
        ILogger<SyncProductosController> logger)
    {
        _service = service;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Cambia el flag Activo de un producto identificado por (EmisorId, Codigo).
    /// Disenado para mantener Smartix sincronizado con SmartInventory cuando se
    /// (des)activa alli. No 404ea si el producto no existe en Smartix: devuelve
    /// Encontrado=false (puede ocurrir si nunca se sincronizo).
    /// </summary>
    [HttpPost("toggle-active")]
    public async Task<ActionResult<SyncProductoToggleResponseDto>> ToggleActive([FromBody] SyncProductoToggleRequestDto request)
    {
        if (!ValidarApiKey()) return Unauthorized(new { error = "API key invalida." });
        if (request.EmisorId <= 0) return BadRequest(new { error = "EmisorId requerido." });
        if (string.IsNullOrWhiteSpace(request.Codigo)) return BadRequest(new { error = "Codigo requerido." });

        var result = await _service.SetActivoByCodigoAsync(request.EmisorId, request.Codigo, request.Activo);

        if (!result.Encontrado)
        {
            _logger.LogInformation("[SYNC-PRODUCTOS] Producto codigo={Codigo} emisor={EmisorId} no existe en Smartix; sync no-op",
                request.Codigo, request.EmisorId);
        }
        else if (result.Cambio)
        {
            _logger.LogInformation("[SYNC-PRODUCTOS] Producto id={Id} codigo={Codigo} emisor={EmisorId} activo={Activo}",
                result.Id, request.Codigo, request.EmisorId, result.Activo);
        }

        return Ok(result);
    }

    /// <summary>
    /// Crea o actualiza un producto desde SmartInventory por (EmisorId, Codigo).
    /// Si Smartix no tiene el producto lo crea con defaults (TipoImpuesto=Gravado,
    /// IVA=13%, AccesoTodasSucursales=true). Si lo tiene actualiza nombre, precios,
    /// stock minimo, codigo de barras y activo. Idempotente.
    /// </summary>
    [HttpPost("upsert")]
    public async Task<ActionResult<SyncProductoUpsertResponseDto>> Upsert([FromBody] SyncProductoUpsertRequestDto request)
    {
        if (!ValidarApiKey()) return Unauthorized(new { error = "API key invalida." });
        if (request.EmisorId <= 0) return BadRequest(new { error = "EmisorId requerido." });
        if (string.IsNullOrWhiteSpace(request.Codigo)) return BadRequest(new { error = "Codigo requerido." });
        if (string.IsNullOrWhiteSpace(request.Nombre)) return BadRequest(new { error = "Nombre requerido." });

        try
        {
            var result = await _service.UpsertByCodigoAsync(request);
            _logger.LogInformation("[SYNC-PRODUCTOS] Upsert id={Id} codigo={Codigo} emisor={Emisor} creado={Creado} cambio={Cambio}",
                result.Id, result.Codigo, request.EmisorId, result.Creado, result.Cambio);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private bool ValidarApiKey()
    {
        var apiKey = Request.Headers["X-Api-Key"].FirstOrDefault();
        return !string.IsNullOrEmpty(_settings.ApiKey) && apiKey == _settings.ApiKey;
    }
}
