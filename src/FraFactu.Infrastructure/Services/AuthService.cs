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
using FraFactu.Application.DTOs.Hub;
using FraFactu.Application.Interfaces;
using FraFactu.Infrastructure.Persistence;
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
        private readonly ISmartHubApiService _smartHubApi;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            ApplicationDbContext context,
            IOptions<JwtSettings> jwtSettings,
            IOptions<GoogleAuthSettings> googleAuthSettings,
            IGoogleTokenValidator googleTokenValidator,
            ISmartHubApiService smartHubApi,
            ILogger<AuthService> logger)
        {
            _context = context;
            _jwtSettings = jwtSettings.Value;
            _googleAuthSettings = googleAuthSettings.Value;
            _googleTokenValidator = googleTokenValidator;
            _smartHubApi = smartHubApi;
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

            // Actualizar último acceso
            usuario.UltimoAcceso = DateTime.UtcNow;

            // Actualizar ambiente del emisor si se envía desde el frontend
            if (!string.IsNullOrEmpty(loginDto.Ambiente) && usuario.Emisor != null)
            {
                usuario.Emisor.CatAmbienteDestinoId = loginDto.Ambiente == "01" ? 2 : 1;
            }

            await _context.SaveChangesAsync();

            var sucursalIds = usuario.UsuarioSucursales.Select(us => us.SucursalId).ToList();

            // Bug #3 fix de raiz (2026-05-29): lookup en SmartHub por email para
            // que el JWT lleve usuario_hub_id + token_version + emisores_accesibles
            // aunque el login NO sea SSO. Sin esto, el wizard cross-Emisor (Plan-C2)
            // bloquea por claim vacio. Best-effort: si Hub no responde, sale sin
            // claims Hub (back-compat).
            var hubClaims = await _smartHubApi.LookupHubUserByEmailAsync(usuario.Email);

            // Generar token
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
                hubUsuarioId: hubClaims?.HubUsuarioId,
                tokenVersion: hubClaims?.TokenVersion,
                emisoresAccesibles: hubClaims?.EmisoresAccesibles
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
            List<int>? emisoresAccesibles = null)
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

            // SSO Hub: emitir usuario_hub_id + token_version para que el middleware
            // OnTokenValidated pueda detectar revocacion cross-app (cambio de org,
            // logout, asignacion de Hub) y forzar re-SSO. En login local estos
            // claims no aplican y se omiten; el middleware fail-opens en ese caso.
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

            // Bug #3 fix de raiz (2026-05-29): mismo lookup que LoginAsync.
            var hubClaims = await _smartHubApi.LookupHubUserByEmailAsync(usuario.Email);

            // 4. Generar JWT token
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
                hubUsuarioId: hubClaims?.HubUsuarioId,
                tokenVersion: hubClaims?.TokenVersion,
                emisoresAccesibles: hubClaims?.EmisoresAccesibles
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

        // ============================================
        // SSO HUB → SMARTIX
        // ============================================

        public async Task<LoginResponseDto> HubLoginAsync(HubLoginRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Code))
                throw new UnauthorizedAccessException("Code requerido.");

            var hubUser = await _smartHubApi.ValidateExchangeCodeAsync(request.Code);
            if (hubUser == null)
            {
                _logger.LogWarning("[HubLogin] SmartHub rechazo el code");
                throw new UnauthorizedAccessException("Code de SmartHub invalido o expirado.");
            }

            if (hubUser.OrganizacionId == null)
            {
                _logger.LogWarning("[HubLogin] HubUsuarioId={Id} sin Hub activo", hubUser.HubUsuarioId);
                throw new InvalidOperationException("El usuario no tiene un Hub activo en SmartHub.");
            }

            // F6: Hub = Emisor 1:1. Lookup explicito por Emisor.HubId.
            var emisor = await _context.Emisores
                .FirstOrDefaultAsync(e => e.HubId == hubUser.OrganizacionId);

            if (emisor == null)
            {
                _logger.LogWarning("[HubLogin] HubId={HubId} sin Emisor vinculado en Smartix", hubUser.OrganizacionId);
                throw new InvalidOperationException(
                    $"El Hub {hubUser.OrganizacionId} no esta vinculado a ningun Emisor en Smartix.");
            }

            // Buscar usuario por HubUsuarioId; fallback Email para enlazar cuentas pre-SSO.
            var usuario = await _context.Usuarios
                .Include(u => u.Rol)
                .Include(u => u.UsuarioSucursales)
                .FirstOrDefaultAsync(u => u.HubUsuarioId == hubUser.HubUsuarioId);

            if (usuario == null)
            {
                // Fallback por Email case-insensitive (Postgres compara strings
                // case-sensitive por defecto; ToLower -> LOWER() en SQL). Evita no
                // enlazar la cuenta pre-SSO por diferencia de mayusculas en el email.
                var emailNorm = hubUser.Email.ToLower();
                usuario = await _context.Usuarios
                    .Include(u => u.Rol)
                    .Include(u => u.UsuarioSucursales)
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == emailNorm);

                if (usuario != null)
                    usuario.HubUsuarioId = hubUser.HubUsuarioId;
            }

            if (usuario == null)
            {
                // Crear usuario nuevo via SSO.
                var rolNombre = MapHubRolToSmartixRol(hubUser.Rol);
                var rol = await _context.Roles.FirstOrDefaultAsync(r => r.Nombre == rolNombre)
                          ?? await _context.Roles.FirstOrDefaultAsync(r => r.Nombre == "EmisorAdmin")
                          ?? throw new InvalidOperationException("Rol default 'EmisorAdmin' no existe en BD.");

                usuario = new Usuario
                {
                    Email = hubUser.Email,
                    NombreCompleto = string.IsNullOrWhiteSpace(hubUser.NombreCompleto) ? hubUser.Email : hubUser.NombreCompleto,
                    HubUsuarioId = hubUser.HubUsuarioId,
                    EmisorId = emisor.Id,
                    RolId = rol.Id,
                    Rol = rol,
                    Estado = EstadoUsuario.Activo,
                    ProveedorAuth = ProveedorAutenticacion.SmartHub,
                    AccesoTodasSucursales = hubUser.Rol == "SuperAdmin" || hubUser.Rol == "AdminOrg",
                };

                if (!usuario.AccesoTodasSucursales)
                {
                    foreach (var sucId in hubUser.SucursalIds)
                        usuario.UsuarioSucursales.Add(new UsuarioSucursal { SucursalId = sucId });
                }

                _context.Usuarios.Add(usuario);
                _logger.LogInformation("[HubLogin] Usuario nuevo creado via SSO: Email={Email} HubUsuarioId={HId} EmisorId={EId} Rol={Rol}",
                    usuario.Email, hubUser.HubUsuarioId, emisor.Id, rol.Nombre);
            }
            else
            {
                // Usuario existente: sync EmisorId si cambio (multi-Hub) y registrar acceso.
                if (usuario.EmisorId != emisor.Id)
                {
                    _logger.LogInformation("[HubLogin] Sync EmisorId Usuario={Id} de {Old} a {New}",
                        usuario.Id, usuario.EmisorId, emisor.Id);
                    usuario.EmisorId = emisor.Id;
                }
                usuario.UltimoAcceso = DateTime.UtcNow;
            }

            if (usuario.Estado != EstadoUsuario.Activo)
            {
                _logger.LogWarning("[HubLogin] Usuario {Id} inactivo en Smartix", usuario.Id);
                throw new UnauthorizedAccessException("Usuario deshabilitado en Smartix.");
            }

            // Plan B Hub-as-Emisor — Fase 2 Task 16 (Bloque D).
            // Refrescamos el cache del Emisor desde SmartHub justo antes de emitir
            // el JWT: si un admin acaba de editar datos fiscales en SmartHub y el
            // webhook B.3 todavia no llego, el SSO lo trae al instante. Best-effort:
            // si el GET falla, el login continua con el cache local (capa b de defensa).
            await RefrescarEmisorFiscalDesdeHubAsync(emisor);

            await _context.SaveChangesAsync();

            // Recargar Rol si recien se creo (ya esta seteado en memoria) o por seguridad.
            if (usuario.Rol == null)
                await _context.Entry(usuario).Reference(u => u.Rol).LoadAsync();

            var sucursalIds = usuario.UsuarioSucursales.Select(us => us.SucursalId).ToList();

            var rolNombreFinal = usuario.Rol!.Nombre;

            var token = GenerateJwtToken(
                usuario.Id,
                usuario.Email,
                usuario.NombreCompleto,
                emisor.Id,
                emisor.NombreRazonSocial,
                usuario.RolId,
                rolNombreFinal,
                usuario.AccesoTodasSucursales,
                sucursalIds,
                hubUser.HubUsuarioId,
                hubUser.TokenVersion,
                hubUser.EmisoresAccesibles);

            return new LoginResponseDto
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
                RolNombre = rolNombreFinal,
                RequiereCambioPwd = false
            };
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

        /// <summary>
        /// Bug #3 (UsuarioCompartido / Plan B Hub-as-Emisor) — 2026-05-29.
        /// Refresh durable de la lista <c>emisores_accesibles</c> sin re-SSO.
        /// Pull a SmartHub via internal endpoint (server-to-server con X-Api-Key),
        /// reemision del JWT con la lista nueva preservando todo el resto de la
        /// sesion (hub_usuario_id, token_version, Emisor activo, sucursales).
        ///
        /// Cubre el caso reportado el 2026-05-29: a un SuperAdmin del Hub se le
        /// asigna un Hub nuevo (UsuariosHubs.Insert) y al abrir el wizard de
        /// Smartix el claim sigue stale, mostrando "No tenes acceso al Emisor".
        /// Tambien autosana cualquier futuro bug del SSO que deje la lista corta.
        ///
        /// Fail-open por diseno: si el Hub no responde, retornamos null y la
        /// sesion sigue funcionando con el claim viejo del JWT (el caller
        /// muestra el mismo error que antes — no rompemos nada).
        /// </summary>
        public async Task<LoginResponseDto?> RefreshEmisoresAsync(
            int usuarioId,
            int? hubUsuarioId,
            int? tokenVersion)
        {
            var usuario = await _context.Usuarios
                .Include(u => u.Rol)
                .Include(u => u.Emisor)
                .Include(u => u.UsuarioSucursales)
                .FirstOrDefaultAsync(u => u.Id == usuarioId);

            if (usuario == null)
                return null;

            if (usuario.Estado != EstadoUsuario.Activo)
            {
                _logger.LogWarning("[RefreshEmisores] Usuario {Id} inactivo en Smartix; refresh denegado.", usuarioId);
                return null;
            }

            List<int> lista;
            if (hubUsuarioId.HasValue)
            {
                // JWT SSO (HubLoginAsync emitio los claims): pull por hubUsuarioId.
                var listaSso = await _smartHubApi.GetEmisoresAccesiblesAsync(hubUsuarioId.Value);
                if (listaSso == null)
                {
                    _logger.LogWarning(
                        "[RefreshEmisores] SmartHub no respondio para hubUsuarioId={Id}; claim queda intacto.",
                        hubUsuarioId.Value);
                    return null;
                }
                lista = listaSso;
            }
            else
            {
                // Bug #3 fix de raiz (2026-05-29): JWT no-SSO (login local / Google
                // directo / Supabase Sync) no tiene usuario_hub_id. Fallback por
                // email — reconstruimos los 3 claims Hub desde el lookup.
                var lookup = await _smartHubApi.LookupHubUserByEmailAsync(usuario.Email);
                if (lookup == null)
                {
                    _logger.LogWarning(
                        "[RefreshEmisores] lookup-by-email fallo para {Email}; claim queda intacto.",
                        usuario.Email);
                    return null;
                }
                hubUsuarioId = lookup.HubUsuarioId;
                tokenVersion = lookup.TokenVersion;
                lista = lookup.EmisoresAccesibles;
                _logger.LogInformation(
                    "[RefreshEmisores] JWT no-SSO ({Email}): hidratamos via lookup-by-email a hubUsuarioId={HubId}.",
                    usuario.Email, hubUsuarioId.Value);
            }

            var sucursalIds = usuario.UsuarioSucursales.Select(us => us.SucursalId).ToList();
            var rolNombre = usuario.Rol?.Nombre ?? string.Empty;

            var token = GenerateJwtToken(
                usuario.Id,
                usuario.Email,
                usuario.NombreCompleto,
                usuario.EmisorId,
                usuario.Emisor?.NombreRazonSocial,
                usuario.RolId,
                rolNombre,
                usuario.AccesoTodasSucursales,
                sucursalIds,
                hubUsuarioId: hubUsuarioId,
                tokenVersion: tokenVersion,
                emisoresAccesibles: lista);

            _logger.LogInformation(
                "[RefreshEmisores] Usuario {UsuarioId} (hubId={HubId}) lista refrescada a {Lista}",
                usuario.Id, hubUsuarioId.Value, string.Join(",", lista));

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
                RolNombre = rolNombre,
                RequiereCambioPwd = false
            };
        }

        private static string MapHubRolToSmartixRol(string hubRol) => hubRol switch
        {
            "SuperAdmin" => "SuperAdmin",
            "AdminOrg" => "EmisorAdmin",
            _ => "EmisorAdmin"
        };

        /// <summary>
        /// Plan B Hub-as-Emisor — Fase 2 Task 16 (Bloque D).
        /// Pull fresco de los datos fiscales del Hub para refrescar el cache local
        /// del Emisor antes de emitir el JWT. Best-effort end-to-end: cualquier
        /// fallo (SmartHub apagado, payload incompleto, catalogo MH faltante) se
        /// loguea como warning y deja el Emisor con los valores que ya tenia.
        /// Nunca lanza al caller — el SSO debe completarse incluso si SmartHub no
        /// esta accesible.
        /// </summary>
        private async Task RefrescarEmisorFiscalDesdeHubAsync(Emisor emisor)
        {
            if (emisor.HubId is null) return;

            try
            {
                var payload = await _smartHubApi.GetFiscalPayloadForHubAsync(emisor.HubId.Value);
                if (payload?.Emisor is null) return;

                var fiscal = payload.Emisor;

                var catDepId = await _context.CatDepartamentos
                    .Where(d => d.Codigo == fiscal.CodDepartamento)
                    .Select(d => (int?)d.Id)
                    .FirstOrDefaultAsync();
                var catMunId = await _context.CatMunicipios
                    .Where(m => m.CodigoDepartamento == fiscal.CodDepartamento && m.Codigo == fiscal.CodMunicipio)
                    .Select(m => (int?)m.Id)
                    .FirstOrDefaultAsync();
                var catTipoId = await _context.CatTiposEstablecimiento
                    .Where(t => t.Codigo == fiscal.CodTipoEstablecimiento)
                    .Select(t => (int?)t.Id)
                    .FirstOrDefaultAsync();

                if (catDepId is null || catMunId is null || catTipoId is null)
                {
                    _logger.LogWarning(
                        "[HubLogin] Catalogo MH faltante al refrescar Emisor {EmisorId}: depto={Dep} muni={Mun} tipo={Tipo}; refresh omitido.",
                        emisor.Id, fiscal.CodDepartamento, fiscal.CodMunicipio, fiscal.CodTipoEstablecimiento);
                    return;
                }

                // Solo campos identitarios. NO tocar Mh*, Smtp*, Gmail*, LogoUrl,
                // CatAmbienteDestinoId — esos son secretos/config Smartix-only.
                emisor.Nit = fiscal.Nit;
                emisor.Nrc = fiscal.Nrc;
                emisor.NombreRazonSocial = fiscal.NombreRazonSocial;
                emisor.NombreComercial = fiscal.NombreComercial;
                emisor.CodigoActividad = fiscal.CodActividadEconomica;
                emisor.DescripcionActividad = fiscal.DescActividadEconomica;
                emisor.CatDepartamentoId = catDepId.Value;
                emisor.CatMunicipioId = catMunId.Value;
                emisor.CatTipoEstablecimientoId = catTipoId.Value;
                emisor.Direccion = fiscal.DireccionComplemento;
                if (!string.IsNullOrWhiteSpace(fiscal.TelefonoFiscal))
                    emisor.Telefono = fiscal.TelefonoFiscal;
                if (!string.IsNullOrWhiteSpace(fiscal.CorreoFiscal))
                    emisor.CorreoElectronico = fiscal.CorreoFiscal;

                _logger.LogInformation(
                    "[HubLogin] Emisor {EmisorId} (HubId={HubId}) refrescado desde SmartHub via SSO.",
                    emisor.Id, emisor.HubId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "[HubLogin] Refresh fiscal del Emisor {EmisorId} fallo; login continua con cache local.",
                    emisor.Id);
            }
        }

    }
}
