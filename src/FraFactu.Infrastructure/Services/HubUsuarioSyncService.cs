using FraFactu.Application.DTOs.Hub;
using FraFactu.Application.Interfaces;
using FraFactu.Domain.Entities;
using FraFactu.Domain.Enums;
using FraFactu.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FraFactu.Infrastructure.Services;

/// <summary>
/// Aplica al estado local de Smartix las mutaciones que dispara SmartHub vía
/// webhooks <c>/api/hub/*</c> (F2 plan centralización). Todas las operaciones
/// son idempotentes y devuelven HubSyncResult con un Status mapeable a HTTP.
/// </summary>
public class HubUsuarioSyncService : IHubUsuarioSyncService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<HubUsuarioSyncService> _logger;

    public HubUsuarioSyncService(ApplicationDbContext context, ILogger<HubUsuarioSyncService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<HubSyncResult> AsignarAsync(HubUsuarioWebhookDto webhook, CancellationToken ct = default)
    {
        if (webhook.HubUsuarioId <= 0)
            return HubSyncResult.BadRequest("HubUsuarioId requerido.");
        if (string.IsNullOrWhiteSpace(webhook.Email))
            return HubSyncResult.BadRequest("Email requerido para asignar.");
        if (string.IsNullOrWhiteSpace(webhook.RolEnApp))
            return HubSyncResult.BadRequest("RolEnApp requerido para asignar.");
        if (webhook.OrganizacionId is null or <= 0)
            return HubSyncResult.BadRequest("OrganizacionId (HubId) requerido para asignar.");

        var emisor = await _context.Emisores
            .FirstOrDefaultAsync(e => e.HubId == webhook.OrganizacionId, ct);
        if (emisor is null)
        {
            _logger.LogWarning("[HUB-SYNC asignar] HubId={HubId} sin Emisor vinculado", webhook.OrganizacionId);
            return HubSyncResult.Conflict(
                $"El Hub {webhook.OrganizacionId} no está vinculado a ningún Emisor en Smartix.");
        }

        var rol = await _context.Roles.FirstOrDefaultAsync(r => r.Nombre == webhook.RolEnApp, ct);
        if (rol is null)
        {
            return HubSyncResult.BadRequest(
                $"Rol '{webhook.RolEnApp}' no existe en el catálogo de Smartix.");
        }

        // Buscar por HubUsuarioId; fallback por Email para enlazar cuentas pre-SSO.
        // El match por email es case-insensitive: Postgres compara strings
        // case-sensitive por defecto, asi que sin normalizar "Jimenez@x" no
        // matchearia "jimenez@x" -> se crearia un usuario duplicado o se perderia
        // el enlace. ToLower() se traduce a LOWER() en SQL.
        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.HubUsuarioId == webhook.HubUsuarioId, ct);
        if (usuario is null)
        {
            var emailNorm = webhook.Email.ToLower();
            usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email.ToLower() == emailNorm, ct);
            if (usuario is not null)
                usuario.HubUsuarioId = webhook.HubUsuarioId;
        }

        bool cambio;
        if (usuario is null)
        {
            usuario = new Usuario
            {
                Email = webhook.Email,
                NombreCompleto = string.IsNullOrWhiteSpace(webhook.NombreCompleto)
                    ? webhook.Email
                    : webhook.NombreCompleto,
                HubUsuarioId = webhook.HubUsuarioId,
                EmisorId = emisor.Id,
                RolId = rol.Id,
                Estado = EstadoUsuario.Activo,
                ProveedorAuth = ProveedorAutenticacion.SmartHub,
                AccesoTodasSucursales = rol.Nombre is "SuperAdmin" or "EmisorAdmin"
            };
            _context.Usuarios.Add(usuario);
            cambio = true;
            _logger.LogInformation(
                "[HUB-SYNC asignar] Usuario creado HubUsuarioId={HId} Email={Email} EmisorId={EId} Rol={Rol}",
                webhook.HubUsuarioId, webhook.Email, emisor.Id, rol.Nombre);
        }
        else
        {
            cambio = false;
            if (usuario.RolId != rol.Id)
            {
                usuario.RolId = rol.Id;
                cambio = true;
            }
            if (usuario.EmisorId != emisor.Id)
            {
                usuario.EmisorId = emisor.Id;
                cambio = true;
            }
            if (usuario.Estado != EstadoUsuario.Activo)
            {
                usuario.Estado = EstadoUsuario.Activo;
                cambio = true;
            }
            if (cambio)
            {
                _logger.LogInformation(
                    "[HUB-SYNC asignar] Usuario id={Id} reasignado a Rol={Rol} EmisorId={EId}",
                    usuario.Id, rol.Nombre, emisor.Id);
            }
        }

        await _context.SaveChangesAsync(ct);
        return HubSyncResult.Ok(cambio);
    }

    public async Task<HubSyncResult> CambiarRolAsync(HubUsuarioWebhookDto webhook, CancellationToken ct = default)
    {
        if (webhook.HubUsuarioId <= 0)
            return HubSyncResult.BadRequest("HubUsuarioId requerido.");
        if (string.IsNullOrWhiteSpace(webhook.RolEnApp))
            return HubSyncResult.BadRequest("RolEnApp requerido para cambiar-rol.");

        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.HubUsuarioId == webhook.HubUsuarioId, ct);
        if (usuario is null)
            return HubSyncResult.NotFound($"Usuario HubUsuarioId={webhook.HubUsuarioId} no existe en Smartix.");

        var rol = await _context.Roles.FirstOrDefaultAsync(r => r.Nombre == webhook.RolEnApp, ct);
        if (rol is null)
            return HubSyncResult.BadRequest($"Rol '{webhook.RolEnApp}' no existe en el catálogo de Smartix.");

        if (usuario.RolId == rol.Id)
        {
            _logger.LogInformation("[HUB-SYNC cambiar-rol] Usuario id={Id} ya tiene Rol={Rol}, no-op",
                usuario.Id, rol.Nombre);
            return HubSyncResult.Ok(cambio: false);
        }

        usuario.RolId = rol.Id;
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("[HUB-SYNC cambiar-rol] Usuario id={Id} → Rol={Rol}", usuario.Id, rol.Nombre);
        return HubSyncResult.Ok(cambio: true);
    }

    public Task<HubSyncResult> DeshabilitarAsync(int hubUsuarioId, CancellationToken ct = default) =>
        ChangeEstadoAsync(hubUsuarioId, EstadoUsuario.Inactivo, "deshabilitar", ct);

    public Task<HubSyncResult> HabilitarAsync(int hubUsuarioId, CancellationToken ct = default) =>
        ChangeEstadoAsync(hubUsuarioId, EstadoUsuario.Activo, "habilitar", ct);

    private async Task<HubSyncResult> ChangeEstadoAsync(int hubUsuarioId, EstadoUsuario nuevoEstado, string accion, CancellationToken ct)
    {
        if (hubUsuarioId <= 0)
            return HubSyncResult.BadRequest("HubUsuarioId requerido.");

        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.HubUsuarioId == hubUsuarioId, ct);
        if (usuario is null)
            return HubSyncResult.NotFound($"Usuario HubUsuarioId={hubUsuarioId} no existe en Smartix.");

        if (usuario.Estado == nuevoEstado)
        {
            _logger.LogInformation("[HUB-SYNC {Accion}] Usuario id={Id} ya está en {Estado}, no-op",
                accion, usuario.Id, nuevoEstado);
            return HubSyncResult.Ok(cambio: false);
        }

        usuario.Estado = nuevoEstado;
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("[HUB-SYNC {Accion}] Usuario id={Id} → {Estado}",
            accion, usuario.Id, nuevoEstado);
        return HubSyncResult.Ok(cambio: true);
    }

    public async Task<HubSyncResult> CambiarAsignacionSucursalAsync(HubSucursalAsignacionDto payload, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(payload.Email))
            return HubSyncResult.BadRequest("Email requerido.");
        if (payload.SucursalId <= 0)
            return HubSyncResult.BadRequest("SucursalId requerido.");
        if (payload.Accion is not ("asignar" or "remover"))
            return HubSyncResult.BadRequest("Accion debe ser 'asignar' o 'remover'.");

        // Match por email case-insensitive (ver nota en AsignarAsync).
        var emailNorm = payload.Email.ToLower();
        var usuario = await _context.Usuarios
            .Include(u => u.UsuarioSucursales)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == emailNorm, ct);
        if (usuario is null)
            return HubSyncResult.NotFound($"Usuario Email={payload.Email} no existe en Smartix.");

        var existeAsignacion = usuario.UsuarioSucursales.Any(us => us.SucursalId == payload.SucursalId);

        if (payload.Accion == "asignar")
        {
            if (existeAsignacion)
                return HubSyncResult.Ok(cambio: false);

            usuario.UsuarioSucursales.Add(new UsuarioSucursal { SucursalId = payload.SucursalId });
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("[HUB-SYNC sucursal] Asignada SucursalId={SId} a Usuario id={UId}",
                payload.SucursalId, usuario.Id);
            return HubSyncResult.Ok(cambio: true);
        }

        // remover
        if (!existeAsignacion)
            return HubSyncResult.Ok(cambio: false);

        var asignacion = usuario.UsuarioSucursales.First(us => us.SucursalId == payload.SucursalId);
        usuario.UsuarioSucursales.Remove(asignacion);
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("[HUB-SYNC sucursal] Removida SucursalId={SId} de Usuario id={UId}",
            payload.SucursalId, usuario.Id);
        return HubSyncResult.Ok(cambio: true);
    }

    public async Task<List<HubUsuarioRolInfoDto>> ListarUsuariosRolesAsync(CancellationToken ct = default)
    {
        // Incluye huérfanos (HubUsuarioId=null) y devuelve Email para que /reconciliar
        // del Hub pueda matchear por email cuentas pre-SSO (F3 plan centralización).
        return await _context.Usuarios
            .Select(u => new HubUsuarioRolInfoDto
            {
                HubUsuarioId = u.HubUsuarioId,
                Email = u.Email,
                RolEnApp = u.Rol.Nombre,
                Estado = u.Estado.ToString()
            })
            .ToListAsync(ct);
    }
}
