using FraFactu.Application.Common.Settings;
using FraFactu.Application.DTOs.Hub;
using FraFactu.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FraFactu.API.Controllers;

/// <summary>
/// Endpoints server-to-server (autenticados con X-Api-Key contra
/// <c>SmartHubSettings.ApiKey</c>) que recibe los webhooks de gestión de usuarios
/// que dispara SmartHub: asignar app, cambiar rol, deshabilitar/habilitar y
/// asignación de sucursal. Forman parte de la Fase 2 del plan de centralización
/// de usuarios — convierten en activo el catálogo de webhooks que el Hub ya
/// emitía pero ninguna app consumía.
/// </summary>
[ApiController]
[Route("api/hub")]
[AllowAnonymous]
public class HubWebhookController : ControllerBase
{
    private readonly IHubUsuarioSyncService _sync;
    private readonly SmartHubSettings _settings;
    private readonly ILogger<HubWebhookController> _logger;

    public HubWebhookController(
        IHubUsuarioSyncService sync,
        IOptions<SmartHubSettings> settings,
        ILogger<HubWebhookController> logger)
    {
        _sync = sync;
        _settings = settings.Value;
        _logger = logger;
    }

    [HttpPost("asignar")]
    public async Task<IActionResult> Asignar([FromBody] HubUsuarioWebhookDto webhook, CancellationToken ct)
    {
        if (!ValidarApiKey()) return Unauthorized(new { error = "API key inválida." });
        return ToResponse(await _sync.AsignarAsync(webhook, ct));
    }

    [HttpPost("cambiar-rol")]
    public async Task<IActionResult> CambiarRol([FromBody] HubUsuarioWebhookDto webhook, CancellationToken ct)
    {
        if (!ValidarApiKey()) return Unauthorized(new { error = "API key inválida." });
        return ToResponse(await _sync.CambiarRolAsync(webhook, ct));
    }

    [HttpPost("deshabilitar")]
    public async Task<IActionResult> Deshabilitar([FromBody] HubUsuarioWebhookDto webhook, CancellationToken ct)
    {
        if (!ValidarApiKey()) return Unauthorized(new { error = "API key inválida." });
        return ToResponse(await _sync.DeshabilitarAsync(webhook.HubUsuarioId, ct));
    }

    [HttpPost("habilitar")]
    public async Task<IActionResult> Habilitar([FromBody] HubUsuarioWebhookDto webhook, CancellationToken ct)
    {
        if (!ValidarApiKey()) return Unauthorized(new { error = "API key inválida." });
        return ToResponse(await _sync.HabilitarAsync(webhook.HubUsuarioId, ct));
    }

    [HttpPost("sucursal-asignacion-cambio")]
    public async Task<IActionResult> SucursalAsignacionCambio([FromBody] HubSucursalAsignacionDto payload, CancellationToken ct)
    {
        if (!ValidarApiKey()) return Unauthorized(new { error = "API key inválida." });
        return ToResponse(await _sync.CambiarAsignacionSucursalAsync(payload, ct));
    }

    [HttpGet("usuarios-roles")]
    public async Task<ActionResult<List<HubUsuarioRolInfoDto>>> GetUsuariosRoles(CancellationToken ct)
    {
        if (!ValidarApiKey()) return Unauthorized(new { error = "API key inválida." });
        var roles = await _sync.ListarUsuariosRolesAsync(ct);
        return Ok(roles);
    }

    private bool ValidarApiKey()
    {
        var apiKey = Request.Headers["X-Api-Key"].FirstOrDefault();
        return !string.IsNullOrEmpty(_settings.ApiKey) && apiKey == _settings.ApiKey;
    }

    private IActionResult ToResponse(HubSyncResult result) => result.Status switch
    {
        HubSyncStatus.Ok => Ok(new { cambio = result.Cambio, mensaje = result.Mensaje }),
        HubSyncStatus.BadRequest => BadRequest(new { error = result.Mensaje }),
        HubSyncStatus.NotFound => NotFound(new { error = result.Mensaje }),
        HubSyncStatus.Conflict => Conflict(new { error = result.Mensaje }),
        _ => StatusCode(500, new { error = "Estado de sync no manejado." })
    };
}
