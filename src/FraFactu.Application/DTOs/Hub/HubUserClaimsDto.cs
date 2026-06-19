namespace FraFactu.Application.DTOs.Hub
{
    /// <summary>
    /// Bug #3 (UsuarioCompartido / Plan B Hub-as-Emisor) — 2026-05-29.
    /// Respuesta del internal lookup <c>GET /api/internal/usuarios/lookup-by-email</c>
    /// de SmartHub. Trae los 3 datos que Smartix usa para emitir un JWT alineado
    /// con Hub cuando la sesion NO viene del SSO (login local, Google directo,
    /// Supabase Sync, o refresh con claim ausente).
    /// </summary>
    public class HubUserClaimsDto
    {
        public int HubUsuarioId { get; set; }
        public int TokenVersion { get; set; }
        public List<int> EmisoresAccesibles { get; set; } = new();
    }
}
