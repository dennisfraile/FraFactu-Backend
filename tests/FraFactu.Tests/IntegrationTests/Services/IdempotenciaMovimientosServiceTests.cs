using FraFactu.Infrastructure.Persistence;
using FraFactu.Infrastructure.Services;
using FraFactu.Tests.Fixtures;
using FluentAssertions;
using Xunit;

namespace FraFactu.Tests.IntegrationTests.Services;

/// <summary>
/// F3.4 (G4): infraestructura reutilizable de idempotencia para movimientos
/// externos. Una misma clave externa por emisor solo se registra una vez.
/// </summary>
public class IdempotenciaMovimientosServiceTests : IDisposable
{
    private const int EmisorId = 1;

    private readonly ApplicationDbContext _context;
    private readonly IdempotenciaMovimientosService _service;

    public IdempotenciaMovimientosServiceTests()
    {
        _context = TestDatabaseHelper.CreateInMemoryContext();
        _service = new IdempotenciaMovimientosService(_context);
    }

    [Fact]
    public async Task IntentarRegistrar_ClaveNueva_RegistraYDevuelveTrue()
    {
        var registrado = await _service.IntentarRegistrarAsync(
            EmisorId, "compra-123", "COMPRA_EXTERNA", itemsProcesados: 3);

        registrado.Should().BeTrue();
        _context.MovimientosExternosRegistrados
            .Should().ContainSingle(m => m.EmisorId == EmisorId && m.MovimientoIdExterno == "compra-123");
    }

    [Fact]
    public async Task IntentarRegistrar_ClaveDuplicada_NoDuplicaYDevuelveFalse()
    {
        await _service.IntentarRegistrarAsync(EmisorId, "compra-123", "COMPRA_EXTERNA", 3);

        var segundo = await _service.IntentarRegistrarAsync(EmisorId, "compra-123", "COMPRA_EXTERNA", 3);

        segundo.Should().BeFalse();
        _context.MovimientosExternosRegistrados
            .Count(m => m.EmisorId == EmisorId && m.MovimientoIdExterno == "compra-123")
            .Should().Be(1);
    }

    [Fact]
    public async Task IntentarRegistrar_MismaClaveDistintoEmisor_SonIndependientes()
    {
        await _service.IntentarRegistrarAsync(EmisorId, "compra-123", "COMPRA_EXTERNA", 3);

        var otroEmisor = await _service.IntentarRegistrarAsync(2, "compra-123", "COMPRA_EXTERNA", 3);

        otroEmisor.Should().BeTrue();
    }

    [Fact]
    public async Task YaRegistrado_RefleEjaElEstado()
    {
        (await _service.YaRegistradoAsync(EmisorId, "compra-9")).Should().BeFalse();

        await _service.IntentarRegistrarAsync(EmisorId, "compra-9", null, 1);

        (await _service.YaRegistradoAsync(EmisorId, "compra-9")).Should().BeTrue();
    }

    public void Dispose() => _context.Dispose();
}
