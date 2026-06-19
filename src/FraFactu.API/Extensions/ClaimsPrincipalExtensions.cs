using System.Security.Claims;
using System.Text.Json;

namespace FraFactu.API.Extensions
{
    /// <summary>
    /// Métodos de extensión para extraer claims del usuario autenticado
    /// </summary>
    public static class ClaimsPrincipalExtensions
    {
        // ============================================
        // Claims emitidos por SmartHub (cross-app JWT)
        // ============================================

        /// <summary>
        /// Rol del usuario en SmartHub (claim "rol"): SuperAdmin, AdminOrg, etc.
        /// Vacío si el JWT no fue emitido por el Hub.
        /// </summary>
        public static string GetHubRol(this ClaimsPrincipal user)
        {
            return user.FindFirst("rol")?.Value ?? string.Empty;
        }

        /// <summary>
        /// True si el JWT del Hub indica SuperAdmin.
        /// </summary>
        public static bool IsHubSuperAdmin(this ClaimsPrincipal user)
        {
            return string.Equals(user.GetHubRol(), "SuperAdmin", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// True si el JWT del Hub indica un rol con scope cross-Hub para gestion
        /// admin de Emisores/Sucursales: SuperAdmin (cross-Grupo) o UsuarioCompartido
        /// (cross-Hub-en-Grupo). AdminOrg paso a Hub-unico (2026-05-21) y ya no
        /// califica para operaciones cross-Hub administrativas.
        /// </summary>
        public static bool IsHubCrossHubScopeRole(this ClaimsPrincipal user)
        {
            var rol = user.GetHubRol();
            return string.Equals(rol, "SuperAdmin", StringComparison.OrdinalIgnoreCase)
                || string.Equals(rol, "UsuarioCompartido", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Lista de Hub IDs accesibles al usuario (claim "hubs_accesibles", JSON array).
        /// SuperAdmin: todos los Hubs del sistema. UsuarioCompartido: los Hubs
        /// del Grupo donde tiene UsuarioHub. Resto (incluido AdminOrg post
        /// 2026-05-21): solo su Hub primario.
        /// Vacía si el JWT no trae el claim.
        /// </summary>
        public static List<int> GetHubsAccesibles(this ClaimsPrincipal user)
        {
            var raw = user.FindFirst("hubs_accesibles")?.Value;
            if (string.IsNullOrWhiteSpace(raw)) return new List<int>();
            try { return JsonSerializer.Deserialize<List<int>>(raw) ?? new List<int>(); }
            catch (JsonException)
            {
                // Visibilidad operacional sin Logger inyectable en static helper: marcar el
                // Activity actual con un tag que captura el AppInsights workbook cross-app.
                // Si vemos este tag aparecer, el JWT del Hub esta emitiendo data corrupta.
                System.Diagnostics.Activity.Current?.AddTag("auth.hubs_accesibles_parse_failed", true);
                return new List<int>();
            }
        }

        // ============================================
        // Claims propios de Smartix (login local de Smartix)
        // ============================================


        /// <summary>
        /// Obtiene el EmisorId del usuario autenticado
        /// </summary>
        public static int GetEmisorId(this ClaimsPrincipal user)
        {
            var emisorIdClaim = user.FindFirst("EmisorId")?.Value;
            return int.TryParse(emisorIdClaim, out var emisorId) ? emisorId : 0;
        }

        /// <summary>
        /// Obtiene el UserId del usuario autenticado
        /// </summary>
        public static int GetUserId(this ClaimsPrincipal user)
        {
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        /// <summary>
        /// Obtiene el RolId del usuario autenticado
        /// </summary>
        public static int GetRolId(this ClaimsPrincipal user)
        {
            var rolIdClaim = user.FindFirst("RolId")?.Value;
            return int.TryParse(rolIdClaim, out var rolId) ? rolId : 0;
        }

        /// <summary>
        /// Obtiene el nombre del rol del usuario autenticado
        /// </summary>
        public static string GetRolNombre(this ClaimsPrincipal user)
        {
            return user.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
        }

        /// <summary>
        /// Verifica si el usuario tiene un rol específico
        /// </summary>
        public static bool IsInRole(this ClaimsPrincipal user, string roleName)
        {
            return user.GetRolNombre().Equals(roleName, StringComparison.OrdinalIgnoreCase);
        }
    }
}
