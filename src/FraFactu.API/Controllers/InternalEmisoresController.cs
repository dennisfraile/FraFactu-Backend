using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using FraFactu.API.Extensions;
using FraFactu.Application.DTOs;
using FraFactu.Infrastructure.Persistence;

namespace FraFactu.API.Controllers;

/// <summary>
/// Plan B Hub-as-Emisor — Endpoints internos consumidos por SmartHub para
/// gestionar la vinculación cross-app Hub ↔ Emisor de Smartix.
///
/// Auth: JWT compartido entre SmartHub y Smartix (mismo SecretKey). El claim
/// "rol" debe ser "SuperAdmin" — un Emisor de Smartix puede pertenecer a un
/// Grupo distinto al del admin que lo lista, por eso AdminOrg NO autoriza.
/// </summary>
[ApiController]
[Route("api/internal/emisores")]
[Authorize]
public class InternalEmisoresController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public InternalEmisoresController(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Lista Emisores de Smartix para el modal "Vincular con Smartix" del Hub
    /// en SmartHub. `soloLibres=true` (default) deja afuera los Emisores ya
    /// vinculados a un Hub. `q` filtra por NIT o NombreRazonSocial.
    /// Solo SuperAdmin.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EmisorParaVincularDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<EmisorParaVincularDto>>> Listar(
        [FromQuery] bool soloLibres = true,
        [FromQuery] string? q = null,
        CancellationToken ct = default)
    {
        if (!User.IsHubSuperAdmin()) return Forbid();

        var query = _db.Emisores.AsNoTracking().Where(e => e.Activo);
        if (soloLibres) query = query.Where(e => e.HubId == null);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(e =>
                EF.Functions.ILike(e.Nit, $"%{term}%")
                || EF.Functions.ILike(e.NombreRazonSocial, $"%{term}%"));
        }

        var items = await query
            .OrderBy(e => e.NombreRazonSocial)
            .Take(50)
            .Select(e => new EmisorParaVincularDto
            {
                Id = e.Id,
                Nit = e.Nit,
                NombreRazonSocial = e.NombreRazonSocial,
                NombreComercial = e.NombreComercial,
                HubId = e.HubId
            })
            .ToListAsync(ct);

        return Ok(items);
    }

    /// <summary>
    /// Setea o quita el vínculo Hub ↔ Emisor. Body: `{ hubId: int | null }`.
    /// Solo SuperAdmin. Rechaza con 409 si el HubId ya está tomado por otro Emisor
    /// (el unique index lo previene a nivel BD pero respondemos con mensaje claro).
    ///
    /// Al VINCULAR (hubId != null) devuelve 200 OK con <see cref="EmisorFiscalSnapshotDto"/>
    /// para que SmartHub precargue los campos fiscales vacios del Hub recien vinculado
    /// y el admin no tenga que tipear los datos dos veces. Al DESVINCULAR (hubId = null)
    /// devuelve 204 NoContent (no hay datos que migrar de vuelta).
    /// </summary>
    [HttpPatch("{id:int}/hub-link")]
    [ProducesResponseType(typeof(EmisorFiscalSnapshotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SetHubLink(int id, [FromBody] PatchEmisorHubLinkRequest body, CancellationToken ct)
    {
        if (!User.IsHubSuperAdmin()) return Forbid();

        // I3 review fix: solo emisores activos. Pretendemos que inactivos no existen
        // para que SetHubLink/Listar tengan superficie uniforme.
        // Include de los catalogos para poder armar el snapshot fiscal sin re-query.
        var emisor = await _db.Emisores
            .Include(e => e.TipoEstablecimiento)
            .Include(e => e.Departamento)
            .Include(e => e.Municipio)
            .Include(e => e.Distrito)
            .FirstOrDefaultAsync(e => e.Id == id && e.Activo, ct);
        if (emisor is null) return NotFound(new { error = $"Emisor {id} no existe." });

        if (body.HubId.HasValue)
        {
            // Pre-check best-effort. La race entre dos PATCHes concurrentes la captura
            // el unique filtered index a nivel BD (Postgres 23505); el catch de
            // SaveChanges abajo la traduce al mismo shape de Conflict.
            var tomado = await _db.Emisores
                .AnyAsync(e => e.HubId == body.HubId.Value && e.Id != id, ct);
            if (tomado)
                return Conflict(new { error = $"Hub {body.HubId} ya está vinculado a otro Emisor. Desvincular primero." });
        }

        emisor.HubId = body.HubId;
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg && pg.SqlState == "23505")
        {
            // Race: otro request ganó el unique. Devolvemos 409 con el mismo shape
            // que la pre-check para que el FE no maneje dos formatos.
            return Conflict(new { error = $"Hub {body.HubId} ya está vinculado a otro Emisor. Desvincular primero." });
        }

        if (!body.HubId.HasValue) return NoContent();

        // Vinculado: armar snapshot desde la entidad ya cargada (incluye los catalogos).
        var snapshot = new EmisorFiscalSnapshotDto
        {
            Id = emisor.Id,
            Nit = emisor.Nit,
            Nrc = emisor.Nrc,
            NombreRazonSocial = emisor.NombreRazonSocial,
            NombreComercial = emisor.NombreComercial,
            CodigoActividad = emisor.CodigoActividad,
            DescripcionActividad = emisor.DescripcionActividad,
            CodTipoEstablecimientoMH = emisor.TipoEstablecimiento?.Codigo,
            CodDepartamentoMH = emisor.Departamento?.Codigo,
            CodMunicipioMH = emisor.Municipio?.Codigo,
            CodDistritoMH = emisor.Distrito?.Codigo,
            Direccion = emisor.Direccion,
            Telefono = emisor.Telefono,
            CorreoElectronico = emisor.CorreoElectronico
        };

        return Ok(snapshot);
    }

    // F3: el toggle de SmartInventory se movio a SyncInventoryAppController
    // para usar el patron server-to-server (X-Api-Key) consistente con el
    // resto de webhooks SmartHub -> Smartix. Este controller queda solo para
    // operaciones JWT del UI del Hub (vincular emisor, listar libres).
}
