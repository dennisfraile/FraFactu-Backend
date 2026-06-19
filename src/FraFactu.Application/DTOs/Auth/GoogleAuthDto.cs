namespace FraFactu.Application.DTOs.Auth
{
    /// <summary>
    /// DTO para autenticación con Google OAuth 2.0 (Access Token via popup)
    /// </summary>
    public class GoogleAuthDto
    {
        /// <summary>
        /// Access Token obtenido de Google OAuth2 initTokenClient
        /// </summary>
        public string AccessToken { get; set; } = string.Empty;

        public string? Ambiente { get; set; }
    }
}
