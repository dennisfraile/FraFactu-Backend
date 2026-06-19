using FraFactu.Application.Common.Settings;
using FraFactu.Application.DTOs.Sync;
using FraFactu.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FraFactu.API.Controllers;

/// <summary>
/// Endpoint server-to-server (X-Api-Key con SmartHubSettings.ApiKey) para
/// mantener el flag Activo del Emisor sincronizado con el Hub equivalente
/// en SmartHub. Cuando un admin desactiva un Hub en SmartHub, esto se reenvia
/// aqui y el Emisor 1:1 queda marcado como inactivo en Smartix tambien.
/// </summary>
[ApiController]
[Route("api/sync/emisor")]
[AllowAnonymous]
public class SyncEmisorController : ControllerBase
{
    private readonly IEmisorService _service;
    private readonly SmartHubSettings _settings;
    private readonly ILogger<SyncEmisorController> _logger;

    public SyncEmisorController(
        IEmisorService service,
        IOptions<SmartHubSettings> settings,
        ILogger<SyncEmisorController> logger)
    {
        _service = service;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Cambia el flag Activo del Emisor identificado por HubId (1:1 con SmartHub).
    /// Idempotente. Si el emisor no existe en Smartix devuelve Encontrado=false
    /// (puede ocurrir si el Hub fue creado en SmartHub pero aun no hay emisor
    /// hidratado aqui).
    /// </summary>
    [HttpPost("toggle-active")]
    public async Task<ActionResult<SyncEmisorToggleResponseDto>> ToggleActive([FromBody] SyncEmisorToggleRequestDto request)
    {
        if (!ValidarApiKey()) return Unauthorized(new { error = "API key invalida." });
        if (request.EmisorId <= 0) return BadRequest(new { error = "EmisorId requerido." });

        var result = await _service.SetActivoByHubIdAsync(request.EmisorId, request.Activo);

        if (!result.Encontrado)
        {
            _logger.LogInformation("[SYNC-EMISOR] Hub={HubId} no tiene Emisor en Smartix; sync no-op", request.EmisorId);
        }
        else if (result.Cambio)
        {
            _logger.LogInformation("[SYNC-EMISOR] Emisor id={Id} (Hub={HubId}) activo={Activo}",
                result.Id, request.EmisorId, result.Activo);
        }

        return Ok(result);
    }

    private bool ValidarApiKey()
    {
        var apiKey = Request.Headers["X-Api-Key"].FirstOrDefault();
        return !string.IsNullOrEmpty(_settings.ApiKey) && apiKey == _settings.ApiKey;
    }
}
