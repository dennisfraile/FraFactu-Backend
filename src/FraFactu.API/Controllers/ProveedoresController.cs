using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FraFactu.Application.Services;
using FraFactu.Application.DTOs.Compras;
using FraFactu.Application.Common;

namespace FraFactu.API.Controllers;

/// <summary>
/// Controller para gestión de proveedores
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProveedoresController : ControllerBase
{
    private readonly IProveedorService _proveedorService;

    public ProveedoresController(IProveedorService proveedorService)
    {
        _proveedorService = proveedorService;
    }

    /// <summary>
    /// Obtiene el EmisorId del token JWT
    /// </summary>
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
    /// Crear un nuevo proveedor
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Contador")]
    public async Task<ActionResult<ProveedorDto>> Crear([FromBody] CrearProveedorDto dto)
    {
        try
        {
            var emisorId = GetEmisorId();
            var resultado = await _proveedorService.CrearAsync(dto, emisorId);
            return CreatedAtAction(nameof(ObtenerPorId), new { id = resultado.Id }, resultado);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Actualizar un proveedor existente
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Contador")]
    public async Task<ActionResult<ProveedorDto>> Actualizar(int id, [FromBody] ActualizarProveedorDto dto)
    {
        try
        {
            var resultado = await _proveedorService.ActualizarAsync(id, dto);
            return Ok(resultado);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Obtener un proveedor por ID
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "SuperAdmin,EmisorAdmin,GerenteSucursal,Contador,Auditor")]
    public async Task<ActionResult<ProveedorDto>> ObtenerPorId(int id)
    {
        try
        {
            var emisorId = GetEmisorId();
            var resultado = await _proveedorService.ObtenerPorIdAsync(id, emisorId);
            return Ok(resultado);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Buscar proveedor por NIT
    /// </summary>
    [HttpGet("nit/{nit}")]
    [Authorize(Roles = "SuperAdmin,EmisorAdmin,GerenteSucursal,Contador,Auditor")]
    public async Task<ActionResult<ProveedorDto>> ObtenerPorNIT(string nit)
    {
        var emisorId = GetEmisorId();
        var resultado = await _proveedorService.ObtenerPorNITAsync(nit, emisorId);
        if (resultado == null)
            return NotFound(new { error = "Proveedor no encontrado" });

        return Ok(resultado);
    }

    /// <summary>
    /// Listar proveedores con paginación
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "SuperAdmin,EmisorAdmin,GerenteSucursal,Contador,Auditor")]
    public async Task<ActionResult<PagedResult<ProveedorDto>>> Listar(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20,
        [FromQuery] string? filtro = null,
        [FromQuery] bool? soloActivos = true)
    {
        var emisorId = GetEmisorId();
        var resultado = await _proveedorService.ListarAsync(emisorId, pagina, tamanoPagina, filtro, soloActivos);
        return Ok(resultado);
    }

    /// <summary>
    /// Cambiar estado de un proveedor (activar/desactivar)
    /// </summary>
    [HttpPatch("{id}/estado")]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Contador")]
    public async Task<ActionResult> CambiarEstado(int id, [FromBody] bool activo)
    {
        var resultado = await _proveedorService.CambiarEstadoAsync(id, activo);
        if (!resultado)
            return NotFound(new { error = "Proveedor no encontrado" });

        return NoContent();
    }

    /// <summary>
    /// Eliminar un proveedor
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Contador")]
    public async Task<ActionResult> Eliminar(int id)
    {
        try
        {
            var resultado = await _proveedorService.EliminarAsync(id);
            if (!resultado)
                return NotFound(new { error = "Proveedor no encontrado" });

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
