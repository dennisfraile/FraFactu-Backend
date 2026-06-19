using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FraFactu.Application.DTOs.Common;
using FraFactu.Application.DTOs.Contingencia;
using FraFactu.Application.Interfaces;
using FraFactu.API.Helpers;

namespace FraFactu.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EventosContingenciaController : ControllerBase
{
    private readonly IEventoContingenciaService _eventoService;

    public EventosContingenciaController(IEventoContingenciaService eventoService)
    {
        _eventoService = eventoService;
    }

    /// <summary>
    /// Crear un nuevo evento de contingencia
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Contador,Cajero")]
    public async Task<IActionResult> CrearEvento([FromBody] CrearEventoContingenciaDto dto)
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
    /// Obtener un evento por ID
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Contador,Cajero,Auditor")]
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
    /// Listar todos los eventos del emisor, opcionalmente filtrados por sucursal
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Contador,Cajero,Auditor")]
    public async Task<IActionResult> Listar(
        [FromQuery] PaginatedRequest request,
        [FromQuery] int? sucursalId = null,
        [FromQuery] string? search = null,
        [FromQuery] int? tipoContingencia = null,
        [FromQuery] string? estadoHacienda = null,
        [FromQuery] DateTime? fechaDesde = null,
        [FromQuery] DateTime? fechaHasta = null)
    {
        var emisorId = int.Parse(User.FindFirst("EmisorId")?.Value
            ?? throw new UnauthorizedAccessException("EmisorId no encontrado"));

        var rol = ScopeHelper.GetRolFromClaims(User);
        List<int>? sucursalIds = null;
        int? usuarioId = null;

        if (ScopeHelper.RequiereRestriccionSucursal(rol) && !ScopeHelper.GetAccesoTodasSucursales(User))
        {
            var userSucursalIds = ScopeHelper.GetSucursalIdsFromClaims(User);
            if (userSucursalIds.Count == 0)
                return BadRequest(new { message = "Usuario con rol restringido no tiene sucursal asignada" });

            if (sucursalId.HasValue)
            {
                if (!userSucursalIds.Contains(sucursalId.Value))
                    return Forbid();
                sucursalIds = new List<int> { sucursalId.Value };
            }
            else
            {
                sucursalIds = userSucursalIds;
            }
        }
        else if (sucursalId.HasValue)
        {
            sucursalIds = new List<int> { sucursalId.Value };
        }

        if (rol == "Cajero")
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out var uid))
                usuarioId = uid;
        }

        var resultados = await _eventoService.ListarPorEmisorAsync(request, emisorId, sucursalIds, usuarioId, search, tipoContingencia, estadoHacienda, fechaDesde, fechaHasta);
        return Ok(resultados);
    }

    /// <summary>
    /// Generar JSON del evento para transmisión a MH
    /// </summary>
    [HttpGet("{id}/json")]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Contador,Cajero,Auditor")]
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

    /// <summary>
    /// Marcar un DTE como diferido por contingencia manual (tipos 2-5)
    /// </summary>
    [HttpPost("marcar-diferido")]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Contador,Cajero")]
    public async Task<IActionResult> MarcarDteDiferido([FromBody] MarcarDteDiferidoRequestDto request)
    {
        try
        {
            var emisorId = int.Parse(User.FindFirst("EmisorId")?.Value
                ?? throw new UnauthorizedAccessException("EmisorId no encontrado en el token"));

            await _eventoService.MarcarDteDiferidoAsync(request, emisorId);
            return Ok(new { message = "DTE marcado como diferido exitosamente" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Obtener tiempo restante del plazo de 24 horas para un evento de contingencia
    /// </summary>
    [HttpGet("{id}/tiempo-restante")]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Contador,Cajero,Auditor")]
    public async Task<IActionResult> ObtenerTiempoRestante(int id)
    {
        try
        {
            var emisorId = int.Parse(User.FindFirst("EmisorId")?.Value
                ?? throw new UnauthorizedAccessException("EmisorId no encontrado en el token"));

            var resultado = await _eventoService.ObtenerTiempoRestanteAsync(id, emisorId);
            return Ok(resultado);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
