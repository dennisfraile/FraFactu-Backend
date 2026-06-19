using FraFactu.Application.Common.Settings;
using FraFactu.Application.DTOs.FromSmartCare;
using FraFactu.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FraFactu.API.Controllers;

/// <summary>
/// Endpoint que expone el catálogo de productos/servicios de un emisor para que
/// SmartCare-BE lo consuma al armar el selector "Productos adicionales" en BillingStep.
///
/// Auth: X-Api-Key (SmartCareSettings.ApiKey), mismo patrón que los demás
/// endpoints from-smartcare.
/// </summary>
[ApiController]
[Route("api/from-smartcare")]
[AllowAnonymous]
public class FromSmartCareCatalogoController : ControllerBase
{
    private readonly IFromSmartCarePrefillService _service;
    private readonly SmartCareSettings _settings;
    private readonly ILogger<FromSmartCareCatalogoController> _logger;

    public FromSmartCareCatalogoController(
        IFromSmartCarePrefillService service,
        IOptions<SmartCareSettings> settings,
        ILogger<FromSmartCareCatalogoController> logger)
    {
        _service = service;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Busca productos/servicios activos del catálogo Smartix accesibles desde la
    /// sucursal indicada. SmartCare usa este endpoint en BillingStep para poblar
    /// el selector "Productos adicionales" (insumos, extras, etc.).
    ///
    /// Respeta el mismo filtro de acceso por sucursal que
    /// <c>GET /api/from-smartcare/sucursales/{id}/servicios</c>:
    /// AccesoTodasSucursales=true OR fila en ProductosServiciosSucursales.
    /// Los precios devueltos incluyen IVA para ítems Gravado.
    ///
    /// Requiere header X-Api-Key (SmartCareSettings.ApiKey).
    /// 404 si la sucursal no existe o está inactiva.
    /// </summary>
    [HttpGet("catalogo")]
    [ProducesResponseType(typeof(CatalogoBusquedaResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Buscar(
        [FromQuery] int sucursalSmartixId,
        [FromQuery] string? search = null,
        [FromQuery] int limit = 50,
        [FromQuery] string? tipo = null,
        CancellationToken ct = default)
    {
        if (!ValidarApiKey())
        {
            _logger.LogWarning("Intento a /api/from-smartcare/catalogo sin X-Api-Key válida.");
            return Unauthorized(new { error = "API key invalida." });
        }

        // Validar que tipo sea uno de los valores reconocidos (o null/vacío).
        if (!string.IsNullOrEmpty(tipo) && tipo != "producto" && tipo != "servicio")
        {
            return BadRequest(new { error = "tipo debe ser 'producto', 'servicio' o estar ausente." });
        }

        var resultado = await _service.BuscarCatalogoAsync(sucursalSmartixId, search, limit, tipo);
        if (resultado == null)
            return NotFound(new { error = $"Sucursal {sucursalSmartixId} no existe o esta inactiva." });

        return Ok(resultado);
    }

    private bool ValidarApiKey() =>
        Request.Headers.TryGetValue("X-Api-Key", out var key) &&
        !string.IsNullOrEmpty(_settings.ApiKey) &&
        key.FirstOrDefault() == _settings.ApiKey;
}
