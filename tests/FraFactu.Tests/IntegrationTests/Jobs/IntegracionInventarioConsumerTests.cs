using System.Text.Json;
using FraFactu.Application.Common.Settings;
using FraFactu.Application.DTOs.Integraciones;
using FraFactu.Application.Interfaces;
using FraFactu.Domain.Entities;
using FraFactu.Domain.Enums;
using FraFactu.Infrastructure.Jobs;
using FraFactu.Infrastructure.Persistence;
using FraFactu.Tests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace FraFactu.Tests.IntegrationTests.Jobs;

/// <summary>
/// F3 (Plan inventario desde DTE): cubre el pickup
/// <c>ProcesarSiguienteJobAsync</c> del <see cref="IntegracionInventarioConsumer"/>.
/// El bucle del BackgroundService se ejercita en runtime; aqui validamos
/// transiciones (ENCOLADO -&gt; EN_PROCESO -&gt; COMPLETADO / FALLIDO),
/// backoff exponencial, idempotencia (409 vista como exito) y limite
/// de intentos.
/// </summary>
public class IntegracionInventarioConsumerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ISmartInventoryClient> _client = new();
    private readonly Emisor _emisor;
    private readonly SmartInventorySettings _settings = new()
    {
        BaseUrl = "https://si.test",
        ApiKey = "test-key",
        PollingSeconds = 1,
        MaxIntentos = 3
    };

    public IntegracionInventarioConsumerTests()
    {
        _context = TestDatabaseHelper.CreateInMemoryContext();
        TestDatabaseHelper.SeedTestData(_context);
        _emisor = _context.Emisores.First();
        _client.SetupGet(c => c.EstaConfigurado).Returns(true);
    }

    private IntegracionInventarioPendiente SeedJob(
        EstadoIntegracionInventario estado = EstadoIntegracionInventario.ENCOLADO,
        int intentos = 0,
        DateTime? proximoIntento = null,
        DateTime? fechaCreacion = null,
        string? movimientoId = null)
    {
        var payload = new SmartInventoryMovimientoRequestDto
        {
            OrganizacionId = 1,
            MovimientoIdExterno = movimientoId ?? $"mov-{Guid.NewGuid()}",
            DocumentoOrigenId = 100,
            NumeroDocumento = "F-001",
            Items = new()
            {
                new SmartInventoryMovimientoItemDto
                {
                    CodigoProducto = "PROD-001",
                    NombreProducto = "Test",
                    BodegaNombre = "Bodega Principal",
                    Cantidad = 1,
                    CostoUnitario = 50,
                    TipoInventario = "Ventas"
                }
            }
        };

        var job = new IntegracionInventarioPendiente
        {
            EmisorId = _emisor.Id,
            TipoEvento = "MOVIMIENTO_ENTRADA",
            PayloadJson = JsonSerializer.Serialize(payload),
            Estado = estado,
            Intentos = intentos,
            FechaProximoIntento = proximoIntento,
            MovimientoIdExterno = payload.MovimientoIdExterno,
            FechaCreacion = fechaCreacion ?? DateTime.UtcNow
        };
        _context.IntegracionInventarioPendientes.Add(job);
        _context.SaveChanges();
        return job;
    }

    [Fact]
    public async Task ProcesarSiguienteJob_SinPendientes_NoLlamaCliente()
    {
        await IntegracionInventarioConsumer.ProcesarSiguienteJobAsync(
            _context, _client.Object, _settings, NullLogger.Instance, CancellationToken.None);

        _client.Verify(c => c.RegistrarEntradaAsync(
            It.IsAny<SmartInventoryMovimientoRequestDto>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcesarSiguienteJob_ExitoCliente_TransicionaACompletado()
    {
        var job = SeedJob();
        _client.Setup(c => c.RegistrarEntradaAsync(
            It.IsAny<SmartInventoryMovimientoRequestDto>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(SmartInventoryEnvioResultado.Exito());

        await IntegracionInventarioConsumer.ProcesarSiguienteJobAsync(
            _context, _client.Object, _settings, NullLogger.Instance, CancellationToken.None);

        var persistido = await _context.IntegracionInventarioPendientes.SingleAsync();
        persistido.Estado.Should().Be(EstadoIntegracionInventario.COMPLETADO);
        persistido.Intentos.Should().Be(1);
        persistido.FechaProcesado.Should().NotBeNull();
        persistido.UltimoError.Should().BeNull();
    }

    [Fact]
    public async Task ProcesarSiguienteJob_DuplicadoEnSmartInventory_TratadoComoExito()
    {
        // F3: 409 desde SmartInventory => movimiento ya procesado previamente;
        // el consumidor lo trata como COMPLETADO sin reintentar.
        var job = SeedJob();
        _client.Setup(c => c.RegistrarEntradaAsync(
            It.IsAny<SmartInventoryMovimientoRequestDto>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(SmartInventoryEnvioResultado.Duplicado("ya existe"));

        await IntegracionInventarioConsumer.ProcesarSiguienteJobAsync(
            _context, _client.Object, _settings, NullLogger.Instance, CancellationToken.None);

        var persistido = await _context.IntegracionInventarioPendientes.SingleAsync();
        persistido.Estado.Should().Be(EstadoIntegracionInventario.COMPLETADO);
        persistido.UltimoError.Should().Contain("Idempotente");
    }

    [Fact]
    public async Task ProcesarSiguienteJob_ErrorPermanente_MarcaFallidoSinReintento()
    {
        // 400/401/403 desde SmartInventory: no tiene sentido reintentar.
        var job = SeedJob();
        _client.Setup(c => c.RegistrarEntradaAsync(
            It.IsAny<SmartInventoryMovimientoRequestDto>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(SmartInventoryEnvioResultado.ErrorPermanente("HTTP 400: bodega no encontrada"));

        await IntegracionInventarioConsumer.ProcesarSiguienteJobAsync(
            _context, _client.Object, _settings, NullLogger.Instance, CancellationToken.None);

        var persistido = await _context.IntegracionInventarioPendientes.SingleAsync();
        persistido.Estado.Should().Be(EstadoIntegracionInventario.FALLIDO);
        persistido.FechaProcesado.Should().NotBeNull();
        persistido.FechaProximoIntento.Should().BeNull();  // sin reintento
        persistido.UltimoError.Should().Contain("bodega no encontrada");
    }

    [Fact]
    public async Task ProcesarSiguienteJob_ExcepcionTransitoria_ReencolaConBackoff()
    {
        var job = SeedJob();
        _client.Setup(c => c.RegistrarEntradaAsync(
            It.IsAny<SmartInventoryMovimientoRequestDto>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("503 transitorio"));

        var antes = DateTime.UtcNow;
        await IntegracionInventarioConsumer.ProcesarSiguienteJobAsync(
            _context, _client.Object, _settings, NullLogger.Instance, CancellationToken.None);

        var persistido = await _context.IntegracionInventarioPendientes.SingleAsync();
        persistido.Estado.Should().Be(EstadoIntegracionInventario.FALLIDO);
        persistido.Intentos.Should().Be(1);
        persistido.FechaProcesado.Should().BeNull();       // todavia hay reintento
        persistido.FechaProximoIntento.Should().NotBeNull();
        persistido.FechaProximoIntento!.Value.Should().BeAfter(antes);
        persistido.UltimoError.Should().Contain("503");
    }

    [Fact]
    public async Task ProcesarSiguienteJob_AlcanzaMaxIntentos_MarcaFallidoPermanente()
    {
        // MaxIntentos = 3. Cuando un job que ya tiene 2 intentos falla,
        // el contador sube a 3 == max y se marca FALLIDO sin reencolar.
        var job = SeedJob(estado: EstadoIntegracionInventario.FALLIDO, intentos: 2,
            proximoIntento: DateTime.UtcNow.AddMinutes(-1));
        _client.Setup(c => c.RegistrarEntradaAsync(
            It.IsAny<SmartInventoryMovimientoRequestDto>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("503"));

        await IntegracionInventarioConsumer.ProcesarSiguienteJobAsync(
            _context, _client.Object, _settings, NullLogger.Instance, CancellationToken.None);

        var persistido = await _context.IntegracionInventarioPendientes.SingleAsync();
        persistido.Estado.Should().Be(EstadoIntegracionInventario.FALLIDO);
        persistido.Intentos.Should().Be(3);
        persistido.FechaProcesado.Should().NotBeNull();
        persistido.FechaProximoIntento.Should().BeNull(); // permanente
    }

    [Fact]
    public async Task ProcesarSiguienteJob_JobConProximoIntentoFuturo_NoSeProcesa()
    {
        SeedJob(
            estado: EstadoIntegracionInventario.FALLIDO,
            intentos: 1,
            proximoIntento: DateTime.UtcNow.AddHours(1));
        _client.Setup(c => c.RegistrarEntradaAsync(
            It.IsAny<SmartInventoryMovimientoRequestDto>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(SmartInventoryEnvioResultado.Exito());

        await IntegracionInventarioConsumer.ProcesarSiguienteJobAsync(
            _context, _client.Object, _settings, NullLogger.Instance, CancellationToken.None);

        _client.Verify(c => c.RegistrarEntradaAsync(
            It.IsAny<SmartInventoryMovimientoRequestDto>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcesarSiguienteJob_VariosListos_TomaElMasAntiguo()
    {
        var viejo = SeedJob(fechaCreacion: DateTime.UtcNow.AddMinutes(-10), movimientoId: "mov-viejo");
        var nuevo = SeedJob(fechaCreacion: DateTime.UtcNow, movimientoId: "mov-nuevo");
        _client.Setup(c => c.RegistrarEntradaAsync(
            It.IsAny<SmartInventoryMovimientoRequestDto>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(SmartInventoryEnvioResultado.Exito());

        await IntegracionInventarioConsumer.ProcesarSiguienteJobAsync(
            _context, _client.Object, _settings, NullLogger.Instance, CancellationToken.None);

        _client.Verify(c => c.RegistrarEntradaAsync(
            It.Is<SmartInventoryMovimientoRequestDto>(p => p.MovimientoIdExterno == "mov-viejo"),
            It.IsAny<CancellationToken>()), Times.Once);

        var nuevoPersistido = await _context.IntegracionInventarioPendientes
            .SingleAsync(j => j.Id == nuevo.Id);
        nuevoPersistido.Estado.Should().Be(EstadoIntegracionInventario.ENCOLADO);
    }

    [Fact]
    public async Task ProcesarSiguienteJob_PayloadInvalido_MarcaFallidoPermanente()
    {
        var job = new IntegracionInventarioPendiente
        {
            EmisorId = _emisor.Id,
            TipoEvento = "MOVIMIENTO_ENTRADA",
            PayloadJson = "{ esto no es JSON valido",
            Estado = EstadoIntegracionInventario.ENCOLADO,
            MovimientoIdExterno = "mov-bad",
            FechaCreacion = DateTime.UtcNow
        };
        _context.IntegracionInventarioPendientes.Add(job);
        await _context.SaveChangesAsync();

        await IntegracionInventarioConsumer.ProcesarSiguienteJobAsync(
            _context, _client.Object, _settings, NullLogger.Instance, CancellationToken.None);

        var persistido = await _context.IntegracionInventarioPendientes.SingleAsync();
        persistido.Estado.Should().Be(EstadoIntegracionInventario.FALLIDO);
        persistido.UltimoError.Should().Contain("Payload invalido");
        persistido.FechaProximoIntento.Should().BeNull(); // permanente

        // Cliente nunca se llama porque el payload se rechaza antes.
        _client.Verify(c => c.RegistrarEntradaAsync(
            It.IsAny<SmartInventoryMovimientoRequestDto>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    public void Dispose() => _context.Dispose();
}
