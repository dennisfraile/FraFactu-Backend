using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FraFactu.Application.Common.Settings;
using FraFactu.Application.DTOs.Auth;
using FraFactu.Application.Interfaces;
using FraFactu.Infrastructure.Persistence;
using FraFactu.Domain.Entities;
using FraFactu.Domain.Enums;

namespace FraFactu.Infrastructure.Services
{
    /// <summary>
    /// Servicio para integración con Supabase Auth
    /// </summary>
    public class SupabaseAuthService : ISupabaseAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly SupabaseSettings _supabaseSettings;
        private readonly IAuthService _authService;
        private readonly ISmartHubApiService _smartHubApi;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SupabaseAuthService> _logger;

        public SupabaseAuthService(
            ApplicationDbContext context,
            IOptions<SupabaseSettings> supabaseSettings,
            IAuthService authService,
            ISmartHubApiService smartHubApi,
            IHttpClientFactory httpClientFactory,
            ILogger<SupabaseAuthService> logger)
        {
            _context = context;
            _supabaseSettings = supabaseSettings.Value;
            _authService = authService;
            _smartHubApi = smartHubApi;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        /// <summary>
        /// Valida un token de Supabase llamando al endpoint /auth/v1/user
        /// </summary>
        public async Task<bool> ValidateSupabaseToken(string token)
        {
            try
            {
                _logger.LogInformation("[SUPABASE AUTH] Validating Supabase token...");

                var httpClient = _httpClientFactory.CreateClient();

                var request = new HttpRequestMessage(HttpMethod.Get,
                    $"{_supabaseSettings.Url}/auth/v1/user");

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                request.Headers.Add("apikey", _supabaseSettings.AnonKey);

                var response = await httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("[SUPABASE AUTH] ✅ Token validation successful");
                }
                else
                {
                    _logger.LogWarning("[SUPABASE AUTH] ❌ Token validation failed. Status: {StatusCode}", response.StatusCode);
                }

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SUPABASE AUTH] ❌ Exception during token validation");
                return false;
            }
        }

        /// <summary>
        /// Sincroniza un usuario autenticado con Supabase con la base de datos local
        /// Si el usuario no existe, lo crea automáticamente con un Emisor por defecto
        /// </summary>
        public async Task<LoginResponseDto?> SyncSupabaseUser(SupabaseSyncDto dto)
        {
            try
            {
                _logger.LogInformation("[SUPABASE SYNC] Starting user sync for email: {Email}", dto.Email);
                _logger.LogInformation("[SUPABASE SYNC] Full Name: {FullName}, SupabaseUserId: {SupabaseUserId}", dto.FullName, dto.SupabaseUserId);

                // Buscar usuario existente por email
                _logger.LogInformation("[SUPABASE SYNC] Searching for existing user with email: {Email}", dto.Email);
                var usuario = await _context.Usuarios
                    .Include(u => u.Emisor)
                    .Include(u => u.Rol)
                        .ThenInclude(r => r.RolesPermisos!)
                            .ThenInclude(rp => rp.Permiso)
                    .Include(u => u.UsuarioSucursales)
                    .FirstOrDefaultAsync(u => u.Email == dto.Email);

                if (usuario == null)
                {
                    _logger.LogInformation("[SUPABASE SYNC] ✨ New user detected. Creating user and default Emisor...");

                    // Crear nuevo usuario con rol EmisorAdmin por defecto
                    var emisorAdminRole = await _context.Roles
                        .Include(r => r.RolesPermisos!)
                            .ThenInclude(rp => rp.Permiso)
                        .FirstOrDefaultAsync(r => r.Nombre == "EmisorAdmin");

                    if (emisorAdminRole == null)
                    {
                        _logger.LogError("[SUPABASE SYNC] ❌ Fatal: 'EmisorAdmin' role not found in database");
                        throw new InvalidOperationException("Rol 'EmisorAdmin' no encontrado en la base de datos");
                    }

                    _logger.LogInformation("[SUPABASE SYNC] Found EmisorAdmin role (Id: {RoleId})", emisorAdminRole.Id);

                    // Auto-crear Emisor por defecto para usuarios de Google
                    _logger.LogInformation("[SUPABASE SYNC] Creating default Emisor for new Google user...");

                    // Obtener catálogos por defecto (primeros de cada tabla)
                    var defaultDepartamento = await _context.CatDepartamentos.FirstOrDefaultAsync();
                    var defaultMunicipio = await _context.CatMunicipios.FirstOrDefaultAsync();

                    if (defaultDepartamento == null || defaultMunicipio == null)
                    {
                        _logger.LogError("[SUPABASE SYNC] ❌ Required catalog data missing (Departamento or Municipio)");
                        throw new InvalidOperationException("Datos de catálogo requeridos no encontrados. Asegúrese de que los catálogos estén cargados.");
                    }

                    var nuevoEmisor = new Emisor
                    {
                        Nit = "PENDIENTE",
                        Nrc = "PENDIENTE",
                        NombreRazonSocial = $"{dto.FullName ?? "Usuario"} - Compañía",
                        NombreComercial = dto.FullName ?? "Usuario",
                        CodigoActividad = "00000", // Código placeholder
                        DescripcionActividad = "Actividad pendiente de configurar",
                        CorreoElectronico = dto.Email,
                        Telefono = "0000-0000", // Placeholder
                        CatDepartamentoId = defaultDepartamento.Id,
                        CatMunicipioId = defaultMunicipio.Id,
                        Direccion = "Dirección pendiente de configurar",
                        CatAmbienteDestinoId = 1, // 1 = Modo prueba ("00")
                        MhUsuario = string.Empty,
                        MhClaveApi = string.Empty,
                        MhLlavePrivada = string.Empty,
                        MhLlavePublica = string.Empty,
                        MhPassPrivada = string.Empty
                    };

                    _context.Emisores.Add(nuevoEmisor);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("[SUPABASE SYNC] ✅ Default Emisor created with Id: {EmisorId}", nuevoEmisor.Id);

                    usuario = new Usuario
                    {
                        Email = dto.Email,
                        NombreCompleto = dto.FullName ?? dto.Email,
                        RolId = emisorAdminRole.Id,
                        Estado = EstadoUsuario.Activo,
                        ProveedorAuth = ProveedorAutenticacion.Google,
                        ProveedorExternoId = dto.SupabaseUserId,
                        UltimoAcceso = DateTime.UtcNow,
                        RequiereCambioPwd = false, // Usuarios OAuth no requieren cambio de contraseña
                        PasswordHash = null, // No tiene contraseña local
                        EmisorId = nuevoEmisor.Id, // ✅ Asignar el Emisor recién creado
                        AccesoTodasSucursales = true
                    };

                    _context.Usuarios.Add(usuario);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("[SUPABASE SYNC] ✅ New user created with Id: {UserId}, EmisorId: {EmisorId}", usuario.Id, usuario.EmisorId);

                    // Recargar con includes
                    usuario = await _context.Usuarios
                        .Include(u => u.Emisor)
                        .Include(u => u.Rol)
                            .ThenInclude(r => r.RolesPermisos!)
                                .ThenInclude(rp => rp.Permiso)
                        .Include(u => u.UsuarioSucursales)
                        .FirstOrDefaultAsync(u => u.Id == usuario.Id);

                    _logger.LogInformation("[SUPABASE SYNC] User data reloaded with all relationships");
                }
                else
                {
                    _logger.LogInformation("[SUPABASE SYNC] Existing user found (Id: {UserId}, EmisorId: {EmisorId})", usuario.Id, usuario.EmisorId);

                    // Actualizar información del usuario existente
                    usuario.UltimoAcceso = DateTime.UtcNow;

                    // Si no tiene ProveedorExternoId, asignarlo
                    if (string.IsNullOrEmpty(usuario.ProveedorExternoId))
                    {
                        _logger.LogInformation("[SUPABASE SYNC] Linking user to Supabase provider");
                        usuario.ProveedorAuth = ProveedorAutenticacion.Google;
                        usuario.ProveedorExternoId = dto.SupabaseUserId;
                    }

                    // Actualizar nombre si cambió
                    if (!string.IsNullOrEmpty(dto.FullName) && usuario.NombreCompleto != dto.FullName)
                    {
                        _logger.LogInformation("[SUPABASE SYNC] Updating user name from '{OldName}' to '{NewName}'", usuario.NombreCompleto, dto.FullName);
                        usuario.NombreCompleto = dto.FullName;
                    }

                    await _context.SaveChangesAsync();
                    _logger.LogInformation("[SUPABASE SYNC] User information updated");
                }

                // Verificar que el usuario esté activo
                if (usuario == null || usuario.Estado != EstadoUsuario.Activo)
                {
                    _logger.LogWarning("[SUPABASE SYNC] ❌ User is null or inactive. Estado: {Estado}", usuario?.Estado);
                    return null;
                }

                // Generar JWT personalizado para la aplicación
                _logger.LogInformation("[SUPABASE SYNC] Generating JWT token with claims:");
                _logger.LogInformation("  - UserId: {UserId}", usuario.Id);
                _logger.LogInformation("  - Email: {Email}", usuario.Email);
                _logger.LogInformation("  - EmisorId: {EmisorId}", usuario.EmisorId);
                _logger.LogInformation("  - EmisorNombre: {EmisorNombre}", usuario.Emisor?.NombreRazonSocial);
                _logger.LogInformation("  - RolId: {RolId}", usuario.RolId);
                _logger.LogInformation("  - RolNombre: {RolNombre}", usuario.Rol.Nombre);
                _logger.LogInformation("  - AccesoTodasSucursales: {AccesoTodas}", usuario.AccesoTodasSucursales);

                var sucursalIds = usuario.UsuarioSucursales.Select(us => us.SucursalId).ToList();

                // Bug #3 fix de raiz (2026-05-29): lookup en SmartHub por email
                // para propagar usuario_hub_id + token_version + emisores_accesibles
                // al JWT. Sin esto, el JWT emitido vendria sin claims Hub y el
                // wizard cross-Emisor (Plan-C2) bloquearia con "No tenes acceso al
                // Emisor". Best-effort: si Hub no responde, sale sin claims Hub.
                var hubClaims = await _smartHubApi.LookupHubUserByEmailAsync(usuario.Email);
                if (hubClaims != null)
                {
                    _logger.LogInformation(
                        "[SUPABASE SYNC] Hub claims hidratados: hubUsuarioId={HubId} tokenVersion={Tv} emisoresAccesibles={Lista}",
                        hubClaims.HubUsuarioId, hubClaims.TokenVersion, string.Join(",", hubClaims.EmisoresAccesibles));
                }

                var token = _authService.GenerateJwtToken(
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

                _logger.LogInformation("[SUPABASE SYNC] ✅ JWT token generated successfully");

                // Obtener permisos del rol
                var permisos = usuario.Rol.RolesPermisos?
                    .Select(rp => rp.Permiso.Nombre)
                    .ToList() ?? new List<string>();

                _logger.LogInformation("[SUPABASE SYNC] User has {PermisosCount} permissions", permisos.Count);

                // Construir respuesta
                var response = new LoginResponseDto
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
                    Permisos = permisos,
                    RequiereCambioPwd = false // Usuarios OAuth nunca requieren cambio de contraseña
                };

                _logger.LogInformation("[SUPABASE SYNC] ✅ Sync completed successfully for user: {Email}", dto.Email);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SUPABASE SYNC] ❌ Fatal error during user sync for email: {Email}", dto.Email);
                throw;
            }
        }
    }
}
