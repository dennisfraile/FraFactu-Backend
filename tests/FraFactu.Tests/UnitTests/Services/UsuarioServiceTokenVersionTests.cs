using AutoMapper;
using FluentValidation;
using FluentValidation.Results;
using FraFactu.Application.DTOs.Usuarios;
using FraFactu.Application.Interfaces;
using FraFactu.Domain.Entities;
using FraFactu.Domain.Enums;
using FraFactu.Infrastructure.Persistence;
using FraFactu.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FraFactu.Tests.UnitTests.Services
{
    /// <summary>
    /// Revocación local (F2): la gestión de usuarios bumpea TokenVersion cuando
    /// cambia el rol o se desactiva al usuario, invalidando sus JWT vigentes.
    /// </summary>
    public class UsuarioServiceTokenVersionTests
    {
        private readonly ApplicationDbContext _context;
        private readonly UsuarioService _service;

        public UsuarioServiceTokenVersionTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);

            var createValidator = new Mock<IValidator<CreateUsuarioDto>>();
            createValidator
                .Setup(v => v.ValidateAsync(It.IsAny<CreateUsuarioDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            var updateValidator = new Mock<IValidator<UpdateUsuarioDto>>();
            updateValidator
                .Setup(v => v.ValidateAsync(It.IsAny<UpdateUsuarioDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _service = new UsuarioService(
                _context,
                new Mock<IMapper>().Object,
                createValidator.Object,
                updateValidator.Object,
                new Mock<IAuthService>().Object,
                new Mock<IAuthEmailService>().Object);
        }

        private async Task<Usuario> SeedAsync(int rolId, int tokenVersion, bool activo = true)
        {
            _context.Roles.Add(new Rol { Id = 1, Nombre = "EmisorAdmin" });
            _context.Roles.Add(new Rol { Id = 2, Nombre = "Contador" });
            var user = new Usuario
            {
                NombreCompleto = "User",
                Email = "user@test.com",
                RolId = rolId,
                EmisorId = 10,
                Estado = EstadoUsuario.Activo,
                Activo = activo,
                AccesoTodasSucursales = true,
                TokenVersion = tokenVersion
            };
            _context.Usuarios.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        private static UpdateUsuarioDto BuildUpdate(int rolId, bool activo) => new()
        {
            NombreCompleto = "User",
            Email = "user@test.com",
            RolId = rolId,
            Activo = activo,
            AccesoTodasSucursales = true,
            SucursalIds = new List<int>(),
            CajaIds = new List<int>()
        };

        [Fact]
        public async Task UpdateAsync_IncrementsTokenVersion_WhenRoleChanges()
        {
            var user = await SeedAsync(rolId: 1, tokenVersion: 0);

            await _service.UpdateAsync(user.Id, BuildUpdate(rolId: 2, activo: true));

            var updated = await _context.Usuarios.FindAsync(user.Id);
            Assert.Equal(1, updated!.TokenVersion);
        }

        [Fact]
        public async Task UpdateAsync_DoesNotIncrementTokenVersion_WhenNothingSecuritySensitiveChanges()
        {
            var user = await SeedAsync(rolId: 1, tokenVersion: 0);

            await _service.UpdateAsync(user.Id, BuildUpdate(rolId: 1, activo: true));

            var updated = await _context.Usuarios.FindAsync(user.Id);
            Assert.Equal(0, updated!.TokenVersion);
        }

        [Fact]
        public async Task UpdateAsync_IncrementsTokenVersion_WhenUserGetsDeactivated()
        {
            var user = await SeedAsync(rolId: 1, tokenVersion: 0, activo: true);

            await _service.UpdateAsync(user.Id, BuildUpdate(rolId: 1, activo: false));

            var updated = await _context.Usuarios.FindAsync(user.Id);
            Assert.Equal(1, updated!.TokenVersion);
        }

        [Fact]
        public async Task DeactivateAsync_IncrementsTokenVersion()
        {
            var user = await SeedAsync(rolId: 1, tokenVersion: 3);

            await _service.DeactivateAsync(user.Id);

            var updated = await _context.Usuarios.FindAsync(user.Id);
            Assert.Equal(4, updated!.TokenVersion);
        }
    }
}
