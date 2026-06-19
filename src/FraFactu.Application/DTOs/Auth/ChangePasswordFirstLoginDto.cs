namespace FraFactu.Application.DTOs.Auth
{
    /// <summary>
    /// DTO para el cambio de contraseña obligatorio en el primer ingreso.
    /// No requiere la contraseña actual: el usuario ya se autenticó con su clave
    /// temporal y llega con un token restringido.
    /// </summary>
    public class ChangePasswordFirstLoginDto
    {
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
