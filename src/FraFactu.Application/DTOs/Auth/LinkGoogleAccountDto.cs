namespace FraFactu.Application.DTOs.Auth
{
    /// <summary>
    /// DTO para vincular cuenta de Google a usuario existente
    /// </summary>
    public class LinkGoogleAccountDto
    {
        /// <summary>
        /// ID Token JWT de Google
        /// </summary>
        public string IdToken { get; set; } = string.Empty;
    }
}
