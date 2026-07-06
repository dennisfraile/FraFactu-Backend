using System.IdentityModel.Tokens.Jwt;
using FraFactu.Application.Common.Settings;
using FraFactu.Application.DTOs.Auth;
using FraFactu.Application.Interfaces;
using FraFactu.Domain.Entities;
using FraFactu.Domain.Enums;
using FraFactu.Infrastructure.Persistence;
using FraFactu.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace FraFactu.Tests.UnitTests.Services
{
    /// <summary>
    /// Primer ingreso con clave temporal (F2): login emite un token restringido
    /// con el claim pwd_change_required; la temporal expirada bloquea el login; y
    /// el cambio obligatorio limpia las banderas, revoca el token restringido y
    /// emite un JWT completo.
    /// </summary>
    public class AuthServiceFirstLoginTests
    {
        private readonly ApplicationDbContext _context;
        private readonly AuthService _authService;

        public AuthServiceFirstLoginTests()
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

            var loginSec = new Mock<IOptions<LoginSecuritySettings>>();
            loginSec.Setup(x => x.Value).Returns(new LoginSecuritySettings());

            _authService = new AuthService(
                _context,
                jwt.Object,
                google.Object,
                new Mock<IGoogleTokenValidator>().Object,
                new Mock<IAuthEmailService>().Object,
                NullLogger<AuthService>.Instance,
                loginSec.Object);
        }

        private async Task<Usuario> SeedTempPasswordUserAsync(string password, DateTime? expira)
        {
            _context.Roles.Add(new Rol { Id = 1, Nombre = "EmisorAdmin" });
            var user = new Usuario
            {
                Email = "temp@test.com",
                NombreCompleto = "Temp User",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                ProveedorAuth = ProveedorAutenticacion.Local,
                RolId = 1,
                Estado = EstadoUsuario.Activo,
                Activo = true,
                RequiereCambioPwd = true,
                PermiteCambioPwd = true,
                ExpiracionPwdTemporal = expira,
                TokenVersion = 0
            };
            _context.Usuarios.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        private static string? GetClaim(string jwt, string claimType)
        {
            var token = new JwtSecurityTokenHandler().ReadJwtToken(jwt);
            return token.Claims.FirstOrDefault(c => c.Type == claimType)?.Value;
        }

        [Fact]
        public async Task LoginAsync_EmitsRestrictedToken_WhenRequiereCambioPwd()
        {
            await SeedTempPasswordUserAsync("Temp1234", expira: DateTime.UtcNow.AddDays(1));

            var result = await _authService.LoginAsync(new LoginDto { Email = "temp@test.com", Password = "Temp1234" });

            Assert.Equal(LoginStatus.Ok, result.Status);
            Assert.NotNull(result.Response);
            Assert.True(result.Response!.RequiereCambioPwd);
            Assert.Equal("true", GetClaim(result.Response.Token, "pwd_change_required"));
        }

        [Fact]
        public async Task LoginAsync_ReturnsNull_WhenTemporaryPasswordExpired()
        {
            await SeedTempPasswordUserAsync("Temp1234", expira: DateTime.UtcNow.AddDays(-1));

            var result = await _authService.LoginAsync(new LoginDto { Email = "temp@test.com", Password = "Temp1234" });

            Assert.Equal(LoginStatus.CredencialesInvalidas, result.Status);
        }

        [Fact]
        public async Task ChangePasswordFirstLoginAsync_SetsPassword_ClearsFlags_BumpsTokenVersion_AndReturnsFullToken()
        {
            var user = await SeedTempPasswordUserAsync("Temp1234", expira: DateTime.UtcNow.AddDays(1));

            var result = await _authService.ChangePasswordFirstLoginAsync(user.Id, "NuevaPass456");

            Assert.NotNull(result);
            Assert.False(result!.RequiereCambioPwd);
            Assert.Null(GetClaim(result.Token, "pwd_change_required")); // token completo, sin restricción
            Assert.Equal(60 * 60, result.ExpiresIn); // ExpiresIn en segundos (ExpirationMinutes=60 en el setup)

            var updated = await _context.Usuarios.FindAsync(user.Id);
            Assert.False(updated!.RequiereCambioPwd);
            Assert.Null(updated.ExpiracionPwdTemporal);
            Assert.Equal(1, updated.TokenVersion); // revoca el token restringido
            Assert.True(BCrypt.Net.BCrypt.Verify("NuevaPass456", updated.PasswordHash));
        }

        [Fact]
        public async Task ChangePasswordFirstLoginAsync_ReturnsNull_WhenNoChangePending()
        {
            _context.Roles.Add(new Rol { Id = 1, Nombre = "EmisorAdmin" });
            var user = new Usuario
            {
                Email = "normal@test.com",
                NombreCompleto = "Normal",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pass1234"),
                RolId = 1,
                Estado = EstadoUsuario.Activo,
                Activo = true,
                RequiereCambioPwd = false
            };
            _context.Usuarios.Add(user);
            await _context.SaveChangesAsync();

            var result = await _authService.ChangePasswordFirstLoginAsync(user.Id, "Otra123456");

            Assert.Null(result);
        }
    }
}
