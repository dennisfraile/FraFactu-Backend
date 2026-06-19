using FraFactu.Application.DTOs.Hub;

namespace FraFactu.Application.Interfaces;

/// <summary>
/// Aplica al estado local de Smartix las mutaciones de gestión de usuarios que
/// dispara el Hub vía webhooks (F2 del plan de centralización). Todos los métodos
/// son idempotentes: re-aplicar la misma acción no produce cambios ni errores.
/// </summary>
public interface IHubUsuarioSyncService
{
    Task<HubSyncResult> AsignarAsync(HubUsuarioWebhookDto webhook, CancellationToken ct = default);
    Task<HubSyncResult> CambiarRolAsync(HubUsuarioWebhookDto webhook, CancellationToken ct = default);
    Task<HubSyncResult> DeshabilitarAsync(int hubUsuarioId, CancellationToken ct = default);
    Task<HubSyncResult> HabilitarAsync(int hubUsuarioId, CancellationToken ct = default);
    Task<HubSyncResult> CambiarAsignacionSucursalAsync(HubSucursalAsignacionDto payload, CancellationToken ct = default);
    Task<List<HubUsuarioRolInfoDto>> ListarUsuariosRolesAsync(CancellationToken ct = default);
}

/// <summary>
/// Resultado uniforme para todas las mutaciones webhook. Permite al controller
/// mapear a HTTP coherentemente sin lanzar excepciones para flujos de negocio.
/// </summary>
public class HubSyncResult
{
    public bool Exito { get; init; }
    public bool Cambio { get; init; }
    public string? Mensaje { get; init; }
    public HubSyncStatus Status { get; init; }

    public static HubSyncResult Ok(bool cambio, string? mensaje = null) =>
        new() { Exito = true, Cambio = cambio, Mensaje = mensaje, Status = HubSyncStatus.Ok };

    public static HubSyncResult BadRequest(string mensaje) =>
        new() { Exito = false, Cambio = false, Mensaje = mensaje, Status = HubSyncStatus.BadRequest };

    public static HubSyncResult Conflict(string mensaje) =>
        new() { Exito = false, Cambio = false, Mensaje = mensaje, Status = HubSyncStatus.Conflict };

    public static HubSyncResult NotFound(string mensaje) =>
        new() { Exito = false, Cambio = false, Mensaje = mensaje, Status = HubSyncStatus.NotFound };
}

public enum HubSyncStatus
{
    Ok,
    BadRequest,
    NotFound,
    Conflict
}
