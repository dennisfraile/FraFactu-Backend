using FraFactu.Application.DTOs.Auth;
using Google.Apis.Auth;

namespace FraFactu.Application.Interfaces
{
    public interface IAuthService
    {
        /// <summary>
        /// Autentica un usuario y genera un JWT token
        /// </summary>
        Task<LoginResponseDto?> LoginAsync(LoginDto loginDto);

        /// <summary>
        /// Genera un JWT token para un usuario.
        /// El parametro <paramref name="emisoresAccesibles"/> emite el claim
        /// <c>emisores_accesibles</c> (JSON array de SmartixEmisorId) para
        /// UsuarioCompartido + Plan B Hub-as-Emisor; cuando viene null o vacio,
        /// el claim toma el fallback <c>[emisorId]</c> (back-compat con login
        /// local y con SSO desde un Hub que aun no expone el campo).
        /// </summary>
        string GenerateJwtToken(int usuarioId, string email, string nombreCompleto, int? emisorId, string? emisorNombre, int rolId, string rolNombre, bool accesoTodasSucursales, List<int> sucursalIds, int? hubUsuarioId = null, int? tokenVersion = null, List<int>? emisoresAccesibles = null, bool pwdChangeRequired = false);

        /// <summary>
        /// Hashea una contraseña usando BCrypt
        /// </summary>
        string HashPassword(string password);

        /// <summary>
        /// Verifica una contraseña contra su hash
        /// </summary>
        bool VerifyPassword(string password, string passwordHash);


        /// <summary>
        /// Cambia la contraseña de un usuario
        /// </summary>
        Task<bool> ChangePasswordAsync(int usuarioId, string currentPassword, string newPassword);

        /// <summary>
        /// Cierra la sesión del usuario incrementando su <c>TokenVersion</c>, lo
        /// que invalida todos los JWT emitidos hasta el momento (revocación local).
        /// </summary>
        Task<bool> LogoutAsync(int usuarioId);

        /// <summary>
        /// Inicia el flujo "olvidé mi contraseña". Si existe un usuario activo con
        /// ese email, genera un token de reset (guarda solo su hash + expiración) y
        /// envía el correo con el enlace. No revela si el email existe o no: el
        /// método siempre completa sin error (protección anti-enumeración).
        /// </summary>
        Task ForgotPasswordAsync(string email);

        /// <summary>
        /// Completa el reset de contraseña con el token recibido por correo.
        /// Valida hash + vigencia, es de un solo uso (limpia el token), fija la
        /// nueva contraseña e incrementa <c>TokenVersion</c> (revoca sesiones).
        /// Devuelve false si el token es inválido, expiró o ya se usó.
        /// </summary>
        Task<bool> ResetPasswordAsync(string token, string newPassword);

        /// <summary>
        /// Cambio de contraseña obligatorio en el primer ingreso. El usuario llega
        /// autenticado con un JWT restringido (emitido tras validar su clave
        /// temporal), por lo que no se pide la contraseña actual. Limpia
        /// <c>RequiereCambioPwd</c>/<c>ExpiracionPwdTemporal</c>, incrementa
        /// <c>TokenVersion</c> (invalida el token restringido) y devuelve un JWT
        /// completo. Devuelve null si el usuario no existe o no tenía cambio pendiente.
        /// </summary>
        Task<LoginResponseDto?> ChangePasswordFirstLoginAsync(int usuarioId, string newPassword);

        /// <summary>
        /// Cambia el ambiente (Pruebas/Producción) del Emisor especificado.
        /// Valida que el código sea "00" o "01" antes de persistir.
        /// </summary>
        Task<bool> ChangeAmbienteAsync(int emisorId, string ambienteCodigo);

        /// <summary>
        /// Autentica un usuario con Google OAuth (crea usuario si no existe)
        /// </summary>
        Task<LoginResponseDto?> GoogleLoginAsync(FraFactu.Application.DTOs.Auth.GoogleAuthDto googleAuthDto);

        /// <summary>
        /// Cambia el Emisor activo del
        /// usuario autenticado al que solicite, siempre que figure en la lista
        /// <paramref name="emisoresAccesibles"/> (claim del JWT actual). Persiste
        /// <c>Usuarios.EmisorId</c> y reemite un JWT con el nuevo Emisor activo y
        /// la misma lista de accesibles. No bumpea TokenVersion: el usuario ya
        /// estaba autorizado para este Emisor.
        /// Outcome:
        /// - <c>NoAutorizado</c> si <paramref name="nuevoEmisorId"/> no esta en la lista (controller → 403).
        /// - <c>NoExiste</c> si el Emisor no existe en BD (controller → 404).
        /// - <c>Ok</c> con un <see cref="LoginResponseDto"/> en <c>Response</c>.
        /// </summary>
        Task<FraFactu.Application.DTOs.Auth.CambiarEmisorActivoResultado> CambiarEmisorActivoAsync(
            int usuarioId,
            int nuevoEmisorId,
            IReadOnlyCollection<int> emisoresAccesibles,
            int? hubUsuarioId = null,
            int? tokenVersion = null);

        /// <summary>
        /// Vincula una cuenta de Google a un usuario existente
        /// </summary>
        Task<bool> LinkGoogleAccountAsync(int usuarioId, FraFactu.Application.DTOs.Auth.LinkGoogleAccountDto linkDto);

        /// <summary>
        /// Desvincula la cuenta de Google de un usuario
        /// </summary>
        Task<bool> UnlinkGoogleAccountAsync(int usuarioId);

        /// <summary>
        /// Valida un ID token de Google y retorna el payload
        /// </summary>
        Task<Google.Apis.Auth.GoogleJsonWebSignature.Payload?> ValidateGoogleTokenAsync(string idToken);
    }
}
