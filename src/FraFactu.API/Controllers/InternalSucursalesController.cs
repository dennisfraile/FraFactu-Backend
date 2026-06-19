using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using FraFactu.API.Extensions;
using FraFactu.Application.DTOs;
using FraFactu.Infrastructure.Persistence;

namespace FraFactu.API.Controllers;

/// <summary>
/// Plan B Hub-as-Emisor — Endpoints internos para vincular Sucursales del Hub
/// con Sucursales de Smartix.
///
/// Auth: JWT compartido. Acepta SuperAdmin (cross-Grupo) y UsuarioCompartido
/// (cross-Hub-en-Grupo). AdminOrg paso a Hub-unico (2026-05-21) y ya NO puede
/// invocar estos endpoints. El controller valida que el Hub del Emisor este
/// en `hubs_accesibles` para UsuarioCompartido (SuperAdmin pasa libre).
/// </summary>
[ApiController]
[Route("api/internal/sucursales")]
[Authorize]
public class InternalSucursalesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public InternalSucursalesController(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Lista Sucursales de un Emisor de Smartix para el modal "Vincular con Smartix"
    /// de SucursalesPage del Hub. `soloLibres=true` (default) excluye las ya vinculadas.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SucursalParaVincularDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<SucursalParaVincularDto>>> Listar(
        [FromQuery] int emisorId,
        [FromQuery] bool soloLibres = true,
        CancellationToken ct = default)
    {
        if (!User.IsHubCrossHubScopeRole()) return Forbid();
        if (emisorId <= 0) return BadRequest(new { error = "emisorId es requerido." });

        // Scope check para UsuarioCompartido: el Emisor debe estar vinculado a un Hub accesible.
        if (!User.IsHubSuperAdmin())
        {
            var hubDelEmisor = await _db.Emisores
                .AsNoTracking()
                .Where(e => e.Id == emisorId)
                .Select(e => e.HubId)
                .FirstOrDefaultAsync(ct);

            if (hubDelEmisor is null) return Forbid();

            var hubsAccesibles = User.GetHubsAccesibles();
            if (!hubsAccesibles.Contains(hubDelEmisor.Value)) return Forbid();
        }

        var query = _db.Sucursales.AsNoTracking().Where(s => s.EmisorId == emisorId && s.Activo);
        if (soloLibres) query = query.Where(s => s.HubSucursalId == null);

        var items = await query
            .OrderBy(s => s.Nombre)
            .Select(s => new SucursalParaVincularDto
            {
                Id = s.Id,
                EmisorId = s.EmisorId,
                Nombre = s.Nombre,
                Codigo = s.Codigo,
                CodigoEstablecimiento = s.CodigoEstablecimiento,
                HubSucursalId = s.HubSucursalId
            })
            .ToListAsync(ct);

        return Ok(items);
    }

    /// <summary>
    /// Setea o quita el vínculo Sucursal Smartix ↔ Sucursal Hub.
    /// Body: `{ hubSucursalId: int | null }`. Acepta SuperAdmin o AdminOrg del
    /// Grupo del Hub padre. Devuelve 409 si el HubSucursalId ya está tomado.
    ///
    /// Al VINCULAR devuelve 200 OK con <see cref="SucursalFiscalSnapshotDto"/> para
    /// que SmartHub precargue los campos fiscales vacios de la Sucursal del Hub.
    /// Al DESVINCULAR devuelve 204 NoContent.
    /// </summary>
    [HttpPatch("{id:int}/hub-link")]
    [ProducesResponseType(typeof(SucursalFiscalSnapshotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SetHubLink(int id, [FromBody] PatchSucursalHubLinkRequest body, CancellationToken ct)
    {
        if (!User.IsHubCrossHubScopeRole()) return Forbid();

        // I3 review fix: solo sucursales activas, igual que Listar.
        // Include de catalogos para poder armar el snapshot sin re-query.
        var sucursal = await _db.Sucursales
            .Include(s => s.TipoEstablecimiento)
            .Include(s => s.Departamento)
            .Include(s => s.Municipio)
            .Include(s => s.Distrito)
            .FirstOrDefaultAsync(s => s.Id == id && s.Activo, ct);
        if (sucursal is null) return NotFound(new { error = $"Sucursal {id} no existe." });

        // UsuarioCompartido: validar que la Sucursal pertenece a un Emisor vinculado a un Hub accesible.
        if (!User.IsHubSuperAdmin())
        {
            var hubDelEmisor = await _db.Emisores
                .AsNoTracking()
                .Where(e => e.Id == sucursal.EmisorId)
                .Select(e => e.HubId)
                .FirstOrDefaultAsync(ct);

            if (hubDelEmisor is null) return Forbid();

            var hubsAccesibles = User.GetHubsAccesibles();
            if (!hubsAccesibles.Contains(hubDelEmisor.Value)) return Forbid();
        }

        if (body.HubSucursalId.HasValue)
        {
            var tomado = await _db.Sucursales
                .AnyAsync(s => s.HubSucursalId == body.HubSucursalId.Value && s.Id != id, ct);
            if (tomado)
                return Conflict(new { error = $"Sucursal Hub {body.HubSucursalId} ya está vinculada a otra Sucursal Smartix. Desvincular primero." });
        }

        sucursal.HubSucursalId = body.HubSucursalId;
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg && pg.SqlState == "23505")
        {
            return Conflict(new { error = $"Sucursal Hub {body.HubSucursalId} ya está vinculada a otra Sucursal Smartix. Desvincular primero." });
        }

        if (!body.HubSucursalId.HasValue) return NoContent();

        var snapshot = new SucursalFiscalSnapshotDto
        {
            Id = sucursal.Id,
            EmisorId = sucursal.EmisorId,
            Nombre = sucursal.Nombre,
            CodigoEstablecimientoMH = sucursal.CodigoEstablecimiento,
            CodTipoEstablecimientoMH = sucursal.TipoEstablecimiento?.Codigo,
            CodDepartamentoMH = sucursal.Departamento?.Codigo,
            CodMunicipioMH = sucursal.Municipio?.Codigo,
            CodDistritoMH = sucursal.Distrito?.Codigo,
            Direccion = sucursal.Direccion,
            Telefono = sucursal.Telefono,
            CorreoElectronico = sucursal.CorreoElectronico,
            ContingenciaNombreResponsable = sucursal.ContingenciaNombreResponsable,
            ContingenciaTipoDocResponsable = sucursal.ContingenciaTipoDocResponsable,
            ContingenciaNumeroDocResponsable = sucursal.ContingenciaNumeroDocResponsable
        };

        return Ok(snapshot);
    }
}
