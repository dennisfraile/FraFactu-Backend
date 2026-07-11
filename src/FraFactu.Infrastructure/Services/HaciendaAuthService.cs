using System.Net.Http.Json;
using FraFactu.Application.Interfaces;
using FraFactu.Application.Interfaces.Hacienda;
using FraFactu.Domain.Entities;
using FraFactu.Domain.Enums;
using FraFactu.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FraFactu.Infrastructure.Services
{
    public class HaciendaAuthService : IHaciendaAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<HaciendaAuthService> _logger;
        private readonly IEncryptionService _encryptionService;

        private const string UrlAuthPruebas = "https://apitest.dtes.mh.gob.sv/seguridad/auth";
        private const string UrlAuthProduccion = "https://api.dtes.mh.gob.sv/seguridad/auth";

        public HaciendaAuthService(
            HttpClient httpClient,
            ApplicationDbContext context,
            IMemoryCache cache,
            ILogger<HaciendaAuthService> logger,
            IEncryptionService encryptionService)
        {
            _httpClient = httpClient;
            _context = context;
            _cache = cache;
            _encryptionService = encryptionService;
            _logger = logger;
        }

        public async Task<string> ObtenerTokenAsync(int emisorId)
        {
            // 1. Obtener emisor para determinar ambiente y credenciales
            var emisor = await _context.Emisores
                .Include(e => e.AmbienteDestino)
                .FirstOrDefaultAsync(e => e.Id == emisorId);
            if (emisor == null)
            {
                throw new Exception($"Emisor con ID {emisorId} no encontrado.");
            }

            // 2. Intentar obtener del caché (incluye ambienteId para evitar usar token de pruebas en producción)
            string cacheKey = $"MH_Token_{emisorId}_{emisor.CatAmbienteDestinoId}";
            if (_cache.TryGetValue(cacheKey, out string? cachedToken) && !string.IsNullOrEmpty(cachedToken))
            {
                return cachedToken;
            }

            // 3. Seleccionar credenciales según ambiente
            var esProd = emisor.CatAmbienteDestinoId == 2;
            var mhUsuario = esProd ? emisor.MhUsuarioProd : emisor.MhUsuario;
            var mhClaveApiEnc = esProd ? emisor.MhClaveApiProd : emisor.MhClaveApi;

            if (string.IsNullOrEmpty(mhUsuario) || string.IsNullOrEmpty(mhClaveApiEnc))
            {
                var ambienteDesc = esProd ? "producción" : "pruebas";
                throw new Exception($"El emisor no tiene configuradas las credenciales de {ambienteDesc} del Ministerio de Hacienda.");
            }

            // 4. Solicitar nuevo token
            var token = await SolicitarTokenRemoto(emisor, mhUsuario, mhClaveApiEnc);

            // 5. Guardar en caché (con expiración segura, ej: 23 horas si dura 24)
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromHours(23));

            _cache.Set(cacheKey, token, cacheEntryOptions);

            return token;
        }

        private string DecryptField(string? value, string fieldName, bool optional = false)
        {
            if (string.IsNullOrEmpty(value))
            {
                if (optional) return string.Empty;
                throw new InvalidOperationException(
                    $"El campo '{fieldName}' del emisor está vacío o no configurado. " +
                    "Configure las credenciales de Hacienda en la sección del emisor.");
            }

            try
            {
                return _encryptionService.Decrypt(value);
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException(
                    $"Error al desencriptar '{fieldName}' del emisor. " +
                    "Verifique que la Encryption:MasterKey en Azure App Settings coincida con la que se usó al guardar las credenciales.", ex);
            }
        }

        private async Task<string> SolicitarTokenRemoto(Emisor emisor, string mhUsuario, string mhClaveApiEnc)
        {
            try
            {
                _logger.LogInformation("=== INICIO AUTENTICACIÓN CON HACIENDA ===");

                var esProd = emisor.CatAmbienteDestinoId == 2;
                var url = esProd ? UrlAuthProduccion : UrlAuthPruebas;
                _logger.LogInformation("[AUTH-DEBUG] URL autenticación: {Url}", url);
                _logger.LogInformation("[AUTH-DEBUG] Ambiente ID: {AmbienteId}, EsProd: {EsProd}", emisor.CatAmbienteDestinoId, esProd);
                _logger.LogInformation("[AUTH-DEBUG] Usuario MH: {Usuario}", mhUsuario);
                _logger.LogInformation("[AUTH-DEBUG] Clave API (longitud): {ClaveLength} chars", mhClaveApiEnc?.Length ?? 0);

                // Verificar que las credenciales no tengan espacios o caracteres extraños
                if (mhUsuario.Contains(" ") || mhUsuario.Contains("\n"))
                {
                    _logger.LogWarning("[AUTH-DEBUG] ADVERTENCIA: MhUsuario contiene espacios o saltos de línea!");
                }
                if (mhClaveApiEnc?.Contains("\n") == true || mhClaveApiEnc?.Contains("\r") == true)
                {
                    _logger.LogWarning("[AUTH-DEBUG] ADVERTENCIA: MhClaveApi contiene saltos de línea!");
                }

                var claveApiDesencriptada = DecryptField(mhClaveApiEnc, esProd ? "MhClaveApiProd" : "MhClaveApi");

                var requestBody = new Dictionary<string, string>
                {
                    { "user", mhUsuario },
                    { "pwd", claveApiDesencriptada }
                };

                using var requestContent = new FormUrlEncodedContent(requestBody);

                // Limpiar headers previos y configurar User-Agent
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "Facturacion-Backend-v1");

                // Log de headers de autenticación
                _logger.LogInformation("=== AUTH REQUEST HEADERS ===");
                foreach (var header in _httpClient.DefaultRequestHeaders)
                {
                    _logger.LogInformation("[AUTH-HEADER] {Key}: {Value}",
                        header.Key, string.Join(", ", header.Value));
                }

                // Log del Content-Type que se enviará
                _logger.LogInformation("[AUTH-DEBUG] Content-Type: {ContentType}",
                    requestContent.Headers.ContentType?.ToString() ?? "N/A");

                _logger.LogInformation("[AUTH-DEBUG] Enviando request POST a Hacienda...");
                var response = await _httpClient.PostAsync(url, requestContent);

                _logger.LogInformation("=== AUTH RESPONSE ===");
                _logger.LogInformation("[AUTH-DEBUG] HTTP Status: {StatusCode} ({StatusCodeNumber})",
                    response.ReasonPhrase, (int)response.StatusCode);

                // Log de response headers
                _logger.LogInformation("=== AUTH RESPONSE HEADERS ===");
                foreach (var header in response.Headers)
                {
                    _logger.LogInformation("[AUTH-RESPONSE-HEADER] {Key}: {Value}",
                        header.Key, string.Join(", ", header.Value));
                }

                var contentString = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("[AUTH-DEBUG] Response Body:\n{ResponseBody}", contentString);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("=== ERROR EN AUTENTICACIÓN ===");
                    _logger.LogError("[AUTH-ERROR] HTTP Status: {StatusCode}", (int)response.StatusCode);
                    _logger.LogError("[AUTH-ERROR] Reason: {ReasonPhrase}", response.ReasonPhrase);
                    _logger.LogError("[AUTH-ERROR] Response Body: {Content}", contentString);
                    throw new Exception($"Error HTTP al autenticar con MH: HTTP {(int)response.StatusCode} - {response.ReasonPhrase}. Detalle: {contentString}");
                }

                var authResult = System.Text.Json.JsonSerializer.Deserialize<AuthResponseMhDto>(contentString, new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                _logger.LogInformation("[AUTH-DEBUG] Respuesta deserializada - Status: {Status}", authResult?.Status);

                if (authResult?.Status != "OK" || authResult?.Body?.Token == null)
                {
                    // Intentar leer mensaje de error del API
                    var errorMsg = "Respuesta de autenticación inválida.";
                    // Si el status no es OK, verificar si viene 'message' o 'error'
                    // Estructura error: { "status": "ERROR", "message": "Usuario no valido", ... }

                    // Hack rápido para mapear error dinámicamente si el DTO no coincide
                    if (contentString.Contains("\"message\""))
                    {
                        // Aquí podríamos parsear mejor el mensaje, por ahora logueamos el raw
                        errorMsg = $"Error de MH: {contentString}";
                    }

                    _logger.LogError("=== ERROR EN RESPUESTA DE AUTENTICACIÓN ===");
                    _logger.LogError("[AUTH-ERROR] Status recibido: {Status}", authResult?.Status ?? "NULL");
                    _logger.LogError("[AUTH-ERROR] Body es null: {IsNull}", authResult?.Body == null);
                    _logger.LogError("[AUTH-ERROR] Token es null: {IsNull}", authResult?.Body?.Token == null);
                    _logger.LogError("[AUTH-ERROR] Respuesta completa: {Content}", contentString);
                    throw new Exception(errorMsg);
                }

                var token = authResult.Body.Token;

                // El API de MH devuelve el token con prefijo "Bearer ".
                // Lo removemos aquí porque AuthenticationHeaderValue("Bearer", token) lo agrega automáticamente.
                if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    token = token.Substring("Bearer ".Length).Trim();
                }

                _logger.LogInformation("[AUTH-DEBUG] Token obtenido exitosamente (longitud: {Length})", token.Length);
                _logger.LogInformation("[AUTH-DEBUG] Token (primeros 30 chars): '{TokenPrefix}...'",
                    token.Length > 30 ? token.Substring(0, 30) : token);
                _logger.LogInformation("=== FIN AUTENTICACIÓN ===");

                return token;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== EXCEPCIÓN AL SOLICITAR TOKEN A MH ===");
                _logger.LogError("[AUTH-ERROR] Tipo de excepción: {ExceptionType}", ex.GetType().Name);
                _logger.LogError("[AUTH-ERROR] Mensaje: {Message}", ex.Message);
                throw;
            }
        }

        // Clases internas para deserializar respuesta de MH
        private class AuthResponseMhDto
        {
            public string Status { get; set; } = string.Empty;
            public AuthBodyDto? Body { get; set; }
        }

        private class AuthBodyDto
        {
            public string User { get; set; } = string.Empty;
            public string Token { get; set; } = string.Empty;
            // Otros campos como rols, tokenType etc. no son estrictamente necesarios para obtener el token
            public List<string> Roles { get; set; } = new();
        }
    }
}
