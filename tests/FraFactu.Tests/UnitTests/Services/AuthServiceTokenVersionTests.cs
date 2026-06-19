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
    /// Tests de la revocación local (F2): el JWT local lleva el claim
    /// token_version desde Usuario.TokenVersion, y los eventos de seguridad
    /// (logout, cambio de contraseña) lo incrementan.
    /// </summary>
    public class AuthServiceTokenVersionTests
    {
        private readonly ApplicationDbContext _context;
        private readonly AuthService _authService;

        public AuthServiceTokenVersionTests()
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
                new Mock<IAuthEmailService>().Object,
                NullLogger<AuthService>.Instance);
        }

        private async Task<Usuario> SeedLocalUserAsync(string password, int tokenVersion)
        {
            _context.Roles.Add(new Rol { Id = 1, Nombre = "EmisorAdmin" });
            var user = new Usuario
            {
                Email = "user@test.com",
                NombreCompleto = "User",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                ProveedorAuth = ProveedorAutenticacion.Local,
                RolId = 1,
                Estado = EstadoUsuario.Activo,
                Activo = true,
                PermiteCambioPwd = true,
                TokenVersion = tokenVersion
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
        public async Task LoginAsync_EmitsTokenVersionClaim_FromUsuario()
        {
            await SeedLocalUserAsync("Secret123", tokenVersion: 7);

            var result = await _authService.LoginAsync(new LoginDto { Email = "user@test.com", Password = "Secret123" });

            Assert.NotNull(result);
            Assert.Equal("7", GetClaim(result!.Token, "token_version"));
        }

        [Fact]
        public async Task ChangePasswordAsync_IncrementsTokenVersion()
        {
            var user = await SeedLocalUserAsync("OldPass123", tokenVersion: 2);

            var ok = await _authService.ChangePasswordAsync(user.Id, "OldPass123", "NewPass456");

            Assert.True(ok);
            var updated = await _context.Usuarios.FindAsync(user.Id);
            Assert.Equal(3, updated!.TokenVersion);
        }

        [Fact]
        public async Task LogoutAsync_IncrementsTokenVersion()
        {
            var user = await SeedLocalUserAsync("Secret123", tokenVersion: 5);

            var ok = await _authService.LogoutAsync(user.Id);

            Assert.True(ok);
            var updated = await _context.Usuarios.FindAsync(user.Id);
            Assert.Equal(6, updated!.TokenVersion);
        }
    }
}
