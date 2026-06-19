using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FraFactu.Application.DTOs.Lotes;
using FraFactu.Application.DTOs.Common;
using FraFactu.Application.Services;
using FraFactu.API.Helpers;

namespace FraFactu.API.Controllers;

/// <summary>
/// Controlador para gestión de lotes de envío de DTEs
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LotesController : ControllerBase
{
    private readonly ILoteService _loteService;
    private readonly ILogger<LotesController> _logger;

    public LotesController(
        ILoteService loteService,
        ILogger<LotesController> logger)
    {
        _loteService = loteService;
        _logger = logger;
    }

    /// <summary>
    /// Crea un nuevo lote con las facturas especificadas
    /// </summary>
    /// <param name="emisorId">ID del emisor</param>
    /// <param name="dto">Datos del lote a crear</param>
    /// <returns>Lote creado</returns>
    [HttpPost]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Contador,Cajero")]
    [ProducesResponseType(typeof(LoteDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LoteDto>> CrearLote(
        [FromQuery] int emisorId,
        [FromBody] CrearLoteDto dto)
    {
        try
        {
            _logger.LogInformation("Creando lote para emisor {EmisorId}", emisorId);

            var lote = await _loteService.CrearLoteAsync(emisorId, dto);

            return CreatedAtAction(
                nameof(ObtenerLote),
                new { id = lote.Id },
                lote);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Error de validación al crear lote");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear lote");
            return StatusCode(500, new { error = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Envía un lote al Ministerio de Hacienda
    /// </summary>
    /// <param name="id">ID del lote</param>
    /// <returns>Lote con estado actualizado</returns>
    [HttpPost("{id}/enviar")]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Contador,Cajero")]
    [ProducesResponseType(typeof(LoteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LoteDto>> EnviarLote(int id)
    {
        try
        {
            _logger.LogInformation("Enviando lote {LoteId}", id);

            var lote = await _loteService.EnviarLoteAsync(id);

            return Ok(lote);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Lote {LoteId} no encontrado", id);
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Error de validación al enviar lote {LoteId}", id);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar lote {LoteId}", id);
            return StatusCode(500, new { error = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Consulta el estado individual de cada DTE en el lote
    /// </summary>
    /// <param name="id">ID del lote</param>
    /// <returns>Lote con estados actualizados</returns>
    [HttpPost("{id}/consultar-estados")]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Contador,Cajero")]
    [ProducesResponseType(typeof(LoteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LoteDto>> ConsultarEstados(int id)
    {
        try
        {
            _logger.LogInformation("Consultando estados de lote {LoteId}", id);

            var lote = await _loteService.ConsultarEstadosIndividualesAsync(id);

            return Ok(lote);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Lote {LoteId} no encontrado", id);
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Error al consultar estados del lote {LoteId}", id);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al consultar estados del lote {LoteId}", id);
            return StatusCode(500, new { error = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtiene un lote por ID con todos sus detalles
    /// </summary>
    /// <param name="id">ID del lote</param>
    /// <returns>Lote con detalles</returns>
    [HttpGet("{id}")]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Contador,Cajero,Auditor")]
    [ProducesResponseType(typeof(LoteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LoteDto>> ObtenerLote(int id)
    {
        try
        {
            var lote = await _loteService.ObtenerLoteAsync(id);
            return Ok(lote);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Lote {LoteId} no encontrado", id);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener lote {LoteId}", id);
            return StatusCode(500, new { error = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtiene todos los lotes de un emisor de forma paginada
    /// </summary>
    /// <param name="request">Parámetros de paginación</param>
    /// <param name="sucursalId">ID de la sucursal (opcional)</param>
    /// <param name="esContingencia">Filtrar por lotes de contingencia (opcional)</param>
    /// <param name="estado">Estado del lote para filtrar (opcional)</param>
    /// <param name="ambiente">Ambiente para filtrar (opcional)</param>
    /// <param name="fechaDesde">Fecha desde para filtrar (opcional)</param>
    /// <param name="fechaHasta">Fecha hasta para filtrar (opcional)</param>
    /// <param name="search">Búsqueda por texto (opcional)</param>
    /// <returns>Lista paginada de lotes</returns>
    [HttpGet]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Contador,Cajero,Auditor")]
    [ProducesResponseType(typeof(PaginatedResponse<LoteDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<LoteDto>>> ListarLotes(
        [FromQuery] PaginatedRequest request,
        [FromQuery] int? sucursalId = null,
        [FromQuery] bool? esContingencia = null,
        [FromQuery] string? estado = null,
        [FromQuery] string? ambiente = null,
        [FromQuery] DateTime? fechaDesde = null,
        [FromQuery] DateTime? fechaHasta = null,
        [FromQuery] string? search = null)
    {
        try
        {
            var emisorId = int.Parse(User.FindFirst("EmisorId")?.Value
                ?? throw new UnauthorizedAccessException("EmisorId no encontrado en el token"));

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

            _logger.LogInformation("Obteniendo lotes del emisor {EmisorId} (Página {Page})", emisorId, request.PageNumber);

            var response = await _loteService.ObtenerLotesPorEmisorAsync(request, emisorId, sucursalIds, esContingencia, usuarioId, estado, ambiente, fechaDesde, fechaHasta, search);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al listar lotes");
            return StatusCode(500, new { error = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Envío manual de lote con selección de facturas pendientes
    /// </summary>
    /// <param name="emisorId">ID del emisor</param>
    /// <param name="dto">Datos del envío manual (IDs de facturas)</param>
    /// <returns>Lote creado y enviado</returns>
    [HttpPost("enviar-manual")]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Contador,Cajero")]
    [ProducesResponseType(typeof(LoteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LoteDto>> EnviarManual(
        [FromQuery] int emisorId,
        [FromBody] EnviarLoteManualDto dto)
    {
        try
        {
            _logger.LogInformation("Envío manual de lote para emisor {EmisorId} con {Total} facturas",
                emisorId, dto.FacturaIds.Count);

            var lote = await _loteService.EnviarLoteManualAsync(emisorId, dto);

            return Ok(lote);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Facturas no encontradas para emisor {EmisorId}", emisorId);
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Error de validación en envío manual");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en envío manual de lote para emisor {EmisorId}", emisorId);
            return StatusCode(500, new { error = "Error interno del servidor" });
        }
    }
}
