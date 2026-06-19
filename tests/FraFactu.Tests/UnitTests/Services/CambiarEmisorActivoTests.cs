using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
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
    /// UsuarioCompartido + Plan B Hub-as-Emisor — Task B.2.
    /// Tests del cambio de Emisor activo via POST /api/auth/cambiar-emisor-activo.
    /// Cubren autorizacion via claim, lookup en BD, persistencia del cambio y
    /// reemision del JWT con la misma lista de accesibles.
    /// </summary>
    public class CambiarEmisorActivoTests
    {
        private readonly ApplicationDbContext _context;
        private readonly AuthService _authService;

        public CambiarEmisorActivoTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: $"CambiarEmisorActivoTests_{Guid.NewGuid()}")
                .Options;
            _context = new ApplicationDbContext(options);

            var jwtSettings = new Mock<IOptions<JwtSettings>>();
            jwtSettings.Setup(x => x.Value).Returns(new JwtSettings
            {
                SecretKey = "super_secret_key_for_testing_purposes_only_long_enough",
                Issuer = "TestIssuer",
                Audience = "TestAudience",
                ExpirationMinutes = 60
            });

            var googleAuthSettings = new Mock<IOptions<GoogleAuthSettings>>();
            googleAuthSettings.Setup(x => x.Value).Returns(new GoogleAuthSettings
            {
                ClientId = "test-client-id",
                ClientSecret = "test-client-secret"
            });

            var googleValidator = new Mock<IGoogleTokenValidator>();

            _authService = new AuthService(
                _context,
                jwtSettings.Object,
                googleAuthSettings.Object,
                googleValidator.Object,
                new Mock<IAuthEmailService>().Object,
                NullLogger<AuthService>.Instance
            );

            SeedDatos();
        }

        private void SeedDatos()
        {
            _context.Roles.Add(new Rol { Id = 2, Nombre = "EmisorAdmin" });

            _context.Emisores.AddRange(
                new Emisor
                {
                    Id = 10,
                    NombreRazonSocial = "Empresa Uno",
                    Nit = "06140506141011",
                    Nrc = "111111-1",
                    CodigoActividad = "47111",
                    DescripcionActividad = "Comercio",
                    CorreoElectronico = "uno@empresa.com",
                    Telefono = "22221111",
                    CatDepartamentoId = 1,
                    CatMunicipioId = 1
                },
                new Emisor
                {
                    Id = 20,
                    NombreRazonSocial = "Empresa Dos",
                    Nit = "06140506141022",
                    Nrc = "222222-2",
                    CodigoActividad = "47111",
                    DescripcionActividad = "Comercio",
                    CorreoElectronico = "dos@empresa.com",
                    Telefono = "22222222",
                    CatDepartamentoId = 1,
                    CatMunicipioId = 1
                },
                new Emisor
                {
                    Id = 30,
                    NombreRazonSocial = "Empresa Tres",
                    Nit = "06140506141033",
                    Nrc = "333333-3",
                    CodigoActividad = "47111",
                    DescripcionActividad = "Comercio",
                    CorreoElectronico = "tres@empresa.com",
                    Telefono = "22223333",
                    CatDepartamentoId = 1,
                    CatMunicipioId = 1
                }
            );

            _context.Usuarios.Add(new Usuario
            {
                Id = 50,
                Email = "compartido@empresa.com",
                NombreCompleto = "Usuario Compartido",
                EmisorId = 10,
                RolId = 2,
                Estado = EstadoUsuario.Activo,
                ProveedorAuth = ProveedorAutenticacion.SmartHub,
                AccesoTodasSucursales = true
            });

            _context.SaveChanges();
        }

        private static List<int> LeerEmisoresAccesiblesDelJwt(string jwt)
        {
            var token = new JwtSecurityTokenHandler().ReadJwtToken(jwt);
            var claim = token.Claims.FirstOrDefault(c => c.Type == "emisores_accesibles");
            Assert.NotNull(claim);
            return JsonSerializer.Deserialize<List<int>>(claim!.Value) ?? new List<int>();
        }

        private static int? LeerEmisorIdDelJwt(string jwt)
        {
            var token = new JwtSecurityTokenHandler().ReadJwtToken(jwt);
            var claim = token.Claims.FirstOrDefault(c => c.Type == "EmisorId");
            return claim == null ? null : int.Parse(claim.Value);
        }

        [Fact]
        public async Task CambiarEmisorActivo_ConEmisorEnLista_ReemiteJwt()
        {
            // Usuario activo en Emisor 10, accesibles=[10,20,30], cambia a 20.
            var result = await _authService.CambiarEmisorActivoAsync(
                usuarioId: 50,
                nuevoEmisorId: 20,
                emisoresAccesibles: new[] { 10, 20, 30 });

            Assert.Equal(CambiarEmisorActivoStatus.Ok, result.Status);
            Assert.NotNull(result.Response);
            Assert.Equal(20, result.Response!.EmisorId);
            Assert.Equal("Empresa Dos", result.Response.EmisorNombre);
            Assert.NotEmpty(result.Response.Token);

            // El JWT lleva el nuevo EmisorId y la misma lista de accesibles.
            Assert.Equal(20, LeerEmisorIdDelJwt(result.Response.Token));
            Assert.Equal(new[] { 10, 20, 30 }, LeerEmisoresAccesiblesDelJwt(result.Response.Token));
        }

        [Fact]
        public async Task CambiarEmisorActivo_ConEmisorFueraDeLista_Devuelve403()
        {
            // Accesibles=[10,20], intenta cambiar a 30 → NoAutorizado (controller mapea a 403).
            var result = await _authService.CambiarEmisorActivoAsync(
                usuarioId: 50,
                nuevoEmisorId: 30,
                emisoresAccesibles: new[] { 10, 20 });

            Assert.Equal(CambiarEmisorActivoStatus.NoAutorizado, result.Status);
            Assert.Null(result.Response);

            // No se persistio nada: el usuario sigue en Emisor 10.
            var usuario = await _context.Usuarios.FindAsync(50);
            Assert.Equal(10, usuario!.EmisorId);
        }

        [Fact]
        public async Task CambiarEmisorActivo_ActualizaUsuarioEmisorIdEnBd()
        {
            var antes = await _context.Usuarios.FindAsync(50);
            Assert.Equal(10, antes!.EmisorId);

            await _authService.CambiarEmisorActivoAsync(
                usuarioId: 50,
                nuevoEmisorId: 30,
                emisoresAccesibles: new[] { 10, 20, 30 });

            // Forzar recarga desde el contexto.
            await _context.Entry(antes).ReloadAsync();
            Assert.Equal(30, antes.EmisorId);
            Assert.NotNull(antes.UltimoAcceso);
        }

        [Fact]
        public async Task CambiarEmisorActivo_EmisorNoExisteEnBd_Devuelve404()
        {
            // El claim incluye 999 (caso defensivo: el Hub reporta un emisor
            // que aun no existe en Smartix), pero la BD no lo tiene.
            var result = await _authService.CambiarEmisorActivoAsync(
                usuarioId: 50,
                nuevoEmisorId: 999,
                emisoresAccesibles: new[] { 10, 20, 999 });

            Assert.Equal(CambiarEmisorActivoStatus.NoExiste, result.Status);
            Assert.Null(result.Response);

            // No se persistio: el usuario sigue en su Emisor original.
            var usuario = await _context.Usuarios.FindAsync(50);
            Assert.Equal(10, usuario!.EmisorId);
        }
    }
}
