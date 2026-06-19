using Google.Apis.Auth;

namespace FraFactu.Application.Interfaces
{
    /// <summary>
    /// Interfaz para validar tokens de Google (permite mocking en tests)
    /// </summary>
    public interface IGoogleTokenValidator
    {
        Task<GoogleJsonWebSignature.Payload?> ValidateAsync(string idToken, string clientId);
        Task<string?> ExchangeCodeForIdTokenAsync(string code, string clientId, string clientSecret, string redirectUri);
        Task<GoogleAccessTokenUserInfo?> ValidateAccessTokenAsync(string accessToken);
    }

    public class GoogleAccessTokenUserInfo
    {
        public string Sub { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
