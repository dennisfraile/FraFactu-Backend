using FraFactu.Application.Common.Settings;
using FraFactu.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FraFactu.API.Controllers;

/// <summary>
/// F3 (Plan inventario desde DTE): endpoint server-to-server que SmartHub
/// invoca cuando se activa o desactiva la app SmartInventory para un Hub.
/// Smartix actualiza el flag <c>Emisor.TieneSmartInventoryActiva</c> usando
/// el HubId como llave (1:1 Hub ↔ Emisor).
///
/// Auth: X-Api-Key compartida con SmartHub (<c>SmartHubSettings.ApiKey</c>),
/// mismo patron que <c>SyncEmisorController</c>. Idempotente: invocaciones
/// repetidas con el mismo valor no son error.
/// </summary>
[ApiController]
[Route("api/sync/inventory-app")]
[AllowAnonymous]
public class SyncInventoryAppController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly SmartHubSettings _settings;
    private readonly ILogger<SyncInventoryAppController> _logger;

    public SyncInventoryAppController(
        ApplicationDbContext context,
        IOptions<SmartHubSettings> settings,
        ILogger<SyncInventoryAppController> logger)
    {
        _context = context;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Cambia <c>TieneSmartInventoryActiva</c> del Emisor identificado por
    /// HubId. Body: <c>{ hubId, activa }</c>. Idempotente. Si no hay Emisor
    /// hidratado para ese Hub devuelve 200 con <c>Encontrado=false</c>
    /// (mismo criterio que el toggle de Activo) para no romper el flujo de
    /// SmartHub cuando el orden de creacion fue Hub -&gt; Emisor.
    /// </summary>
    [HttpPost("toggle")]
    public async Task<ActionResult<InventoryAppToggleResponseDto>> Toggle(
        [FromBody] InventoryAppToggleRequestDto request,
        CancellationToken ct)
    {
        if (!ValidarApiKey()) return Unauthorized(new { error = "API key invalida." });
        if (request.HubId <= 0) return BadRequest(new { error = "HubId requerido." });

        var emisor = await _context.Emisores
            .FirstOrDefaultAsync(e => e.HubId == request.HubId && e.Activo, ct);

        if (emisor is null)
        {
            _logger.LogInformation(
                "[SYNC-INVENTORY-APP] Hub={HubId} sin Emisor en Smartix; sync no-op (activa={Activa})",
                request.HubId, request.Activa);
            return Ok(new InventoryAppToggleResponseDto
            {
                HubId = request.HubId,
                EmisorId = null,
                TieneSmartInventoryActiva = request.Activa,
                Encontrado = false
            });
        }

        var cambio = emisor.TieneSmartInventoryActiva != request.Activa;
        if (cambio)
        {
            emisor.TieneSmartInventoryActiva = request.Activa;
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation(
                "[SYNC-INVENTORY-APP] Emisor id={Id} (Hub={HubId}) TieneSmartInventoryActiva={Activa}",
                emisor.Id, request.HubId, request.Activa);
        }

        return Ok(new InventoryAppToggleResponseDto
        {
            HubId = request.HubId,
            EmisorId = emisor.Id,
            TieneSmartInventoryActiva = emisor.TieneSmartInventoryActiva,
            Encontrado = true
        });
    }

    private bool ValidarApiKey()
    {
        var apiKey = Request.Headers["X-Api-Key"].FirstOrDefault() ?? string.Empty;
        var expected = _settings.ApiKey ?? string.Empty;
        return !string.IsNullOrEmpty(expected) && apiKey == expected;
    }
}

/// <summary>F3: payload del webhook server-to-server desde SmartHub.</summary>
public class InventoryAppToggleRequestDto
{
    public int HubId { get; set; }
    public bool Activa { get; set; }
}

/// <summary>F3: respuesta tras persistir (o no-op si no hay Emisor).</summary>
public class InventoryAppToggleResponseDto
{
    public int HubId { get; set; }
    public int? EmisorId { get; set; }
    public bool TieneSmartInventoryActiva { get; set; }

    /// <summary>
    /// False si no habia Emisor para el Hub (caso valido durante onboarding
    /// del Hub antes de hidratar Smartix); el flag queda guardado en SmartHub
    /// y se aplicara cuando el Emisor sea creado.
    /// </summary>
    public bool Encontrado { get; set; }
}
