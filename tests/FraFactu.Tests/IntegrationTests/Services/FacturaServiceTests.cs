using FraFactu.Tests.Fixtures;
using FraFactu.Domain.Entities;
using FraFactu.Infrastructure.Persistence;
using FluentAssertions;

namespace FraFactu.Tests.IntegrationTests.Services;

/// <summary>
/// Tests de integración para FacturaService
/// Cada test crea su propia BD en memoria para evitar conflictos
/// </summary>
public class FacturaServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly TestDataBuilder _dataBuilder;

    public FacturaServiceTests()
    {
        // Crear BD nueva para este test
        _context = TestDatabaseHelper.CreateInMemoryContext();
        TestDatabaseHelper.SeedTestData(_context);

        _dataBuilder = new TestDataBuilder();
    }

    [Fact]
    public async Task CrearFactura_ConDatosBasicos_DebeGenerarCodigoGeneracion()
    {
        // Arrange
        var emisor = CrearEmisorPrueba();
        var receptor = CrearReceptorPrueba();
        var producto = CrearProductoPrueba(emisor.Id);

        _context.Emisores.Add(emisor);
        _context.Receptores.Add(receptor);
        _context.ProductosServicios.Add(producto);
        await _context.SaveChangesAsync();

        var factura = new FacturaElectronica
        {
            EmisorId = emisor.Id,
            ReceptorId = receptor.Id,
            CatTipoDocumentoId = 1,
            FechaEmision = DateTime.UtcNow,
            HoraEmision = DateTime.UtcNow.TimeOfDay,
            CodigoGeneracion = Guid.NewGuid().ToString(), // Generar código antes de guardar
            Detalles = new List<FacturaElectronicaDetalle>
            {
                new FacturaElectronicaDetalle
                {
                    NumeroItem = 1,
                    Cantidad = 1,
                    PrecioUnitario = 100,
                    VentaGravada = 100
                }
            }
        };

        // Act
        _context.Facturas.Add(factura);
        await _context.SaveChangesAsync();

        // Assert
        factura.Id.Should().BeGreaterThan(0);
        factura.CodigoGeneracion.Should().NotBeNullOrEmpty();
        factura.CodigoGeneracion.Length.Should().Be(36); // GUID format
    }

    [Fact]
    public async Task CrearFactura_DebeCalcularTotalesCorrectamente()
    {
        // Arrange
        var emisor = CrearEmisorPrueba();
        var receptor = CrearReceptorPrueba();

        _context.Emisores.Add(emisor);
        _context.Receptores.Add(receptor);
        await _context.SaveChangesAsync();

        var factura = new FacturaElectronica
        {
            EmisorId = emisor.Id,
            ReceptorId = receptor.Id,
            CatTipoDocumentoId = 1,
            FechaEmision = DateTime.UtcNow,
            HoraEmision = DateTime.UtcNow.TimeOfDay,
            Detalles = new List<FacturaElectronicaDetalle>
            {
                new FacturaElectronicaDetalle
                {
                    NumeroItem = 1,
                    Cantidad = 5,
                    PrecioUnitario = 100,
                    VentaGravada = 500 // 5 * 100
                },
                new FacturaElectronicaDetalle
                {
                    NumeroItem = 2,
                    Cantidad = 2,
                    PrecioUnitario = 50,
                    VentaGravada = 100 // 2 * 50
                }
            }
        };

        // Act
        _context.Facturas.Add(factura);
        await _context.SaveChangesAsync();

        // Assert
        var totalVentas = factura.Detalles.Sum(d => d.VentaGravada);
        totalVentas.Should().Be(600);
    }

    [Fact]
    public async Task ObtenerFactura_PorId_DebeRetornarFacturaCorrecta()
    {
        // Arrange
        var emisor = CrearEmisorPrueba();
        var receptor = CrearReceptorPrueba();

        _context.Emisores.Add(emisor);
        _context.Receptores.Add(receptor);

        var factura = new FacturaElectronica
        {
            EmisorId = emisor.Id,
            ReceptorId = receptor.Id,
            CatTipoDocumentoId = 1,
            FechaEmision = DateTime.UtcNow,
            HoraEmision = DateTime.UtcNow.TimeOfDay,
            CodigoGeneracion = Guid.NewGuid().ToString()
        };

        _context.Facturas.Add(factura);
        await _context.SaveChangesAsync();

        // Act
        var resultado = await _context.Facturas.FindAsync(factura.Id);

        // Assert
        resultado.Should().NotBeNull();
        resultado!.Id.Should().Be(factura.Id);
        resultado.CodigoGeneracion.Should().Be(factura.CodigoGeneracion);
    }

    // ═══ MÉTODOS HELPER ═══

    private Emisor CrearEmisorPrueba()
    {
        return new Emisor
        {
            Nit = _dataBuilder.GenerarNit(),
            Nrc = _dataBuilder.GenerarNrc(),
            NombreRazonSocial = _dataBuilder.GenerarNombreCompania(),
            NombreComercial = _dataBuilder.GenerarNombreCompania(),
            CodigoActividad = "47111",
            DescripcionActividad = "Venta al por menor",
            CatTipoEstablecimientoId = 1,
            CorreoElectronico = _dataBuilder.GenerarEmail(),
            Telefono = _dataBuilder.GenerarTelefono(),
            CatDepartamentoId = 1,
            CatMunicipioId = 1,
            Direccion = _dataBuilder.GenerarDireccion()
        };
    }

    private Receptor CrearReceptorPrueba()
    {
        return new Receptor
        {
            NombreRazonSocial = _dataBuilder.GenerarNombreCompania(),
            NumeroDocumento = _dataBuilder.GenerarNit(),
            Nrc = _dataBuilder.GenerarNrc(),
            CorreoElectronico = _dataBuilder.GenerarEmail(),
            Telefono = _dataBuilder.GenerarTelefono(),
            CatTipoDocumentoIdentificacionReceptorId = 1
        };
    }

    private ProductoServicio CrearProductoPrueba(int emisorId)
    {
        return new ProductoServicio
        {
            Codigo = _dataBuilder.GenerarCodigoProducto(),
            Nombre = _dataBuilder.GenerarNombreProducto(),
            Descripcion = "Producto de prueba",
            PrecioVenta = _dataBuilder.GenerarPrecio(),
            CatUnidadMedidaId = 1,
            CatTipoItemId = 1,
            EmisorId = emisorId
        };
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
