using FraFactu.Tests.Fixtures;
using FraFactu.Domain.Entities;
using FraFactu.Infrastructure.Persistence;
using FluentAssertions;

namespace FraFactu.Tests.IntegrationTests.Services;

/// <summary>
/// Tests de integración para LoteService
/// Prueba la funcionalidad de lotes (batch processing)
/// </summary>
public class LoteServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly TestDataBuilder _dataBuilder;

    public LoteServiceTests()
    {
        _context = TestDatabaseHelper.CreateInMemoryContext();
        TestDatabaseHelper.SeedTestData(_context);
        _dataBuilder = new TestDataBuilder();
    }

    [Fact]
    public async Task CrearLote_ConDatosValidos_DebeCrearLote()
    {
        // Arrange
        var emisor = await ObtenerEmisorPrueba();

        var lote = new Lote
        {
            EmisorId = emisor.Id,
            CodigoLote = Guid.NewGuid(),
            TotalDtes = 0,
            Estado = "Pendiente"
        };

        // Act
        _context.Lotes.Add(lote);
        await _context.SaveChangesAsync();

        // Assert
        lote.Id.Should().BeGreaterThan(0);
        lote.CodigoLote.Should().NotBeEmpty();
        lote.Estado.Should().Be("Pendiente");
    }

    [Fact]
    public async Task AgregarFacturaALote_DebeIncrementarContador()
    {
        // Arrange
        var emisor = await ObtenerEmisorPrueba();
        var receptor = await CrearReceptorPrueba();

        var lote = new Lote
        {
            EmisorId = emisor.Id,
            CodigoLote = Guid.NewGuid(),
            TotalDtes = 0,
            Estado = "Pendiente"
        };
        _context.Lotes.Add(lote);
        await _context.SaveChangesAsync();

        var factura = new FacturaElectronica
        {
            EmisorId = emisor.Id,
            ReceptorId = receptor.Id,
            LoteId = lote.Id,
            CatTipoDocumentoId = 1,
            FechaEmision = DateTime.UtcNow,
            HoraEmision = DateTime.UtcNow.TimeOfDay,
            CodigoGeneracion = Guid.NewGuid().ToString()
        };

        // Act
        _context.Facturas.Add(factura);
        lote.TotalDtes = 1;
        await _context.SaveChangesAsync();

        // Assert
        var loteActualizado = await _context.Lotes.FindAsync(lote.Id);
        loteActualizado.Should().NotBeNull();
        loteActualizado!.TotalDtes.Should().Be(1);
    }

    [Fact]
    public async Task ObtenerLotePorId_DebeRetornarLoteConFacturas()
    {
        // Arrange
        var emisor = await ObtenerEmisorPrueba();
        var receptor = await CrearReceptorPrueba();

        var lote = new Lote
        {
            EmisorId = emisor.Id,
            CodigoLote = Guid.NewGuid(),
            TotalDtes = 2,
            Estado = "Pendiente"
        };
        _context.Lotes.Add(lote);
        await _context.SaveChangesAsync();

        // Agregar 2 facturas al lote
        for (int i = 0; i < 2; i++)
        {
            var factura = new FacturaElectronica
            {
                EmisorId = emisor.Id,
                ReceptorId = receptor.Id,
                LoteId = lote.Id,
                CatTipoDocumentoId = 1,
                FechaEmision = DateTime.UtcNow,
                HoraEmision = DateTime.UtcNow.TimeOfDay,
                CodigoGeneracion = Guid.NewGuid().ToString()
            };
            _context.Facturas.Add(factura);
        }
        await _context.SaveChangesAsync();

        // Act
        var resultado = await _context.Lotes.FindAsync(lote.Id);

        // Assert
        resultado.Should().NotBeNull();
        resultado!.TotalDtes.Should().Be(2);
    }

    // ═══ MÉTODOS HELPER ═══

    private async Task<Emisor> ObtenerEmisorPrueba()
    {
        // El emisor ya está en el seed
        return await _context.Emisores.FindAsync(1)
            ?? throw new Exception("Emisor de prueba no encontrado");
    }

    private async Task<Receptor> CrearReceptorPrueba()
    {
        var receptor = new Receptor
        {
            NombreRazonSocial = _dataBuilder.GenerarNombreCompania(),
            NumeroDocumento = _dataBuilder.GenerarNit(),
            Nrc = _dataBuilder.GenerarNrc(),
            CorreoElectronico = _dataBuilder.GenerarEmail(),
            Telefono = _dataBuilder.GenerarTelefono(),
            CatTipoDocumentoIdentificacionReceptorId = 1
        };

        _context.Receptores.Add(receptor);
        await _context.SaveChangesAsync();

        return receptor;
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
