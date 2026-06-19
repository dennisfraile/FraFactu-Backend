using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using FraFactu.Infrastructure.Services;
using FraFactu.Infrastructure.Persistence;
using FraFactu.Domain.Entities;
using FraFactu.Application.DTOs.Lotes;
using FraFactu.Domain.Entities.Catalogos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FraFactu.Tests.IntegrationTests.Services
{
    /// <summary>
    /// Test de integración para validar que los lotes NO permiten facturas sin firmar
    /// Este test demuestra que la validación está activa y funcionando correctamente
    /// </summary>
    public class LoteValidacionIntegrationTest
    {
        private (ApplicationDbContext context, int emisorId, int tipoDocId) CreateTestContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;

            var context = new ApplicationDbContext(options);

            // Seed de catálogos requeridos por los Includes en LoteService
            var ambiente = new CatAmbienteDestino { Codigo = "00", Valor = "Modo Prueba" };
            context.CatAmbientes.Add(ambiente);

            var tipoDoc = new CatTipoDocumento { Codigo = "01", Valor = "Factura" };
            context.CatTiposDocumento.Add(tipoDoc);

            context.SaveChanges();

            // Seed de emisor
            var emisor = new Emisor
            {
                Nit = "0614-010101-101-1",
                NombreRazonSocial = "Empresa Test SA de CV",
                NombreComercial = "Empresa Test",
                MhLlavePrivada = "test_key",
                MhPassPrivada = "test_pass",
                Activo = true,
                CatAmbienteDestinoId = ambiente.Id
            };

            context.Emisores.Add(emisor);
            context.SaveChanges();

            return (context, emisor.Id, tipoDoc.Id);
        }

        private LoteService CreateLoteService(ApplicationDbContext context)
        {
            var logger = new Mock<ILogger<LoteService>>().Object;
            var httpClientFactory = new Mock<System.Net.Http.IHttpClientFactory>().Object;
            var currentUserServiceMock = new Mock<Application.Interfaces.ICurrentUserService>();
            var haciendaServiceMock = new Mock<Application.Interfaces.Hacienda.IHaciendaApiService>();
            var facturaServiceMock = new Mock<Application.Interfaces.IFacturaService>();
            var signerServiceMock = new Mock<Application.Interfaces.Hacienda.IDteSignerService>();
            var authServiceMock = new Mock<Application.Interfaces.Hacienda.IHaciendaAuthService>();
            var inventarioServiceMock = new Mock<Application.Services.IInventarioIntegrationService>();
            var emailServiceMock = new Mock<Application.Interfaces.IEmailService>();
            var encryptionServiceMock = new Mock<Application.Interfaces.IEncryptionService>();
            var smartCareWebhookMock = new Mock<Application.Interfaces.ISmartCareWebhookService>();

            currentUserServiceMock.Setup(x => x.GetUsuarioNombre()).Returns("Test User");
            encryptionServiceMock.Setup(x => x.Decrypt(It.IsAny<string>())).Returns<string>(s => s);
            encryptionServiceMock.Setup(x => x.Encrypt(It.IsAny<string>())).Returns<string>(s => s);

            return new LoteService(context, logger, httpClientFactory, currentUserServiceMock.Object, haciendaServiceMock.Object, facturaServiceMock.Object, signerServiceMock.Object, authServiceMock.Object, inventarioServiceMock.Object, emailServiceMock.Object, encryptionServiceMock.Object, smartCareWebhookMock.Object);
        }

        [Fact]
        public async Task CrearLote_DebeLanzarExcepcion_CuandoTodasFacturasNoEstanFirmadas()
        {
            // Arrange
            var (context, emisorId, tipoDocId) = CreateTestContext();
            var loteService = CreateLoteService(context);

            // Crear facturas SIN JsonFirmado usando navegación directa para evitar
            // problemas de resolución de FK en InMemory provider
            var emisor = await context.Emisores.FindAsync(emisorId);

            var factura1 = new FacturaElectronica
            {
                Emisor = emisor!,
                EmisorId = emisorId,
                CatTipoDocumentoId = tipoDocId,
                CodigoGeneracion = Guid.NewGuid().ToString().ToUpper(),
                NumeroControl = "DTE-01-00000001-000000000000001",
                JsonFirmado = null, // ❌ NO FIRMADA
                EstadoHacienda = "GENERADO",
                TotalPagar = 100,
                FechaCreacion = DateTime.UtcNow,
                Activo = true
            };

            var factura2 = new FacturaElectronica
            {
                Emisor = emisor!,
                EmisorId = emisorId,
                CatTipoDocumentoId = tipoDocId,
                CodigoGeneracion = Guid.NewGuid().ToString().ToUpper(),
                NumeroControl = "DTE-01-00000001-000000000000002",
                JsonFirmado = "", // ❌ VACÍA
                EstadoHacienda = "GENERADO",
                TotalPagar = 200,
                FechaCreacion = DateTime.UtcNow,
                Activo = true
            };

            context.Facturas.AddRange(factura1, factura2);
            await context.SaveChangesAsync();

            var dto = new CrearLoteDto
            {
                FacturaIds = new List<int> { factura1.Id, factura2.Id }
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => loteService.CrearLoteAsync(emisorId, dto)
            );

            // Verificar mensaje de error
            Assert.Contains("no están firmadas", exception.Message);
            Assert.Contains("2 facturas", exception.Message);
        }

        [Fact]
        public async Task CrearLote_DebeCrearseExitosamente_CuandoTodasFacturasEstanFirmadas()
        {
            // Arrange
            var (context, emisorId, tipoDocId) = CreateTestContext();
            var loteService = CreateLoteService(context);

            var emisor = await context.Emisores.FindAsync(emisorId);

            // Crear facturas CON JsonFirmado
            var factura1 = new FacturaElectronica
            {
                Emisor = emisor!,
                EmisorId = emisorId,
                CatTipoDocumentoId = tipoDocId,
                CodigoGeneracion = Guid.NewGuid().ToString().ToUpper(),
                NumeroControl = "DTE-01-00000001-000000000000003",
                JsonFirmado = "{\"firmado\":true}", // ✅ FIRMADA
                EstadoHacienda = "GENERADO",
                TotalPagar = 100,
                FechaCreacion = DateTime.UtcNow,
                Activo = true
            };

            var factura2 = new FacturaElectronica
            {
                Emisor = emisor!,
                EmisorId = emisorId,
                CatTipoDocumentoId = tipoDocId,
                CodigoGeneracion = Guid.NewGuid().ToString().ToUpper(),
                NumeroControl = "DTE-01-00000001-000000000000004",
                JsonFirmado = "{\"firmado\":true}", // ✅ FIRMADA
                EstadoHacienda = "GENERADO",
                TotalPagar = 200,
                FechaCreacion = DateTime.UtcNow,
                Activo = true
            };

            context.Facturas.AddRange(factura1, factura2);
            await context.SaveChangesAsync();

            var dto = new CrearLoteDto
            {
                FacturaIds = new List<int> { factura1.Id, factura2.Id }
            };

            // Act
            var result = await loteService.CrearLoteAsync(emisorId, dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalPendientes);
            Assert.Equal("Pendiente", result.Estado);
        }
    }
}
