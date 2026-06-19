using System.Text.Json;
using AutoMapper;
using FraFactu.Application.Common.Settings;
using FraFactu.Application.DTOs.DtesRecibidos;
using FraFactu.Application.DTOs.Integraciones;
using FraFactu.Application.Interfaces;
using FraFactu.Domain.Entities;
using FraFactu.Domain.Enums;
using FraFactu.Infrastructure.Jobs;
using FraFactu.Infrastructure.Persistence;
using FraFactu.Infrastructure.Services;
using FraFactu.Tests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace FraFactu.Tests.IntegrationTests;

/// <summary>
/// F5 (Plan inventario desde DTE): tests transversales que ejercen el
/// camino completo del plan combinando F1+F2+F3+F4. Cada test arma un DTE,
/// lo mapea con el wizard, confirma la compra y verifica el efecto
/// extremo a extremo (stock interno, outbox, snapshot, divergencias).
/// Sustituye N tests por feature por escenarios que demuestran que las
/// piezas funcionan juntas.
/// </summary>
public class PlanInventarioE2ETests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly DteRecibidoService _dteService;
    private readonly CompraExternaService _compraService;
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Emisor _emisor;
    private readonly Sucursal _sucursal;
    private readonly Bodega _bodega;
    private readonly Categoria _categoria;

    public PlanInventarioE2ETests()
    {
        _context = TestDatabaseHelper.CreateInMemoryContext();
        TestDatabaseHelper.SeedTestData(_context);
        _emisor = _context.Emisores.First();
        _sucursal = _context.Sucursales.First(s => s.EmisorId == _emisor.Id);
        _sucursal.HubSucursalId = 55;  // sucursal vinculada a SmartHub
        _context.SaveChanges();
        _bodega = _context.Bodegas.First(b => b.SucursalId == _sucursal.Id);
        _categoria = _context.Categorias.First();
        _currentUser.Setup(c => c.GetUsuarioId()).Returns((int?)null);

        var parser = new DteParserService(Mock.Of<ILogger<DteParserService>>());
        var ingesta = new DteIngestaService(_context, parser, Mock.Of<ILogger<DteIngestaService>>());
        _dteService = new DteRecibidoService(
            _context, Mock.Of<IMapper>(), ingesta, parser, Mock.Of<ILogger<DteRecibidoService>>());
        _compraService = new CompraExternaService(_context, _currentUser.Object);
    }

    private DteRecibido SeedDte(decimal subtotal = 200m, decimal iva = 26m, decimal total = 226m)
    {
        var dte = new DteRecibido
        {
            EmisorId = _emisor.Id,
            CodigoGeneracion = $"DTE-{Guid.NewGuid()}",
            TipoDte = "03",
            NumeroControl = "DTE-03-0001-000000000000001",
            FechaEmision = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            EmisorNit = "06141804941035",
            EmisorNombre = "PROVEEDOR SA",
            JsonDte = "{}",
            SubTotal = subtotal,
            IVA = iva,
            Total = total,
            Estado = EstadoDteRecibido.PENDIENTE,
            FuenteRecepcion = FuenteRecepcionDte.CORREO,
            FechaCreacion = DateTime.UtcNow
        };
        _context.DtesRecibidos.Add(dte);
        _context.SaveChanges();
        return dte;
    }

    private ProductoServicio SeedProducto(string codigo, TipoInventario tipo = TipoInventario.Ventas)
    {
        var unidad = _context.CatUnidadesMedida.First();
        var tipoItem = _context.CatTiposItem.First(t => t.Codigo == "1");
        var prod = new ProductoServicio
        {
            Codigo = codigo,
            Nombre = $"Producto {codigo}",
            CatTipoItemId = tipoItem.Id,
            CatUnidadMedidaId = unidad.Id,
            EmisorId = _emisor.Id,
            PrecioVenta = 10m,
            PrecioCosto = 50m,
            AccesoTodasSucursales = true,
            TipoInventario = tipo,
            FechaCreacion = DateTime.UtcNow
        };
        _context.ProductosServicios.Add(prod);
        _context.SaveChanges();
        return prod;
    }

    private MapearDteCompraDto WizardProductoExistente(ProductoServicio prod, decimal cantidad, decimal costoUnit)
    {
        return new MapearDteCompraDto
        {
            SucursalId = _sucursal.Id,
            Items = new()
            {
                new()
                {
                    DescripcionDte = $"Linea {prod.Codigo}",
                    MontoDte = cantidad * costoUnit * 1.13m,
                    Accion = AccionMapeoItem.ProductoExistente,
                    ProductoId = prod.Id,
                    Cantidad = cantidad,
                    BodegaId = _bodega.Id,
                    CostoUnitario = costoUnit
                }
            }
        };
    }

    [Fact]
    public async Task DteCompletoSinSmartInventory_ActualizaStockInternoYNoEncolaOutbox()
    {
        var dte = SeedDte();
        var producto = SeedProducto("PROD-001");
        // Default: TieneSmartInventoryActiva = false.

        var mapeo = await _dteService.MapearYCrearCompraAsync(
            dte.Id, WizardProductoExistente(producto, 4m, 50m), _emisor.Id);

        await _compraService.ConfirmarAsync(mapeo.CompraId, _emisor.Id);

        // Stock interno actualizado.
        var stock = await _context.StocksBodega
            .SingleAsync(s => s.ProductoId == producto.Id && s.BodegaId == _bodega.Id);
        stock.CantidadDisponible.Should().Be(4m);

        // Movimiento registrado.
        var movs = await _context.MovimientosInventario
            .Where(m => m.ProductoId == producto.Id).ToListAsync();
        movs.Should().ContainSingle();
        movs[0].TipoDocumento.Should().Be("COMPRA_EXTERNA");

        // Sin SmartInventory: outbox vacio.
        (await _context.IntegracionInventarioPendientes.AnyAsync()).Should().BeFalse();

        // DTE quedo VINCULADO + Compra CONFIRMADA.
        var dteRefrescado = await _context.DtesRecibidos.SingleAsync(d => d.Id == dte.Id);
        dteRefrescado.Estado.Should().Be(EstadoDteRecibido.VINCULADO);
        var compraRefrescada = await _context.ComprasExternas.SingleAsync(c => c.Id == mapeo.CompraId);
        compraRefrescada.Estado.Should().Be("CONFIRMADA");
    }

    [Fact]
    public async Task DteCompletoConSmartInventory_ActualizaStockYReplica()
    {
        _emisor.TieneSmartInventoryActiva = true;
        _emisor.HubId = 42;
        await _context.SaveChangesAsync();

        var dte = SeedDte();
        var producto = SeedProducto("PROD-001");

        var mapeo = await _dteService.MapearYCrearCompraAsync(
            dte.Id, WizardProductoExistente(producto, 4m, 50m), _emisor.Id);
        await _compraService.ConfirmarAsync(mapeo.CompraId, _emisor.Id);

        // Stock interno: si se actualiza tambien (Smartix es la fuente de verdad).
        var stock = await _context.StocksBodega.SingleAsync();
        stock.CantidadDisponible.Should().Be(4m);

        // Outbox encolado con payload correcto.
        var job = await _context.IntegracionInventarioPendientes.SingleAsync();
        job.Estado.Should().Be(EstadoIntegracionInventario.ENCOLADO);
        job.MovimientoIdExterno.Should().Be($"compra-{mapeo.CompraId}");
        var payload = JsonSerializer.Deserialize<SmartInventoryMovimientoRequestDto>(job.PayloadJson)!;
        payload.OrganizacionId.Should().Be(42);
        payload.Items.Should().ContainSingle();
        // Identidad estable cross-app (Bug #1): se envia el id del producto en Smartix,
        // para que SmartInventory matchee por id y no por codigo (que colisiona).
        payload.Items[0].ProductoIdExterno.Should().Be(producto.Id);
        payload.Items[0].SucursalIdExterno.Should().Be(55);  // HubSucursalId de la sucursal
        payload.Items[0].CodigoProducto.Should().Be("PROD-001");
        payload.Items[0].Cantidad.Should().Be(4m);
        payload.Items[0].TipoInventario.Should().Be("Ventas");

        // Consumer procesa el job: simulamos que SmartInventory responde 200.
        var client = new Mock<ISmartInventoryClient>();
        client.SetupGet(c => c.EstaConfigurado).Returns(true);
        SmartInventoryMovimientoRequestDto? recibido = null;
        client.Setup(c => c.RegistrarEntradaAsync(It.IsAny<SmartInventoryMovimientoRequestDto>(), It.IsAny<CancellationToken>()))
            .Callback<SmartInventoryMovimientoRequestDto, CancellationToken>((r, _) => recibido = r)
            .ReturnsAsync(SmartInventoryEnvioResultado.Exito());

        var settings = new SmartInventorySettings { BaseUrl = "x", ApiKey = "y", MaxIntentos = 3 };
        await IntegracionInventarioConsumer.ProcesarSiguienteJobAsync(
            _context, client.Object, settings, NullLogger.Instance, CancellationToken.None);

        // El cliente HTTP recibio el payload tal cual lo armo CompraExternaService.
        recibido.Should().NotBeNull();
        recibido!.MovimientoIdExterno.Should().Be($"compra-{mapeo.CompraId}");
        recibido.Items[0].CodigoProducto.Should().Be("PROD-001");

        // Job marcado COMPLETADO.
        var jobFinal = await _context.IntegracionInventarioPendientes.SingleAsync();
        jobFinal.Estado.Should().Be(EstadoIntegracionInventario.COMPLETADO);
        jobFinal.FechaProcesado.Should().NotBeNull();
    }

    [Fact]
    public async Task DteConActivoFijo_CreaProductoContableYRegistraMovimientoSinStock()
    {
        _emisor.TieneSmartInventoryActiva = true;
        _emisor.HubId = 42;
        await _context.SaveChangesAsync();

        var dte = SeedDte(subtotal: 1200m, iva: 156m, total: 1356m);
        var unidad = _context.CatUnidadesMedida.First();
        var tipoItem = _context.CatTiposItem.First(t => t.Codigo == "1");

        var dto = new MapearDteCompraDto
        {
            SucursalId = _sucursal.Id,
            Items = new()
            {
                new()
                {
                    DescripcionDte = "Laptop Dell para administracion",
                    MontoDte = 1356m,
                    Accion = AccionMapeoItem.ProductoNuevo,
                    ProductoNuevo = new ProductoNuevoMapeoDto
                    {
                        Codigo = "ACT-001",
                        Nombre = "Laptop Dell",
                        CatTipoItemId = tipoItem.Id,
                        CatUnidadMedidaId = unidad.Id,
                        TipoInventario = (int)TipoInventario.MobiliarioEquipo,
                        AniosVidaUtil = 5,
                        ValorResidual = 100m
                    },
                    Cantidad = 1m,
                    BodegaId = _bodega.Id,
                    CostoUnitario = 1200m
                }
            }
        };

        var mapeo = await _dteService.MapearYCrearCompraAsync(dte.Id, dto, _emisor.Id);
        await _compraService.ConfirmarAsync(mapeo.CompraId, _emisor.Id);

        // Producto creado con campos contables.
        var producto = await _context.ProductosServicios.SingleAsync(p => p.Codigo == "ACT-001");
        producto.TipoInventario.Should().Be(TipoInventario.MobiliarioEquipo);
        producto.AniosVidaUtil.Should().Be(5);
        producto.ValorResidual.Should().Be(100m);
        producto.ValorActual.Should().Be(1200m);
        producto.FechaAdquisicion.Should().Be(dte.FechaEmision);

        // Movimiento si se registra (trazabilidad).
        var movs = await _context.MovimientosInventario
            .Where(m => m.ProductoId == producto.Id).ToListAsync();
        movs.Should().ContainSingle();

        // StockBodega NO se crea para activo fijo.
        (await _context.StocksBodega.AnyAsync(s => s.ProductoId == producto.Id))
            .Should().BeFalse();

        // Outbox: payload con campos de activo fijo.
        var job = await _context.IntegracionInventarioPendientes.SingleAsync();
        var payload = JsonSerializer.Deserialize<SmartInventoryMovimientoRequestDto>(job.PayloadJson)!;
        payload.Items[0].TipoInventario.Should().Be("MobiliarioEquipo");
        payload.Items[0].FechaAdquisicion.Should().Be(dte.FechaEmision);
        payload.Items[0].AniosVidaUtil.Should().Be(5);
        payload.Items[0].ValorActual.Should().Be(1200m);
        payload.Items[0].ValorResidual.Should().Be(100m);
    }

    [Fact]
    public async Task Reconciliacion_DetectaPersisteYResuelveDivergenciaEnCorridasSucesivas()
    {
        _emisor.TieneSmartInventoryActiva = true;
        _emisor.HubId = 42;
        await _context.SaveChangesAsync();

        var producto = SeedProducto("PROD-001");
        _context.StocksBodega.Add(new StockBodega
        {
            ProductoId = producto.Id, BodegaId = _bodega.Id, CantidadDisponible = 15m
        });
        await _context.SaveChangesAsync();

        var client = new Mock<ISmartInventoryClient>();
        client.SetupGet(c => c.EstaConfigurado).Returns(true);

        // --- Corrida 1: Smartix=15, SnapshotSI=10 => diff=5 DETECTADA
        ConfigurarSnapshot(client, ("PROD-001", 10));
        var ejec1 = Guid.NewGuid();
        var r1 = await ReconciliacionInventarioService.EjecutarParaEmisorAsync(
            _context, client.Object, _emisor, ejec1, NullLogger.Instance, CancellationToken.None);
        r1.DivergenciasNuevas.Should().Be(1);

        // --- Corrida 2: diff cambia => UPDATE in-place (no duplica)
        ConfigurarSnapshot(client, ("PROD-001", 8));
        var ejec2 = Guid.NewGuid();
        var r2 = await ReconciliacionInventarioService.EjecutarParaEmisorAsync(
            _context, client.Object, _emisor, ejec2, NullLogger.Instance, CancellationToken.None);
        r2.DivergenciasReaparecidas.Should().Be(1);
        r2.DivergenciasNuevas.Should().Be(0);
        (await _context.DivergenciasInventario.CountAsync()).Should().Be(1);

        // --- Corrida 3: stocks coinciden => RESUELTA
        ConfigurarSnapshot(client, ("PROD-001", 15));
        var ejec3 = Guid.NewGuid();
        var r3 = await ReconciliacionInventarioService.EjecutarParaEmisorAsync(
            _context, client.Object, _emisor, ejec3, NullLogger.Instance, CancellationToken.None);
        r3.DivergenciasResueltas.Should().Be(1);

        var div = await _context.DivergenciasInventario.SingleAsync();
        div.Estado.Should().Be(EstadoDivergenciaInventario.RESUELTA);
        div.FechaResolucion.Should().NotBeNull();
        div.EjecucionId.Should().Be(ejec3);
    }

    [Fact]
    public async Task OutboxPendiente_ReconciliacionDetectaQueSmartixTieneStockSinReplicar()
    {
        _emisor.TieneSmartInventoryActiva = true;
        _emisor.HubId = 42;
        await _context.SaveChangesAsync();

        // Compra confirmada: Smartix encola job, Smartix.StockBodega ya en 4.
        var dte = SeedDte();
        var producto = SeedProducto("PROD-001");
        var mapeo = await _dteService.MapearYCrearCompraAsync(
            dte.Id, WizardProductoExistente(producto, 4m, 50m), _emisor.Id);
        await _compraService.ConfirmarAsync(mapeo.CompraId, _emisor.Id);

        var outboxAntes = await _context.IntegracionInventarioPendientes.SingleAsync();
        outboxAntes.Estado.Should().Be(EstadoIntegracionInventario.ENCOLADO);

        // SmartInventory todavia no ha recibido el movimiento -> snapshot=0.
        // La reconciliacion debe detectar Smartix=4 vs SI=0 = diff=4.
        var client = new Mock<ISmartInventoryClient>();
        client.SetupGet(c => c.EstaConfigurado).Returns(true);
        ConfigurarSnapshot(client);  // snapshot vacio
        var resultado = await ReconciliacionInventarioService.EjecutarParaEmisorAsync(
            _context, client.Object, _emisor, Guid.NewGuid(),
            NullLogger.Instance, CancellationToken.None);

        resultado.DivergenciasNuevas.Should().Be(1);
        var div = await _context.DivergenciasInventario.SingleAsync();
        div.CodigoProducto.Should().Be("PROD-001");
        div.StockSmartix.Should().Be(4);
        div.StockSmartInventory.Should().Be(0);
        div.Diff.Should().Be(4);

        // El outbox sigue intacto: la reconciliacion no consume jobs ni los altera.
        var outboxDespues = await _context.IntegracionInventarioPendientes.SingleAsync();
        outboxDespues.Estado.Should().Be(EstadoIntegracionInventario.ENCOLADO);
        outboxDespues.Id.Should().Be(outboxAntes.Id);
    }

    private void ConfigurarSnapshot(
        Mock<ISmartInventoryClient> client,
        params (string codigo, int cantidad)[] items)
    {
        client.Setup(c => c.GetSnapshotAsync(
                It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmartInventorySnapshotResponseDto
            {
                OrganizacionId = _emisor.HubId ?? 0,
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

    public void Dispose() => _context.Dispose();
}
