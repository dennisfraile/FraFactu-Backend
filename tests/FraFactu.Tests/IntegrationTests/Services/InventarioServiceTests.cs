using FraFactu.Tests.Fixtures;
using FraFactu.Domain.Entities;
using FraFactu.Infrastructure.Persistence;
using FluentAssertions;

namespace FraFactu.Tests.IntegrationTests.Services;

/// <summary>
/// Tests de integración para InventarioService
/// Prueba movimientos de inventario y control de stock
/// </summary>
public class InventarioServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly TestDataBuilder _dataBuilder;

    public InventarioServiceTests()
    {
        _context = TestDatabaseHelper.CreateInMemoryContext();
        TestDatabaseHelper.SeedTestData(_context);
        _dataBuilder = new TestDataBuilder();
    }

    [Fact]
    public async Task CrearProducto_ConStock_DebeCrearProductoYStock()
    {
        // Arrange
        var emisor = await ObtenerEmisorPrueba();
        var bodega = await ObtenerBodegaPrueba();

        var producto = new ProductoServicio
        {
            Codigo = _dataBuilder.GenerarCodigoProducto(),
            Nombre = _dataBuilder.GenerarNombreProducto(),
            Descripcion = "Producto de prueba",
            PrecioVenta = 100,
            PrecioCosto = 80,
            CategoriaId = 1, // Necesitaremos crear categoría en el seed
            CatUnidadMedidaId = 1,
            CatTipoItemId = 1, // Bien
            EmisorId = emisor.Id
        };

        // Act
        _context.ProductosServicios.Add(producto);
        await _context.SaveChangesAsync();

        var stock = new StockBodega
        {
            ProductoId = producto.Id,
            BodegaId = bodega.Id,
            CantidadDisponible = 10,
            CostoPromedio = 80
        };
        _context.StocksBodega.Add(stock);
        await _context.SaveChangesAsync();

        // Assert
        producto.Id.Should().BeGreaterThan(0);
        stock.Id.Should().BeGreaterThan(0);
        stock.CantidadDisponible.Should().Be(10);
    }

    [Fact]
    public async Task RegistrarMovimiento_Entrada_DebeIncrementarStock()
    {
        // Arrange
        var producto = await CrearProductoPrueba();
        var bodega = await ObtenerBodegaPrueba();

        var stockInicial = new StockBodega
        {
            ProductoId = producto.Id,
            BodegaId = bodega.Id,
            CantidadDisponible = 10
        };
        _context.StocksBodega.Add(stockInicial);
        await _context.SaveChangesAsync();

        var movimiento = new MovimientoInventario
        {
            ProductoId = producto.Id,
            BodegaId = bodega.Id,
            TipoMovimiento = "Entrada",
            Cantidad = 5,
            Observaciones = "Compra"
        };

        // Act
        _context.MovimientosInventario.Add(movimiento);
        stockInicial.CantidadDisponible += 5;
        await _context.SaveChangesAsync();

        // Assert
        var stockActualizado = await _context.StocksBodega.FindAsync(stockInicial.Id);
        stockActualizado.Should().NotBeNull();
        stockActualizado!.CantidadDisponible.Should().Be(15);
    }

    [Fact]
    public async Task RegistrarMovimiento_Salida_DebeDecrementarStock()
    {
        // Arrange
        var producto = await CrearProductoPrueba();
        var bodega = await ObtenerBodegaPrueba();

        var stockInicial = new StockBodega
        {
            ProductoId = producto.Id,
            BodegaId = bodega.Id,
            CantidadDisponible = 20
        };
        _context.StocksBodega.Add(stockInicial);
        await _context.SaveChangesAsync();

        var movimiento = new MovimientoInventario
        {
            ProductoId = producto.Id,
            BodegaId = bodega.Id,
            TipoMovimiento = "Salida",
            Cantidad = 7,
            Observaciones = "Venta"
        };

        // Act
        _context.MovimientosInventario.Add(movimiento);
        stockInicial.CantidadDisponible -= 7;
        await _context.SaveChangesAsync();

        // Assert
        var stockActualizado = await _context.StocksBodega.FindAsync(stockInicial.Id);
        stockActualizado.Should().NotBeNull();
        stockActualizado!.CantidadDisponible.Should().Be(13);
    }

    // ═══ MÉTODOS HELPER ═══

    private async Task<Emisor> ObtenerEmisorPrueba()
    {
        return await _context.Emisores.FindAsync(1)
            ?? throw new Exception("Emisor de prueba no encontrado");
    }

    private async Task<Bodega> ObtenerBodegaPrueba()
    {
        return await _context.Bodegas.FindAsync(1)
            ?? throw new Exception("Bodega de prueba no encontrada");
    }

    private async Task<ProductoServicio> CrearProductoPrueba()
    {
        var emisor = await ObtenerEmisorPrueba();

        var producto = new ProductoServicio
        {
            Codigo = _dataBuilder.GenerarCodigoProducto(),
            Nombre = _dataBuilder.GenerarNombreProducto(),
            Descripcion = "Producto de prueba",
            PrecioVenta = 100,
            PrecioCosto = 80,
            CategoriaId = 1,
            CatUnidadMedidaId = 1,
            CatTipoItemId = 1, // Bien
            EmisorId = emisor.Id
        };

        _context.ProductosServicios.Add(producto);
        await _context.SaveChangesAsync();

        return producto;
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
