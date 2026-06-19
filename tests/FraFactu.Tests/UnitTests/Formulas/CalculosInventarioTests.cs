using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using FraFactu.Application.Common.Interfaces;
using FraFactu.Infrastructure.Services;
using FraFactu.Infrastructure.Persistence;
using FraFactu.Domain.Entities;
using FraFactu.Application.Interfaces;

namespace FraFactu.Tests.UnitTests.Formulas
{
    /// <summary>
    /// Subclase de InventarioIntegrationService que reemplaza el acceso con raw SQL
    /// (FOR UPDATE) por una consulta LINQ compatible con InMemory.
    /// </summary>
    internal class TestableInventarioIntegrationService : InventarioIntegrationService
    {
        private readonly ApplicationDbContext _testContext;

        public TestableInventarioIntegrationService(ApplicationDbContext context, ILogger<InventarioIntegrationService> logger, ICurrentUserService currentUserService, ITelemetryService telemetry)
            : base(context, logger, currentUserService, telemetry)
        {
            _testContext = context;
        }

        protected override async Task<StockBodega?> ObtenerStockConBloqueoAsync(int productoId, int bodegaId)
        {
            return await _testContext.StocksBodega
                .Include(s => s.Producto)
                .Include(s => s.Bodega)
                .FirstOrDefaultAsync(s => s.ProductoId == productoId && s.BodegaId == bodegaId);
        }

        protected override Task BloquearFacturaAsync(int facturaId) => Task.CompletedTask;

        protected override Task BloquearInvalidacionAsync(int invalidacionId) => Task.CompletedTask;
    }

    /// <summary>
    /// Pruebas unitarias para cálculos de inventario
    /// Nota: Estas son pruebas de integración que usan una base de datos en memoria
    /// </summary>
    public class CalculosInventarioTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly InventarioIntegrationService _service;

        public CalculosInventarioTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            _context = new ApplicationDbContext(options);
            var mockLogger = new Mock<ILogger<InventarioIntegrationService>>();
            var mockCurrentUserService = new Mock<ICurrentUserService>();
            _service = new TestableInventarioIntegrationService(_context, mockLogger.Object, mockCurrentUserService.Object, Mock.Of<ITelemetryService>());
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        #region Validación de Stock Disponible

        [Fact]
        public async Task ValidarStockDisponible_ConStockSuficiente_RetornaTrue()
        {
            // Arrange
            var producto = CreateProducto();
            var bodega = CreateBodega();
            var stock = CreateStock(producto.Id, bodega.Id, cantidadDisponible: 100);
            var factura = CreateFactura();
            var detalle = CreateDetalle(factura.Id, producto.Id, bodega.Id, cantidad: 10);

            _context.ProductosServicios.Add(producto);
            _context.Bodegas.Add(bodega);
            _context.StocksBodega.Add(stock);
            _context.Facturas.Add(factura);
            factura.Detalles.Add(detalle);
            await _context.SaveChangesAsync();

            // Act
            var resultado = await _service.ValidarStockDisponibleAsync(factura.Id);

            // Assert
            Assert.True(resultado);
        }

        [Fact]
        public async Task ValidarStockDisponible_ConStockInsuficiente_RetornaFalse()
        {
            // Arrange
            var producto = CreateProducto();
            var bodega = CreateBodega();
            var stock = CreateStock(producto.Id, bodega.Id, cantidadDisponible: 5);
            var factura = CreateFactura();
            var detalle = CreateDetalle(factura.Id, producto.Id, bodega.Id, cantidad: 10);

            _context.ProductosServicios.Add(producto);
            _context.Bodegas.Add(bodega);
            _context.StocksBodega.Add(stock);
            _context.Facturas.Add(factura);
            factura.Detalles.Add(detalle);
            await _context.SaveChangesAsync();

            // Act
            var resultado = await _service.ValidarStockDisponibleAsync(factura.Id);

            // Assert
            Assert.False(resultado);
        }

        [Fact]
        public async Task ValidarStockDisponible_SinStock_RetornaFalse()
        {
            // Arrange
            var producto = CreateProducto();
            var bodega = CreateBodega();
            var factura = CreateFactura();
            var detalle = CreateDetalle(factura.Id, producto.Id, bodega.Id, cantidad: 10);

            _context.ProductosServicios.Add(producto);
            _context.Bodegas.Add(bodega);
            _context.Facturas.Add(factura);
            factura.Detalles.Add(detalle);
            await _context.SaveChangesAsync();

            // Act
            var resultado = await _service.ValidarStockDisponibleAsync(factura.Id);

            // Assert
            Assert.False(resultado);
        }

        [Fact]
        public async Task ValidarStockDisponible_ConMultiplesDetalles_ValidaTodos()
        {
            // Arrange
            var producto1 = CreateProducto(1);
            var producto2 = CreateProducto(2);
            var bodega = CreateBodega();
            var stock1 = CreateStock(producto1.Id, bodega.Id, cantidadDisponible: 100);
            var stock2 = CreateStock(producto2.Id, bodega.Id, cantidadDisponible: 50);
            var factura = CreateFactura();
            var detalle1 = CreateDetalle(factura.Id, producto1.Id, bodega.Id, cantidad: 10);
            var detalle2 = CreateDetalle(factura.Id, producto2.Id, bodega.Id, cantidad: 5);

            _context.ProductosServicios.Add(producto1);
            _context.ProductosServicios.Add(producto2);
            _context.Bodegas.Add(bodega);
            _context.StocksBodega.Add(stock1);
            _context.StocksBodega.Add(stock2);
            _context.Facturas.Add(factura);
            factura.Detalles.Add(detalle1);
            factura.Detalles.Add(detalle2);
            await _context.SaveChangesAsync();

            // Act
            var resultado = await _service.ValidarStockDisponibleAsync(factura.Id);

            // Assert
            Assert.True(resultado);
        }

        [Fact]
        public async Task ValidarStockDisponible_ConUnDetalleInsuficiente_RetornaFalse()
        {
            // Arrange
            var producto1 = CreateProducto(1);
            var producto2 = CreateProducto(2);
            var bodega = CreateBodega();
            var stock1 = CreateStock(producto1.Id, bodega.Id, cantidadDisponible: 100);
            var stock2 = CreateStock(producto2.Id, bodega.Id, cantidadDisponible: 3); // Insuficiente
            var factura = CreateFactura();
            var detalle1 = CreateDetalle(factura.Id, producto1.Id, bodega.Id, cantidad: 10);
            var detalle2 = CreateDetalle(factura.Id, producto2.Id, bodega.Id, cantidad: 5);

            _context.ProductosServicios.Add(producto1);
            _context.ProductosServicios.Add(producto2);
            _context.Bodegas.Add(bodega);
            _context.StocksBodega.Add(stock1);
            _context.StocksBodega.Add(stock2);
            _context.Facturas.Add(factura);
            factura.Detalles.Add(detalle1);
            factura.Detalles.Add(detalle2);
            await _context.SaveChangesAsync();

            // Act
            var resultado = await _service.ValidarStockDisponibleAsync(factura.Id);

            // Assert
            Assert.False(resultado);
        }

        #endregion

        #region Reserva de Stock

        [Fact]
        public async Task ReservarStock_ConStockSuficiente_ActualizaCantidades()
        {
            // Arrange
            var producto = CreateProducto();
            var bodega = CreateBodega();
            var stock = CreateStock(producto.Id, bodega.Id, cantidadDisponible: 100, cantidadReservada: 0);
            var factura = CreateFactura();
            var detalle = CreateDetalle(factura.Id, producto.Id, bodega.Id, cantidad: 10);

            _context.ProductosServicios.Add(producto);
            _context.Bodegas.Add(bodega);
            _context.StocksBodega.Add(stock);
            _context.Facturas.Add(factura);
            factura.Detalles.Add(detalle);
            await _context.SaveChangesAsync();

            // Act
            await _service.ReservarStockAsync(factura.Id);

            // Assert
            var stockActualizado = await _context.StocksBodega.FindAsync(stock.Id);
            Assert.NotNull(stockActualizado);
            Assert.Equal(90m, stockActualizado.CantidadDisponible); // 100 - 10
            Assert.Equal(10m, stockActualizado.CantidadReservada);  // 0 + 10
        }

        [Fact]
        public async Task ReservarStock_ConStockInsuficiente_LanzaExcepcion()
        {
            // Arrange
            var producto = CreateProducto();
            var bodega = CreateBodega();
            var stock = CreateStock(producto.Id, bodega.Id, cantidadDisponible: 5);
            var factura = CreateFactura();
            var detalle = CreateDetalle(factura.Id, producto.Id, bodega.Id, cantidad: 10);

            _context.ProductosServicios.Add(producto);
            _context.Bodegas.Add(bodega);
            _context.StocksBodega.Add(stock);
            _context.Facturas.Add(factura);
            factura.Detalles.Add(detalle);
            await _context.SaveChangesAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.ReservarStockAsync(factura.Id));

            Assert.Contains("Stock insuficiente", exception.Message);
        }

        #endregion

        #region Confirmación de Venta

        [Fact]
        public async Task ConfirmarVenta_ConReservaExistente_CreaMovimiento()
        {
            // Arrange
            var producto = CreateProducto();
            var bodega = CreateBodega();
            var stock = CreateStock(producto.Id, bodega.Id,
                cantidadTotal: 100,
                cantidadDisponible: 90,
                cantidadReservada: 10);
            var factura = CreateFactura();
            var detalle = CreateDetalle(factura.Id, producto.Id, bodega.Id, cantidad: 10);

            _context.ProductosServicios.Add(producto);
            _context.Bodegas.Add(bodega);
            _context.StocksBodega.Add(stock);
            _context.Facturas.Add(factura);
            factura.Detalles.Add(detalle);
            await _context.SaveChangesAsync();

            // Act
            await _service.ConfirmarVentaAsync(factura.Id);

            // Assert
            var stockActualizado = await _context.StocksBodega.FindAsync(stock.Id);
            Assert.NotNull(stockActualizado);
            Assert.Equal(0m, stockActualizado.CantidadReservada); // 10 - 10

            var movimiento = await _context.MovimientosInventario
                .FirstOrDefaultAsync(m => m.DocumentoId == factura.Id);
            Assert.NotNull(movimiento);
            Assert.Equal("SALIDA", movimiento.TipoMovimiento);
            Assert.Equal(-10m, movimiento.Cantidad);
        }

        #endregion

        #region Liberación de Reservas

        [Fact]
        public async Task LiberarReservas_ConReservaExistente_DevuelveStock()
        {
            // Arrange
            var producto = CreateProducto();
            var bodega = CreateBodega();
            var stock = CreateStock(producto.Id, bodega.Id,
                cantidadDisponible: 90,
                cantidadReservada: 10);
            var factura = CreateFactura();
            var detalle = CreateDetalle(factura.Id, producto.Id, bodega.Id, cantidad: 10);

            _context.ProductosServicios.Add(producto);
            _context.Bodegas.Add(bodega);
            _context.StocksBodega.Add(stock);
            _context.Facturas.Add(factura);
            factura.Detalles.Add(detalle);
            await _context.SaveChangesAsync();

            // Act
            await _service.LiberarReservasAsync(factura.Id);

            // Assert
            var stockActualizado = await _context.StocksBodega.FindAsync(stock.Id);
            Assert.NotNull(stockActualizado);
            Assert.Equal(100m, stockActualizado.CantidadDisponible); // 90 + 10
            Assert.Equal(0m, stockActualizado.CantidadReservada);    // 10 - 10
        }

        #endregion

        #region Métodos Helper

        private ProductoServicio CreateProducto(int id = 1)
        {
            return new ProductoServicio
            {
                Id = id,
                EmisorId = 1,
                Codigo = $"PROD-{id:000}",
                Descripcion = $"Producto {id}",
                TipoImpuesto = Domain.Enums.TipoImpuesto.Gravado,
                CatUnidadMedidaId = 59,
                CatTipoItemId = 1,
                PrecioVenta = 10.00m, // Cambiar de PrecioUnitario a PrecioVenta
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };
        }

        private Bodega CreateBodega(int id = 1)
        {
            return new Bodega
            {
                Id = id,
                Nombre = $"Bodega Principal {id}",
                Codigo = $"BOD-{id:000}",
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };
        }

        private StockBodega CreateStock(int productoId, int bodegaId,
            decimal cantidadTotal = 100,
            decimal cantidadDisponible = 100,
            decimal cantidadReservada = 0)
        {
            return new StockBodega
            {
                ProductoId = productoId,
                BodegaId = bodegaId,
                CantidadDisponible = cantidadDisponible,
                CantidadReservada = cantidadReservada,
                CostoPromedio = 5.00m
            };
        }

        private FacturaElectronica CreateFactura(int id = 1)
        {
            return new FacturaElectronica
            {
                Id = id,
                EmisorId = 1,
                SucursalId = 1,
                Version = 1,
                // Ambiente se obtiene del emisor asociado, ya no se almacena en la factura
                CatTipoDocumentoId = 1,
                NumeroControl = $"DTE-01-00000001-{id:00000000}",
                CodigoGeneracion = Guid.NewGuid().ToString().ToUpper(),
                CatModeloFacturacionId = 1,
                CatTipoTransmisionId = 1,
                FechaEmision = DateTime.UtcNow.Date,
                HoraEmision = DateTime.UtcNow.TimeOfDay,
                TotalPagar = 100.00m,
                EstadoHacienda = "GENERADO",
                FechaCreacion = DateTime.UtcNow,
                Activo = true
            };
        }

        private FacturaElectronicaDetalle CreateDetalle(int facturaId, int? productoId, int? bodegaId, decimal cantidad)
        {
            return new FacturaElectronicaDetalle
            {
                FacturaId = facturaId,
                NumeroItem = 1,
                CatTipoItemId = 1,
                Cantidad = cantidad,
                CatUnidadMedidaId = 59,
                Descripcion = "Producto de prueba",
                PrecioUnitario = 10.00m,
                VentaGravada = 10.00m * cantidad,
                IvaItem = 0m,
                ProductoId = productoId,
                BodegaId = bodegaId,
                CostoUnitario = 5.00m,
                FechaCreacion = DateTime.UtcNow,
                Activo = true
            };
        }

        #endregion
    }
}
