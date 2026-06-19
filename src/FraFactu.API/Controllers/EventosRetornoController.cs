using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FraFactu.Application.DTOs.Retorno;
using FraFactu.Application.Interfaces;

namespace FraFactu.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EventosRetornoController : ControllerBase
{
    private readonly IEventoRetornoService _eventoService;

    public EventosRetornoController(IEventoRetornoService eventoService)
    {
        _eventoService = eventoService;
    }

    /// <summary>
    /// Crear y transmitir un Evento de Retorno (tipoEvento 18).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Contador")]
    public async Task<IActionResult> CrearEvento([FromBody] CrearEventoRetornoDto dto)
    {
        try
        {
            var emisorId = int.Parse(User.FindFirst("EmisorId")?.Value
                ?? throw new UnauthorizedAccessException("EmisorId no encontrado en el token"));

            var resultado = await _eventoService.CrearEventoAsync(dto, emisorId);
            return Ok(resultado);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Obtener un evento de retorno por ID.
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Contador,Auditor")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        try
        {
            var emisorId = int.Parse(User.FindFirst("EmisorId")?.Value
                ?? throw new UnauthorizedAccessException("EmisorId no encontrado"));

            var resultado = await _eventoService.ObtenerPorIdAsync(id, emisorId);
            return Ok(resultado);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Generar el JSON del evento según el esquema del MH.
    /// </summary>
    [HttpGet("{id}/json")]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Contador,Auditor")]
    public async Task<IActionResult> GenerarJson(int id)
    {
        try
        {
            var emisorId = int.Parse(User.FindFirst("EmisorId")?.Value
                ?? throw new UnauthorizedAccessException("EmisorId no encontrado"));

            var json = await _eventoService.GenerarJsonEventoAsync(id, emisorId);
            return Ok(new { json });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
