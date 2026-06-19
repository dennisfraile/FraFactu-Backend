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
/// F4 (Plan inventario desde DTE): cubre <see cref="ReconciliacionInventarioService"/>.
/// Valida: deteccion de divergencia nueva, reaparicion sin duplicar fila,
/// resolucion cuando una corrida posterior ya no observa la divergencia,
/// exclusion de MobiliarioEquipo, scope por emisor y precondiciones.
/// </summary>
public class ReconciliacionInventarioServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ISmartInventoryClient> _client = new();
    private readonly Emisor _emisor;
    private readonly Bodega _bodega;

    public ReconciliacionInventarioServiceTests()
    {
        _context = TestDatabaseHelper.CreateInMemoryContext();
        TestDatabaseHelper.SeedTestData(_context);

        // Activa SmartInventory en el emisor para que el job lo considere.
        _emisor = _context.Emisores.First();
        _emisor.HubId = 42;
        _emisor.TieneSmartInventoryActiva = true;
        _bodega = _context.Bodegas.First();
        _context.SaveChanges();

        _client.SetupGet(c => c.EstaConfigurado).Returns(true);
    }

    private ProductoServicio SeedProducto(
        string codigo,
        TipoInventario tipo = TipoInventario.Ventas,
        bool activo = true)
    {
        var unidad = _context.CatUnidadesMedida.First();
        var tipoItem = _context.CatTiposItem.First();
        var producto = new ProductoServicio
        {
            Codigo = codigo,
            Nombre = $"Producto {codigo}",
            EmisorId = _emisor.Id,
            CatUnidadMedidaId = unidad.Id,
            CatTipoItemId = tipoItem.Id,
            TipoInventario = tipo,
            Activo = activo,
            PrecioVenta = 1m
        };
        _context.ProductosServicios.Add(producto);
        _context.SaveChanges();
        return producto;
    }

    private void SeedStock(int productoId, int cantidad)
    {
        _context.StocksBodega.Add(new StockBodega
        {
            ProductoId = productoId,
            BodegaId = _bodega.Id,
            CantidadDisponible = cantidad
        });
        _context.SaveChanges();
    }

    private void SetupSnapshot(params (string codigo, int cantidad)[] items)
    {
        _client.Setup(c => c.GetSnapshotAsync(
                It.IsAny<int>(),
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmartInventorySnapshotResponseDto
            {
                OrganizacionId = _emisor.HubId!.Value,
                ContinuationToken = null,
                TotalEnviado = items.Length,
                Items = items.Select(i => new SmartInventorySnapshotItemDto
                {
                    CodigoProducto = i.codigo,
                    NombreProducto = i.codigo,
                    NombreBodega = _bodega.Nombre,
                    Cantidad = i.cantidad,
                    TipoInventario = "Ventas"
                }).ToList()
            });
    }

    [Fact]
    public async Task EjecutarParaEmisor_StocksCoinciden_NoInsertaDivergencias()
    {
        var prod = SeedProducto("P-001");
        SeedStock(prod.Id, 10);
        SetupSnapshot(("P-001", 10));

        var resultado = await ReconciliacionInventarioService.EjecutarParaEmisorAsync(
            _context, _client.Object, _emisor, Guid.NewGuid(),
            NullLogger.Instance, CancellationToken.None);

        resultado.DivergenciasNuevas.Should().Be(0);
        resultado.DivergenciasReaparecidas.Should().Be(0);
        resultado.DivergenciasResueltas.Should().Be(0);
        (await _context.DivergenciasInventario.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task EjecutarParaEmisor_SmartixTieneMas_RegistraDivergenciaDetectada()
    {
        var prod = SeedProducto("P-001");
        SeedStock(prod.Id, 15);
        SetupSnapshot(("P-001", 10));
        var ejec = Guid.NewGuid();

        var resultado = await ReconciliacionInventarioService.EjecutarParaEmisorAsync(
            _context, _client.Object, _emisor, ejec, NullLogger.Instance, CancellationToken.None);

        resultado.DivergenciasNuevas.Should().Be(1);
        var div = await _context.DivergenciasInventario.SingleAsync();
        div.Estado.Should().Be(EstadoDivergenciaInventario.DETECTADA);
        div.CodigoProducto.Should().Be("P-001");
        div.NombreBodega.Should().Be(_bodega.Nombre);
        div.StockSmartix.Should().Be(15);
        div.StockSmartInventory.Should().Be(10);
        div.Diff.Should().Be(5);
        div.EjecucionId.Should().Be(ejec);
        div.FechaResolucion.Should().BeNull();
    }

    [Fact]
    public async Task EjecutarParaEmisor_SmartInventoryTieneMas_DiffNegativo()
    {
        var prod = SeedProducto("P-001");
        SeedStock(prod.Id, 3);
        SetupSnapshot(("P-001", 10));

        await ReconciliacionInventarioService.EjecutarParaEmisorAsync(
            _context, _client.Object, _emisor, Guid.NewGuid(),
            NullLogger.Instance, CancellationToken.None);

        var div = await _context.DivergenciasInventario.SingleAsync();
        div.Diff.Should().Be(-7);
    }

    [Fact]
    public async Task EjecutarParaEmisor_DivergenciaReaparece_ActualizaSinDuplicar()
    {
        var prod = SeedProducto("P-001");
        SeedStock(prod.Id, 15);

        // Primera corrida: diff=5
        SetupSnapshot(("P-001", 10));
        var ejec1 = Guid.NewGuid();
        await ReconciliacionInventarioService.EjecutarParaEmisorAsync(
            _context, _client.Object, _emisor, ejec1,
            NullLogger.Instance, CancellationToken.None);

        // Segunda corrida: misma divergencia con valor distinto.
        SetupSnapshot(("P-001", 8));
        var ejec2 = Guid.NewGuid();
        var resultado = await ReconciliacionInventarioService.EjecutarParaEmisorAsync(
            _context, _client.Object, _emisor, ejec2,
            NullLogger.Instance, CancellationToken.None);

        resultado.DivergenciasNuevas.Should().Be(0);
        resultado.DivergenciasReaparecidas.Should().Be(1);

        var div = await _context.DivergenciasInventario.SingleAsync();
        div.EjecucionId.Should().Be(ejec2);
        div.StockSmartInventory.Should().Be(8);
        div.Diff.Should().Be(7);
    }

    [Fact]
    public async Task EjecutarParaEmisor_DivergenciaYaNoAparece_MarcaResuelta()
    {
        var prod = SeedProducto("P-001");
        SeedStock(prod.Id, 15);

        // Corrida 1: divergencia.
        SetupSnapshot(("P-001", 10));
        await ReconciliacionInventarioService.EjecutarParaEmisorAsync(
            _context, _client.Object, _emisor, Guid.NewGuid(),
            NullLogger.Instance, CancellationToken.None);

        // Corrida 2: ambos lados coinciden. Misma divergencia previa debe RESUELTA.
        SetupSnapshot(("P-001", 15));
        var ejec2 = Guid.NewGuid();
        var resultado = await ReconciliacionInventarioService.EjecutarParaEmisorAsync(
            _context, _client.Object, _emisor, ejec2,
            NullLogger.Instance, CancellationToken.None);

        resultado.DivergenciasResueltas.Should().Be(1);
        resultado.DivergenciasAbiertasTotal.Should().Be(0);

        var div = await _context.DivergenciasInventario.SingleAsync();
        div.Estado.Should().Be(EstadoDivergenciaInventario.RESUELTA);
        div.FechaResolucion.Should().NotBeNull();
        div.EjecucionId.Should().Be(ejec2);
    }

    [Fact]
    public async Task EjecutarParaEmisor_ExcluyeMobiliarioEquipo()
    {
        // Mobiliario en Smartix con stock; SmartInventory no lo trae (porque
        // no expone activos fijos en el snapshot). Si no lo filtramos al
        // comparar, lo cuenta como divergencia falsa.
        var mobiliario = SeedProducto("ACT-001", tipo: TipoInventario.MobiliarioEquipo);
        SeedStock(mobiliario.Id, 1);
        SetupSnapshot();  // snapshot vacio

        var resultado = await ReconciliacionInventarioService.EjecutarParaEmisorAsync(
            _context, _client.Object, _emisor, Guid.NewGuid(),
            NullLogger.Instance, CancellationToken.None);

        resultado.DivergenciasNuevas.Should().Be(0);
        (await _context.DivergenciasInventario.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task EjecutarParaEmisor_ExcluyeProductosInactivos()
    {
        var inactivo = SeedProducto("P-001", activo: false);
        SeedStock(inactivo.Id, 99);
        SetupSnapshot();  // snapshot vacio

        var resultado = await ReconciliacionInventarioService.EjecutarParaEmisorAsync(
            _context, _client.Object, _emisor, Guid.NewGuid(),
            NullLogger.Instance, CancellationToken.None);

        resultado.DivergenciasNuevas.Should().Be(0);
        (await _context.DivergenciasInventario.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task EjecutarParaEmisor_ProductoSoloEnSmartInventory_Detecta()
    {
        // Smartix no tiene este producto: el snapshot SI lo reporta con
        // stock; debemos detectarlo como divergencia (Smartix=0, SI=N).
        SetupSnapshot(("FUERA-001", 7));

        var resultado = await ReconciliacionInventarioService.EjecutarParaEmisorAsync(
            _context, _client.Object, _emisor, Guid.NewGuid(),
            NullLogger.Instance, CancellationToken.None);

        resultado.DivergenciasNuevas.Should().Be(1);
        var div = await _context.DivergenciasInventario.SingleAsync();
        div.CodigoProducto.Should().Be("FUERA-001");
        div.StockSmartix.Should().Be(0);
        div.StockSmartInventory.Should().Be(7);
        div.Diff.Should().Be(-7);
    }

    [Fact]
    public async Task EjecutarParaEmisor_ClienteNoConfigurado_Lanza()
    {
        _client.SetupGet(c => c.EstaConfigurado).Returns(false);

        var act = async () => await ReconciliacionInventarioService.EjecutarParaEmisorAsync(
            _context, _client.Object, _emisor, Guid.NewGuid(),
            NullLogger.Instance, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*no esta configurado*");
    }

    [Fact]
    public async Task EjecutarParaEmisor_EmisorSinHubId_Lanza()
    {
        _emisor.HubId = null;
        _context.SaveChanges();

        var act = async () => await ReconciliacionInventarioService.EjecutarParaEmisorAsync(
            _context, _client.Object, _emisor, Guid.NewGuid(),
            NullLogger.Instance, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*HubId*");
    }

    [Fact]
    public async Task EjecutarParaEmisor_SnapshotPaginado_AcumulaTodaLaInfo()
    {
        var p1 = SeedProducto("P-001");
        var p2 = SeedProducto("P-002");
        SeedStock(p1.Id, 5);
        SeedStock(p2.Id, 5);

        // Pagina 1: P-001 con 4 (diff=1). Token continua.
        // Pagina 2: P-002 con 6 (diff=-1). Token null.
        _client.SetupSequence(c => c.GetSnapshotAsync(
                It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmartInventorySnapshotResponseDto
            {
                OrganizacionId = _emisor.HubId!.Value,
                ContinuationToken = 100,
                TotalEnviado = 1,
                Items = new()
                {
                    new SmartInventorySnapshotItemDto
                    {
                        CodigoProducto = "P-001",
                        NombreProducto = "P-001",
                        NombreBodega = _bodega.Nombre,
                        Cantidad = 4
                    }
                }
            })
            .ReturnsAsync(new SmartInventorySnapshotResponseDto
            {
                OrganizacionId = _emisor.HubId!.Value,
                ContinuationToken = null,
                TotalEnviado = 1,
                Items = new()
                {
                    new SmartInventorySnapshotItemDto
                    {
                        CodigoProducto = "P-002",
                        NombreProducto = "P-002",
                        NombreBodega = _bodega.Nombre,
                        Cantidad = 6
                    }
                }
            });

        var resultado = await ReconciliacionInventarioService.EjecutarParaEmisorAsync(
            _context, _client.Object, _emisor, Guid.NewGuid(),
            NullLogger.Instance, CancellationToken.None);

        resultado.DivergenciasNuevas.Should().Be(2);
        _client.Verify(c => c.GetSnapshotAsync(
            It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    public void Dispose() => _context.Dispose();
}
