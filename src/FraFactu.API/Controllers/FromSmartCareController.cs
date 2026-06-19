using FraFactu.Application.Common.Settings;
using FraFactu.Application.DTOs;
using FraFactu.Application.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FraFactu.API.Controllers;

/// <summary>
/// Endpoints para que SmartCare deposite "prefills" (borradores) que la UI de
/// Smartix usa para abrir el wizard de factura pre-cargado. El usuario revisa,
/// ajusta, y emite manualmente desde el wizard.
/// </summary>
[ApiController]
[Route("api/from-smartcare")]
[AllowAnonymous]
public class FromSmartCareController : ControllerBase
{
    private readonly IFromSmartCarePrefillService _service;
    private readonly IValidator<FromSmartCareInvoiceRequestDto> _validator;
    private readonly SmartCareSettings _settings;
    private readonly ILogger<FromSmartCareController> _logger;

    public FromSmartCareController(
        IFromSmartCarePrefillService service,
        IValidator<FromSmartCareInvoiceRequestDto> validator,
        IOptions<SmartCareSettings> settings,
        ILogger<FromSmartCareController> logger)
    {
        _service = service;
        _validator = validator;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Crea (o reutiliza por idempotencia) un Prefill para que la UI de Smartix
    /// abra el wizard de factura pre-cargado. Idempotente por CorrelationId.
    /// Requiere header X-Api-Key (SmartCareSettings.ApiKey).
    /// </summary>
    [HttpPost("prefills")]
    [ProducesResponseType(typeof(FromSmartCarePrefillResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CrearPrefill([FromBody] FromSmartCareInvoiceRequestDto request)
    {
        if (!ValidarApiKey())
        {
            _logger.LogWarning("Intento a /api/from-smartcare/prefills sin X-Api-Key válida.");
            return Unauthorized(new { error = "API key inválida o ausente." });
        }

        var validationResult = await _validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            return BadRequest(new ValidationProblemDetails(errors)
            {
                Title = "Errores de validación en el payload SmartCare",
                Status = StatusCodes.Status400BadRequest
            });
        }

        try
        {
            var resultado = await _service.CrearAsync(request);
            return Ok(resultado);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Recupera un Prefill por Id. Usado por la UI Vue de Smartix al hidratar el
    /// wizard de factura. No requiere X-Api-Key — el PrefillId se considera un
    /// token privilegiado en esta iteración.
    /// </summary>
    [HttpGet("prefills/{id:int}")]
    [ProducesResponseType(typeof(FromSmartCarePrefillReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    public async Task<IActionResult> ObtenerPrefill(int id)
    {
        var prefill = await _service.ObtenerAsync(id);
        if (prefill is null)
            return NotFound(new { error = $"Prefill {id} no encontrado." });

        return Ok(prefill);
    }

    /// <summary>
    /// Lookup de Factura por correlationId. Lo usa SmartCare cuando su
    /// <c>visits.smartix_invoice_id</c> esta null (webhook perdido o atrasado)
    /// para hacer pull-mode polling y completar el estado. El correlationId lo
    /// genera SmartCare al crear el prefill, por lo que siempre lo conoce.
    /// Requiere X-Api-Key. Devuelve 404 si no hay factura con ese correlation
    /// (caso normal cuando la emision todavia no se concreto).
    /// </summary>
    [HttpGet("facturas/by-correlation/{correlationId}")]
    [ProducesResponseType(typeof(FromSmartCareFacturaEstadoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerEstadoFacturaPorCorrelation(string correlationId)
    {
        if (!ValidarApiKey())
        {
            return Unauthorized(new { error = "API key inválida o ausente." });
        }

        var estado = await _service.ObtenerEstadoFacturaPorCorrelationAsync(correlationId);
        if (estado is null)
            return NotFound(new { error = $"No hay factura emitida para correlationId '{correlationId}'." });

        return Ok(estado);
    }

    private bool ValidarApiKey() =>
        Request.Headers.TryGetValue("X-Api-Key", out var key) &&
        !string.IsNullOrEmpty(_settings.ApiKey) &&
        key.FirstOrDefault() == _settings.ApiKey;
}
