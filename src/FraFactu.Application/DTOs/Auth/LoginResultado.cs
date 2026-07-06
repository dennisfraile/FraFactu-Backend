namespace FraFactu.Application.DTOs.Auth
{
    /// <summary>
    /// Outcome del login. Lo serializa <c>AuthService.LoginAsync</c> y lo mapea
    /// <c>AuthController</c> a status codes HTTP (Ok→200, Bloqueado→423, resto→401).
    /// </summary>
    public enum LoginStatus
    {
        /// <summary>Autenticación exitosa; <c>Response</c> trae el JWT.</summary>
        Ok,
        /// <summary>Email/clave inválidos, cuenta no activa o clave temporal caducada (→401 genérico).</summary>
        CredencialesInvalidas,
        /// <summary>Cuenta bloqueada temporalmente por intentos fallidos; <c>SegundosRestantes</c> indica cuánto falta.</summary>
        Bloqueado
    }

    /// <summary>Resultado del login local.</summary>
    public class LoginResultado
    {
        public LoginStatus Status { get; init; }
        public LoginResponseDto? Response { get; init; }
        public int SegundosRestantes { get; init; }

        public static LoginResultado Ok(LoginResponseDto response) =>
            new() { Status = LoginStatus.Ok, Response = response };

        public static LoginResultado CredencialesInvalidas() =>
            new() { Status = LoginStatus.CredencialesInvalidas };

        public static LoginResultado Bloqueado(int segundosRestantes) =>
            new() { Status = LoginStatus.Bloqueado, SegundosRestantes = segundosRestantes };
    }
}
