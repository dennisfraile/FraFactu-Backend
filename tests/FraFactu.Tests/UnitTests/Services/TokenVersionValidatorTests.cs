using FraFactu.Domain.Entities;
using FraFactu.Domain.Enums;
using FraFactu.Infrastructure.Persistence;
using FraFactu.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace FraFactu.Tests.UnitTests.Services
{
    public class TokenVersionValidatorTests
    {
        private readonly ApplicationDbContext _context;
        private readonly TokenVersionValidator _validator;

        public TokenVersionValidatorTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
            _validator = new TokenVersionValidator(_context);
        }

        private async Task<Usuario> SeedUserAsync(int tokenVersion)
        {
            _context.Roles.Add(new Rol { Id = 1, Nombre = "EmisorAdmin" });
            var user = new Usuario
            {
                Email = "user@test.com",
                NombreCompleto = "User",
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
        public async Task IsCurrentAsync_ReturnsTrue_WhenClaimMatchesUserTokenVersion()
        {
            var user = await SeedUserAsync(tokenVersion: 3);

            var result = await _validator.IsCurrentAsync(user.Id, tokenVersionClaim: 3);

            Assert.True(result);
        }

        [Fact]
        public async Task IsCurrentAsync_ReturnsFalse_WhenClaimIsStale()
        {
            // El usuario hizo logout/cambio de contraseña → TokenVersion subió a 4,
            // pero el JWT viejo trae 3.
            var user = await SeedUserAsync(tokenVersion: 4);

            var result = await _validator.IsCurrentAsync(user.Id, tokenVersionClaim: 3);

            Assert.False(result);
        }

        [Fact]
        public async Task IsCurrentAsync_ReturnsFalse_WhenClaimIsMissing()
        {
            var user = await SeedUserAsync(tokenVersion: 0);

            var result = await _validator.IsCurrentAsync(user.Id, tokenVersionClaim: null);

            Assert.False(result);
        }

        [Fact]
        public async Task IsCurrentAsync_ReturnsFalse_WhenUserDoesNotExist()
        {
            var result = await _validator.IsCurrentAsync(usuarioId: 999, tokenVersionClaim: 0);

            Assert.False(result);
        }
    }
}
