namespace FraFactu.Application.Interfaces
{
    /// <summary>
    /// Servicio para envío de correos mediante Gmail API OAuth2
    /// </summary>
    public interface IGmailApiService
    {
        /// <summary>
        /// Genera la URL de autorización de Google OAuth2 para Gmail
        /// </summary>
        string GenerateAuthorizationUrl(string encryptedState);

        /// <summary>
        /// Intercambia el código de autorización por un refresh token y obtiene el email
        /// </summary>
        Task<(string refreshToken, string email)> ExchangeCodeForTokensAsync(string authorizationCode);

        /// <summary>
        /// Envía un correo electrónico usando Gmail API
        /// </summary>
        Task EnviarEmailGmailAsync(
            string refreshTokenEncrypted,
            string fromEmail,
            string to,
            string subject,
            string htmlBody,
            List<(byte[] content, string name, string mimeType)>? attachments = null);

        /// <summary>
        /// Valida que el refresh token siga siendo válido
        /// </summary>
        Task<bool> ValidateRefreshTokenAsync(string refreshTokenEncrypted);
    }
}
