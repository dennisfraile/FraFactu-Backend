using System.Net.Http.Json;
using FraFactu.Application.Common.Settings;
using FraFactu.Application.DTOs.Auth;
using FraFactu.Application.DTOs.Hub;
using FraFactu.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FraFactu.Infrastructure.Services
{
    public class SmartHubApiService : ISmartHubApiService
    {
        private static readonly TimeSpan TokenVersionCacheTtl = TimeSpan.FromSeconds(60);

        private readonly HttpClient _httpClient;
        private readonly SmartHubSettings _settings;
        private readonly ILogger<SmartHubApiService> _logger;
        private readonly IMemoryCache _cache;

        public SmartHubApiService(
            HttpClient httpClient,
            IOptions<SmartHubSettings> settings,
            ILogger<SmartHubApiService> logger,
            IMemoryCache cache)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _logger = logger;
            _cache = cache;
        }

        public async Task<HubUserInfoDto?> ValidateExchangeCodeAsync(string code, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(_settings.BaseUrl) || string.IsNullOrWhiteSpace(_settings.ApiKey))
            {
                _logger.LogError("[SmartHubApi] SmartHub.BaseUrl o SmartHub.ApiKey no configurados");
                return null;
            }

            try
            {
                var requestUrl = $"{_settings.BaseUrl.TrimEnd('/')}/api/auth/validate-code";
                var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
                {
                    Content = JsonContent.Create(new { code })
                };
                request.Headers.Add("X-Api-Key", _settings.ApiKey);

                var response = await _httpClient.SendAsync(request, ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("[SmartHubApi] validate-code fallo: {Status}", response.StatusCode);
                    return null;
                }

                var raw = await response.Content.ReadFromJsonAsync<HubValidateCodeResponse>(cancellationToken: ct);
                if (raw == null) return null;

                return new HubUserInfoDto
                {
                    HubUsuarioId = raw.HubUsuarioId,
                    Email = raw.Email,
                    NombreCompleto = raw.NombreCompleto,
                    AvatarUrl = raw.AvatarUrl,
                    OrganizacionId = raw.OrganizacionId,
                    SucursalIds = raw.SucursalIds ?? new List<int>(),
                    Rol = raw.Rol ?? string.Empty,
                    TokenVersion = raw.TokenVersion,
                    EmisoresAccesibles = raw.EmisoresAccesibles ?? new List<int>(),
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SmartHubApi] Error validando exchange code");
                return null;
            }
        }

        public async Task<HubFiscalPayloadDto?> GetFiscalPayloadForHubAsync(int hubId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(_settings.BaseUrl) || string.IsNullOrWhiteSpace(_settings.ApiKey))
            {
                _logger.LogWarning("[SmartHubApi] BaseUrl/ApiKey no configurados; refresh fiscal omitido.");
                return null;
            }

            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

            try
            {
                var requestUrl = $"{_settings.BaseUrl.TrimEnd('/')}/api/internal/hubs/{hubId}/fiscal-payload";
                var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                request.Headers.Add("X-Api-Key", _settings.ApiKey);

                var response = await _httpClient.SendAsync(request, linked.Token);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "[SmartHubApi] GetFiscalPayloadForHub({HubId}) status={Status}; refresh omitido.",
                        hubId, response.StatusCode);
                    return null;
                }

                return await response.Content.ReadFromJsonAsync<HubFiscalPayloadDto>(cancellationToken: linked.Token);
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
            {
                _logger.LogWarning("[SmartHubApi] GetFiscalPayloadForHub({HubId}) timeout >5s.", hubId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[SmartHubApi] GetFiscalPayloadForHub({HubId}) fallo; refresh omitido.", hubId);
                return null;
            }
        }

        public async Task<int?> GetTokenVersionAsync(int usuarioHubId)
        {
            if (string.IsNullOrWhiteSpace(_settings.BaseUrl) || string.IsNullOrWhiteSpace(_settings.ApiKey))
            {
                _logger.LogWarning("[SmartHubApi] BaseUrl/ApiKey no configurados; token-version check omitido.");
                return null;
            }

            var cacheKey = $"smarthub:token-version:{usuarioHubId}";
            if (_cache.TryGetValue(cacheKey, out int cached)) return cached;

            try
            {
                var requestUrl = $"{_settings.BaseUrl.TrimEnd('/')}/api/auth/token-version/{usuarioHubId}";
                var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                request.Headers.Add("X-Api-Key", _settings.ApiKey);

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("[SmartHubApi] token-version fallo: {Status} para usuarioHubId={Id}", response.StatusCode, usuarioHubId);
                    return null;
                }

                var result = await response.Content.ReadFromJsonAsync<HubTokenVersionResponse>();
                if (result == null) return null;

                _cache.Set(cacheKey, result.TokenVersion, TokenVersionCacheTtl);
                return result.TokenVersion;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SmartHubApi] Error obteniendo token-version para usuarioHubId={Id}", usuarioHubId);
                return null;
            }
        }

        /// <summary>
        /// Bug #3 (UsuarioCompartido / Plan B Hub-as-Emisor) — 2026-05-29.
        /// GET /api/internal/usuarios/{id}/emisores-accesibles del SmartHub.
        /// Best-effort: timeout 5s y captura de cualquier fallo de red retorna null.
        /// El caller (refresh-emisores) traduce null a 502 sin romper la sesion.
        /// </summary>
        public async Task<List<int>?> GetEmisoresAccesiblesAsync(int hubUsuarioId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(_settings.BaseUrl) || string.IsNullOrWhiteSpace(_settings.ApiKey))
            {
                _logger.LogWarning("[SmartHubApi] BaseUrl/ApiKey no configurados; refresh-emisores omitido.");
                return null;
            }

            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

            try
            {
                var requestUrl = $"{_settings.BaseUrl.TrimEnd('/')}/api/internal/usuarios/{hubUsuarioId}/emisores-accesibles";
                var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                request.Headers.Add("X-Api-Key", _settings.ApiKey);

                var response = await _httpClient.SendAsync(request, linked.Token);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "[SmartHubApi] GetEmisoresAccesibles({HubUsuarioId}) status={Status}",
                        hubUsuarioId, response.StatusCode);
                    return null;
                }

                var raw = await response.Content.ReadFromJsonAsync<HubEmisoresAccesiblesResponse>(cancellationToken: linked.Token);
                return raw?.EmisoresAccesibles;
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
            {
                _logger.LogWarning("[SmartHubApi] GetEmisoresAccesibles({HubUsuarioId}) timeout >5s.", hubUsuarioId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[SmartHubApi] GetEmisoresAccesibles({HubUsuarioId}) fallo.", hubUsuarioId);
                return null;
            }
        }

        private class HubEmisoresAccesiblesResponse
        {
            public List<int>? EmisoresAccesibles { get; set; }
        }

        /// <summary>
        /// Bug #3 fix de raiz (UsuarioCompartido / Plan B Hub-as-Emisor) — 2026-05-29.
        /// GET /api/internal/usuarios/lookup-by-email?email={email} en SmartHub.
        /// Best-effort: timeout 5s y captura de fallos de red retorna null. El
        /// caller (Login/Google/Supabase/Refresh paths) emite el JWT sin claims
        /// Hub en ese caso (back-compat con comportamiento previo).
        /// </summary>
        public async Task<HubUserClaimsDto?> LookupHubUserByEmailAsync(string email, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(_settings.BaseUrl) || string.IsNullOrWhiteSpace(_settings.ApiKey))
            {
                _logger.LogWarning("[SmartHubApi] BaseUrl/ApiKey no configurados; lookup-by-email omitido.");
                return null;
            }

            if (string.IsNullOrWhiteSpace(email))
                return null;

            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

            try
            {
                var encoded = Uri.EscapeDataString(email.Trim());
                var requestUrl = $"{_settings.BaseUrl.TrimEnd('/')}/api/internal/usuarios/lookup-by-email?email={encoded}";
                var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                request.Headers.Add("X-Api-Key", _settings.ApiKey);

                var response = await _httpClient.SendAsync(request, linked.Token);
                if (!response.IsSuccessStatusCode)
                {
                    // 404 = email no registrado en Hub (caso normal: usuario solo
                    // Smartix, sin vinculo Hub). Log DEBUG, no warning.
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        _logger.LogDebug("[SmartHubApi] lookup-by-email({Email}) 404 — sin vinculo Hub.", email);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "[SmartHubApi] lookup-by-email({Email}) status={Status}",
                            email, response.StatusCode);
                    }
                    return null;
                }

                return await response.Content.ReadFromJsonAsync<HubUserClaimsDto>(cancellationToken: linked.Token);
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
            {
                _logger.LogWarning("[SmartHubApi] lookup-by-email({Email}) timeout >5s.", email);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[SmartHubApi] lookup-by-email({Email}) fallo.", email);
                return null;
            }
        }

        private class HubValidateCodeResponse
        {
            public int HubUsuarioId { get; set; }
            public string Email { get; set; } = string.Empty;
            public string NombreCompleto { get; set; } = string.Empty;
            public string? AvatarUrl { get; set; }
            public int? OrganizacionId { get; set; }
            public List<int>? SucursalIds { get; set; }
            public int TokenVersion { get; set; }
            public string? Rol { get; set; }

            /// <summary>
            /// UsuarioCompartido + Plan B Hub-as-Emisor: lista de SmartixEmisorId
            /// facturables por la sesion. Llega del Hub como JSON array; null o
            /// ausente para deploys donde SmartHub-BE aun no expone el campo
            /// (back-compat — Smartix cae al fallback [EmisorId] en ese caso).
            /// </summary>
            public List<int>? EmisoresAccesibles { get; set; }
        }

        private class HubTokenVersionResponse
        {
            public int TokenVersion { get; set; }
        }
    }
}
