using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FraFactu.Application.DTOs.OperacionesEspeciales;
using FraFactu.Application.Interfaces;

namespace FraFactu.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EventosOperacionesEspecialesController : ControllerBase
{
    private readonly IEventoOperacionEspecialService _eventoService;

    public EventosOperacionesEspecialesController(IEventoOperacionEspecialService eventoService)
    {
        _eventoService = eventoService;
    }

    /// <summary>
    /// Crear y transmitir un Evento de Operaciones Especiales (tipoEvento 17).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Contador")]
    public async Task<IActionResult> CrearEvento([FromBody] CrearEventoOperacionEspecialDto dto)
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
    /// Obtener un evento de operaciones especiales por ID.
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
