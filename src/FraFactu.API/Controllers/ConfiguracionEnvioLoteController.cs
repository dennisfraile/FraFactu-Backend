using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FraFactu.Application.DTOs.Configuracion;
using FraFactu.Application.Interfaces;

namespace FraFactu.API.Controllers;

/// <summary>
/// Controller para gestionar la configuración de envío automático de lotes
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ConfiguracionEnvioLoteController : ControllerBase
{
    private readonly IConfiguracionEnvioLoteService _service;

    public ConfiguracionEnvioLoteController(IConfiguracionEnvioLoteService service)
    {
        _service = service;
    }

    /// <summary>
    /// Obtener la configuración de envío de lotes del emisor
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Cajero")]
    [ProducesResponseType(typeof(ConfiguracionEnvioLoteDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObtenerConfiguracion()
    {
        try
        {
            var emisorId = int.Parse(User.FindFirst("EmisorId")?.Value
                ?? throw new UnauthorizedAccessException("EmisorId no encontrado"));

            var config = await _service.ObtenerOCrearConfiguracionAsync(emisorId);
            return Ok(config);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Actualizar la configuración de envío de lotes
    /// </summary>
    /// <param name="dto">Configuración actualizada</param>
    [HttpPut]
    [Authorize(Roles = "EmisorAdmin")]
    [ProducesResponseType(typeof(ConfiguracionEnvioLoteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ActualizarConfiguracion([FromBody] ActualizarConfiguracionEnvioDto dto)
    {
        if (dto == null)
            return BadRequest(new { error = "Datos de configuración inválidos" });

        var emisorId = int.Parse(User.FindFirst("EmisorId")?.Value
            ?? throw new UnauthorizedAccessException("EmisorId no encontrado"));

        var config = await _service.ActualizarConfiguracionAsync(emisorId, dto);
        return Ok(config);
    }
}
