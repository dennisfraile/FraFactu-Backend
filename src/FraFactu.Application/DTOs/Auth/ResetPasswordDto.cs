namespace FraFactu.Application.DTOs.Auth
{
    /// <summary>
    /// DTO para completar el reset de contraseña con el token recibido por correo.
    /// </summary>
    public class ResetPasswordDto
    {
        /// <summary>
        /// Token recibido en el enlace del correo.
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Nueva contraseña.
        /// </summary>
        public string NewPassword { get; set; } = string.Empty;

        /// <summary>
        /// Confirmación de la nueva contraseña.
        /// </summary>
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
