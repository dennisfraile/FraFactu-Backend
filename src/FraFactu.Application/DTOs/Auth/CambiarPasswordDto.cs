namespace FraFactu.Application.DTOs.Auth
{
    /// <summary>
    /// DTO para cambiar password del usuario actual
    /// </summary>
    public class CambiarPasswordDto
    {
        /// <summary>
        /// Password actual (para verificación)
        /// </summary>
        public string PasswordActual { get; set; } = string.Empty;

        /// <summary>
        /// Nuevo password
        /// </summary>
        public string PasswordNuevo { get; set; } = string.Empty;

        /// <summary>
        /// Confirmación del nuevo password
        /// </summary>
        public string PasswordNuevoConfirmacion { get; set; } = string.Empty;
    }
}
