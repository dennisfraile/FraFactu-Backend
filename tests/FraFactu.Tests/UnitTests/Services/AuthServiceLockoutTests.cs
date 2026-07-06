using FraFactu.Application.Common.Settings;
using FraFactu.Application.DTOs.Auth;
using FraFactu.Domain.Entities;
using FraFactu.Domain.Enums;
using FraFactu.Infrastructure.Persistence;
using FraFactu.Infrastructure.Services;
using FraFactu.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace FraFactu.Tests.UnitTests.Services
{
    /// <summary>
    /// Lockout temporal por cuenta: IntentosFallidos + BloqueadoHasta cableados
    /// en LoginAsync. MaxFailedAttempts=3 en los tests para acortar.
    /// </summary>
    public class AuthServiceLockoutTests
    {
        private readonly ApplicationDbContext _context;
        private readonly AuthService _authService;

        public AuthServiceLockoutTests()
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
            loginSec.Setup(x => x.Value).Returns(new LoginSecuritySettings
            {
                MaxFailedAttempts = 3,
                LockoutMinutes = 15
            });

            _authService = new AuthService(
                _context,
                jwt.Object,
                google.Object,
                new Mock<IGoogleTokenValidator>().Object,
                new Mock<IAuthEmailService>().Object,
                NullLogger<AuthService>.Instance,
                loginSec.Object);
        }

        private async Task<Usuario> SeedUserAsync(string password, int intentosFallidos = 0, DateTime? bloqueadoHasta = null)
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
                IntentosFallidos = intentosFallidos,
                BloqueadoHasta = bloqueadoHasta
            };
            _context.Usuarios.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        [Fact]
        public async Task WrongPassword_IncrementsIntentosFallidos()
        {
            var user = await SeedUserAsync("Correct123");

            var result = await _authService.LoginAsync(new LoginDto { Email = "user@test.com", Password = "Wrong" });

            Assert.Equal(LoginStatus.CredencialesInvalidas, result.Status);
            var updated = await _context.Usuarios.FindAsync(user.Id);
            Assert.Equal(1, updated!.IntentosFallidos);
            Assert.Null(updated.BloqueadoHasta);
        }

        [Fact]
        public async Task ReachingMaxAttempts_SetsBloqueadoHasta_AndReturnsBloqueado()
        {
            var user = await SeedUserAsync("Correct123", intentosFallidos: 2); // ya con 2, el 3ro bloquea

            var result = await _authService.LoginAsync(new LoginDto { Email = "user@test.com", Password = "Wrong" });

            Assert.Equal(LoginStatus.Bloqueado, result.Status);
            Assert.True(result.SegundosRestantes > 0);
            var updated = await _context.Usuarios.FindAsync(user.Id);
            Assert.NotNull(updated!.BloqueadoHasta);
            Assert.True(updated.BloqueadoHasta > DateTime.UtcNow);
            Assert.Equal(0, updated.IntentosFallidos); // reseteado tras bloquear
        }

        [Fact]
        public async Task LockedAccount_RejectsEvenWithCorrectPassword_DuringWindow()
        {
            await SeedUserAsync("Correct123", bloqueadoHasta: DateTime.UtcNow.AddMinutes(10));

            var result = await _authService.LoginAsync(new LoginDto { Email = "user@test.com", Password = "Correct123" });

            Assert.Equal(LoginStatus.Bloqueado, result.Status);
            Assert.True(result.SegundosRestantes > 0);
        }

        [Fact]
        public async Task ExpiredLockout_AllowsLogin_AndClearsState()
        {
            var user = await SeedUserAsync("Correct123", bloqueadoHasta: DateTime.UtcNow.AddMinutes(-1));

            var result = await _authService.LoginAsync(new LoginDto { Email = "user@test.com", Password = "Correct123" });

            Assert.Equal(LoginStatus.Ok, result.Status);
            Assert.NotNull(result.Response);
            var updated = await _context.Usuarios.FindAsync(user.Id);
            Assert.Null(updated!.BloqueadoHasta);
            Assert.Equal(0, updated.IntentosFallidos);
        }

        [Fact]
        public async Task SuccessfulLogin_ResetsIntentosFallidos()
        {
            var user = await SeedUserAsync("Correct123", intentosFallidos: 2);

            var result = await _authService.LoginAsync(new LoginDto { Email = "user@test.com", Password = "Correct123" });

            Assert.Equal(LoginStatus.Ok, result.Status);
            var updated = await _context.Usuarios.FindAsync(user.Id);
            Assert.Equal(0, updated!.IntentosFallidos);
        }

        [Fact]
        public async Task NonexistentEmail_ReturnsCredencialesInvalidas_NoThrow()
        {
            var result = await _authService.LoginAsync(new LoginDto { Email = "nope@test.com", Password = "x" });

            Assert.Equal(LoginStatus.CredencialesInvalidas, result.Status);
        }
    }
}
