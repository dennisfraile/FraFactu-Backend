using FraFactu.Tests.Fixtures;
using FraFactu.Domain.Entities;
using FraFactu.Infrastructure.Persistence;
using FluentAssertions;

namespace FraFactu.Tests.IntegrationTests.Services;

/// <summary>
/// Tests de integración para CompraService
/// Prueba registro de compras externas
/// </summary>
public class CompraServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly TestDataBuilder _dataBuilder;

    public CompraServiceTests()
    {
        _context = TestDatabaseHelper.CreateInMemoryContext();
        TestDatabaseHelper.SeedTestData(_context);
        _dataBuilder = new TestDataBuilder();
    }

    [Fact]
    public async Task CrearCompra_ConDatosValidos_DebeCrearCompra()
    {
        // Arrange
        var proveedor = new Proveedor
        {
            NIT = _dataBuilder.GenerarNit(),
            Nombre = _dataBuilder.GenerarNombreCompania(),
            Email = _dataBuilder.GenerarEmail()
        };
        _context.Proveedores.Add(proveedor);
        await _context.SaveChangesAsync();

        var compra = new CompraExterna
        {
            ProveedorId = proveedor.Id,
            NumeroFactura = $"COMP-{DateTime.UtcNow:yyyyMMddHHmmss}",
            FechaEmision = DateTime.UtcNow.Date,
            Total = 1000.00m,
            Estado = "BORRADOR"
        };

        // Act
        _context.ComprasExternas.Add(compra);
        await _context.SaveChangesAsync();

        // Assert
        compra.Id.Should().BeGreaterThan(0);
        compra.Total.Should().Be(1000.00m);
    }

    [Fact]
    public async Task BuscarCompraPorNumeroFactura_DebeRetornarCompra()
    {
        // Arrange
        var proveedor = new Proveedor
        {
            NIT = _dataBuilder.GenerarNit(),
            Nombre = "Proveedor Test"
        };
        _context.Proveedores.Add(proveedor);
        await _context.SaveChangesAsync();

        var numeroFactura = $"COMP-TEST-{Guid.NewGuid()}";
        var compra = new CompraExterna
        {
            ProveedorId = proveedor.Id,
            NumeroFactura = numeroFactura,
            FechaEmision = DateTime.UtcNow.Date,
            Total = 500.00m,
            Estado = "BORRADOR"
        };
        _context.ComprasExternas.Add(compra);
        await _context.SaveChangesAsync();

        // Act
        var resultado = _context.ComprasExternas
            .FirstOrDefault(c => c.NumeroFactura == numeroFactura);

        // Assert
        resultado.Should().NotBeNull();
        resultado!.Total.Should().Be(500.00m);
    }

    [Fact]
    public async Task ConfirmarCompra_DebeActualizarEstado()
    {
        // Arrange
        var proveedor = new Proveedor
        {
            NIT = _dataBuilder.GenerarNit(),
            Nombre = "Proveedor Test"
        };
        _context.Proveedores.Add(proveedor);
        await _context.SaveChangesAsync();

        var compra = new CompraExterna
        {
            ProveedorId = proveedor.Id,
            NumeroFactura = $"COMP-{DateTime.UtcNow:yyyyMMddHHmmss}",
            FechaEmision = DateTime.UtcNow.Date,
            Total = 750.00m,
            Estado = "BORRADOR"
        };
        _context.ComprasExternas.Add(compra);
        await _context.SaveChangesAsync();

        // Act
        compra.Estado = "CONFIRMADA";
        compra.FechaConfirmacion = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Assert
        var compraActualizada = await _context.ComprasExternas.FindAsync(compra.Id);
        compraActualizada.Should().NotBeNull();
        compraActualizada!.Estado.Should().Be("CONFIRMADA");
        compraActualizada.FechaConfirmacion.Should().NotBeNull();
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
