namespace FraFactu.Application.DTOs.Auth
{
    /// <summary>
    /// DTO para solicitud de inicio de sesión
    /// </summary>
    public class LoginDto
    {
        /// <summary>
        /// Email del usuario
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Contraseña del usuario
        /// </summary>
        public string Password { get; set; } = string.Empty;

        public string? Ambiente { get; set; } // "00"=Pruebas, "01"=Producción
    }
}
