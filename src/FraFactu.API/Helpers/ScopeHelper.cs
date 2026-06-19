using System.Security.Claims;
using System.Text.Json;

namespace FraFactu.API.Helpers
{
    /// <summary>
    /// Helper para validación de scope y permisos por sucursal
    /// </summary>
    public static class ScopeHelper
    {
        /// <summary>
        /// Roles que requieren restricción por sucursal
        /// </summary>
        private static readonly string[] RolesRestringidosPorSucursal =
        {
            "GerenteSucursal",
            "Cajero",
            "Contador",
            "Auditor"
        };

        /// <summary>
        /// Verifica si un rol requiere restricción por sucursal
        /// </summary>
        public static bool RequiereRestriccionSucursal(string rol)
        {
            return RolesRestringidosPorSucursal.Contains(rol);
        }

        /// <summary>
        /// Obtiene si el usuario tiene acceso a todas las sucursales desde los claims
        /// </summary>
        public static bool GetAccesoTodasSucursales(ClaimsPrincipal user)
        {
            var claim = user.FindFirst("AccesoTodasSucursales")?.Value;
            return claim == "true";
        }

        /// <summary>
        /// Obtiene la lista de sucursalIds del usuario desde los claims del JWT
        /// </summary>
        public static List<int> GetSucursalIdsFromClaims(ClaimsPrincipal user)
        {
            return user.FindAll("SucursalId")
                .Select(c => int.TryParse(c.Value, out var id) ? id : 0)
                .Where(id => id > 0)
                .ToList();
        }

        /// <summary>
        /// Backward compat: obtiene una sola sucursalId (la primera) o null
        /// </summary>
        public static int? GetSucursalIdFromClaims(ClaimsPrincipal user)
        {
            var ids = GetSucursalIdsFromClaims(user);
            return ids.Count > 0 ? ids[0] : null;
        }

        /// <summary>
        /// Obtiene el rol del usuario desde los claims
        /// </summary>
        public static string GetRolFromClaims(ClaimsPrincipal user)
        {
            return user.FindFirst(ClaimTypes.Role)?.Value ?? "";
        }

        /// <summary>
        /// Obtiene el emisorId del usuario desde los claims
        /// </summary>
        public static int GetEmisorIdFromClaims(ClaimsPrincipal user)
        {
            var claim = user.FindFirst("EmisorId")?.Value;
            if (string.IsNullOrEmpty(claim))
                throw new UnauthorizedAccessException("EmisorId no encontrado en token");
            return int.Parse(claim);
        }

        /// <summary>
        /// Valida si un usuario tiene acceso a una sucursal específica
        /// </summary>
        public static bool ValidarAccesoSucursal(ClaimsPrincipal user, int sucursalId)
        {
            var rol = GetRolFromClaims(user);

            // SuperAdmin y EmisorAdmin tienen acceso a todas las sucursales
            if (rol == "SuperAdmin" || rol == "EmisorAdmin")
                return true;

            // Si el usuario tiene acceso a todas las sucursales
            if (GetAccesoTodasSucursales(user))
                return true;

            // Roles restringidos: validar que la sucursal esté en su lista
            if (RequiereRestriccionSucursal(rol))
            {
                var sucursalIds = GetSucursalIdsFromClaims(user);
                return sucursalIds.Contains(sucursalId);
            }

            return true;
        }

        /// <summary>
        /// Verifica si el usuario es SuperAdmin
        /// </summary>
        public static bool EsSuperAdmin(ClaimsPrincipal user)
        {
            var rol = GetRolFromClaims(user);
            return rol == "SuperAdmin";
        }

        /// <summary>
        /// UsuarioCompartido + Plan B Hub-as-Emisor: lista de SmartixEmisorId que
        /// la sesion puede facturar. Lee el claim <c>emisores_accesibles</c>
        /// (JSON array) emitido por <c>AuthService.GenerateJwtToken</c>. Para
        /// roles que no son UsuarioCompartido este claim contiene <c>[EmisorId]</c>;
        /// asi los autorizadores aguas abajo pueden filtrar uniformemente.
        ///
        /// Back-compat: si el claim no esta (JWT emitido antes del deploy de
        /// Task B.1), cae al claim <c>EmisorId</c> del propio JWT y devuelve
        /// <c>[EmisorId]</c>. Asi los usuarios con sesiones antiguas pueden
        /// seguir facturando en su Emisor sin perder acceso hasta que re-loguean.
        /// Solo devuelve lista vacia cuando tampoco hay <c>EmisorId</c> (caso
        /// muy raro: JWT corrupto o sin emisor asignado).
        /// </summary>
        public static List<int> GetEmisoresAccesibles(ClaimsPrincipal user)
        {
            var raw = user.FindFirst("emisores_accesibles")?.Value;
            if (!string.IsNullOrWhiteSpace(raw))
            {
                try
                {
                    var parsed = JsonSerializer.Deserialize<List<int>>(raw);
                    if (parsed != null && parsed.Count > 0)
                        return parsed;
                }
                catch
                {
                    // Cae al fallback de abajo.
                }
            }

            // Fallback: usar el EmisorId del propio JWT (back-compat con
            // sesiones pre-Task-B.1 y JWTs corruptos).
            var emisorClaim = user.FindFirst("EmisorId")?.Value;
            if (!string.IsNullOrEmpty(emisorClaim) && int.TryParse(emisorClaim, out var emisorId) && emisorId > 0)
                return new List<int> { emisorId };

            return new List<int>();
        }
    }
}
