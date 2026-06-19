using FraFactu.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace FraFactu.Infrastructure.Services
{
    /// <inheritdoc cref="IAuthEmailService" />
    public class AuthEmailService : IAuthEmailService
    {
        private readonly IEmailService _emailService;
        private readonly string _frontendBaseUrl;

        public AuthEmailService(IEmailService emailService, IConfiguration configuration)
        {
            _emailService = emailService;
            _frontendBaseUrl = (configuration["App:FrontendBaseUrl"] ?? "http://localhost:5173")
                .TrimEnd('/');
        }

        public Task EnviarClaveTemporalAsync(string email, string nombreCompleto, string claveTemporal)
        {
            var asunto = "FraFactu — Tu contraseña temporal";
            var cuerpo = $@"
                <p>Hola {nombreCompleto},</p>
                <p>Se ha creado tu cuenta en <strong>FraFactu</strong>. Tu contraseña temporal es:</p>
                <p style=""font-size:18px;font-weight:bold;letter-spacing:1px"">{claveTemporal}</p>
                <p>Por seguridad, deberás cambiarla la primera vez que inicies sesión.</p>
                <p>Si no esperabas este correo, contacta al administrador.</p>
                <p>— Equipo FraFactu</p>";

            return _emailService.EnviarEmailGenericoAsync(
                email,
                asunto,
                cuerpo,
                new List<(byte[] contenido, string nombre, string mimeType)>());
        }

        public Task EnviarResetPasswordAsync(string email, string nombreCompleto, string resetToken)
        {
            var enlace = $"{_frontendBaseUrl}/reset-password?token={resetToken}";
            var asunto = "FraFactu — Restablece tu contraseña";
            var cuerpo = $@"
                <p>Hola {nombreCompleto},</p>
                <p>Recibimos una solicitud para restablecer tu contraseña en <strong>FraFactu</strong>.</p>
                <p>Haz clic en el siguiente enlace para crear una nueva contraseña:</p>
                <p><a href=""{enlace}"">Restablecer mi contraseña</a></p>
                <p>Este enlace caduca en 30 minutos y solo puede usarse una vez.</p>
                <p>Si no solicitaste el cambio, ignora este correo; tu contraseña no se modificará.</p>
                <p>— Equipo FraFactu</p>";

            return _emailService.EnviarEmailGenericoAsync(
                email,
                asunto,
                cuerpo,
                new List<(byte[] contenido, string nombre, string mimeType)>());
        }
    }
}
