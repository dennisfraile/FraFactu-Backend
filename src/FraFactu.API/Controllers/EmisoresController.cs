using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using FraFactu.Application.Common.Settings;
using FraFactu.Application.DTOs.Common;
using FraFactu.Application.DTOs.Emisores;
using FraFactu.Application.Interfaces;

namespace FraFactu.API.Controllers
{
    /// <summary>
    /// Controller para gestión de Emisores (empresas)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EmisoresController : ControllerBase
    {
        private readonly IEmisorService _emisorService;
        private readonly ISupabaseStorageService _storageService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IValidator<CreateEmisorDto> _createValidator;
        private readonly IValidator<UpdateEmisorDto> _updateValidator;
        private readonly IValidator<UpdatePerfilEmisorDto> _updatePerfilValidator;
        private readonly IGmailApiService _gmailApiService;
        private readonly IEncryptionService _encryptionService;
        private readonly GoogleAuthSettings _googleAuthSettings;
        private readonly ILogger<EmisoresController> _logger;

        public EmisoresController(
            IEmisorService emisorService,
            ISupabaseStorageService storageService,
            ICurrentUserService currentUserService,
            IValidator<CreateEmisorDto> createValidator,
            IValidator<UpdateEmisorDto> updateValidator,
            IValidator<UpdatePerfilEmisorDto> updatePerfilValidator,
            IGmailApiService gmailApiService,
            IEncryptionService encryptionService,
            IOptions<GoogleAuthSettings> googleAuthSettings,
            ILogger<EmisoresController> logger)
        {
            _emisorService = emisorService;
            _storageService = storageService;
            _currentUserService = currentUserService;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _updatePerfilValidator = updatePerfilValidator;
            _gmailApiService = gmailApiService;
            _encryptionService = encryptionService;
            _googleAuthSettings = googleAuthSettings.Value;
            _logger = logger;
        }

        /// <summary>
        /// UsuarioCompartido + Plan B Hub-as-Emisor: lista la metadata (id,
        /// nombre, NIT) de los Emisores que vienen en el claim
        /// <c>emisores_accesibles</c> del JWT actual. La usa el selector de
        /// Emisor del FE (dropdown post-SSO + alternancia desde prefills) para
        /// mostrar nombres en lugar de IDs. Cualquier rol con sesion valida
        /// puede llamarlo: el filtro lo aplica el claim, no el rol.
        /// </summary>
        [HttpGet("accesibles")]
        [ProducesResponseType(typeof(IEnumerable<EmisorResumenDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAccesibles()
        {
            var ids = FraFactu.API.Helpers.ScopeHelper.GetEmisoresAccesibles(User);
            var resumenes = await _emisorService.GetResumenByIdsAsync(ids);
            return Ok(resumenes);
        }

        /// <summary>
        /// Obtener todos los emisores (Solo SuperAdmin)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(typeof(PaginatedResponse<EmisorDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var request = new PaginatedRequest
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var result = await _emisorService.GetAllAsync(request);
            return Ok(result);
        }

        /// <summary>
        /// Obtener emisor por ID (Solo SuperAdmin)
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(typeof(EmisorDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var emisor = await _emisorService.GetByIdAsync(id);
            if (emisor == null)
                return NotFound(new { error = $"Emisor con ID {id} no encontrado" });

            return Ok(emisor);
        }

        /// <summary>
        /// Obtener perfil del emisor actual (EmisorAdmin, GerenteSucursal)
        /// </summary>
        [HttpGet("mi-perfil")]
        [Authorize(Roles = "EmisorAdmin,GerenteSucursal,Cajero,Contador,Auditor")]
        [ProducesResponseType(typeof(EmisorDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMiPerfil()
        {
            try
            {
                _logger.LogInformation("[EMISOR CONTROLLER] GetMiPerfil called by user");

                // Log all claims for debugging
                _logger.LogWarning("[DEV ONLY - SENSITIVE DATA] User claims:");
                foreach (var claim in User.Claims)
                {
                    _logger.LogWarning("  - {ClaimType}: {ClaimValue}", claim.Type, claim.Value);
                }

                // Intentar obtener EmisorId del token
                var emisorIdClaim = User.FindFirst("EmisorId")?.Value;

                if (string.IsNullOrEmpty(emisorIdClaim))
                {
                    _logger.LogError("[EMISOR CONTROLLER] EmisorId claim not found in JWT token");
                    _logger.LogError("[EMISOR CONTROLLER] This usually means the user was created before auto-Emisor feature was implemented");
                    return Unauthorized(new
                    {
                        error = "EmisorId no encontrado en el token",
                        detail = "Por favor contacte al administrador para que le asigne un Emisor, o cierre sesión e inicie sesión nuevamente"
                    });
                }

                if (!int.TryParse(emisorIdClaim, out int emisorId))
                {
                    _logger.LogError("[EMISOR CONTROLLER] EmisorId claim value is not a valid integer: {EmisorIdClaim}", emisorIdClaim);
                    return Unauthorized(new { error = "EmisorId inválido en el token" });
                }

                _logger.LogInformation("[EMISOR CONTROLLER] Fetching profile for EmisorId: {EmisorId}", emisorId);

                var emisor = await _emisorService.GetMiPerfilAsync(emisorId);

                if (emisor == null)
                {
                    _logger.LogWarning("[EMISOR CONTROLLER] Emisor profile not found for Id: {EmisorId}", emisorId);
                    return NotFound(new { error = "Perfil de emisor no encontrado" });
                }

                _logger.LogInformation("[EMISOR CONTROLLER] Profile retrieved successfully for Emisor: {EmisorNombre}", emisor.NombreRazonSocial);
                return Ok(emisor);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[EMISOR CONTROLLER] Unexpected error in GetMiPerfil");
                return StatusCode(500, new { error = "Error interno del servidor", detail = ex.Message });
            }
        }

        /// <summary>
        /// Crear nuevo emisor - Onboarding (Solo SuperAdmin)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(typeof(EmisorDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateEmisorDto dto)
        {
            var validationResult = await _createValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

                return BadRequest(new ValidationProblemDetails(errors)
                {
                    Title = "Errores de validación",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            try
            {
                var emisor = await _emisorService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = emisor.Id }, emisor);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Actualizar emisor (Solo SuperAdmin)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(typeof(EmisorDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateEmisorDto dto)
        {
            var validationResult = await _updateValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

                return BadRequest(new ValidationProblemDetails(errors)
                {
                    Title = "Errores de validación",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            try
            {
                var emisor = await _emisorService.UpdateAsync(id, dto);
                if (emisor == null)
                    return NotFound(new { error = $"Emisor con ID {id} no encontrado" });

                return Ok(emisor);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Actualizar perfil del emisor actual (EmisorAdmin)
        /// </summary>
        [HttpPut("mi-perfil")]
        [Authorize(Roles = "EmisorAdmin")]
        [ProducesResponseType(typeof(EmisorDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateMiPerfil([FromBody] UpdatePerfilEmisorDto dto)
        {
            var validationResult = await _updatePerfilValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

                return BadRequest(new ValidationProblemDetails(errors)
                {
                    Title = "Errores de validación",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            var emisorId = int.Parse(User.FindFirst("EmisorId")?.Value
                ?? throw new UnauthorizedAccessException("EmisorId no encontrado"));

            var emisor = await _emisorService.UpdateMiPerfilAsync(emisorId, dto);
            if (emisor == null)
                return NotFound(new { error = "Perfil de emisor no encontrado" });

            return Ok(emisor);
        }

        /// <summary>
        /// Alternar estado activo/inactivo del emisor (Solo SuperAdmin)
        /// </summary>
        [HttpPatch("{id}/toggle-active")]
        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ToggleActive(int id)
        {
            try
            {
                var isActive = await _emisorService.ToggleActiveAsync(id);
                return Ok(new { id, activo = isActive });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { error = $"Emisor con ID {id} no encontrado" });
            }
        }

        /// <summary>
        /// Desactivar emisor (Solo SuperAdmin)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Deactivate(int id)
        {
            var result = await _emisorService.DeactivateAsync(id);
            if (!result)
                return NotFound(new { error = $"Emisor con ID {id} no encontrado" });

            return Ok(new { message = "Emisor desactivado exitosamente", emisorId = id });
        }

        /// <summary>
        /// Subir logo de la empresa (EmisorAdmin)
        /// </summary>
        [HttpPost("logo")]
        [Authorize(Roles = "EmisorAdmin,SuperAdmin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UploadLogo(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "No se ha proporcionado ningún archivo" });

            // Validar tipo de archivo (solo imágenes)
            if (!file.ContentType.StartsWith("image/"))
                return BadRequest(new { error = "El archivo debe ser una imagen (jpg, png, etc)" });

            if (file.Length > 2 * 1024 * 1024) // 2MB
                return BadRequest(new { error = "El tamaño del logo no debe exceder 2MB" });

            try
            {
                var userId = _currentUserService.GetUsuarioId();
                if (!userId.HasValue)
                    return Unauthorized(new { error = "Usuario no identificado" });

                // Obtener EmisorId
                var emisorId = _currentUserService.GetEmisorId();
                if (emisorId == 0)
                {
                    return Unauthorized(new { error = "No se pudo identificar la empresa del usuario" });
                }

                // 1. Subir archivo a Supabase
                using var stream = file.OpenReadStream();
                var publicUrl = await _storageService.UploadLogoAsync(stream, file.FileName, userId.Value.ToString());

                // 2. Actualizar entidad Emisor
                var result = await _emisorService.UpdateLogoAsync(emisorId, publicUrl);
                if (!result)
                    return NotFound(new { error = "Emisor no encontrado" });

                return Ok(new { Url = publicUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading logo");
                return StatusCode(500, new { error = "Error interno al subir el logo", detail = ex.Message });
            }
        }

        /// <summary>
        /// Subir logo de un emisor específico (Solo SuperAdmin)
        /// </summary>
        [HttpPost("{id}/logo")]
        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UploadLogoForEmisor(int id, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "No se ha proporcionado ningún archivo" });

            // Validar tipo de archivo (solo imágenes)
            if (!file.ContentType.StartsWith("image/"))
                return BadRequest(new { error = "El archivo debe ser una imagen (jpg, png, etc)" });

            if (file.Length > 2 * 1024 * 1024) // 2MB
                return BadRequest(new { error = "El tamaño del logo no debe exceder 2MB" });

            try
            {
                // Obtener userId del claim para la ruta de storage
                var userId = _currentUserService.GetUsuarioId();

                // Subir a Supabase
                using var stream = file.OpenReadStream();
                var publicUrl = await _storageService.UploadLogoAsync(stream, file.FileName, id.ToString());

                // Actualizar el emisor especificado (no el del token)
                var result = await _emisorService.UpdateLogoAsync(id, publicUrl);
                if (!result)
                    return NotFound(new { error = $"Emisor con ID {id} no encontrado" });

                return Ok(new { url = publicUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading logo for emisor {EmisorId}", id);
                return StatusCode(500, new { error = "Error interno al subir el logo", detail = ex.Message });
            }
        }

        /// <summary>
        /// Subir logo del sistema (Solo SuperAdmin)
        /// </summary>
        [HttpPost("system-logo")]
        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UploadSystemLogo(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "No se ha proporcionado ningún archivo" });

            // Validar tipo de archivo (solo imágenes)
            if (!file.ContentType.StartsWith("image/"))
                return BadRequest(new { error = "El archivo debe ser una imagen (jpg, png, etc)" });

            if (file.Length > 2 * 1024 * 1024) // 2MB
                return BadRequest(new { error = "El tamaño del logo no debe exceder 2MB" });

            try
            {
                using var stream = file.OpenReadStream();
                var publicUrl = await _storageService.UploadSystemLogoAsync(stream, file.FileName);

                return Ok(new { url = publicUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading system logo");
                return StatusCode(500, new { error = "Error interno al subir el logo del sistema", detail = ex.Message });
            }
        }

        /// <summary>
        /// Obtener URL del logo del sistema
        /// </summary>
        [HttpGet("system-logo")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetSystemLogo()
        {
            var baseUrl = _storageService.GetSystemLogoUrl();
            return Ok(new { url = baseUrl });
        }

        // ====== GMAIL OAUTH2 ======

        /// <summary>
        /// Genera la URL de autorización de Gmail OAuth2
        /// </summary>
        [HttpGet("gmail-connect")]
        [Authorize(Roles = "EmisorAdmin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GmailConnect()
        {
            try
            {
                var emisorId = _currentUserService.GetEmisorId();

                // Crear state encriptado con emisorId y expiración
                var statePayload = JsonSerializer.Serialize(new
                {
                    emisorId,
                    exp = DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds()
                });
                var encryptedState = _encryptionService.Encrypt(statePayload);

                var authorizationUrl = _gmailApiService.GenerateAuthorizationUrl(encryptedState);

                return Ok(new { authorizationUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando URL de autorización de Gmail");
                return StatusCode(500, new { error = "Error al generar URL de autorización" });
            }
        }

        /// <summary>
        /// Intercambia el código de autorización de Gmail por tokens.
        /// El frontend recibe el callback de Google y envía el código aquí.
        /// </summary>
        [HttpPost("gmail-exchange-code")]
        [Authorize(Roles = "EmisorAdmin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GmailExchangeCode([FromBody] GmailExchangeCodeDto dto)
        {
            try
            {
                if (string.IsNullOrEmpty(dto.Code) || string.IsNullOrEmpty(dto.State))
                    return BadRequest(new { error = "Código o estado faltante" });

                // Desencriptar y validar state
                var stateJson = _encryptionService.Decrypt(dto.State);
                var stateData = JsonSerializer.Deserialize<JsonElement>(stateJson);
                var emisorId = stateData.GetProperty("emisorId").GetInt32();
                var exp = stateData.GetProperty("exp").GetInt64();

                // Verificar que el emisorId del state coincida con el del usuario autenticado
                var currentEmisorId = _currentUserService.GetEmisorId();
                if (emisorId != currentEmisorId)
                    return Forbid();

                if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > exp)
                    return BadRequest(new { error = "expired", message = "La autorización ha expirado. Intente nuevamente." });

                // Intercambiar código por tokens
                var (encryptedRefreshToken, email) = await _gmailApiService.ExchangeCodeForTokensAsync(dto.Code);

                // Guardar en el emisor
                var emisor = await _emisorService.GetByIdAsync(emisorId);
                if (emisor == null)
                    return NotFound(new { error = "Emisor no encontrado" });

                await _emisorService.UpdateGmailOAuthAsync(emisorId, encryptedRefreshToken, email);

                _logger.LogInformation("[GMAIL-API] Emisor {EmisorId} conectó Gmail: {Email}", emisorId, email);

                return Ok(new { success = true, email });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error intercambiando código de Gmail OAuth2");
                return StatusCode(500, new { error = "Error al conectar Gmail" });
            }
        }

        /// <summary>
        /// Desconectar cuenta de Gmail OAuth2
        /// </summary>
        [HttpDelete("gmail-disconnect")]
        [Authorize(Roles = "EmisorAdmin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GmailDisconnect()
        {
            try
            {
                var emisorId = _currentUserService.GetEmisorId();

                await _emisorService.DisconnectGmailOAuthAsync(emisorId);

                _logger.LogInformation("[GMAIL-API] Emisor {EmisorId} desconectó Gmail", emisorId);

                return Ok(new { message = "Cuenta de Gmail desconectada exitosamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error desconectando Gmail OAuth2");
                return StatusCode(500, new { error = "Error al desconectar Gmail" });
            }
        }
    }
}
