using FraFactu.Tests.Fixtures;
using FraFactu.Domain.Entities;
using FraFactu.Infrastructure.Persistence;
using FluentAssertions;

namespace FraFactu.Tests.IntegrationTests.Services;

/// <summary>
/// Tests de integración para ReceptorService
/// Prueba gestión de receptores (clientes)
/// </summary>
public class ReceptorServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly TestDataBuilder _dataBuilder;

    public ReceptorServiceTests()
    {
        _context = TestDatabaseHelper.CreateInMemoryContext();
        TestDatabaseHelper.SeedTestData(_context);
        _dataBuilder = new TestDataBuilder();
    }

    [Fact]
    public async Task CrearReceptor_ConDatosValidos_DebeCrearReceptor()
    {
        // Arrange
        var receptor = new Receptor
        {
            NombreRazonSocial = _dataBuilder.GenerarNombreCompania(),
            NumeroDocumento = _dataBuilder.GenerarNit(),
            Nrc = _dataBuilder.GenerarNrc(),
            CorreoElectronico = _dataBuilder.GenerarEmail(),
            Telefono = _dataBuilder.GenerarTelefono(),
            CatTipoDocumentoIdentificacionReceptorId = 1
        };

        // Act
        _context.Receptores.Add(receptor);
        await _context.SaveChangesAsync();

        // Assert
        receptor.Id.Should().BeGreaterThan(0);
        receptor.NombreRazonSocial.Should().NotBeNullOrEmpty();
        receptor.NumeroDocumento.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task BuscarReceptorPorNit_DebeRetornarReceptor()
    {
        // Arrange
        var nit = _dataBuilder.GenerarNit();
        var receptor = new Receptor
        {
            NombreRazonSocial = "Empresa Test",
            NumeroDocumento = nit,
            Nrc = _dataBuilder.GenerarNrc(),
            CorreoElectronico = "test@empresa.com",
            CatTipoDocumentoIdentificacionReceptorId = 1
        };
        _context.Receptores.Add(receptor);
        await _context.SaveChangesAsync();

        // Act
        var resultado = _context.Receptores.FirstOrDefault(r => r.NumeroDocumento == nit);

        // Assert
        resultado.Should().NotBeNull();
        resultado!.NumeroDocumento.Should().Be(nit);
    }

    [Fact]
    public async Task ActualizarDatosReceptor_DebeActualizarCorrectamente()
    {
        // Arrange
        var receptor = new Receptor
        {
            NombreRazonSocial = "Empresa Original",
            NumeroDocumento = _dataBuilder.GenerarNit(),
            CorreoElectronico = "original@empresa.com",
            CatTipoDocumentoIdentificacionReceptorId = 1
        };
        _context.Receptores.Add(receptor);
        await _context.SaveChangesAsync();

        // Act
        receptor.CorreoElectronico = "nuevo@empresa.com";
        receptor.Telefono = "22223333";
        await _context.SaveChangesAsync();

        // Assert
        var receptorActualizado = await _context.Receptores.FindAsync(receptor.Id);
        receptorActualizado.Should().NotBeNull();
        receptorActualizado!.CorreoElectronico.Should().Be("nuevo@empresa.com");
        receptorActualizado.Telefono.Should().Be("22223333");
    }

    // ===================================================================
    // Receptor "Sin documento" — FC tipo 01 admite tipoDoc + numDoc null.
    // Migration: MakeReceptorTipoDocumentoYNumeroDocumentoNullable.
    // ===================================================================

    [Fact]
    public async Task CrearReceptor_SinDocumento_DebePersistirAmbosCamposNulos()
    {
        var receptor = new Receptor
        {
            NombreRazonSocial = "Cliente Final Sin DUI",
            NumeroDocumento = null,
            CatTipoDocumentoIdentificacionReceptorId = null,
            CorreoElectronico = string.Empty,
            Telefono = string.Empty,
            EmisorId = 1
        };

        _context.Receptores.Add(receptor);
        await _context.SaveChangesAsync();

        var persistido = await _context.Receptores.FindAsync(receptor.Id);
        persistido.Should().NotBeNull();
        persistido!.NumeroDocumento.Should().BeNull();
        persistido.CatTipoDocumentoIdentificacionReceptorId.Should().BeNull();
        persistido.NombreRazonSocial.Should().Be("Cliente Final Sin DUI");
    }

    [Fact]
    public async Task CrearMultiplesReceptores_SinDocumento_MismoEmisor_NoDebeViolarUniqueIndex()
    {
        var receptor1 = new Receptor
        {
            NombreRazonSocial = "Cliente Final A",
            NumeroDocumento = null,
            CatTipoDocumentoIdentificacionReceptorId = null,
            CorreoElectronico = string.Empty,
            Telefono = string.Empty,
            EmisorId = 1,
            Activo = true
        };
        var receptor2 = new Receptor
        {
            NombreRazonSocial = "Cliente Final B",
            NumeroDocumento = null,
            CatTipoDocumentoIdentificacionReceptorId = null,
            CorreoElectronico = string.Empty,
            Telefono = string.Empty,
            EmisorId = 1,
            Activo = true
        };

        _context.Receptores.Add(receptor1);
        _context.Receptores.Add(receptor2);
        await _context.SaveChangesAsync();

        var count = _context.Receptores.Count(r => r.EmisorId == 1
            && r.NumeroDocumento == null && r.Activo);
        count.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task BuscarReceptores_ConSinDocumento_NoDebeCrashearAlFiltrarPorNumero()
    {
        var conDoc = new Receptor
        {
            NombreRazonSocial = "Cliente Con NIT",
            NumeroDocumento = "06141507881013",
            CatTipoDocumentoIdentificacionReceptorId = 1,
            CorreoElectronico = "test@test.com",
            Telefono = "22223333",
            EmisorId = 1,
            Activo = true
        };
        var sinDoc = new Receptor
        {
            NombreRazonSocial = "Cliente Final Anonimo",
            NumeroDocumento = null,
            CatTipoDocumentoIdentificacionReceptorId = null,
            CorreoElectronico = string.Empty,
            Telefono = string.Empty,
            EmisorId = 1,
            Activo = true
        };
        _context.Receptores.AddRange(conDoc, sinDoc);
        await _context.SaveChangesAsync();

        var term = "0614";
        var resultados = _context.Receptores
            .Where(r => r.EmisorId == 1 && r.Activo)
            .ToList()
            .Where(r => (r.NumeroDocumento != null && r.NumeroDocumento.Contains(term)))
            .ToList();

        resultados.Should().HaveCount(1);
        resultados[0].NombreRazonSocial.Should().Be("Cliente Con NIT");
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
