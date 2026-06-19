namespace FraFactu.Application.DTOs.Auth
{
    /// <summary>
    /// DTO para iniciar el flujo "olvidé mi contraseña".
    /// </summary>
    public class ForgotPasswordDto
    {
        /// <summary>
        /// Email de la cuenta a recuperar.
        /// </summary>
        public string Email { get; set; } = string.Empty;
    }
}
