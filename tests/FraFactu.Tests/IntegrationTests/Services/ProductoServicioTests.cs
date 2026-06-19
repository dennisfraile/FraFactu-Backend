using FraFactu.Tests.Fixtures;
using FraFactu.Domain.Entities;
using FraFactu.Infrastructure.Persistence;
using FluentAssertions;

namespace FraFactu.Tests.IntegrationTests.Services;

/// <summary>
/// Tests de integración para ProductoServicio (antes ProductoService)
/// Prueba CRUD de productos/servicios
/// </summary>
public class ProductoServicioTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly TestDataBuilder _dataBuilder;

    public ProductoServicioTests()
    {
        _context = TestDatabaseHelper.CreateInMemoryContext();
        TestDatabaseHelper.SeedTestData(_context);
        _dataBuilder = new TestDataBuilder();
    }

    [Fact]
    public async Task CrearProductoServicio_ConDatosValidos_DebeCrearRegistro()
    {
        // Arrange
        var emisor = await ObtenerEmisorPrueba();
        var producto = new ProductoServicio
        {
            Codigo = _dataBuilder.GenerarCodigoProducto(),
            Nombre = _dataBuilder.GenerarNombreProducto(),
            Descripcion = "Producto de prueba",
            PrecioVenta = 150.00m,
            PrecioCosto = 100.00m,
            CategoriaId = 1,
            CatUnidadMedidaId = 1,
            CatTipoItemId = 1, // Bien
            TipoImpuesto = Domain.Enums.TipoImpuesto.Gravado,
            EmisorId = emisor.Id
        };

        // Act
        _context.ProductosServicios.Add(producto);
        await _context.SaveChangesAsync();

        // Assert
        producto.Id.Should().BeGreaterThan(0);
        producto.Codigo.Should().NotBeNullOrEmpty();
        // producto.Margen removed as property does not exist directly
        // Verification of manual calculation if needed:
        var margen = (producto.PrecioVenta - (producto.PrecioCosto ?? 0)) / (producto.PrecioCosto ?? 1) * 100;
        margen.Should().BeApproximately(50.0m, 0.01m);
    }

    [Fact]
    public async Task ActualizarPrecioProducto_DebePersistir()
    {
        // Arrange
        var emisor = await ObtenerEmisorPrueba();
        var producto = new ProductoServicio
        {
            Codigo = _dataBuilder.GenerarCodigoProducto(),
            Nombre = _dataBuilder.GenerarNombreProducto(),
            PrecioVenta = 100.00m,
            PrecioCosto = 80.00m,
            CategoriaId = 1,
            CatUnidadMedidaId = 1,
            CatTipoItemId = 1,
            EmisorId = emisor.Id
        };
        _context.ProductosServicios.Add(producto);
        await _context.SaveChangesAsync();

        // Act
        producto.PrecioVenta = 120.00m;
        await _context.SaveChangesAsync();

        // Assert
        var productoActualizado = await _context.ProductosServicios.FindAsync(producto.Id);
        productoActualizado.Should().NotBeNull();
        productoActualizado!.PrecioVenta.Should().Be(120.00m);
    }

    [Fact]
    public async Task BuscarProductoPorCodigo_DebeRetornarProducto()
    {
        // Arrange
        var emisor = await ObtenerEmisorPrueba();
        var codigo = _dataBuilder.GenerarCodigoProducto();
        var producto = new ProductoServicio
        {
            Codigo = codigo,
            Nombre = "Producto Test",
            PrecioVenta = 100m,
            PrecioCosto = 80m,
            CategoriaId = 1,
            CatUnidadMedidaId = 1,
            CatTipoItemId = 1,
            EmisorId = emisor.Id
        };
        _context.ProductosServicios.Add(producto);
        await _context.SaveChangesAsync();

        // Act
        var resultado = _context.ProductosServicios.FirstOrDefault(p => p.Codigo == codigo);

        // Assert
        resultado.Should().NotBeNull();
        resultado!.Codigo.Should().Be(codigo);
    }

    [Fact]
    public async Task CrearProducto_DefaultTipoInventarioEsVentas()
    {
        // F2: el catalogo existente queda en Ventas al migrar; los productos
        // nuevos creados sin especificar deben tomar el mismo default.
        var emisor = await ObtenerEmisorPrueba();
        var producto = new ProductoServicio
        {
            Codigo = _dataBuilder.GenerarCodigoProducto(),
            Nombre = "Sin clasificar",
            PrecioVenta = 50m,
            CatUnidadMedidaId = 1,
            CatTipoItemId = 1,
            EmisorId = emisor.Id
        };

        _context.ProductosServicios.Add(producto);
        await _context.SaveChangesAsync();

        var leido = await _context.ProductosServicios.FindAsync(producto.Id);
        leido!.TipoInventario.Should().Be(Domain.Enums.TipoInventario.Ventas);
    }

    [Fact]
    public async Task CrearProducto_MobiliarioEquipo_PersisteCamposDeActivoFijo()
    {
        // F2: cuando el usuario crea un activo fijo, todos los campos opcionales
        // (fecha adquisicion, vida util, valor actual/residual) deben persistir
        // tal cual. Smartix no devalua: la responsabilidad de actualizar
        // ValorActual con el tiempo es de SmartInventory cuando este activo.
        var emisor = await ObtenerEmisorPrueba();
        var producto = new ProductoServicio
        {
            Codigo = _dataBuilder.GenerarCodigoProducto(),
            Nombre = "Escritorio gerencial",
            PrecioVenta = 0m,
            PrecioCosto = 850m,
            CatUnidadMedidaId = 1,
            CatTipoItemId = 1,
            EmisorId = emisor.Id,
            TipoInventario = Domain.Enums.TipoInventario.MobiliarioEquipo,
            FechaAdquisicion = new DateTime(2026, 5, 21, 0, 0, 0, DateTimeKind.Utc),
            AniosVidaUtil = 10,
            ValorActual = 850m,
            ValorResidual = 100m
        };

        _context.ProductosServicios.Add(producto);
        await _context.SaveChangesAsync();

        var leido = await _context.ProductosServicios.FindAsync(producto.Id);
        leido!.TipoInventario.Should().Be(Domain.Enums.TipoInventario.MobiliarioEquipo);
        leido.FechaAdquisicion.Should().Be(new DateTime(2026, 5, 21, 0, 0, 0, DateTimeKind.Utc));
        leido.AniosVidaUtil.Should().Be(10);
        leido.ValorActual.Should().Be(850m);
        leido.ValorResidual.Should().Be(100m);
    }

    [Fact]
    public async Task ActualizarTipoInventarioYValorActual_DebePersistir()
    {
        // F2: el usuario puede reclasificar un producto (raro) o, mas comun,
        // actualizar manualmente ValorActual si SmartInventory no esta activo.
        var emisor = await ObtenerEmisorPrueba();
        var producto = new ProductoServicio
        {
            Codigo = _dataBuilder.GenerarCodigoProducto(),
            Nombre = "Laptop oficina",
            PrecioVenta = 0m,
            PrecioCosto = 1200m,
            CatUnidadMedidaId = 1,
            CatTipoItemId = 1,
            EmisorId = emisor.Id,
            TipoInventario = Domain.Enums.TipoInventario.MobiliarioEquipo,
            ValorActual = 1200m
        };
        _context.ProductosServicios.Add(producto);
        await _context.SaveChangesAsync();

        producto.ValorActual = 1080m; // ajuste manual del usuario
        await _context.SaveChangesAsync();

        var leido = await _context.ProductosServicios.FindAsync(producto.Id);
        leido!.ValorActual.Should().Be(1080m);
        leido.TipoInventario.Should().Be(Domain.Enums.TipoInventario.MobiliarioEquipo);
    }

    private async Task<Emisor> ObtenerEmisorPrueba()
    {
        return await _context.Emisores.FindAsync(1)
            ?? throw new Exception("Emisor de prueba no encontrado");
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
