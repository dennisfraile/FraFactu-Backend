using FraFactu.Application.Common.Settings;
using FraFactu.Application.Interfaces;
using FraFactu.Domain.Entities;
using FraFactu.Domain.Enums;
using FraFactu.Infrastructure.Persistence;
using FraFactu.Infrastructure.Security;
using FraFactu.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace FraFactu.Tests.UnitTests.Services
{
    /// <summary>
    /// Flujo "olvidé mi contraseña" (F2): forgot genera token hasheado + correo
    /// sin revelar si el email existe; reset valida hash/vigencia, es de un solo
    /// uso y revoca las sesiones.
    /// </summary>
    public class AuthServicePasswordResetTests
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<IAuthEmailService> _authEmail = new();
        private readonly AuthService _authService;

        public AuthServicePasswordResetTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);

            var jwt = new Mock<IOptions<JwtSettings>>();
            jwt.Setup(x => x.Value).Returns(new JwtSettings
            {
                SecretKey = "super_secret_key_for_testing_purposes_only_1234567890",
                Issuer = "TestIssuer",
                Audience = "TestAudience",
                ExpirationMinutes = 60
            });
            var google = new Mock<IOptions<GoogleAuthSettings>>();
            google.Setup(x => x.Value).Returns(new GoogleAuthSettings { ClientId = "cid", ClientSecret = "cs" });

            _authService = new AuthService(
                _context,
                jwt.Object,
                google.Object,
                new Mock<IGoogleTokenValidator>().Object,
                _authEmail.Object,
                NullLogger<AuthService>.Instance);
        }

        private async Task<Usuario> SeedUserAsync(string email = "user@test.com", int tokenVersion = 0)
        {
            _context.Roles.Add(new Rol { Id = 1, Nombre = "EmisorAdmin" });
            var user = new Usuario
            {
                Email = email,
                NombreCompleto = "User",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPass123"),
                ProveedorAuth = ProveedorAutenticacion.Local,
                RolId = 1,
                Estado = EstadoUsuario.Activo,
                Activo = true,
                TokenVersion = tokenVersion
            };
            _context.Usuarios.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        [Fact]
        public async Task ForgotPasswordAsync_StoresHashedToken_AndSendsEmail_WhenUserExists()
        {
            var user = await SeedUserAsync();

            await _authService.ForgotPasswordAsync("user@test.com");

            var updated = await _context.Usuarios.FindAsync(user.Id);
            Assert.False(string.IsNullOrEmpty(updated!.PasswordResetTokenHash));
            Assert.NotNull(updated.PasswordResetTokenExpira);
            Assert.True(updated.PasswordResetTokenExpira > DateTime.UtcNow);
            _authEmail.Verify(e => e.EnviarResetPasswordAsync(
                "user@test.com", "User", It.Is<string>(t => !string.IsNullOrEmpty(t))), Times.Once);
        }

        [Fact]
        public async Task ForgotPasswordAsync_DoesNothing_WhenUserDoesNotExist()
        {
            await _authService.ForgotPasswordAsync("ghost@test.com");

            _authEmail.Verify(e => e.EnviarResetPasswordAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ResetPasswordAsync_SetsNewPassword_ClearsToken_BumpsTokenVersion_WhenValid()
        {
            var user = await SeedUserAsync(tokenVersion: 2);
            const string rawToken = "raw-reset-token-abc";
            user.PasswordResetTokenHash = SecureTokenGenerator.Sha256Hex(rawToken);
            user.PasswordResetTokenExpira = DateTime.UtcNow.AddMinutes(15);
            await _context.SaveChangesAsync();

            var ok = await _authService.ResetPasswordAsync(rawToken, "NewPass456");

            Assert.True(ok);
            var updated = await _context.Usuarios.FindAsync(user.Id);
            Assert.True(BCrypt.Net.BCrypt.Verify("NewPass456", updated!.PasswordHash));
            Assert.Null(updated.PasswordResetTokenHash);   // un solo uso
            Assert.Null(updated.PasswordResetTokenExpira);
            Assert.Equal(3, updated.TokenVersion);          // revoca sesiones
        }

        [Fact]
        public async Task ResetPasswordAsync_ReturnsFalse_WhenTokenExpired()
        {
            var user = await SeedUserAsync();
            const string rawToken = "expired-token";
            user.PasswordResetTokenHash = SecureTokenGenerator.Sha256Hex(rawToken);
            user.PasswordResetTokenExpira = DateTime.UtcNow.AddMinutes(-1);
            await _context.SaveChangesAsync();

            var ok = await _authService.ResetPasswordAsync(rawToken, "NewPass456");

            Assert.False(ok);
        }

        [Fact]
        public async Task ResetPasswordAsync_ReturnsFalse_WhenTokenUnknown()
        {
            await SeedUserAsync();

            var ok = await _authService.ResetPasswordAsync("does-not-exist", "NewPass456");

            Assert.False(ok);
        }

        [Fact]
        public async Task ResetPasswordAsync_TokenCannotBeReused()
        {
            var user = await SeedUserAsync();
            const string rawToken = "one-time-token";
            user.PasswordResetTokenHash = SecureTokenGenerator.Sha256Hex(rawToken);
            user.PasswordResetTokenExpira = DateTime.UtcNow.AddMinutes(15);
            await _context.SaveChangesAsync();

            var first = await _authService.ResetPasswordAsync(rawToken, "NewPass456");
            var second = await _authService.ResetPasswordAsync(rawToken, "OtraPass789");

            Assert.True(first);
            Assert.False(second);
        }

        [Fact]
        public async Task ForgotThenReset_WorksEndToEnd()
        {
            await SeedUserAsync();
            string? capturado = null;
            _authEmail
                .Setup(e => e.EnviarResetPasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string, string>((_, _, token) => capturado = token)
                .Returns(Task.CompletedTask);

            await _authService.ForgotPasswordAsync("user@test.com");
            Assert.False(string.IsNullOrEmpty(capturado));

            var ok = await _authService.ResetPasswordAsync(capturado!, "BrandNew123");

            Assert.True(ok);
        }
    }
}
