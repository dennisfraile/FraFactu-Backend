using AutoMapper;
using FluentValidation;
using FluentValidation.Results;
using FraFactu.Application.DTOs.Usuarios;
using FraFactu.Application.Interfaces;
using FraFactu.Domain.Entities;
using FraFactu.Infrastructure.Persistence;
using FraFactu.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FraFactu.Tests.UnitTests.Services
{
    /// <summary>
    /// Clave temporal en el alta por admin (F2): al crear un usuario sin
    /// contraseña, el sistema genera una temporal, marca RequiereCambioPwd y
    /// envía el correo con las credenciales.
    /// </summary>
    public class UsuarioServiceCreateTempPasswordTests
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<IAuthEmailService> _authEmail = new();
        private readonly UsuarioService _service;

        public UsuarioServiceCreateTempPasswordTests()
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

            // AuthService real para hashear/verificar la clave temporal.
            var authService = new Mock<IAuthService>();
            authService.Setup(a => a.HashPassword(It.IsAny<string>()))
                .Returns<string>(p => BCrypt.Net.BCrypt.HashPassword(p));

            _service = new UsuarioService(
                _context,
                new Mock<IMapper>().Object,
                createValidator.Object,
                updateValidator.Object,
                authService.Object,
                _authEmail.Object);
        }

        private CreateUsuarioDto BuildCreate() => new()
        {
            NombreCompleto = "Nuevo Usuario",
            Email = "nuevo@test.com",
            Password = null, // el admin no fija contraseña → temporal
            EmisorId = 10,
            RolId = 1,
            AccesoTodasSucursales = true,
            SucursalIds = new List<int>(),
            CajaIds = new List<int>()
        };

        private async Task SeedRolAsync()
        {
            _context.Roles.Add(new Rol { Id = 1, Nombre = "Cajero" });
            await _context.SaveChangesAsync();
        }

        [Fact]
        public async Task CreateAsync_GeneratesTemporaryPassword_AndMarksRequiereCambioPwd()
        {
            await SeedRolAsync();

            var result = await _service.CreateAsync(BuildCreate());

            var user = await _context.Usuarios.FirstAsync(u => u.Email == "nuevo@test.com");
            Assert.True(user.RequiereCambioPwd);
            Assert.True(user.PermiteCambioPwd); // debe poder cambiarla en el primer ingreso
            Assert.False(string.IsNullOrEmpty(user.PasswordHash));
            Assert.NotNull(user.ExpiracionPwdTemporal);
            Assert.True(user.ExpiracionPwdTemporal > DateTime.UtcNow);
            Assert.True(result.RequiereCambioPwd);
        }

        [Fact]
        public async Task CreateAsync_SendsTemporaryPasswordEmail()
        {
            await SeedRolAsync();

            await _service.CreateAsync(BuildCreate());

            _authEmail.Verify(
                e => e.EnviarClaveTemporalAsync(
                    "nuevo@test.com",
                    "Nuevo Usuario",
                    It.Is<string>(p => !string.IsNullOrEmpty(p))),
                Times.Once);
        }
    }
}
