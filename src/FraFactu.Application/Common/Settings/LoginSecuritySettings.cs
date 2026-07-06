namespace FraFactu.Application.Common.Settings
{
    /// <summary>
    /// Parámetros de política anti-fuerza-bruta del login. Enlazados desde la
    /// sección "LoginSecurity" de appsettings.json.
    /// </summary>
    public class LoginSecuritySettings
    {
        /// <summary>Intentos fallidos por cuenta antes de bloquear temporalmente.</summary>
        public int MaxFailedAttempts { get; set; } = 5;
        /// <summary>Duración del bloqueo temporal de cuenta, en minutos.</summary>
        public int LockoutMinutes { get; set; } = 15;
        /// <summary>Máximo de requests de login por IP dentro de la ventana.</summary>
        public int IpPermitLimit { get; set; } = 10;
        /// <summary>Ancho de la ventana de rate limiting por IP, en segundos.</summary>
        public int IpWindowSeconds { get; set; } = 60;
    }
}
