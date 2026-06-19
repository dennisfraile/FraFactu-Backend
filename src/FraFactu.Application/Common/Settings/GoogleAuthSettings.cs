namespace FraFactu.Application.Common.Settings
{
    /// <summary>
    /// Configuración para Google OAuth 2.0
    /// </summary>
    public class GoogleAuthSettings
    {
        /// <summary>
        /// Client ID de Google Cloud Console
        /// </summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// Client Secret de Google Cloud Console
        /// </summary>
        public string ClientSecret { get; set; } = string.Empty;

        /// <summary>
        /// URI de redirect para el callback de Gmail OAuth2
        /// </summary>
        public string GmailRedirectUri { get; set; } = string.Empty;

        /// <summary>
        /// URL del frontend para redirigir después del callback de Gmail
        /// </summary>
        public string GmailCallbackFrontendUrl { get; set; } = "http://localhost:8081";
    }
}
