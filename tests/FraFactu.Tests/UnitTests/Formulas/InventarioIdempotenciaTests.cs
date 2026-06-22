using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using FraFactu.Application.Common.Interfaces;
using FraFactu.Application.Interfaces;
using FraFactu.Infrastructure.Services;
using FraFactu.Infrastructure.Persistence;
using FraFactu.Domain.Entities;

namespace FraFactu.Tests.UnitTests.Formulas
{
    /// <summary>
    /// F3.4 (G4): el descuento inmediato de stock al facturar debe ser idempotente.
    /// Una segunda llamada para la misma factura no debe volver a descontar stock
    /// ni duplicar el MovimientoInventario FACTURA_EMITIDA.
    /// </summary>
    public class InventarioIdempotenciaTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly InventarioIntegrationService _service;

        public InventarioIdempotenciaTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            _context = new ApplicationDbContext(options);
            _service = new TestableInventarioIntegrationService(
                _context,
                new Mock<ILogger<InventarioIntegrationService>>().Object,
                new Mock<ICurrentUserService>().Object,
                Mock.Of<ITelemetryService>());
        }

        [Fact]
        public async Task DescontarStockInmediato_LlamadoDosVeces_NoDuplicaDescuentoNiMovimiento()
        {
            // Arrange
            var producto = new ProductoServicio
            {
                Id = 1,
                EmisorId = 1,
                Codigo = "PROD-001",
                Descripcion = "Producto",
                CatUnidadMedidaId = 59,
                CatTipoItemId = 1,
                PrecioVenta = 10m,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };
            var bodega = new Bodega { Id = 1, Nombre = "Bodega", Codigo = "BOD-001", Activo = true, FechaCreacion = DateTime.UtcNow };
            var stock = new StockBodega
            {
                ProductoId = 1,
                BodegaId = 1,
                CantidadDisponible = 100m,
                CantidadReservada = 0m,
                CostoPromedio = 5m
            };
            var factura = new FacturaElectronica
            {
                Id = 1,
                EmisorId = 1,
                SucursalId = 1,
                Version = 1,
                CatTipoDocumentoId = 1,
                NumeroControl = "DTE-01-00000001-00000001",
                CodigoGeneracion = Guid.NewGuid().ToString().ToUpper(),
                CatModeloFacturacionId = 1,
                CatTipoTransmisionId = 1,
                FechaEmision = DateTime.UtcNow.Date,
                HoraEmision = DateTime.UtcNow.TimeOfDay,
                TotalPagar = 100m,
                EstadoHacienda = "GENERADO",
                FechaCreacion = DateTime.UtcNow,
                Activo = true
            };
            var detalle = new FacturaElectronicaDetalle
            {
                FacturaId = 1,
                NumeroItem = 1,
                CatTipoItemId = 1,
                Cantidad = 10m,
                CatUnidadMedidaId = 59,
                Descripcion = "Producto de prueba",
                PrecioUnitario = 10m,
                VentaGravada = 100m,
                IvaItem = 0m,
                ProductoId = 1,
                BodegaId = 1,
                CostoUnitario = 5m,
                FechaCreacion = DateTime.UtcNow,
                Activo = true
            };

            _context.ProductosServicios.Add(producto);
            _context.Bodegas.Add(bodega);
            _context.StocksBodega.Add(stock);
            _context.Facturas.Add(factura);
            factura.Detalles.Add(detalle);
            await _context.SaveChangesAsync();

            // Act: dos descuentos inmediatos para la misma factura.
            await _service.DescontarStockInmediatoAsync(factura.Id);
            await _service.DescontarStockInmediatoAsync(factura.Id);

            // Assert: el stock solo bajó una vez y solo hay un movimiento.
            var stockActualizado = await _context.StocksBodega.FindAsync(stock.Id);
            Assert.NotNull(stockActualizado);
            Assert.Equal(90m, stockActualizado!.CantidadDisponible);

            var movimientos = await _context.MovimientosInventario
                .Where(m => m.TipoDocumento == "FACTURA_EMITIDA" && m.DocumentoId == factura.Id)
                .ToListAsync();
            Assert.Single(movimientos);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
