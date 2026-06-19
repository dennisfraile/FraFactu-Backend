using FraFactu.Application.Common.Settings;
using FraFactu.Application.DTOs.Auth;
using FraFactu.Application.Interfaces;
using FraFactu.Domain.Entities;
using FraFactu.Domain.Enums;
using FraFactu.Infrastructure.Persistence;
using FraFactu.Infrastructure.Services;
using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace FraFactu.Tests.UnitTests.Services
{
    public class GoogleAuthServiceTests
    {
        private readonly Mock<IOptions<JwtSettings>> _mockJwtSettings;
        private readonly Mock<IOptions<GoogleAuthSettings>> _mockGoogleAuthSettings;
        private readonly Mock<IGoogleTokenValidator> _mockTokenValidator;
        private readonly ApplicationDbContext _context;
        private readonly AuthService _authService;

        public GoogleAuthServiceTests()
        {
            // Setup In-Memory Database
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);

            // Setup Mocks
            _mockJwtSettings = new Mock<IOptions<JwtSettings>>();
            _mockJwtSettings.Setup(x => x.Value).Returns(new JwtSettings
            {
                SecretKey = "super_secret_key_for_testing_purposes_only",
                Issuer = "TestIssuer",
                Audience = "TestAudience",
                ExpirationMinutes = 60
            });

            _mockGoogleAuthSettings = new Mock<IOptions<GoogleAuthSettings>>();
            _mockGoogleAuthSettings.Setup(x => x.Value).Returns(new GoogleAuthSettings
            {
                ClientId = "test-client-id",
                ClientSecret = "test-client-secret"
            });

            _mockTokenValidator = new Mock<IGoogleTokenValidator>();

            // Initialize Service
            _authService = new AuthService(
                _context,
                _mockJwtSettings.Object,
                _mockGoogleAuthSettings.Object,
                _mockTokenValidator.Object,
                NullLogger<AuthService>.Instance
            );
        }

        [Fact]
        public async Task GoogleLoginAsync_ShouldReturnNull_WhenUserDoesNotExist()
        {
            // Arrange
            var googleDto = new GoogleAuthDto { AccessToken = "valid_access_token" };

            _mockTokenValidator.Setup(x => x.ValidateAccessTokenAsync("valid_access_token"))
                .ReturnsAsync(new GoogleAccessTokenUserInfo
                {
                    Email = "newuser@gmail.com",
                    Sub = "google_id_123",
                    Name = "New User"
                });

            // Act
            var result = await _authService.GoogleLoginAsync(googleDto);

            // Assert — no se crean usuarios automáticamente, se rechaza el login
            Assert.Null(result);

            var userInDb = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == "newuser@gmail.com");
            Assert.Null(userInDb);
        }

        [Fact]
        public async Task GoogleLoginAsync_ShouldLinkAccount_WhenUserExistsWithLocalAuth()
        {
            // Arrange
            var email = "existing@gmail.com";

            // Seed roles needed for JWT generation
            _context.Roles.Add(new Rol { Id = 1, Nombre = "SuperAdmin" });
            _context.Roles.Add(new Rol { Id = 2, Nombre = "EmisorAdmin" });
            await _context.SaveChangesAsync();

            var existingUser = new Usuario
            {
                Email = email,
                NombreCompleto = "Existing User",
                PasswordHash = "hash",
                ProveedorAuth = ProveedorAutenticacion.Local,
                RolId = 2,
                Activo = true,
                Estado = EstadoUsuario.Activo
            };
            _context.Usuarios.Add(existingUser);
            await _context.SaveChangesAsync();

            var googleDto = new GoogleAuthDto { AccessToken = "valid_access_token" };

            _mockTokenValidator.Setup(x => x.ValidateAccessTokenAsync("valid_access_token"))
                .ReturnsAsync(new GoogleAccessTokenUserInfo
                {
                    Email = email,
                    Sub = "google_id_456",
                    Name = "Existing User"
                });

            // Act
            var result = await _authService.GoogleLoginAsync(googleDto);

            // Assert
            Assert.NotNull(result);
            var userInDb = await _context.Usuarios.FindAsync(existingUser.Id);
            Assert.NotNull(userInDb);
            Assert.Equal(ProveedorAutenticacion.Google, userInDb.ProveedorAuth); // Debe actualizarse
            Assert.Equal("google_id_456", userInDb.ProveedorExternoId);
        }

        [Fact]
        public async Task LinkGoogleAccountAsync_ShouldReturnTrue_WhenTokenIsValidAndEmailsMatch()
        {
            // Arrange
            var userId = 1;
            var email = "user@test.com";
            var user = new Usuario
            {
                Id = userId,
                Email = email,
                NombreCompleto = "User",
                ProveedorAuth = ProveedorAutenticacion.Local,
                Estado = EstadoUsuario.Activo,
                Activo = true
            };
            _context.Usuarios.Add(user);
            await _context.SaveChangesAsync();

            var linkDto = new LinkGoogleAccountDto { IdToken = "valid_token" };
            var payload = new GoogleJsonWebSignature.Payload
            {
                Email = email, // Coincide
                Subject = "google_id_789"
            };

            _mockTokenValidator.Setup(x => x.ValidateAsync("valid_token", "test-client-id"))
                .ReturnsAsync(payload);

            // Act
            var result = await _authService.LinkGoogleAccountAsync(userId, linkDto);

            // Assert
            Assert.True(result);
            var userInDb = await _context.Usuarios.FindAsync(userId);
            Assert.NotNull(userInDb);
            Assert.Equal(ProveedorAutenticacion.Google, userInDb.ProveedorAuth);
            Assert.Equal("google_id_789", userInDb.ProveedorExternoId);
        }

        [Fact]
        public async Task LinkGoogleAccountAsync_ShouldReturnFalse_WhenEmailsDoNotMatch()
        {
            // Arrange
            var userId = 1;
            var user = new Usuario
            {
                Id = userId,
                Email = "user@test.com",
                Estado = EstadoUsuario.Activo
            };
            _context.Usuarios.Add(user);
            await _context.SaveChangesAsync();

            var linkDto = new LinkGoogleAccountDto { IdToken = "valid_token_wrong_email" };
            var payload = new GoogleJsonWebSignature.Payload
            {
                Email = "other@test.com", // No coincide
                Subject = "google_id_999"
            };

            _mockTokenValidator.Setup(x => x.ValidateAsync("valid_token_wrong_email", "test-client-id"))
                .ReturnsAsync(payload);

            // Act
            var result = await _authService.LinkGoogleAccountAsync(userId, linkDto);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GoogleLoginAsync_ShouldReturnNull_WhenAccessTokenInvalid()
        {
            // Arrange
            var googleDto = new GoogleAuthDto { AccessToken = "invalid_token" };
            _mockTokenValidator.Setup(x => x.ValidateAccessTokenAsync("invalid_token"))
                .ReturnsAsync((GoogleAccessTokenUserInfo?)null);

            // Act
            var result = await _authService.GoogleLoginAsync(googleDto);

            // Assert
            Assert.Null(result);
        }
    }
}
