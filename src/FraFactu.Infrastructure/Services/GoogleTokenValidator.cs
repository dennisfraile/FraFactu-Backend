using System.Net.Http.Json;
using System.Text.Json;
using FraFactu.Application.Interfaces;
using Google.Apis.Auth;
using Microsoft.Extensions.Logging;

namespace FraFactu.Infrastructure.Services
{
    public class GoogleTokenValidator : IGoogleTokenValidator
    {
        private readonly ILogger<GoogleTokenValidator> _logger;

        public GoogleTokenValidator(ILogger<GoogleTokenValidator> logger)
        {
            _logger = logger;
        }

        public async Task<GoogleJsonWebSignature.Payload?> ValidateAsync(string idToken, string clientId)
        {
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { clientId }
                };

                return await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
            }
            catch
            {
                // Token inválido o expirado
                return null;
            }
        }

        public async Task<string?> ExchangeCodeForIdTokenAsync(string code, string clientId, string clientSecret, string redirectUri)
        {
            try
            {
                using var httpClient = new HttpClient();
                var tokenRequest = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("code", code),
                    new KeyValuePair<string, string>("client_id", clientId),
                    new KeyValuePair<string, string>("client_secret", clientSecret),
                    new KeyValuePair<string, string>("redirect_uri", redirectUri),
                    new KeyValuePair<string, string>("grant_type", "authorization_code"),
                });

                var response = await httpClient.PostAsync("https://oauth2.googleapis.com/token", tokenRequest);

                if (!response.IsSuccessStatusCode)
                    return null;

                var tokenData = await response.Content.ReadFromJsonAsync<JsonElement>();
                if (tokenData.TryGetProperty("id_token", out var idTokenElement))
                    return idTokenElement.GetString();

                return null;
            }
            catch
            {
                return null;
            }
        }
        public async Task<GoogleAccessTokenUserInfo?> ValidateAccessTokenAsync(string accessToken)
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var response = await httpClient.GetAsync("https://www.googleapis.com/oauth2/v3/userinfo");
                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Google userinfo API returned {StatusCode}: {Body}",
                        response.StatusCode, errorBody);
                    return null;
                }

                var userInfo = await response.Content.ReadFromJsonAsync<GoogleAccessTokenUserInfo>();
                _logger.LogInformation("Google token validated for email: {Email}", userInfo?.Email);
                return userInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception validating Google access token");
                return null;
            }
        }
    }
}
