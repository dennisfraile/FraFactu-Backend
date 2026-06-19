namespace FraFactu.Application.Interfaces
{
    /// <summary>
    /// Correos transaccionales de identidad (F2): clave temporal en el alta y
    /// enlace de reset de contraseña. Se envían con el SMTP de la plataforma
    /// (no dependen de un emisor concreto).
    /// </summary>
    public interface IAuthEmailService
    {
        /// <summary>
        /// Envía al nuevo usuario su contraseña temporal con la instrucción de
        /// cambiarla en el primer inicio de sesión.
        /// </summary>
        Task EnviarClaveTemporalAsync(string email, string nombreCompleto, string claveTemporal);

        /// <summary>
        /// Envía el enlace para restablecer la contraseña. Recibe el token en
        /// claro y construye el enlace al frontend.
        /// </summary>
        Task EnviarResetPasswordAsync(string email, string nombreCompleto, string resetToken);
    }
}
