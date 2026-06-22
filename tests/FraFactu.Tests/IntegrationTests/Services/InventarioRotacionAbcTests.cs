using FraFactu.Domain.Entities;
using FraFactu.Infrastructure.Persistence;
using FraFactu.Infrastructure.Services;
using FraFactu.Tests.Fixtures;
using FluentAssertions;
using Xunit;

namespace FraFactu.Tests.IntegrationTests.Services;

/// <summary>
/// F3.5 (G3): reporte de rotación ABC. Clasifica los productos por valor vendido
/// acumulado: A ≤ 80%, B ≤ 95%, C > 95%.
/// </summary>
public class InventarioRotacionAbcTests : IDisposable
{
    private const int EmisorId = 1;
    private const int BodegaId = 1; // sembrada por SeedTestData (sucursal 1 / emisor 1)

    private readonly ApplicationDbContext _context;
    private readonly InventarioReporteService _service;

    public InventarioRotacionAbcTests()
    {
        _context = TestDatabaseHelper.CreateInMemoryContext();
        TestDatabaseHelper.SeedTestData(_context);
        _service = new InventarioReporteService(_context);
    }

    private int SeedProducto(string codigo, decimal precioVenta)
    {
        var p = new ProductoServicio
        {
            Codigo = codigo,
            Nombre = $"Producto {codigo}",
            PrecioVenta = precioVenta,
            CatUnidadMedidaId = 1,
            CatTipoItemId = 1,
            EmisorId = EmisorId,
            Activo = true
        };
        _context.ProductosServicios.Add(p);
        _context.SaveChanges();
        return p.Id;
    }

    private void SeedSalida(int productoId, decimal cantidad, DateTime fecha)
    {
        _context.MovimientosInventario.Add(new MovimientoInventario
        {
            ProductoId = productoId,
            BodegaId = BodegaId,
            TipoMovimiento = "SALIDA",
            Cantidad = -Math.Abs(cantidad),
            CostoUnitario = 1m,
            TipoDocumento = "FACTURA_EMITIDA",
            FechaMovimiento = fecha
        });
        _context.SaveChanges();
    }

    [Fact]
    public async Task ObtenerRotacionAbc_ClasificaPorValorAcumulado()
    {
        var hoy = DateTime.UtcNow;
        // Valores de venta: P1=80, P2=15, P3=5 (total 100).
        var p1 = SeedProducto("ABC-1", 10m);
        var p2 = SeedProducto("ABC-2", 5m);
        var p3 = SeedProducto("ABC-3", 5m);
        SeedSalida(p1, 8, hoy);  // 8 * 10 = 80
        SeedSalida(p2, 3, hoy);  // 3 * 5  = 15
        SeedSalida(p3, 1, hoy);  // 1 * 5  = 5

        var result = await _service.ObtenerRotacionAbcAsync(
            EmisorId, hoy.AddDays(-30), hoy.AddDays(1));

        result.Should().HaveCount(3);
        // Ordenado por valor descendente.
        result[0].ProductoId.Should().Be(p1);
        result[1].ProductoId.Should().Be(p2);
        result[2].ProductoId.Should().Be(p3);
        // Clasificación ABC.
        result[0].Clasificacion.Should().Be("A");
        result[1].Clasificacion.Should().Be("B");
        result[2].Clasificacion.Should().Be("C");
        // Porcentaje acumulado.
        result[0].PorcentajeAcumulado.Should().Be(80m);
        result[1].PorcentajeAcumulado.Should().Be(95m);
        result[2].PorcentajeAcumulado.Should().Be(100m);
        // Valor vendido.
        result[0].ValorVendido.Should().Be(80m);
        result[0].UnidadesVendidas.Should().Be(8m);
    }

    [Fact]
    public async Task ObtenerRotacionAbc_ExcluyeMovimientosFueraDePeriodo()
    {
        var hoy = DateTime.UtcNow;
        var p1 = SeedProducto("ABC-1", 10m);
        SeedSalida(p1, 5, hoy.AddDays(-100)); // fuera del período

        var result = await _service.ObtenerRotacionAbcAsync(
            EmisorId, hoy.AddDays(-30), hoy.AddDays(1));

        result.Should().BeEmpty();
    }

    public void Dispose() => _context.Dispose();
}
