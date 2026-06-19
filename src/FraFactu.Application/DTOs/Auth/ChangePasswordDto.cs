namespace FraFactu.Application.DTOs.Auth
{
    /// <summary>
    /// DTO para cambio de contraseña
    /// </summary>
    public class ChangePasswordDto
    {
        /// <summary>
        /// Contraseña actual
        /// </summary>
        public string CurrentPassword { get; set; } = string.Empty;

        /// <summary>
        /// Nueva contraseña
        /// </summary>
        public string NewPassword { get; set; } = string.Empty;

        /// <summary>
        /// Confirmación de nueva contraseña
        /// </summary>
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
