using FraFactu.Application.DTOs.Compras;
using FraFactu.Application.Interfaces;
using FraFactu.Domain.Entities;
using FraFactu.Infrastructure.Persistence;
using FraFactu.Infrastructure.Services;
using FraFactu.Tests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace FraFactu.Tests.IntegrationTests.Services;

/// <summary>
/// F2 (Plan inventario desde DTE): cubre la regla "MobiliarioEquipo no toca
/// StockBodega" al confirmar y anular compras externas. Para los demas
/// tipos (Ventas / Insumos) el flujo existente sigue intacto y esta
/// cubierto por otras suites.
/// </summary>
public class CompraExternaServiceMobiliarioTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly CompraExternaService _service;
    private readonly Emisor _emisor;
    private readonly Sucursal _sucursal;
    private readonly Bodega _bodega;
    private readonly Mock<ICurrentUserService> _currentUser = new();

    public CompraExternaServiceMobiliarioTests()
    {
        _context = TestDatabaseHelper.CreateInMemoryContext();
        TestDatabaseHelper.SeedTestData(_context);
        _emisor = _context.Emisores.First();
        _sucursal = _context.Sucursales.First(s => s.EmisorId == _emisor.Id);
        _bodega = _context.Bodegas.First(b => b.SucursalId == _sucursal.Id);
        _currentUser.Setup(c => c.GetUsuarioId()).Returns((int?)null);

        _service = new CompraExternaService(_context, _currentUser.Object);
    }

    private ProductoServicio SeedProducto(
        Domain.Enums.TipoInventario tipo,
        string codigo)
    {
        var unidad = _context.CatUnidadesMedida.First();
        var tipoItem = _context.CatTiposItem.First(t => t.Codigo == "1");
        var p = new ProductoServicio
        {
            Codigo = codigo,
            Nombre = codigo,
            CatTipoItemId = tipoItem.Id,
            CatUnidadMedidaId = unidad.Id,
            EmisorId = _emisor.Id,
            PrecioVenta = 0m,
            PrecioCosto = 100m,
            AccesoTodasSucursales = true,
            TipoInventario = tipo,
            FechaCreacion = DateTime.UtcNow
        };
        _context.ProductosServicios.Add(p);
        _context.SaveChanges();
        return p;
    }

    private (CompraExterna compra, Proveedor proveedor) SeedCompraBorrador(
        params (ProductoServicio prod, decimal cantidad, decimal costo)[] items)
    {
        var proveedor = new Proveedor
        {
            NIT = "00000000000099",
            Nombre = "Proveedor activos",
            EmisorId = _emisor.Id,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };
        _context.Proveedores.Add(proveedor);
        _context.SaveChanges();

        var compra = new CompraExterna
        {
            ProveedorId = proveedor.Id,
            SucursalId = _sucursal.Id,
            NumeroFactura = "F-001",
            FechaEmision = new DateTime(2026, 5, 21, 0, 0, 0, DateTimeKind.Utc),
            FechaRegistro = DateTime.UtcNow,
            Subtotal = items.Sum(i => i.cantidad * i.costo),
            IVA = 0m,
            Total = items.Sum(i => i.cantidad * i.costo),
            Estado = "BORRADOR",
            Origen = "MANUAL",
            FechaCreacion = DateTime.UtcNow
        };
        _context.ComprasExternas.Add(compra);
        _context.SaveChanges();

        foreach (var (prod, cant, costo) in items)
        {
            _context.CompraExternaDetalles.Add(new CompraExternaDetalle
            {
                CompraExternaId = compra.Id,
                ProductoId = prod.Id,
                BodegaId = _bodega.Id,
                Cantidad = cant,
                CostoUnitario = costo,
                Subtotal = cant * costo,
                IVA = 0m,
                Total = cant * costo,
                EsParaInventario = true,
                FechaCreacion = DateTime.UtcNow
            });
        }
        _context.SaveChanges();
        return (compra, proveedor);
    }

    [Fact]
    public async Task Confirmar_ConMobiliarioEquipo_NoCreaStockBodega()
    {
        var activo = SeedProducto(Domain.Enums.TipoInventario.MobiliarioEquipo, "ACT-001");
        var (compra, _) = SeedCompraBorrador((activo, 1m, 850m));

        await _service.ConfirmarAsync(compra.Id, _emisor.Id);

        // No debe haber StockBodega creado para el activo fijo.
        (await _context.StocksBodega.AnyAsync(s => s.ProductoId == activo.Id))
            .Should().BeFalse();

        // Si debe existir el movimiento de ENTRADA con marca "Activo fijo".
        var movimiento = await _context.MovimientosInventario
            .SingleAsync(m => m.ProductoId == activo.Id);
        movimiento.TipoMovimiento.Should().Be("ENTRADA");
        movimiento.Cantidad.Should().Be(1m);
        movimiento.Observaciones.Should().Contain("Activo fijo");
        movimiento.SaldoAnterior.Should().BeNull();
        movimiento.NuevoSaldo.Should().BeNull();

        var compraActualizada = await _context.ComprasExternas.FindAsync(compra.Id);
        compraActualizada!.Estado.Should().Be("CONFIRMADA");
    }

    [Fact]
    public async Task Confirmar_MezclaVentasYMobiliario_SoloProductosVentasGeneranStock()
    {
        var venta = SeedProducto(Domain.Enums.TipoInventario.Ventas, "VEN-001");
        var activo = SeedProducto(Domain.Enums.TipoInventario.MobiliarioEquipo, "ACT-002");
        var (compra, _) = SeedCompraBorrador(
            (venta, 10m, 50m),
            (activo, 1m, 1200m));

        await _service.ConfirmarAsync(compra.Id, _emisor.Id);

        // Ventas: si genera StockBodega.
        var stockVenta = await _context.StocksBodega
            .SingleAsync(s => s.ProductoId == venta.Id);
        stockVenta.CantidadDisponible.Should().Be(10m);
        stockVenta.CostoPromedio.Should().Be(50m);

        // Activo fijo: no genera StockBodega.
        (await _context.StocksBodega.AnyAsync(s => s.ProductoId == activo.Id))
            .Should().BeFalse();

        // Ambos generan movimiento de entrada, con observaciones distintas.
        var movimientos = await _context.MovimientosInventario
            .Where(m => m.DocumentoId == compra.Id)
            .OrderBy(m => m.ProductoId)
            .ToListAsync();
        movimientos.Should().HaveCount(2);
        movimientos.Single(m => m.ProductoId == venta.Id).Observaciones.Should().Contain("Compra externa");
        movimientos.Single(m => m.ProductoId == activo.Id).Observaciones.Should().Contain("Activo fijo");
    }

    [Fact]
    public async Task Anular_CompraConMobiliario_SoloRegistraMovimientoSalidaSinTocarStock()
    {
        var activo = SeedProducto(Domain.Enums.TipoInventario.MobiliarioEquipo, "ACT-003");
        var (compra, _) = SeedCompraBorrador((activo, 2m, 500m));
        await _service.ConfirmarAsync(compra.Id, _emisor.Id);

        await _service.AnularAsync(compra.Id, new AnularCompraDto { Motivo = "Equipo devuelto al proveedor" }, _emisor.Id);

        // No hay StockBodega que reverter.
        (await _context.StocksBodega.AnyAsync(s => s.ProductoId == activo.Id))
            .Should().BeFalse();

        // Hay un movimiento SALIDA de anulacion con cantidad negativa.
        var movimientos = await _context.MovimientosInventario
            .Where(m => m.ProductoId == activo.Id)
            .OrderBy(m => m.Id)
            .ToListAsync();
        movimientos.Should().HaveCount(2);
        movimientos[0].TipoMovimiento.Should().Be("ENTRADA");
        movimientos[1].TipoMovimiento.Should().Be("SALIDA");
        movimientos[1].Cantidad.Should().Be(-2m);
        movimientos[1].TipoDocumento.Should().Be("ANULACION_COMPRA");
        movimientos[1].Observaciones.Should().ContainEquivalentOf("activo fijo");

        var compraActualizada = await _context.ComprasExternas.FindAsync(compra.Id);
        compraActualizada!.Estado.Should().Be("ANULADA");

        // El ProductoServicio NO se desactiva (decision producto 2026-05-21).
        var prod = await _context.ProductosServicios.FindAsync(activo.Id);
        prod!.Activo.Should().BeTrue();
    }

    [Fact]
    public async Task Anular_CompraMezcla_RevierteSoloVentasYRegistraMovimientoAmbos()
    {
        var venta = SeedProducto(Domain.Enums.TipoInventario.Ventas, "VEN-002");
        var activo = SeedProducto(Domain.Enums.TipoInventario.MobiliarioEquipo, "ACT-004");
        var (compra, _) = SeedCompraBorrador(
            (venta, 5m, 20m),
            (activo, 1m, 750m));
        await _service.ConfirmarAsync(compra.Id, _emisor.Id);

        await _service.AnularAsync(compra.Id, new AnularCompraDto { Motivo = "Mezcla anulada" }, _emisor.Id);

        // Ventas: stock vuelve a 0 (entro 5, salio 5).
        var stockVenta = await _context.StocksBodega.SingleAsync(s => s.ProductoId == venta.Id);
        stockVenta.CantidadDisponible.Should().Be(0m);

        // Activos: no hay stock.
        (await _context.StocksBodega.AnyAsync(s => s.ProductoId == activo.Id))
            .Should().BeFalse();

        // Movimientos: 4 en total (entrada y salida por cada item).
        var movimientos = await _context.MovimientosInventario
            .Where(m => m.DocumentoId == compra.Id || m.TipoDocumento == "ANULACION_COMPRA")
            .ToListAsync();
        movimientos.Should().HaveCount(4);
    }

    public void Dispose() => _context.Dispose();
}
