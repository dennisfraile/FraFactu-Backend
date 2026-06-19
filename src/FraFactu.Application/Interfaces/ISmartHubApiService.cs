using FraFactu.Application.DTOs.Auth;
using FraFactu.Application.DTOs.Hub;

namespace FraFactu.Application.Interfaces
{
    /// <summary>
    /// Cliente HTTP outgoing hacia SmartHub. Encapsula las llamadas que Smartix
    /// hace al Hub (hoy: validacion del exchange code para SSO + refresh fiscal).
    /// </summary>
    public interface ISmartHubApiService
    {
        /// <summary>
        /// Llama POST {BaseUrl}/api/auth/validate-code con header X-Api-Key
        /// y devuelve la informacion del usuario (Hub Id, email, rol, sucursales,
        /// org activa). Retorna null si el code es invalido, expiro, o si la
        /// llamada fallo (timeout/5xx). El controller traduce null a 401.
        /// </summary>
        Task<HubUserInfoDto?> ValidateExchangeCodeAsync(string code, CancellationToken ct = default);

        /// <summary>
        /// Plan B Hub-as-Emisor — Fase 2 Task 16 (Bloque D).
        /// Llama GET {BaseUrl}/api/internal/hubs/{hubId}/fiscal-payload con X-Api-Key.
        /// Best-effort: si SmartHub no responde, devuelve 400/404, timeout o network
        /// error, retorna null y el login continua con el cache local del Emisor.
        /// </summary>
        Task<HubFiscalPayloadDto?> GetFiscalPayloadForHubAsync(int hubId, CancellationToken ct = default);

        /// <summary>
        /// Llama GET {BaseUrl}/api/auth/token-version/{usuarioHubId} con X-Api-Key
        /// y devuelve el TokenVersion vigente del usuario en SmartHub. Si el caller
        /// detecta mismatch con el token_version del JWT, debe rechazar la request
        /// con 401 (sesion revocada — el usuario debe re-SSO). Cachea 60s local
        /// para reducir round-trips. Devuelve null si el Hub no responde (caller
        /// debe fail-open para no romper el sistema ante un Hub caido).
        /// </summary>
        Task<int?> GetTokenVersionAsync(int usuarioHubId);

        /// <summary>
        /// Bug #3 (UsuarioCompartido / Plan B Hub-as-Emisor) — 2026-05-29.
        /// Llama GET {BaseUrl}/api/internal/usuarios/{hubUsuarioId}/emisores-accesibles
        /// con X-Api-Key y devuelve la lista fresca de SmartixEmisorId que el
        /// usuario puede facturar segun SmartHub en este instante. Se usa para
        /// auto-sanar sesiones cuyo claim <c>emisores_accesibles</c> quedo stale
        /// (admin acaba de asignar/quitar Hubs, o el SSO no propago bien la lista
        /// y necesitamos refrescarla sin re-SSO).
        ///
        /// Best-effort: si el Hub no responde (timeout, 5xx, ApiKey no configurada),
        /// retorna null y el caller mantiene el claim viejo del JWT — preferimos
        /// fail-open antes que romper la sesion ante un Hub caido.
        /// </summary>
        Task<List<int>?> GetEmisoresAccesiblesAsync(int hubUsuarioId, CancellationToken ct = default);

        /// <summary>
        /// Bug #3 fix de raiz (UsuarioCompartido / Plan B Hub-as-Emisor) — 2026-05-29.
        /// Llama GET {BaseUrl}/api/internal/usuarios/lookup-by-email?email={email}
        /// con X-Api-Key y devuelve los 3 datos Hub que Smartix necesita para
        /// emitir un JWT alineado: <c>HubUsuarioId</c>, <c>TokenVersion</c> y
        /// <c>EmisoresAccesibles</c>.
        ///
        /// Justificacion: los paths de Smartix que NO son SSO Hub (login local,
        /// Google directo, Supabase Sync) historicamente no propagaban estos
        /// claims al GenerateJwtToken — un usuario con sesion Smartix via Supabase
        /// queda con <c>usuario_hub_id</c> ausente y <c>emisores_accesibles</c>
        /// como fallback <c>[EmisorId]</c>, lo cual rompe el wizard cross-Emisor.
        ///
        /// Best-effort: si SmartHub no responde, o el email no esta registrado,
        /// retorna null y el caller emite el JWT sin claims Hub (back-compat con
        /// el comportamiento previo — no peor que estaba antes).
        /// </summary>
        Task<HubUserClaimsDto?> LookupHubUserByEmailAsync(string email, CancellationToken ct = default);
    }
}
