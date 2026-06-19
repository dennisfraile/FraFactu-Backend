using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using FraFactu.Application.Common.Settings;
using FraFactu.Application.DTOs.Auth;
using FraFactu.Application.DTOs.Hub;
using FraFactu.Application.Interfaces;
using FraFactu.Domain.Entities;
using FraFactu.Domain.Entities.Catalogos;
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
    /// Tests del SSO Hub→Smartix (POST /api/Auth/hub-login).
    /// Cubren mapping rol Hub→Smartix, lookup Hub→Emisor, upsert por HubUsuarioId
    /// y por Email, sucursales, estado inactivo y errores de configuracion.
    /// </summary>
    public class HubLoginAsyncTests
    {
        private readonly Mock<ISmartHubApiService> _mockHubApi;
        private readonly ApplicationDbContext _context;
        private readonly AuthService _authService;

        public HubLoginAsyncTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: $"HubLoginTests_{Guid.NewGuid()}")
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
            _mockHubApi = new Mock<ISmartHubApiService>();

            _authService = new AuthService(
                _context,
                jwtSettings.Object,
                googleAuthSettings.Object,
                googleValidator.Object,
                _mockHubApi.Object,
                NullLogger<AuthService>.Instance
            );

            SeedRolesYEmisor();
        }

        private void SeedRolesYEmisor()
        {
            _context.Roles.AddRange(
                new Rol { Id = 1, Nombre = "SuperAdmin" },
                new Rol { Id = 2, Nombre = "EmisorAdmin" }
            );
            _context.Emisores.Add(new Emisor
            {
                Id = 10,
                HubId = 100,
                NombreRazonSocial = "Empresa Test",
                Nit = "06140506141011",
                Nrc = "123456-7",
                CodigoActividad = "47111",
                DescripcionActividad = "Comercio",
                CorreoElectronico = "test@empresa.com",
                Telefono = "22223333",
                CatDepartamentoId = 1,
                CatMunicipioId = 1
            });
            _context.SaveChanges();
        }

        [Fact]
        public async Task HubLogin_LanzaUnauthorized_CuandoCodeVacio()
        {
            var request = new HubLoginRequestDto { Code = "" };
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _authService.HubLoginAsync(request));
        }

        [Fact]
        public async Task HubLogin_LanzaUnauthorized_CuandoSmartHubRechazaCode()
        {
            _mockHubApi.Setup(h => h.ValidateExchangeCodeAsync("badcode", default))
                .ReturnsAsync((HubUserInfoDto?)null);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _authService.HubLoginAsync(new HubLoginRequestDto { Code = "badcode" }));
        }

        [Fact]
        public async Task HubLogin_LanzaInvalidOperation_CuandoUsuarioSinHubActivo()
        {
            _mockHubApi.Setup(h => h.ValidateExchangeCodeAsync("c", default))
                .ReturnsAsync(new HubUserInfoDto
                {
                    HubUsuarioId = 1,
                    Email = "u@x.com",
                    NombreCompleto = "U",
                    OrganizacionId = null,
                    Rol = "Usuario"
                });

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _authService.HubLoginAsync(new HubLoginRequestDto { Code = "c" }));
        }

        [Fact]
        public async Task HubLogin_LanzaInvalidOperation_CuandoHubSinEmisorVinculado()
        {
            _mockHubApi.Setup(h => h.ValidateExchangeCodeAsync("c", default))
                .ReturnsAsync(new HubUserInfoDto
                {
                    HubUsuarioId = 1,
                    Email = "u@x.com",
                    NombreCompleto = "U",
                    OrganizacionId = 999, // No existe en BD
                    Rol = "AdminOrg"
                });

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _authService.HubLoginAsync(new HubLoginRequestDto { Code = "c" }));
        }

        [Fact]
        public async Task HubLogin_CreaUsuarioNuevo_CuandoEsAdminOrg()
        {
            _mockHubApi.Setup(h => h.ValidateExchangeCodeAsync("c", default))
                .ReturnsAsync(new HubUserInfoDto
                {
                    HubUsuarioId = 555,
                    Email = "admin@empresa.com",
                    NombreCompleto = "Admin Org",
                    OrganizacionId = 100,
                    Rol = "AdminOrg",
                    SucursalIds = new List<int>()
                });

            var result = await _authService.HubLoginAsync(new HubLoginRequestDto { Code = "c" });

            Assert.NotNull(result);
            Assert.Equal(10, result.EmisorId);
            Assert.Equal("EmisorAdmin", result.RolNombre);
            Assert.True(result.AccesoTodasSucursales);
            Assert.NotEmpty(result.Token);

            var creado = await _context.Usuarios.FirstAsync(u => u.HubUsuarioId == 555);
            Assert.Equal("admin@empresa.com", creado.Email);
            Assert.Equal(ProveedorAutenticacion.SmartHub, creado.ProveedorAuth);
        }

        [Fact]
        public async Task HubLogin_CreaUsuarioNuevo_ConSucursales_CuandoEsUsuario()
        {
            _mockHubApi.Setup(h => h.ValidateExchangeCodeAsync("c", default))
                .ReturnsAsync(new HubUserInfoDto
                {
                    HubUsuarioId = 777,
                    Email = "user@empresa.com",
                    NombreCompleto = "Usuario Basico",
                    OrganizacionId = 100,
                    Rol = "Usuario",
                    SucursalIds = new List<int> { 1, 2 }
                });

            var result = await _authService.HubLoginAsync(new HubLoginRequestDto { Code = "c" });

            Assert.NotNull(result);
            Assert.Equal("EmisorAdmin", result.RolNombre); // Default para "Usuario"
            Assert.False(result.AccesoTodasSucursales);
            Assert.Equal(2, result.SucursalIds.Count);

            var creado = await _context.Usuarios
                .Include(u => u.UsuarioSucursales)
                .FirstAsync(u => u.HubUsuarioId == 777);
            Assert.Equal(2, creado.UsuarioSucursales.Count);
        }

        [Fact]
        public async Task HubLogin_MapeaRol_SuperAdmin_HubASuperAdmin_Smartix()
        {
            _mockHubApi.Setup(h => h.ValidateExchangeCodeAsync("c", default))
                .ReturnsAsync(new HubUserInfoDto
                {
                    HubUsuarioId = 888,
                    Email = "super@empresa.com",
                    NombreCompleto = "Super",
                    OrganizacionId = 100,
                    Rol = "SuperAdmin"
                });

            var result = await _authService.HubLoginAsync(new HubLoginRequestDto { Code = "c" });

            Assert.Equal("SuperAdmin", result.RolNombre);
            Assert.True(result.AccesoTodasSucursales);
        }

        [Fact]
        public async Task HubLogin_EnlazaPorHubUsuarioId_CuandoUsuarioYaExiste()
        {
            _context.Usuarios.Add(new Usuario
            {
                Id = 50,
                Email = "existing@empresa.com",
                NombreCompleto = "Existing",
                HubUsuarioId = 333,
                EmisorId = 10,
                RolId = 2,
                Estado = EstadoUsuario.Activo,
                ProveedorAuth = ProveedorAutenticacion.SmartHub
            });
            await _context.SaveChangesAsync();

            _mockHubApi.Setup(h => h.ValidateExchangeCodeAsync("c", default))
                .ReturnsAsync(new HubUserInfoDto
                {
                    HubUsuarioId = 333,
                    Email = "existing@empresa.com",
                    NombreCompleto = "Existing",
                    OrganizacionId = 100,
                    Rol = "AdminOrg"
                });

            var result = await _authService.HubLoginAsync(new HubLoginRequestDto { Code = "c" });

            Assert.Equal(50, result.UserId);
            // No se duplico
            Assert.Equal(1, await _context.Usuarios.CountAsync(u => u.HubUsuarioId == 333));

            // UltimoAcceso fue actualizado
            var u = await _context.Usuarios.FindAsync(50);
            Assert.NotNull(u!.UltimoAcceso);
        }

        [Fact]
        public async Task HubLogin_EnlazaPorEmail_CuandoUsuarioPreSSO_SinHubUsuarioId()
        {
            _context.Usuarios.Add(new Usuario
            {
                Id = 60,
                Email = "legacy@empresa.com",
                NombreCompleto = "Legacy",
                HubUsuarioId = null, // Pre-SSO
                EmisorId = 10,
                RolId = 2,
                Estado = EstadoUsuario.Activo,
                ProveedorAuth = ProveedorAutenticacion.Local
            });
            await _context.SaveChangesAsync();

            _mockHubApi.Setup(h => h.ValidateExchangeCodeAsync("c", default))
                .ReturnsAsync(new HubUserInfoDto
                {
                    HubUsuarioId = 444,
                    Email = "legacy@empresa.com",
                    NombreCompleto = "Legacy",
                    OrganizacionId = 100,
                    Rol = "AdminOrg"
                });

            var result = await _authService.HubLoginAsync(new HubLoginRequestDto { Code = "c" });

            Assert.Equal(60, result.UserId);

            var u = await _context.Usuarios.FindAsync(60);
            Assert.Equal(444, u!.HubUsuarioId);
        }

        [Fact]
        public async Task HubLogin_LanzaUnauthorized_CuandoUsuarioInactivo()
        {
            _context.Usuarios.Add(new Usuario
            {
                Id = 70,
                Email = "inactivo@empresa.com",
                NombreCompleto = "Inactivo",
                HubUsuarioId = 222,
                EmisorId = 10,
                RolId = 2,
                Estado = EstadoUsuario.Inactivo,
                ProveedorAuth = ProveedorAutenticacion.SmartHub
            });
            await _context.SaveChangesAsync();

            _mockHubApi.Setup(h => h.ValidateExchangeCodeAsync("c", default))
                .ReturnsAsync(new HubUserInfoDto
                {
                    HubUsuarioId = 222,
                    Email = "inactivo@empresa.com",
                    NombreCompleto = "Inactivo",
                    OrganizacionId = 100,
                    Rol = "AdminOrg"
                });

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _authService.HubLoginAsync(new HubLoginRequestDto { Code = "c" }));
        }

        [Fact]
        public async Task HubLogin_SyncEmisorId_CuandoUsuarioCambioDeHub()
        {
            // Usuario existente vinculado a otro Emisor
            _context.Emisores.Add(new Emisor
            {
                Id = 20,
                HubId = 200,
                NombreRazonSocial = "Otra Empresa",
                Nit = "06140506141022",
                Nrc = "654321-7",
                CodigoActividad = "47111",
                DescripcionActividad = "Comercio",
                CorreoElectronico = "otro@empresa.com",
                Telefono = "22224444",
                CatDepartamentoId = 1,
                CatMunicipioId = 1
            });
            _context.Usuarios.Add(new Usuario
            {
                Id = 80,
                Email = "multihub@empresa.com",
                NombreCompleto = "Multi",
                HubUsuarioId = 111,
                EmisorId = 20, // Estaba en Emisor 20
                RolId = 2,
                Estado = EstadoUsuario.Activo,
                ProveedorAuth = ProveedorAutenticacion.SmartHub
            });
            await _context.SaveChangesAsync();

            // Hub ahora reporta OrganizacionId 100 (Emisor 10)
            _mockHubApi.Setup(h => h.ValidateExchangeCodeAsync("c", default))
                .ReturnsAsync(new HubUserInfoDto
                {
                    HubUsuarioId = 111,
                    Email = "multihub@empresa.com",
                    NombreCompleto = "Multi",
                    OrganizacionId = 100,
                    Rol = "AdminOrg"
                });

            var result = await _authService.HubLoginAsync(new HubLoginRequestDto { Code = "c" });

            Assert.Equal(10, result.EmisorId); // Cambio a Emisor 10

            var u = await _context.Usuarios.FindAsync(80);
            Assert.Equal(10, u!.EmisorId);
        }

        // ============================================
        // BLOQUE D — REFRESH FISCAL DESDE SMARTHUB (Task 16)
        // ============================================

        private void SeedCatalogosMH()
        {
            _context.CatDepartamentos.Add(new CatDepartamento
            {
                Id = 6, Codigo = "06", Valor = "San Salvador"
            });
            _context.CatMunicipios.Add(new CatMunicipio
            {
                Id = 14, Codigo = "23", CodigoDepartamento = "06", Valor = "San Salvador Centro"
            });
            _context.CatTiposEstablecimiento.Add(new CatTipoEstablecimiento
            {
                Id = 1, Codigo = "01", Valor = "Sucursal/Agencia/Otro"
            });
            _context.SaveChanges();
        }

        [Fact]
        public async Task HubLogin_RefrescaCamposFiscales_CuandoSmartHubResponde()
        {
            SeedCatalogosMH();
            _mockHubApi.Setup(h => h.ValidateExchangeCodeAsync("c", default))
                .ReturnsAsync(new HubUserInfoDto
                {
                    HubUsuarioId = 1001,
                    Email = "admin@empresa.com",
                    NombreCompleto = "Admin",
                    OrganizacionId = 100,
                    Rol = "AdminOrg"
                });
            _mockHubApi.Setup(h => h.GetFiscalPayloadForHubAsync(100, default))
                .ReturnsAsync(new HubFiscalPayloadDto
                {
                    Emisor = new EmisorFiscalPayloadDto
                    {
                        HubId = 100,
                        Nit = "06140506141099",
                        Nrc = "999999-9",
                        NombreRazonSocial = "Razon Social Actualizada SA",
                        NombreComercial = "Comercial Nuevo",
                        CodActividadEconomica = "47190",
                        DescActividadEconomica = "Otros comercios",
                        CodTipoEstablecimiento = "01",
                        CodDepartamento = "06",
                        CodMunicipio = "23",
                        DireccionComplemento = "Calle Nueva #123",
                        TelefonoFiscal = "22229999",
                        CorreoFiscal = "fiscal@empresa.com"
                    },
                    Sucursal = null
                });

            await _authService.HubLoginAsync(new HubLoginRequestDto { Code = "c" });

            var emisor = await _context.Emisores.FindAsync(10);
            Assert.Equal("06140506141099", emisor!.Nit);
            Assert.Equal("999999-9", emisor.Nrc);
            Assert.Equal("Razon Social Actualizada SA", emisor.NombreRazonSocial);
            Assert.Equal("Comercial Nuevo", emisor.NombreComercial);
            Assert.Equal("47190", emisor.CodigoActividad);
            Assert.Equal("Otros comercios", emisor.DescripcionActividad);
            Assert.Equal(6, emisor.CatDepartamentoId);
            Assert.Equal(14, emisor.CatMunicipioId);
            Assert.Equal(1, emisor.CatTipoEstablecimientoId);
            Assert.Equal("Calle Nueva #123", emisor.Direccion);
            Assert.Equal("22229999", emisor.Telefono);
            Assert.Equal("fiscal@empresa.com", emisor.CorreoElectronico);
        }

        [Fact]
        public async Task HubLogin_NoTocaEmisor_CuandoSmartHubRetornaNull()
        {
            // El caso "SmartHub apagado / endpoint no existe / payload incompleto"
            // retorna null por el cliente; el login debe completarse con el cache local.
            _mockHubApi.Setup(h => h.ValidateExchangeCodeAsync("c", default))
                .ReturnsAsync(new HubUserInfoDto
                {
                    HubUsuarioId = 1002,
                    Email = "admin@empresa.com",
                    NombreCompleto = "Admin",
                    OrganizacionId = 100,
                    Rol = "AdminOrg"
                });
            _mockHubApi.Setup(h => h.GetFiscalPayloadForHubAsync(100, default))
                .ReturnsAsync((HubFiscalPayloadDto?)null);

            var result = await _authService.HubLoginAsync(new HubLoginRequestDto { Code = "c" });

            Assert.Equal(10, result.EmisorId);
            var emisor = await _context.Emisores.FindAsync(10);
            // NIT original del seed preservado
            Assert.Equal("06140506141011", emisor!.Nit);
            Assert.Equal("Empresa Test", emisor.NombreRazonSocial);
        }

        [Fact]
        public async Task HubLogin_OmiteRefresh_CuandoCatalogoMHFaltante_YLoginContinua()
        {
            // No seed de catalogos: la resolucion va a fallar y el refresh se omite,
            // pero el login DEBE completarse igual.
            _mockHubApi.Setup(h => h.ValidateExchangeCodeAsync("c", default))
                .ReturnsAsync(new HubUserInfoDto
                {
                    HubUsuarioId = 1003,
                    Email = "admin@empresa.com",
                    NombreCompleto = "Admin",
                    OrganizacionId = 100,
                    Rol = "AdminOrg"
                });
            _mockHubApi.Setup(h => h.GetFiscalPayloadForHubAsync(100, default))
                .ReturnsAsync(new HubFiscalPayloadDto
                {
                    Emisor = new EmisorFiscalPayloadDto
                    {
                        HubId = 100,
                        CodDepartamento = "ZZ", // No existe
                        CodMunicipio = "ZZ",
                        CodTipoEstablecimiento = "ZZ",
                        Nit = "X", Nrc = "X", NombreRazonSocial = "X",
                        CodActividadEconomica = "X", DescActividadEconomica = "X",
                        DireccionComplemento = "X"
                    }
                });

            var result = await _authService.HubLoginAsync(new HubLoginRequestDto { Code = "c" });

            Assert.Equal(10, result.EmisorId);
            var emisor = await _context.Emisores.FindAsync(10);
            // Datos originales intactos: el refresh fue abortado al no encontrar catalogos.
            Assert.Equal("06140506141011", emisor!.Nit);
            Assert.Equal("Empresa Test", emisor.NombreRazonSocial);
        }

        // ============================================
        // B.1 — Propagacion del claim emisores_accesibles (UsuarioCompartido + Plan B)
        // ============================================

        /// <summary>
        /// Extrae el claim emisores_accesibles del JWT emitido por Smartix.
        /// Lo hace cada test parseando el token devuelto en LoginResponseDto.Token
        /// porque el claim es el contrato observable end-to-end.
        /// </summary>
        private static List<int> LeerEmisoresAccesiblesDelJwt(string jwt)
        {
            var token = new JwtSecurityTokenHandler().ReadJwtToken(jwt);
            var claim = token.Claims.FirstOrDefault(c => c.Type == "emisores_accesibles");
            Assert.NotNull(claim);
            return JsonSerializer.Deserialize<List<int>>(claim!.Value) ?? new List<int>();
        }

        [Fact]
        public async Task HubLogin_ConClaimEmisoresAccesibles_PropagaAlJwtDeSmartix()
        {
            // Hub envia lista explicita (caso UsuarioCompartido con multi-Hub).
            _mockHubApi.Setup(h => h.ValidateExchangeCodeAsync("c", default))
                .ReturnsAsync(new HubUserInfoDto
                {
                    HubUsuarioId = 2001,
                    Email = "compartido@empresa.com",
                    NombreCompleto = "Usuario Compartido",
                    OrganizacionId = 100,
                    Rol = "Usuario",
                    SucursalIds = new List<int> { 1 },
                    EmisoresAccesibles = new List<int> { 10, 20, 30 }
                });

            var result = await _authService.HubLoginAsync(new HubLoginRequestDto { Code = "c" });

            var emisores = LeerEmisoresAccesiblesDelJwt(result.Token);
            Assert.Equal(new[] { 10, 20, 30 }, emisores);
        }

        [Fact]
        public async Task HubLogin_SinClaimEmisoresAccesibles_UsaSoloEmisorIdDelUsuario()
        {
            // Back-compat: SmartHub-BE no expone el campo todavia.
            // HubUserInfoDto inicializa EmisoresAccesibles = new() por default
            // (lista vacia), que es identico a la rama "deploy parcial".
            _mockHubApi.Setup(h => h.ValidateExchangeCodeAsync("c", default))
                .ReturnsAsync(new HubUserInfoDto
                {
                    HubUsuarioId = 2002,
                    Email = "sinclaim@empresa.com",
                    NombreCompleto = "Sin Claim",
                    OrganizacionId = 100,
                    Rol = "AdminOrg"
                    // EmisoresAccesibles omitido -> queda en new() (lista vacia)
                });

            var result = await _authService.HubLoginAsync(new HubLoginRequestDto { Code = "c" });

            var emisores = LeerEmisoresAccesiblesDelJwt(result.Token);
            Assert.Equal(new[] { 10 }, emisores); // Fallback = [EmisorId del usuario]
        }

        [Fact]
        public async Task HubLogin_ConEmisoresAccesiblesVacia_UsaSoloEmisorIdDelUsuario()
        {
            // Caso defensivo: la lista llega explicitamente vacia (e.g. el
            // calculator del Hub devolvio [] porque ningun Hub esta vinculado
            // a Smartix). Igual emitimos [EmisorId] para no romper autorizadores.
            _mockHubApi.Setup(h => h.ValidateExchangeCodeAsync("c", default))
                .ReturnsAsync(new HubUserInfoDto
                {
                    HubUsuarioId = 2003,
                    Email = "vacio@empresa.com",
                    NombreCompleto = "Vacio",
                    OrganizacionId = 100,
                    Rol = "AdminOrg",
                    EmisoresAccesibles = new List<int>()
                });

            var result = await _authService.HubLoginAsync(new HubLoginRequestDto { Code = "c" });

            var emisores = LeerEmisoresAccesiblesDelJwt(result.Token);
            Assert.Equal(new[] { 10 }, emisores);
        }
    }
}
