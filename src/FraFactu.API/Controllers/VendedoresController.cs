using FraFactu.Application.Interfaces;
using FraFactu.Domain.Entities;
using FraFactu.API.Extensions;
using FraFactu.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FraFactu.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VendedoresController : ControllerBase
{
    private readonly IVendedorService _vendedorService;

    public VendedoresController(IVendedorService vendedorService)
    {
        _vendedorService = vendedorService;
    }

    [HttpGet]
    [Authorize(Roles = "SuperAdmin,EmisorAdmin,GerenteSucursal,Cajero,Auditor,Contador")]
    public async Task<ActionResult<List<VendedorDto>>> GetAll([FromQuery] bool activeOnly = true, [FromQuery] int? sucursalId = null)
    {
        var emisorId = User.GetEmisorId();

        var rol = ScopeHelper.GetRolFromClaims(User);
        int? sucursalIdFinal = null;
        List<int>? sucursalIdsFinal = null;

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
            else
            {
                sucursalIdsFinal = userSucursalIds;
            }
        }
        else if (sucursalId.HasValue)
        {
            sucursalIdFinal = sucursalId.Value;
        }

        var vendedores = await _vendedorService.GetAllAsync(emisorId, activeOnly, sucursalIdFinal, sucursalIdsFinal);

        return Ok(vendedores.Select(v => MapToDto(v)));
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "SuperAdmin,EmisorAdmin,GerenteSucursal,Cajero,Auditor,Contador")]
    public async Task<ActionResult<VendedorDto>> GetById(int id)
    {
        var vendedor = await _vendedorService.GetByIdAsync(id);
        if (vendedor == null) return NotFound();

        // Validar emisor (seguridad básica)
        var emisorId = User.GetEmisorId();
        if (vendedor.EmisorId != emisorId) return NotFound();

        // Validar acceso a sucursales del vendedor
        if (!vendedor.AccesoTodasSucursales && vendedor.VendedorSucursales.Any())
        {
            var hasAccess = vendedor.VendedorSucursales
                .Any(vs => ScopeHelper.ValidarAccesoSucursal(User, vs.SucursalId));
            if (!hasAccess)
                return StatusCode(403, new { error = "No tiene permiso para ver vendedores de esta sucursal" });
        }

        return Ok(MapToDto(vendedor));
    }

    [HttpPost]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal")]
    public async Task<ActionResult<VendedorDto>> Create(CreateVendedorRequest request)
    {
        var emisorId = User.GetEmisorId();
        var sucursalIds = request.SucursalIds ?? new List<int>();

        // Validar acceso a cada sucursal
        if (!request.AccesoTodasSucursales)
        {
            foreach (var sid in sucursalIds)
            {
                if (!ScopeHelper.ValidarAccesoSucursal(User, sid))
                    return StatusCode(403, new { error = $"No tiene permiso para asignar vendedores a la sucursal {sid}" });
            }
        }

        var vendedor = new Vendedor
        {
            Codigo = request.Codigo ?? string.Empty,
            Nombre = request.Nombre,
            PorcentajeComision = request.PorcentajeComision,
            EmisorId = emisorId
        };

        try
        {
            var created = await _vendedorService.CreateAsync(vendedor, request.AccesoTodasSucursales, sucursalIds);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, MapToDto(created));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "EmisorAdmin,GerenteSucursal")]
    public async Task<ActionResult<VendedorDto>> Update(int id, UpdateVendedorRequest request)
    {
        var existing = await _vendedorService.GetByIdAsync(id);
        if (existing == null) return NotFound();
        if (existing.EmisorId != User.GetEmisorId()) return NotFound();

        // Validar acceso a sucursales actuales del vendedor
        if (!existing.AccesoTodasSucursales && existing.VendedorSucursales.Any())
        {
            var hasAccess = existing.VendedorSucursales
                .Any(vs => ScopeHelper.ValidarAccesoSucursal(User, vs.SucursalId));
            if (!hasAccess)
                return StatusCode(403, new { error = "No tiene permiso para modificar vendedores de esta sucursal" });
        }

        var sucursalIds = request.SucursalIds ?? new List<int>();
        if (!request.AccesoTodasSucursales)
        {
            foreach (var sid in sucursalIds)
            {
                if (!ScopeHelper.ValidarAccesoSucursal(User, sid))
                    return StatusCode(403, new { error = $"No tiene permiso para asignar vendedores a la sucursal {sid}" });
            }
        }

        var toUpdate = new Vendedor
        {
            Codigo = request.Codigo,
            Nombre = request.Nombre,
            PorcentajeComision = request.PorcentajeComision
        };

        try
        {
            var updated = await _vendedorService.UpdateAsync(id, toUpdate, request.AccesoTodasSucursales, sucursalIds);
            return Ok(MapToDto(updated));
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
        var existing = await _vendedorService.GetByIdAsync(id);
        if (existing == null) return NotFound();
        if (existing.EmisorId != User.GetEmisorId()) return NotFound();

        // Validar acceso a sucursales del vendedor
        if (!existing.AccesoTodasSucursales && existing.VendedorSucursales.Any())
        {
            var hasAccess = existing.VendedorSucursales
                .Any(vs => ScopeHelper.ValidarAccesoSucursal(User, vs.SucursalId));
            if (!hasAccess)
                return StatusCode(403, new { error = "No tiene permiso para modificar vendedores de esta sucursal" });
        }

        await _vendedorService.ToggleActiveAsync(id);
        return NoContent();
    }

    private static VendedorDto MapToDto(Vendedor v) => new VendedorDto(
        v.Id,
        v.Codigo,
        v.Nombre,
        v.Activo,
        v.PorcentajeComision,
        v.AccesoTodasSucursales,
        v.VendedorSucursales?.Select(vs => new SucursalAsignadaVendedorDto(vs.SucursalId, vs.Sucursal?.Nombre ?? string.Empty)).ToList() ?? new(),
        v.FechaCreacion);
}

public record VendedorDto(
    int Id,
    string Codigo,
    string Nombre,
    bool Activo,
    decimal PorcentajeComision,
    bool AccesoTodasSucursales,
    List<SucursalAsignadaVendedorDto> Sucursales,
    DateTime FechaCreacion);

public record SucursalAsignadaVendedorDto(int Id, string Nombre);

public record CreateVendedorRequest(
    string? Codigo,
    string Nombre,
    decimal PorcentajeComision = 0,
    bool AccesoTodasSucursales = false,
    List<int>? SucursalIds = null);

public record UpdateVendedorRequest(
    string Codigo,
    string Nombre,
    decimal PorcentajeComision = 0,
    bool AccesoTodasSucursales = false,
    List<int>? SucursalIds = null);
