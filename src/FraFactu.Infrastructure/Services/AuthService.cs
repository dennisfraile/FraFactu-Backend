using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using FraFactu.Application.Common.Settings;
using FraFactu.Application.DTOs.Auth;
using FraFactu.Application.Interfaces;
using FraFactu.Infrastructure.Persistence;
using FraFactu.Infrastructure.Security;
using Google.Apis.Auth;
using FraFactu.Domain.Enums;
using FraFactu.Domain.Entities;

namespace FraFactu.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtSettings _jwtSettings;
        private readonly GoogleAuthSettings _googleAuthSettings;
        private readonly IGoogleTokenValidator _googleTokenValidator;
        private readonly IAuthEmailService _authEmailService;
        private readonly ILogger<AuthService> _logger;

        // Vigencia del token de reset de contraseña.
        private static readonly TimeSpan ResetTokenVigencia = TimeSpan.FromMinutes(30);

        public AuthService(
            ApplicationDbContext context,
            IOptions<JwtSettings> jwtSettings,
            IOptions<GoogleAuthSettings> googleAuthSettings,
            IGoogleTokenValidator googleTokenValidator,
            IAuthEmailService authEmailService,
            ILogger<AuthService> logger)
        {
            _context = context;
            _jwtSettings = jwtSettings.Value;
            _googleAuthSettings = googleAuthSettings.Value;
            _googleTokenValidator = googleTokenValidator;
            _authEmailService = authEmailService;
            _logger = logger;
        }

        public async Task<LoginResponseDto?> LoginAsync(LoginDto loginDto)
        {
            // Buscar usuario por email
            var usuario = await _context.Usuarios
                .Include(u => u.Emisor)
                .Include(u => u.Rol)
                .Include(u => u.UsuarioSucursales)
                .FirstOrDefaultAsync(u => u.Email == loginDto.Email);

            if (usuario == null || usuario.Estado != EstadoUsuario.Activo)
                return null;

            // Verificar contraseña (solo para usuarios con autenticación local)
            if (string.IsNullOrEmpty(usuario.PasswordHash) || !VerifyPassword(loginDto.Password, usuario.PasswordHash))
                return null;

            // Clave temporal: si caducó, se rechaza el login (el usuario debe usar
            // "olvidé mi contraseña" para obtener un nuevo acceso).
            if (usuario.RequiereCambioPwd
                && usuario.ExpiracionPwdTemporal.HasValue
                && usuario.ExpiracionPwdTemporal.Value < DateTime.UtcNow)
            {
                return null;
            }

            // Actualizar último acceso
            usuario.UltimoAcceso = DateTime.UtcNow;

            // Actualizar ambiente del emisor si se envía desde el frontend
            if (!string.IsNullOrEmpty(loginDto.Ambiente) && usuario.Emisor != null)
            {
                usuario.Emisor.CatAmbienteDestinoId = loginDto.Ambiente == "01" ? 2 : 1;
            }

            await _context.SaveChangesAsync();

            var sucursalIds = usuario.UsuarioSucursales.Select(us => us.SucursalId).ToList();

            // Generar token (identidad 100% local; emisores_accesibles se calcula
            // localmente a partir del EmisorId en GenerateJwtToken). Se emite
            // token_version desde Usuario.TokenVersion para la revocación local.
            var token = GenerateJwtToken(
                usuario.Id,
                usuario.Email,
                usuario.NombreCompleto,
                usuario.EmisorId,
                usuario.Emisor?.NombreRazonSocial,
                usuario.RolId,
                usuario.Rol.Nombre,
                usuario.AccesoTodasSucursales,
                sucursalIds,
                tokenVersion: usuario.TokenVersion,
                pwdChangeRequired: usuario.RequiereCambioPwd
            );

            // Construir respuesta
            return new LoginResponseDto
            {
                Token = token,
                UserId = usuario.Id,
                Email = usuario.Email,
                NombreCompleto = usuario.NombreCompleto,
                EmisorId = usuario.EmisorId,
                EmisorNombre = usuario.Emisor?.NombreRazonSocial,
                AccesoTodasSucursales = usuario.AccesoTodasSucursales,
                SucursalIds = sucursalIds,
                RolId = usuario.RolId,
                RolNombre = usuario.Rol.Nombre,
                RequiereCambioPwd = usuario.RequiereCambioPwd
            };
        }

        public string GenerateJwtToken(int usuarioId, string email, string nombreCompleto,
            int? emisorId, string? emisorNombre, int rolId, string rolNombre,
            bool accesoTodasSucursales, List<int> sucursalIds,
            int? hubUsuarioId = null, int? tokenVersion = null,
            List<int>? emisoresAccesibles = null, bool pwdChangeRequired = false)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, usuarioId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("NombreCompleto", nombreCompleto),
                new Claim("RolId", rolId.ToString()),
                new Claim(ClaimTypes.Role, rolNombre),
                new Claim("AccesoTodasSucursales", accesoTodasSucursales.ToString().ToLower())
            };

            // Token restringido de primer ingreso: solo permite el endpoint de
            // cambio de contraseña obligatorio (lo aplica RequirePasswordChangeMiddleware).
            if (pwdChangeRequired)
            {
                claims.Add(new Claim("pwd_change_required", "true"));
            }

            if (emisorId.HasValue)
            {
                claims.Add(new Claim("EmisorId", emisorId.Value.ToString()));
                if (!string.IsNullOrEmpty(emisorNombre))
                {
                    claims.Add(new Claim("EmisorNombre", emisorNombre));
                }
            }

            // Agregar cada sucursalId como un claim separado (permite múltiples)
            foreach (var sucursalId in sucursalIds)
            {
                claims.Add(new Claim("SucursalId", sucursalId.ToString()));
            }

            // Revocación local (F2): token_version se emite desde Usuario.TokenVersion
            // y el middleware OnTokenValidated lo valida contra el valor actual en BD
            // (fail-closed). Al hacer logout / cambiar contraseña / cambiar de rol /
            // desactivar, TokenVersion sube y los JWT previos quedan invalidados.
            // usuario_hub_id es un claim legado (SSO del Hub, eliminado en F1) que se
            // preserva solo si un caller lo pasa explícitamente; en login local no aplica.
            if (hubUsuarioId.HasValue)
            {
                claims.Add(new Claim("usuario_hub_id", hubUsuarioId.Value.ToString()));
            }
            if (tokenVersion.HasValue)
            {
                claims.Add(new Claim("token_version", tokenVersion.Value.ToString()));
            }

            // UsuarioCompartido + Plan B Hub-as-Emisor: emisores_accesibles
            // enumera los SmartixEmisorId que esta sesion puede facturar. Lo
            // calcula SmartHub (EmisoresAccesiblesCalculator) y lo manda en el
            // response de validate-code; HubLoginAsync lo propaga aqui.
            //
            // Fallback: si la lista viene null o vacia (login local o SSO desde
            // un Hub que aun no expone el campo), emitimos [EmisorId] para que
            // los autorizadores aguas abajo siempre tengan una lista no vacia
            // — es lo mismo que tenian antes de UsuarioCompartido.
            var listaEmisores = (emisoresAccesibles?.Count ?? 0) > 0
                ? emisoresAccesibles!.Distinct().ToList()
                : (emisorId.HasValue ? new List<int> { emisorId.Value } : new List<int>());

            claims.Add(new Claim(
                "emisores_accesibles",
                System.Text.Json.JsonSerializer.Serialize(listaEmisores)));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<bool> LogoutAsync(int usuarioId)
        {
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario == null)
                return false;

            // Revocación local: invalida todos los JWT emitidos hasta ahora.
            usuario.TokenVersion++;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task ForgotPasswordAsync(string email)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email == email && u.Estado == EstadoUsuario.Activo);

            // Anti-enumeración: si no existe (o está inactivo) salimos en silencio,
            // sin revelar nada al llamante.
            if (usuario == null)
                return;

            var rawToken = SecureTokenGenerator.GenerateResetToken();
            usuario.PasswordResetTokenHash = SecureTokenGenerator.Sha256Hex(rawToken);
            usuario.PasswordResetTokenExpira = DateTime.UtcNow.Add(ResetTokenVigencia);
            await _context.SaveChangesAsync();

            // El token en claro solo viaja en el correo; en BD queda únicamente el hash.
            await _authEmailService.EnviarResetPasswordAsync(usuario.Email, usuario.NombreCompleto, rawToken);
        }

        public async Task<bool> ResetPasswordAsync(string token, string newPassword)
        {
            if (string.IsNullOrEmpty(token))
                return false;

            var hash = SecureTokenGenerator.Sha256Hex(token);
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.PasswordResetTokenHash == hash);

            // Token desconocido o ya usado (se limpia al consumirlo).
            if (usuario == null)
                return false;

            // Token expirado.
            if (!usuario.PasswordResetTokenExpira.HasValue
                || usuario.PasswordResetTokenExpira.Value < DateTime.UtcNow)
                return false;

            usuario.PasswordHash = HashPassword(newPassword);
            // Un solo uso: invalidar el token tras consumirlo.
            usuario.PasswordResetTokenHash = null;
            usuario.PasswordResetTokenExpira = null;
            // Si tenía clave temporal pendiente, el reset también la resuelve.
            usuario.RequiereCambioPwd = false;
            usuario.ExpiracionPwdTemporal = null;
            // Revocación local: invalida las sesiones abiertas con la clave anterior.
            usuario.TokenVersion++;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<LoginResponseDto?> ChangePasswordFirstLoginAsync(int usuarioId, string newPassword)
        {
            var usuario = await _context.Usuarios
                .Include(u => u.Emisor)
                .Include(u => u.Rol)
                .Include(u => u.UsuarioSucursales)
                .FirstOrDefaultAsync(u => u.Id == usuarioId);

            // Solo aplica si hay un cambio obligatorio pendiente (el usuario llegó
            // con un token restringido). En cualquier otro caso se rechaza.
            if (usuario == null || !usuario.RequiereCambioPwd)
                return null;

            usuario.PasswordHash = HashPassword(newPassword);
            usuario.RequiereCambioPwd = false;
            usuario.ExpiracionPwdTemporal = null;
            // Revocación local: invalida el token restringido de primer ingreso.
            usuario.TokenVersion++;
            usuario.UltimoAcceso = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var sucursalIds = usuario.UsuarioSucursales.Select(us => us.SucursalId).ToList();

            // JWT completo (sin la marca pwd_change_required), con el TokenVersion ya bumpeado.
            var token = GenerateJwtToken(
                usuario.Id,
                usuario.Email,
                usuario.NombreCompleto,
                usuario.EmisorId,
                usuario.Emisor?.NombreRazonSocial,
                usuario.RolId,
                usuario.Rol.Nombre,
                usuario.AccesoTodasSucursales,
                sucursalIds,
                tokenVersion: usuario.TokenVersion
            );

            return new LoginResponseDto
            {
                Token = token,
                UserId = usuario.Id,
                Email = usuario.Email,
                NombreCompleto = usuario.NombreCompleto,
                EmisorId = usuario.EmisorId,
                EmisorNombre = usuario.Emisor?.NombreRazonSocial,
                AccesoTodasSucursales = usuario.AccesoTodasSucursales,
                SucursalIds = sucursalIds,
                RolId = usuario.RolId,
                RolNombre = usuario.Rol.Nombre,
                RequiereCambioPwd = false
            };
        }

        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public bool VerifyPassword(string password, string passwordHash)
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }

        public async Task<bool> ChangePasswordAsync(int usuarioId, string currentPassword, string newPassword)
        {
            var usuario = await _context.Usuarios.FindAsync(usuarioId);

            if (usuario == null || !usuario.PermiteCambioPwd)
                return false;

            // Verificar que el usuario tenga contraseña y que coincida
            if (string.IsNullOrEmpty(usuario.PasswordHash) || !VerifyPassword(currentPassword, usuario.PasswordHash))
                return false;

            usuario.PasswordHash = HashPassword(newPassword);
            usuario.RequiereCambioPwd = false;
            usuario.ExpiracionPwdTemporal = null;
            // Revocación local: invalida los JWT emitidos con la contraseña anterior.
            usuario.TokenVersion++;

            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Cambia el ambiente (Pruebas/Producción) del Emisor especificado.
        /// Resuelve el ID de cat_ambiente_destino por el Codigo MH ("00"/"01")
        /// y actualiza Emisor.CatAmbienteDestinoId.
        /// </summary>
        public async Task<bool> ChangeAmbienteAsync(int emisorId, string ambienteCodigo)
        {
            if (ambienteCodigo != "00" && ambienteCodigo != "01")
                return false;

            var emisor = await _context.Emisores.FindAsync(emisorId);
            if (emisor == null) return false;

            var nuevoAmbienteId = await _context.CatAmbientes
                .Where(a => a.Codigo == ambienteCodigo)
                .Select(a => a.Id)
                .FirstOrDefaultAsync();
            if (nuevoAmbienteId == 0) return false;

            if (emisor.CatAmbienteDestinoId == nuevoAmbienteId) return true; // sin cambio

            emisor.CatAmbienteDestinoId = nuevoAmbienteId;
            await _context.SaveChangesAsync();
            return true;
        }

        // ============================================
        // GOOGLE OAUTH METHODS
        // ============================================

        public async Task<GoogleJsonWebSignature.Payload?> ValidateGoogleTokenAsync(string idToken)
        {
            // Delegar la validación al servicio inyectado
            return await _googleTokenValidator.ValidateAsync(idToken, _googleAuthSettings.ClientId);
        }

        public async Task<LoginResponseDto?> GoogleLoginAsync(GoogleAuthDto googleAuthDto)
        {
            // Validar el access token llamando a Google's userinfo endpoint
            var userInfo = await _googleTokenValidator.ValidateAccessTokenAsync(googleAuthDto.AccessToken);
            if (userInfo == null)
            {
                _logger.LogWarning("Google login failed: access token validation returned null");
                return null;
            }

            var email = userInfo.Email;
            var googleUserId = userInfo.Sub;
            var nombreCompleto = userInfo.Name;

            // 2. Buscar usuario existente por email
            var usuario = await _context.Usuarios
                .Include(u => u.Emisor)
                .Include(u => u.Rol)
                .Include(u => u.UsuarioSucursales)
                .FirstOrDefaultAsync(u => u.Email == email);

            if (usuario != null)
            {
                // Usuario existe - Vincular cuenta de Google si no está vinculada
                if (usuario.Estado != EstadoUsuario.Activo)
                {
                    _logger.LogWarning("Google login failed: user {Email} has Estado={Estado} (not Active)", email, usuario.Estado);
                    return null;
                }

                if (usuario.ProveedorAuth == ProveedorAutenticacion.Local
                    || string.IsNullOrEmpty(usuario.ProveedorExternoId))
                {
                    // Vincular Google automáticamente
                    usuario.ProveedorAuth = ProveedorAutenticacion.Google;
                    usuario.ProveedorExternoId = googleUserId;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Google account linked for user {Email}", email);
                }
                else if (usuario.ProveedorExternoId != googleUserId)
                {
                    // Google ID no coincide - posible conflicto
                    _logger.LogWarning("Google login failed: Google ID mismatch for {Email}. Expected={Expected}, Got={Got}",
                        email, usuario.ProveedorExternoId, googleUserId);
                    return null;
                }
            }
            else
            {
                // No existe usuario con ese email — no se permite crear cuenta desde Google
                _logger.LogWarning("Google login failed: no user found with email {Email}", email);
                return null;
            }

            if (usuario == null)
                return null;

            // Actualizar último acceso
            usuario.UltimoAcceso = DateTime.UtcNow;

            // Actualizar ambiente del emisor si se envía desde el frontend
            if (!string.IsNullOrEmpty(googleAuthDto.Ambiente) && usuario.Emisor != null)
            {
                usuario.Emisor.CatAmbienteDestinoId = googleAuthDto.Ambiente == "01" ? 2 : 1;
            }

            await _context.SaveChangesAsync();

            var sucursalIds = usuario.UsuarioSucursales.Select(us => us.SucursalId).ToList();

            // 4. Generar JWT token (identidad 100% local; token_version local).
            var token = GenerateJwtToken(
                usuario.Id,
                usuario.Email,
                usuario.NombreCompleto,
                usuario.EmisorId,
                usuario.Emisor?.NombreRazonSocial,
                usuario.RolId,
                usuario.Rol.Nombre,
                usuario.AccesoTodasSucursales,
                sucursalIds,
                tokenVersion: usuario.TokenVersion
            );

            // 5. Construir respuesta
            return new LoginResponseDto
            {
                Token = token,
                UserId = usuario.Id,
                Email = usuario.Email,
                NombreCompleto = usuario.NombreCompleto,
                EmisorId = usuario.EmisorId,
                EmisorNombre = usuario.Emisor?.NombreRazonSocial,
                AccesoTodasSucursales = usuario.AccesoTodasSucursales,
                SucursalIds = sucursalIds,
                RolId = usuario.RolId,
                RolNombre = usuario.Rol.Nombre,
                RequiereCambioPwd = false // Google login nunca requiere cambio de contraseña
            };
        }

        public async Task<bool> LinkGoogleAccountAsync(int usuarioId, LinkGoogleAccountDto linkDto)
        {
            // 1. Validar token de Google
            var googlePayload = await ValidateGoogleTokenAsync(linkDto.IdToken);
            if (googlePayload == null)
                return false;

            // 2. Buscar usuario
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario == null)
                return false;

            // 3. Verificar que el email de Google coincida con el del usuario
            if (usuario.Email != googlePayload.Email)
                return false;

            // 4. Vincular cuenta de Google
            usuario.ProveedorAuth = ProveedorAutenticacion.Google;
            usuario.ProveedorExternoId = googlePayload.Subject;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UnlinkGoogleAccountAsync(int usuarioId)
        {
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario == null)
                return false;

            // Verificar que el usuario tenga una contraseña configurada
            if (string.IsNullOrEmpty(usuario.PasswordHash))
                return false;

            // Limpiar campos de Google
            usuario.ProveedorAuth = ProveedorAutenticacion.Local;
            usuario.ProveedorExternoId = null;
            usuario.ProveedorExternoAccessToken = null;
            usuario.ProveedorExternoTokenExpiracion = null;

            await _context.SaveChangesAsync();
            return true;
        }


        /// <summary>
        /// UsuarioCompartido + Plan B Hub-as-Emisor — Task B.2.
        /// Cambia el Emisor activo del usuario sin re-SSO. La validacion va en
        /// el orden 1) lista del JWT, 2) existencia en BD, 3) update; asi nunca
        /// hacemos lookups innecesarios para Emisores que el caller no podia tocar.
        /// No bumpea TokenVersion: el JWT sigue siendo valido para el resto del
        /// ecosistema; solo cambia el scope activo.
        /// </summary>
        public async Task<CambiarEmisorActivoResultado> CambiarEmisorActivoAsync(
            int usuarioId,
            int nuevoEmisorId,
            IReadOnlyCollection<int> emisoresAccesibles,
            int? hubUsuarioId = null,
            int? tokenVersion = null)
        {
            // 1) Autorizacion: el Emisor debe estar en la lista del JWT actual.
            if (emisoresAccesibles == null || !emisoresAccesibles.Contains(nuevoEmisorId))
                return CambiarEmisorActivoResultado.NoAutorizado();

            // 2) Existencia: el Emisor debe existir en BD.
            var emisor = await _context.Emisores.FindAsync(nuevoEmisorId);
            if (emisor == null)
                return CambiarEmisorActivoResultado.NoExiste();

            // 3) Update + reemision del JWT.
            var usuario = await _context.Usuarios
                .Include(u => u.Rol)
                .Include(u => u.UsuarioSucursales)
                .FirstOrDefaultAsync(u => u.Id == usuarioId);

            if (usuario == null)
                return CambiarEmisorActivoResultado.NoExiste();

            // Defensa: no permitir cambio si el usuario fue desactivado entre
            // la emision del JWT y el request (ventana corta pero existente).
            // El bump de TokenVersion al desactivar deberia haberlo cerrado
            // ya, pero este check evita una race window.
            if (usuario.Estado != EstadoUsuario.Activo)
                return CambiarEmisorActivoResultado.NoAutorizado();

            var emisorAnterior = usuario.EmisorId;
            usuario.EmisorId = nuevoEmisorId;
            usuario.UltimoAcceso = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "[CambiarEmisor] Usuario {UsuarioId} ({Email}) cambio Emisor activo de {EmisorAnterior} a {EmisorNuevo}",
                usuario.Id, usuario.Email, emisorAnterior, nuevoEmisorId);

            var sucursalIds = usuario.UsuarioSucursales.Select(us => us.SucursalId).ToList();
            var rolNombre = usuario.Rol?.Nombre ?? string.Empty;
            var listaAccesibles = emisoresAccesibles.ToList();

            // Preservar usuario_hub_id / token_version del JWT actual para que los
            // siguientes requests sigan pasando OnTokenValidated y respondan a
            // single sign-out cross-app (cambio de rol/org/hub desde SmartHub).
            var token = GenerateJwtToken(
                usuario.Id,
                usuario.Email,
                usuario.NombreCompleto,
                emisor.Id,
                emisor.NombreRazonSocial,
                usuario.RolId,
                rolNombre,
                usuario.AccesoTodasSucursales,
                sucursalIds,
                hubUsuarioId: hubUsuarioId,
                tokenVersion: tokenVersion,
                emisoresAccesibles: listaAccesibles);

            return CambiarEmisorActivoResultado.Ok(new LoginResponseDto
            {
                Token = token,
                UserId = usuario.Id,
                Email = usuario.Email,
                NombreCompleto = usuario.NombreCompleto,
                EmisorId = emisor.Id,
                EmisorNombre = emisor.NombreRazonSocial,
                AccesoTodasSucursales = usuario.AccesoTodasSucursales,
                SucursalIds = sucursalIds,
                RolId = usuario.RolId,
                RolNombre = rolNombre,
                RequiereCambioPwd = false
            });
        }


    }
}