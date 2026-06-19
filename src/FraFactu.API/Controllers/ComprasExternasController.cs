using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FraFactu.Application.Services;
using FraFactu.Application.DTOs.Compras;
using FraFactu.Application.Common;
using FraFactu.API.Helpers;

namespace FraFactu.API.Controllers;

/// <summary>
/// Controller para gestión de compras externas
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ComprasExternasController : ControllerBase
{
    private readonly ICompraExternaService _compraService;

    public ComprasExternasController(ICompraExternaService compraService)
    {
        _compraService = compraService;
    }

    private int GetEmisorId()
    {
        var emisorIdClaim = User.FindFirst("EmisorId")?.Value;
        if (string.IsNullOrEmpty(emisorIdClaim) || !int.TryParse(emisorIdClaim, out var emisorId))
        {
            throw new UnauthorizedAccessException("EmisorId no encontrado en el token");
        }
        return emisorId;
    }

    /// <summary>
    /// Crear una nueva compra externa (estado BORRADOR)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Contador")]
    public async Task<ActionResult<CompraExternaDto>> Crear([FromBody] CrearCompraExternaDto dto)
    {
        try
        {
            if (!ScopeHelper.ValidarAccesoSucursal(User, dto.SucursalId))
                return StatusCode(403, new { error = "No tiene permiso para crear compras en esta sucursal" });

            var emisorId = GetEmisorId();
            var resultado = await _compraService.CrearAsync(dto, emisorId);
            return CreatedAtAction(nameof(ObtenerPorId), new { id = resultado.Id }, resultado);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Actualizar una compra externa en estado BORRADOR
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Contador")]
    public async Task<ActionResult<CompraExternaDto>> Actualizar(int id, [FromBody] ActualizarCompraExternaDto dto)
    {
        try
        {
            var emisorId = GetEmisorId();
            var compra = await _compraService.ObtenerPorIdAsync(id, emisorId);
            if (!ScopeHelper.ValidarAccesoSucursal(User, compra.SucursalId))
                return StatusCode(403, new { error = "No tiene permiso para modificar compras de esta sucursal" });

            var resultado = await _compraService.ActualizarAsync(id, dto, emisorId);
            return Ok(resultado);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Confirmar una compra (BORRADOR → CONFIRMADA)
    /// Afecta inventario y registra movimientos
    /// </summary>
    [HttpPost("{id}/confirmar")]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Contador")]
    public async Task<ActionResult<CompraExternaDto>> Confirmar(int id, [FromBody] ConfirmarCompraDto? dto = null)
    {
        try
        {
            var emisorId = GetEmisorId();
            var compra = await _compraService.ObtenerPorIdAsync(id, emisorId);
            if (!ScopeHelper.ValidarAccesoSucursal(User, compra.SucursalId))
                return StatusCode(403, new { error = "No tiene permiso para confirmar compras de esta sucursal" });

            var resultado = await _compraService.ConfirmarAsync(id, emisorId, dto);
            return Ok(resultado);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Anular una compra confirmada
    /// Revierte inventario y registra movimientos de anulación
    /// </summary>
    [HttpPost("{id}/anular")]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Contador")]
    public async Task<ActionResult<CompraExternaDto>> Anular(int id, [FromBody] AnularCompraDto dto)
    {
        try
        {
            var emisorId = GetEmisorId();
            var compra = await _compraService.ObtenerPorIdAsync(id, emisorId);
            if (!ScopeHelper.ValidarAccesoSucursal(User, compra.SucursalId))
                return StatusCode(403, new { error = "No tiene permiso para anular compras de esta sucursal" });

            var resultado = await _compraService.AnularAsync(id, dto, emisorId);
            return Ok(resultado);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Editar una compra CONFIRMADA de origen MANUAL.
    /// Si cambian los items, revierte y reaplica inventario atómicamente.
    /// </summary>
    [HttpPut("{id}/editar-confirmada")]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Contador")]
    public async Task<ActionResult<CompraExternaDto>> EditarConfirmada(int id, [FromBody] ActualizarCompraExternaDto dto)
    {
        try
        {
            var emisorId = GetEmisorId();
            var compra = await _compraService.ObtenerPorIdAsync(id, emisorId);
            if (!ScopeHelper.ValidarAccesoSucursal(User, compra.SucursalId))
                return StatusCode(403, new { error = "No tiene permiso para editar compras de esta sucursal" });

            var resultado = await _compraService.EditarConfirmadaAsync(id, dto, emisorId);
            return Ok(resultado);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Obtener una compra por ID
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Contador,Auditor")]
    public async Task<ActionResult<CompraExternaDto>> ObtenerPorId(int id)
    {
        try
        {
            var emisorId = GetEmisorId();
            var resultado = await _compraService.ObtenerPorIdAsync(id, emisorId);

            if (!ScopeHelper.ValidarAccesoSucursal(User, resultado.SucursalId))
                return StatusCode(403, new { error = "No tiene permiso para ver compras de esta sucursal" });

            return Ok(resultado);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Listar compras con filtros y paginación
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Contador,Auditor")]
    public async Task<ActionResult<PagedResult<CompraExternaDto>>> Listar(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20,
        [FromQuery] int? proveedorId = null,
        [FromQuery] int? bodegaId = null,
        [FromQuery] int? sucursalId = null,
        [FromQuery] string? estado = null,
        [FromQuery] DateTime? fechaDesde = null,
        [FromQuery] DateTime? fechaHasta = null,
        [FromQuery] DateTime? fechaRegistroDesde = null,
        [FromQuery] DateTime? fechaRegistroHasta = null,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDesc = true)
    {
        var emisorId = GetEmisorId();

        // Determinar filtro de sucursal
        int? sucursalIdFinal = null;
        List<int>? sucursalIdsFinal = null;
        var rol = ScopeHelper.GetRolFromClaims(User);

        if (ScopeHelper.RequiereRestriccionSucursal(rol) && !ScopeHelper.GetAccesoTodasSucursales(User))
        {
            var userSucursalIds = ScopeHelper.GetSucursalIdsFromClaims(User);
            if (sucursalId.HasValue)
            {
                if (!userSucursalIds.Contains(sucursalId.Value))
                    return StatusCode(403, new { error = "No tiene acceso a esta sucursal" });
                sucursalIdFinal = sucursalId.Value;
            }
            else if (userSucursalIds.Count == 1)
            {
                sucursalIdFinal = userSucursalIds[0];
            }
            else
            {
                sucursalIdsFinal = userSucursalIds;
            }
        }
        else if (sucursalId.HasValue)
        {
            sucursalIdFinal = sucursalId.Value;
        }

        var resultado = await _compraService.ListarAsync(
            emisorId,
            pagina,
            tamanoPagina,
            proveedorId,
            bodegaId,
            sucursalIdFinal,
            estado,
            fechaDesde,
            fechaHasta,
            fechaRegistroDesde,
            fechaRegistroHasta,
            search,
            sucursalIdsFinal,
            sortBy,
            sortDesc);

        return Ok(resultado);
    }

    /// <summary>
    /// Obtener últimas compras de un proveedor
    /// </summary>
    [HttpGet("proveedor/{proveedorId}")]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Contador,Auditor")]
    public async Task<ActionResult<List<CompraExternaDto>>> ObtenerPorProveedor(
        int proveedorId,
        [FromQuery] int limite = 10)
    {
        var emisorId = GetEmisorId();
        var resultado = await _compraService.ObtenerPorProveedorAsync(proveedorId, emisorId, limite);
        return Ok(resultado);
    }

    /// <summary>
    /// Obtener total de compras en un período
    /// </summary>
    [HttpGet("totales")]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Contador,Auditor")]
    public async Task<ActionResult<decimal>> ObtenerTotales(
        [FromQuery] DateTime desde,
        [FromQuery] DateTime hasta,
        [FromQuery] int? proveedorId = null,
        [FromQuery] int? sucursalId = null)
    {
        var emisorId = GetEmisorId();

        int? sucursalIdFinal = null;
        var rol = ScopeHelper.GetRolFromClaims(User);
        if (ScopeHelper.RequiereRestriccionSucursal(rol) && !ScopeHelper.GetAccesoTodasSucursales(User))
        {
            var userSucursalIds = ScopeHelper.GetSucursalIdsFromClaims(User);
            if (sucursalId.HasValue)
            {
                if (!userSucursalIds.Contains(sucursalId.Value))
                    return StatusCode(403, new { error = "No tiene acceso a esta sucursal" });
                sucursalIdFinal = sucursalId.Value;
            }
            else if (userSucursalIds.Count == 1)
            {
                sucursalIdFinal = userSucursalIds[0];
            }
        }
        else if (sucursalId.HasValue)
        {
            sucursalIdFinal = sucursalId.Value;
        }

        var total = await _compraService.ObtenerTotalComprasAsync(desde, hasta, emisorId, proveedorId, sucursalIdFinal);
        return Ok(new { desde, hasta, proveedorId, sucursalId = sucursalIdFinal, total });
    }
}
