using FraFactu.Infrastructure.Services; // Para ICajaService
using FraFactu.Domain.Entities;
using FraFactu.API.Extensions;
using FraFactu.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FraFactu.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CajasController : ControllerBase
{
    private readonly ICajaService _cajaService;

    public CajasController(ICajaService cajaService)
    {
        _cajaService = cajaService;
    }

    [HttpGet]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Cajero,Auditor,Contador")]
    public async Task<ActionResult<List<CajaDto>>> GetAll([FromQuery] int? sucursalId, [FromQuery] bool? soloActivos)
    {
        var emisorId = User.GetEmisorId();

        var rol = ScopeHelper.GetRolFromClaims(User);
        int? sucursalIdFinal = null;
        if (ScopeHelper.RequiereRestriccionSucursal(rol) && !ScopeHelper.GetAccesoTodasSucursales(User))
        {
            var userSucursalIds = ScopeHelper.GetSucursalIdsFromClaims(User);
            if (userSucursalIds.Count == 0)
                return BadRequest(new { message = "Usuario con rol restringido no tiene sucursal asignada" });

            if (sucursalId.HasValue)
            {
                if (!userSucursalIds.Contains(sucursalId.Value))
                    return Forbid();
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

        var cajas = await _cajaService.GetAllAsync(emisorId, sucursalIdFinal);

        if (soloActivos.HasValue && soloActivos.Value)
        {
            cajas = cajas.Where(c => c.Activo).ToList();
        }

        return Ok(cajas.Select(c => new CajaDto(c.Id, c.SucursalId, c.Codigo, c.Nombre, c.Activo, c.CodPuntoVenta, c.CodPuntoVentaMH, c.FechaCreacion)));
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Cajero,Auditor,Contador")]
    public async Task<ActionResult<CajaDto>> GetById(int id)
    {
        var caja = await _cajaService.GetByIdAsync(id);
        if (caja == null) return NotFound();

        // Validar emisor
        if (caja.Sucursal.EmisorId != User.GetEmisorId()) return NotFound();

        if (!ScopeHelper.ValidarAccesoSucursal(User, caja.SucursalId))
            return StatusCode(403, new { error = "No tiene permiso para ver cajas de esta sucursal" });

        return Ok(new CajaDto(caja.Id, caja.SucursalId, caja.Codigo, caja.Nombre, caja.Activo, caja.CodPuntoVenta, caja.CodPuntoVentaMH, caja.FechaCreacion));
    }

    [HttpPost]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal")]
    public async Task<ActionResult<CajaDto>> Create(CreateCajaRequest request)
    {
        var emisorId = User.GetEmisorId();

        if (!ScopeHelper.ValidarAccesoSucursal(User, request.SucursalId))
            return StatusCode(403, new { error = "No tiene permiso para crear cajas en esta sucursal" });

        var caja = new Caja
        {
            SucursalId = request.SucursalId,
            Codigo = request.Codigo ?? string.Empty,
            Nombre = request.Nombre,
            CodPuntoVenta = request.CodPuntoVenta ?? string.Empty,
            CodPuntoVentaMH = request.CodPuntoVentaMH ?? string.Empty
        };

        try
        {
            // Ahora pasamos el emisorId para validar propiedad de la sucursal
            var created = await _cajaService.CreateAsync(caja, emisorId);
            return CreatedAtAction(nameof(GetById), new { id = created.Id },
                new CajaDto(created.Id, created.SucursalId, created.Codigo, created.Nombre, created.Activo, created.CodPuntoVenta, created.CodPuntoVentaMH, created.FechaCreacion));
        }
        catch (Exception ex) // Catch all para simpleza aqui, idealmente especificas
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal")]
    public async Task<ActionResult<CajaDto>> Update(int id, UpdateCajaRequest request)
    {
        // Validar ownership
        var existing = await _cajaService.GetByIdAsync(id);
        if (existing == null) return NotFound();
        if (existing.Sucursal.EmisorId != User.GetEmisorId()) return NotFound();

        if (!ScopeHelper.ValidarAccesoSucursal(User, existing.SucursalId))
            return StatusCode(403, new { error = "No tiene permiso para modificar cajas de esta sucursal" });

        var toUpdate = new Caja
        {
            Codigo = request.Codigo,
            Nombre = request.Nombre,
            CodPuntoVenta = request.CodPuntoVenta ?? string.Empty,
            CodPuntoVentaMH = request.CodPuntoVentaMH ?? string.Empty
        };

        try
        {
            var updated = await _cajaService.UpdateAsync(id, toUpdate);
            return Ok(new CajaDto(updated.Id, updated.SucursalId, updated.Codigo, updated.Nombre, updated.Activo, updated.CodPuntoVenta, updated.CodPuntoVentaMH, updated.FechaCreacion));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "EmisorAdmin")]
    public async Task<ActionResult> Delete(int id)
    {
        var existing = await _cajaService.GetByIdAsync(id);
        if (existing == null) return NotFound();
        if (existing.Sucursal.EmisorId != User.GetEmisorId()) return NotFound();

        if (!ScopeHelper.ValidarAccesoSucursal(User, existing.SucursalId))
            return StatusCode(403, new { error = "No tiene permiso para eliminar cajas de esta sucursal" });

        try
        {
            await _cajaService.DeleteAsync(id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id}/toggle-active")]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal")]
    public async Task<ActionResult> ToggleActive(int id)
    {
        // Validar ownership
        var existing = await _cajaService.GetByIdAsync(id);
        if (existing == null) return NotFound();
        if (existing.Sucursal.EmisorId != User.GetEmisorId()) return NotFound();

        if (!ScopeHelper.ValidarAccesoSucursal(User, existing.SucursalId))
            return StatusCode(403, new { error = "No tiene permiso para modificar cajas de esta sucursal" });

        try
        {
            var updated = await _cajaService.ToggleActiveAsync(id);
            return Ok(new { id, activo = updated });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Asignar una caja a un usuario (cajero)
    /// </summary>
    [HttpPost("asignar-usuario")]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal")]
    public async Task<ActionResult> AsignarCajaAUsuario(AsignarCajaUsuarioRequest request)
    {
        try
        {
            // Validar acceso a la sucursal de la caja
            var caja = await _cajaService.GetByIdAsync(request.CajaId);
            if (caja == null)
                return NotFound(new { message = $"Caja {request.CajaId} no encontrada" });

            if (!ScopeHelper.ValidarAccesoSucursal(User, caja.SucursalId))
                return StatusCode(403, new { error = "No tiene permiso para asignar cajas de esta sucursal" });

            await _cajaService.AsignarCajaAUsuarioAsync(request.UsuarioId, request.CajaId, User.GetUserId());
            return Ok(new { message = "Caja asignada al usuario correctamente" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Desasignar la caja actual de un usuario (cajero)
    /// </summary>
    [HttpPost("desasignar-usuario")]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal")]
    public async Task<ActionResult> DesasignarCajaDeUsuario(DesasignarCajaUsuarioRequest request)
    {
        try
        {
            await _cajaService.DesasignarCajaDeUsuarioAsync(request.UsuarioId, User.GetUserId());
            return Ok(new { message = "Caja desasignada del usuario correctamente" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}

public record CajaDto(int Id, int SucursalId, string Codigo, string Nombre, bool Activo, string CodPuntoVenta, string CodPuntoVentaMH, DateTime FechaCreacion);
public record CreateCajaRequest(int SucursalId, string? Codigo, string Nombre, string? CodPuntoVenta, string? CodPuntoVentaMH);
public record UpdateCajaRequest(string Codigo, string Nombre, string? CodPuntoVenta, string? CodPuntoVentaMH);
public record AsignarCajaUsuarioRequest(int UsuarioId, int CajaId);
public record DesasignarCajaUsuarioRequest(int UsuarioId);
