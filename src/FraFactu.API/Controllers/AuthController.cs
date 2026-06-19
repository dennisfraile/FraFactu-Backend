using Microsoft.AspNetCore.Mvc;
using FraFactu.Application.DTOs.Auth;
using FraFactu.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using FraFactu.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using FraFactu.Application.Common.Settings;
using Microsoft.Extensions.Options;
using FraFactu.Domain.Entities;
using FraFactu.API.Helpers;

namespace FraFactu.API.Controllers
{
    /// <summary>
    /// Controlador de autenticación y gestión de sesiones
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ApplicationDbContext _context;
        private readonly JwtSettings _jwtSettings;
        private readonly ISupabaseAuthService _supabaseAuthService;

        public AuthController(
            IAuthService authService,
            ApplicationDbContext context,
            IOptions<JwtSettings> jwtSettings,
            ISupabaseAuthService supabaseAuthService)
        {
            _authService = authService;
            _context = context;
            _jwtSettings = jwtSettings.Value;
            _supabaseAuthService = supabaseAuthService;
        }

        /// <summary>
        /// Inicia sesión y devuelve un JWT token
        /// </summary>
        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginDto loginDto)
        {
            var result = await _authService.LoginAsync(loginDto);

            if (result == null)
                return Unauthorized(new { message = "Email o contraseña incorrectos" });

            return Ok(result);
        }

        /// <summary>
        /// SSO desde SmartHub: canjea un exchange code emitido por el Hub
        /// y devuelve un JWT propio de Smartix. Lo consume el frontend Vue
        /// despues de recibir el code en /auth/hub.
        /// </summary>
        [HttpPost("hub-login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<LoginResponseDto>> HubLogin([FromBody] HubLoginRequestDto request)
        {
            try
            {
                var result = await _authService.HubLoginAsync(request);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                // Hub sin Emisor vinculado o usuario sin Hub activo: error de configuracion.
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Cambia la contraseña del usuario autenticado
        /// </summary>
        [HttpPost("change-password")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (userId == 0)
                return Unauthorized();

            var success = await _authService.ChangePasswordAsync(userId, dto.CurrentPassword, dto.NewPassword);

            if (!success)
                return BadRequest(new { message = "No se pudo cambiar la contraseña. Verifica tu contraseña actual." });

            return Ok(new { message = "Contraseña cambiada exitosamente" });
        }

        /// <summary>
        /// Cambia el ambiente (Pruebas/Producción) del Emisor asociado al usuario
        /// actual. Persiste en Emisor.CatAmbienteDestinoId para que el flujo de
        /// emisión use las credenciales correctas. Requiere [Authorize].
        /// </summary>
        [HttpPost("change-ambiente")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> ChangeAmbiente([FromBody] ChangeAmbienteDto dto)
        {
            var emisorIdClaim = User.FindFirst("EmisorId")?.Value;
            if (string.IsNullOrEmpty(emisorIdClaim) || !int.TryParse(emisorIdClaim, out var emisorId))
                return Unauthorized(new { message = "El usuario no tiene un Emisor asociado." });

            var success = await _authService.ChangeAmbienteAsync(emisorId, dto.Ambiente);
            if (!success)
                return BadRequest(new { message = "No se pudo cambiar el ambiente. Verifique que el código sea '00' (Pruebas) o '01' (Producción)." });

            return Ok(new { message = "Ambiente actualizado.", ambiente = dto.Ambiente });
        }

        /// <summary>
        /// Obtiene información del usuario autenticado actual
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetCurrentUser()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return Unauthorized();

            var usuario = await _context.Usuarios.FindAsync(userId);
            if (usuario == null)
                return NotFound(new { message = "Usuario no encontrado" });

            return Ok(new
            {
                userId = userIdStr,
                email = usuario.Email,
                nombreCompleto = usuario.NombreCompleto,
                emisorId = User.FindFirst("EmisorId")?.Value,
                emisorNombre = User.FindFirst("EmisorNombre")?.Value,
                accesoTodasSucursales = ScopeHelper.GetAccesoTodasSucursales(User),
                sucursalIds = ScopeHelper.GetSucursalIdsFromClaims(User),
                rolNombre = User.FindFirst(ClaimTypes.Role)?.Value,
                proveedorAuth = usuario.ProveedorAuth.ToString()
            });
        }

        /// <summary>
        /// Actualiza nombre y email del usuario autenticado
        /// </summary>
        [HttpPut("update-profile")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (userId == 0)
                return Unauthorized();

            var usuario = await _context.Usuarios.FindAsync(userId);

            if (usuario == null)
                return NotFound(new { message = "Usuario no encontrado" });

            if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email.Trim() != usuario.Email)
            {
                var emailEnUso = await _context.Usuarios
                    .AnyAsync(u => u.Email == dto.Email.Trim() && u.Id != userId);

                if (emailEnUso)
                    return BadRequest(new { message = "El email ya está en uso por otro usuario" });
            }

            if (!string.IsNullOrWhiteSpace(dto.NombreCompleto))
                usuario.NombreCompleto = dto.NombreCompleto.Trim();

            if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email.Trim() != usuario.Email)
            {
                usuario.Email = dto.Email.Trim();
                // Limpiar ProveedorExternoId para que Google re-vincule con el nuevo correo
                usuario.ProveedorExternoId = null;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Perfil actualizado exitosamente" });
        }

        // ========================================
        // GOOGLE OAUTH ENDPOINTS
        // ========================================

        /// <summary>
        /// Autenticación con Google OAuth 2.0 (crea usuario si no existe)
        /// </summary>
        [HttpPost("google")]
        [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<LoginResponseDto>> GoogleLogin([FromBody] GoogleAuthDto googleAuthDto)
        {
            var result = await _authService.GoogleLoginAsync(googleAuthDto);

            if (result == null)
                return Unauthorized(new { message = "Token de Google inválido o expirado" });

            return Ok(result);
        }

        /// <summary>
        /// Vincula una cuenta de Google al usuario autenticado actual
        /// </summary>
        [HttpPost("link-google")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> LinkGoogleAccount([FromBody] LinkGoogleAccountDto linkDto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (userId == 0)
                return Unauthorized();

            var success = await _authService.LinkGoogleAccountAsync(userId, linkDto);

            if (!success)
                return BadRequest(new { message = "No se pudo vincular la cuenta de Google. Verifica que el token sea válido y que el email de Google coincida con tu cuenta." });

            return Ok(new { message = "Cuenta de Google vinculada exitosamente" });
        }

        /// <summary>
        /// Desvincula la cuenta de Google del usuario autenticado
        /// </summary>
        [HttpDelete("unlink-google")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> UnlinkGoogleAccount()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (userId == 0)
                return Unauthorized();

            var success = await _authService.UnlinkGoogleAccountAsync(userId);

            if (!success)
                return BadRequest(new { message = "No se pudo desvincular la cuenta de Google. Asegúrate de tener una contraseña configurada antes de desvincular." });

            return Ok(new { message = "Cuenta de Google desvinculada exitosamente" });
        }

        // ========================================
        // SUPABASE AUTH ENDPOINTS
        // ========================================

        /// <summary>
        /// Sincroniza un usuario autenticado con Supabase con la base de datos local.
        /// Requiere header X-Supabase-Token con el access token de Supabase para validación.
        /// </summary>
        [HttpPost("supabase-sync")]
        [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<LoginResponseDto>> SupabaseSync([FromBody] SupabaseSyncDto dto)
        {
            // Validate the Supabase access token from the custom header
            var supabaseToken = Request.Headers["X-Supabase-Token"].FirstOrDefault();
            if (string.IsNullOrEmpty(supabaseToken))
                return BadRequest(new { message = "Supabase access token is required" });

            var isValid = await _supabaseAuthService.ValidateSupabaseToken(supabaseToken);
            if (!isValid)
                return Unauthorized(new { message = "Invalid Supabase token" });

            var result = await _supabaseAuthService.SyncSupabaseUser(dto);

            if (result == null)
                return Unauthorized(new { message = "No se pudo sincronizar el usuario con Supabase" });

            return Ok(result);
        }

        /// <summary>
        /// UsuarioCompartido + Plan B Hub-as-Emisor: cambia el Emisor activo del
        /// usuario al solicitado, siempre que figure en el claim
        /// <c>emisores_accesibles</c> del JWT actual. Persiste el cambio en BD y
        /// devuelve un JWT reemitido con el nuevo Emisor activo (misma lista de
        /// accesibles, sin bump de TokenVersion).
        /// </summary>
        [HttpPost("cambiar-emisor-activo")]
        [Authorize]
        [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<LoginResponseDto>> CambiarEmisorActivo([FromBody] CambiarEmisorActivoRequestDto dto)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
                return Unauthorized();

            var emisoresAccesibles = ScopeHelper.GetEmisoresAccesibles(User);

            // Preservar claims del SSO Hub para que el nuevo JWT siga validando
            // contra OnTokenValidated (single sign-out cross-app).
            int? hubUsuarioId = int.TryParse(User.FindFirst("usuario_hub_id")?.Value, out var hubId) ? hubId : null;
            int? tokenVersion = int.TryParse(User.FindFirst("token_version")?.Value, out var tv) ? tv : null;

            var result = await _authService.CambiarEmisorActivoAsync(userId, dto.EmisorId, emisoresAccesibles, hubUsuarioId, tokenVersion);

            return result.Status switch
            {
                CambiarEmisorActivoStatus.NoAutorizado =>
                    StatusCode(StatusCodes.Status403Forbidden, new { message = "No tiene acceso al Emisor solicitado" }),
                CambiarEmisorActivoStatus.NoExiste =>
                    NotFound(new { message = "Emisor no encontrado" }),
                CambiarEmisorActivoStatus.Ok =>
                    Ok(result.Response!),
                _ => StatusCode(StatusCodes.Status500InternalServerError)
            };
        }

        /// <summary>
        /// Bug #3 (UsuarioCompartido / Plan B Hub-as-Emisor) — 2026-05-29.
        /// Refresca el claim <c>emisores_accesibles</c> del JWT consultando
        /// SmartHub en vivo (sin re-SSO). Reemite el JWT con la lista
        /// actualizada preservando hub_usuario_id / token_version / Emisor
        /// activo / sucursales. El frontend lo usa antes de mostrar "No tenés
        /// acceso al Emisor de esta clínica" para auto-sanar sesiones cuyo
        /// claim quedo stale (admin asigno/quito Hubs al rol o el SSO no
        /// propago bien la lista).
        ///
        /// Respuestas:
        ///  - 200: LoginResponseDto con JWT reemitido + lista nueva.
        ///  - 401: JWT invalido o sin <c>NameIdentifier</c>.
        ///  - 502: SmartHub no respondio (timeout/5xx). El JWT actual sigue
        ///         siendo valido — el caller puede seguir usando el claim viejo.
        /// </summary>
        [HttpPost("refresh-emisores")]
        [Authorize]
        [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        public async Task<ActionResult<LoginResponseDto>> RefreshEmisores()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
                return Unauthorized();

            int? hubUsuarioId = int.TryParse(User.FindFirst("usuario_hub_id")?.Value, out var hubId) ? hubId : null;
            int? tokenVersion = int.TryParse(User.FindFirst("token_version")?.Value, out var tv) ? tv : null;

            var result = await _authService.RefreshEmisoresAsync(userId, hubUsuarioId, tokenVersion);
            if (result == null)
            {
                return StatusCode(
                    StatusCodes.Status502BadGateway,
                    new { message = "No se pudo refrescar la lista de Emisores desde SmartHub." });
            }

            return Ok(result);
        }

        /// <summary>
        /// Cambia el contexto de emisor para SuperAdmin (genera nuevo token)
        /// </summary>
        [HttpPost("cambiar-contexto")]
        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> CambiarContexto([FromBody] CambiarContextoDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            // Validar que el emisor existe
            var emisor = await _context.Emisores
                .Include(e => e.Sucursales)
                .FirstOrDefaultAsync(e => e.Id == dto.EmisorId);

            if (emisor == null)
                return NotFound(new { message = "Emisor no encontrado" });

            // Obtener datos del usuario para regenerar token
            var usuario = await _context.Usuarios
                .Include(u => u.Rol)
                .Include(u => u.UsuarioSucursales)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (usuario == null)
                return NotFound(new { message = "Usuario no encontrado" });

            var sucursalIds = usuario.UsuarioSucursales.Select(us => us.SucursalId).ToList();

            // Generar nuevo token con el contexto seleccionado
            var tokenString = _authService.GenerateJwtToken(
                usuario.Id,
                usuario.Email,
                usuario.NombreCompleto,
                dto.EmisorId,
                emisor.NombreRazonSocial,
                usuario.RolId,
                usuario.Rol.Nombre,
                usuario.AccesoTodasSucursales,
                sucursalIds
            );

            return Ok(new
            {
                token = tokenString,
                emisorId = dto.EmisorId,
                emisorNombre = emisor.NombreRazonSocial,
                accesoTodasSucursales = usuario.AccesoTodasSucursales,
                sucursalIds = sucursalIds
            });
        }
    }
}
