using FraFactu.API.Extensions;
using FraFactu.Application.DTOs.Integraciones;
using FraFactu.Application.Services;
using FraFactu.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FraFactu.API.Controllers;

/// <summary>
/// F4 (Plan inventario desde DTE): listado de divergencias detectadas por el
/// job de reconciliacion entre Smartix.StockBodega y el snapshot de
/// SmartInventory. Scope tenant: el endpoint solo devuelve divergencias del
/// Emisor del JWT (no cross-tenant).
/// </summary>
[ApiController]
[Route("api/inventario/divergencias")]
[Authorize]
public class InventarioDivergenciasController : ControllerBase
{
    private readonly IDivergenciaInventarioService _service;

    public InventarioDivergenciasController(IDivergenciaInventarioService service)
    {
        _service = service;
    }

    /// <summary>
    /// Lista las divergencias filtrando por estado, fechas o corrida puntual.
    /// Solo admins ven el listado (decision F4: observabilidad/auditoria).
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "EmisorAdmin,Contador,Auditor")]
    [ProducesResponseType(typeof(DivergenciasListadoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<DivergenciasListadoDto>> Listar(
        [FromQuery] DateTime? desdeFecha = null,
        [FromQuery] DateTime? hastaFecha = null,
        [FromQuery] EstadoDivergenciaInventario? estado = null,
        [FromQuery] Guid? ejecucionId = null,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        var emisorId = User.GetEmisorId();
        if (emisorId <= 0) return Unauthorized(new { error = "EmisorId no encontrado en el token" });

        var listado = await _service.ListarAsync(emisorId, new DivergenciasFiltroDto
        {
            DesdeFecha = desdeFecha,
            HastaFecha = hastaFecha,
            Estado = estado,
            EjecucionId = ejecucionId,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        });
        return Ok(listado);
    }
}
