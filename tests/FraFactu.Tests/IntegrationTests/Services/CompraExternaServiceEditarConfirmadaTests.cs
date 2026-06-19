using System.Text.Json;
using FraFactu.Application.DTOs.Compras;
using FraFactu.Application.DTOs.Integraciones;
using FraFactu.Application.Interfaces;
using FraFactu.Domain.Entities;
using FraFactu.Domain.Enums;
using FraFactu.Infrastructure.Persistence;
using FraFactu.Infrastructure.Services;
using FraFactu.Tests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace FraFactu.Tests.IntegrationTests.Services;

/// <summary>
/// Edición de compras CONFIRMADAS (solo origen MANUAL). Verifica: edición de
/// solo-cabecera sin mover inventario, edición de items con revert+reapply,
/// bloqueo por stock negativo, rechazos de estado/origen/emisor, y corrección
/// best-effort a SmartInventory por delta neto.
/// </summary>
public class CompraExternaServiceEditarConfirmadaTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly CompraExternaService _service;
    private readonly Emisor _emisor;
    private readonly Sucursal _sucursal;
    private readonly Bodega _bodega;
    private readonly Proveedor _proveedor;
    private readonly Mock<ICurrentUserService> _currentUser = new();

    public CompraExternaServiceEditarConfirmadaTests()
    {
        _context = TestDatabaseHelper.CreateInMemoryContext();
        TestDatabaseHelper.SeedTestData(_context);
        _emisor = _context.Emisores.First();
        _emisor.HubId = 99;
        _sucursal = _context.Sucursales.First(s => s.EmisorId == _emisor.Id);
        _sucursal.HubSucursalId = 77;
        _bodega = _context.Bodegas.First(b => b.SucursalId == _sucursal.Id);
        _proveedor = new Proveedor
        {
            NIT = "00000000000099", Nombre = "Proveedor", EmisorId = _emisor.Id,
            Activo = true, FechaCreacion = DateTime.UtcNow
        };
        _context.Proveedores.Add(_proveedor);
        _currentUser.Setup(c => c.GetUsuarioId()).Returns((int?)null);
        _context.SaveChanges();
        _service = new CompraExternaService(_context, _currentUser.Object);
    }

    private ProductoServicio SeedProducto(TipoInventario tipo, string codigo)
    {
        var unidad = _context.CatUnidadesMedida.First();
        var tipoItem = _context.CatTiposItem.First(t => t.Codigo == "1");
        var p = new ProductoServicio
        {
            Codigo = codigo, Nombre = $"Producto {codigo}", CatTipoItemId = tipoItem.Id,
            CatUnidadMedidaId = unidad.Id, EmisorId = _emisor.Id, PrecioVenta = 10m,
            PrecioCosto = 50m, AccesoTodasSucursales = true, TipoInventario = tipo,
            FechaCreacion = DateTime.UtcNow
        };
        _context.ProductosServicios.Add(p);
        _context.SaveChanges();
        return p;
    }

    /// <summary>Crea una compra CONFIRMADA con stock ya aplicado (simula la confirmación).</summary>
    private CompraExterna SeedCompraConfirmada(
        params (ProductoServicio prod, decimal cantidad, decimal costo)[] items)
    {
        var compra = new CompraExterna
        {
            ProveedorId = _proveedor.Id, SucursalId = _sucursal.Id, NumeroFactura = "F-001",
            FechaEmision = new DateTime(2026, 2, 6, 0, 0, 0, DateTimeKind.Utc),
            FechaRegistro = DateTime.UtcNow,
            Subtotal = items.Sum(i => i.cantidad * i.costo), Total = items.Sum(i => i.cantidad * i.costo),
            Estado = "CONFIRMADA", Origen = "MANUAL",
            FechaConfirmacion = DateTime.UtcNow, FechaCreacion = DateTime.UtcNow
        };
        _context.ComprasExternas.Add(compra);
        _context.SaveChanges();

        foreach (var (prod, cant, costo) in items)
        {
            _context.CompraExternaDetalles.Add(new CompraExternaDetalle
            {
                CompraExternaId = compra.Id, ProductoId = prod.Id, BodegaId = _bodega.Id,
                Cantidad = cant, CostoUnitario = costo, Subtotal = cant * costo, Total = cant * costo,
                EsParaInventario = true, FechaCreacion = DateTime.UtcNow
            });
            var stock = _context.StocksBodega.FirstOrDefault(s => s.ProductoId == prod.Id && s.BodegaId == _bodega.Id);
            if (stock == null)
                _context.StocksBodega.Add(new StockBodega
                {
                    ProductoId = prod.Id, BodegaId = _bodega.Id, CantidadDisponible = cant,
                    CantidadReservada = 0, CostoPromedio = costo
                });
            else
                stock.CantidadDisponible += cant;
        }
        _context.SaveChanges();
        return compra;
    }

    private ActualizarCompraExternaDto DtoDesde(CompraExterna compra,
        DateTime? fecha = null,
        List<CrearCompraDetalleDto>? detalles = null)
    {
        var dets = detalles ?? compra.Detalles.Select(d => new CrearCompraDetalleDto
        {
            ProductoId = d.ProductoId, BodegaId = d.BodegaId, Cantidad = d.Cantidad,
            CostoUnitario = d.CostoUnitario, Subtotal = d.Subtotal, IVA = d.IVA, Total = d.Total,
            EsParaInventario = d.EsParaInventario
        }).ToList();
        return new ActualizarCompraExternaDto
        {
            ProveedorId = compra.ProveedorId, SucursalId = compra.SucursalId,
            NumeroFactura = compra.NumeroFactura,
            FechaEmision = fecha ?? compra.FechaEmision,
            Subtotal = dets.Sum(d => d.Subtotal), IVA = dets.Sum(d => d.IVA), Total = dets.Sum(d => d.Total),
            Observaciones = compra.Observaciones, Detalles = dets, Gastos = new()
        };
    }

    [Fact]
    public async Task SoloCabecera_CambiaFecha_NoGeneraMovimientos()
    {
        var prod = SeedProducto(TipoInventario.Ventas, "VEN-001");
        var compra = SeedCompraConfirmada((prod, 10m, 50m));
        var nuevaFecha = new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc);
        var dto = DtoDesde(compra, fecha: nuevaFecha);

        var result = await _service.EditarConfirmadaAsync(compra.Id, dto, _emisor.Id);

        result.FechaEmision.Should().Be(nuevaFecha);
        result.Estado.Should().Be("CONFIRMADA");
        (await _context.MovimientosInventario.AnyAsync()).Should().BeFalse();
        var stock = await _context.StocksBodega.SingleAsync(s => s.ProductoId == prod.Id);
        stock.CantidadDisponible.Should().Be(10m);
    }

    [Fact]
    public async Task ItemsCambian_ReduceCantidad_RevierteYReaplicaStock()
    {
        var prod = SeedProducto(TipoInventario.Ventas, "VEN-002");
        var compra = SeedCompraConfirmada((prod, 10m, 50m));
        await _context.Entry(compra).Collection(c => c.Detalles).LoadAsync();
        var dto = DtoDesde(compra, detalles: new()
        {
            new CrearCompraDetalleDto
            {
                ProductoId = prod.Id, BodegaId = _bodega.Id, Cantidad = 8m, CostoUnitario = 50m,
                Subtotal = 400m, IVA = 52m, Total = 452m, EsParaInventario = true
            }
        });

        await _service.EditarConfirmadaAsync(compra.Id, dto, _emisor.Id);

        var stock = await _context.StocksBodega.SingleAsync(s => s.ProductoId == prod.Id);
        stock.CantidadDisponible.Should().Be(8m);
        var movs = await _context.MovimientosInventario.Where(m => m.TipoDocumento == "EDICION_COMPRA").ToListAsync();
        movs.Should().HaveCount(2);
        movs.Should().Contain(m => m.TipoMovimiento == "SALIDA" && m.Cantidad == -10m);
        movs.Should().Contain(m => m.TipoMovimiento == "ENTRADA" && m.Cantidad == 8m);
    }

    [Fact]
    public async Task ItemsCambian_StockYaConsumido_BloqueaYNoEscribe()
    {
        var prod = SeedProducto(TipoInventario.Ventas, "VEN-003");
        var compra = SeedCompraConfirmada((prod, 10m, 50m));
        var stock = _context.StocksBodega.Single(s => s.ProductoId == prod.Id);
        stock.CantidadDisponible = 3m;
        await _context.SaveChangesAsync();
        await _context.Entry(compra).Collection(c => c.Detalles).LoadAsync();
        var dto = DtoDesde(compra, detalles: new()
        {
            new CrearCompraDetalleDto
            {
                ProductoId = prod.Id, BodegaId = _bodega.Id, Cantidad = 2m, CostoUnitario = 50m,
                Subtotal = 100m, IVA = 13m, Total = 113m, EsParaInventario = true
            }
        });

        var act = async () => await _service.EditarConfirmadaAsync(compra.Id, dto, _emisor.Id);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*stock negativo*");
        (await _context.StocksBodega.SingleAsync(s => s.ProductoId == prod.Id)).CantidadDisponible.Should().Be(3m);
        (await _context.MovimientosInventario.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task NoConfirmada_Rechaza()
    {
        var prod = SeedProducto(TipoInventario.Ventas, "VEN-004");
        var compra = SeedCompraConfirmada((prod, 1m, 10m));
        compra.Estado = "BORRADOR";
        await _context.SaveChangesAsync();
        await _context.Entry(compra).Collection(c => c.Detalles).LoadAsync();
        var dto = DtoDesde(compra);

        var act = async () => await _service.EditarConfirmadaAsync(compra.Id, dto, _emisor.Id);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*CONFIRMADA*");
    }

    [Fact]
    public async Task OrigenDte_Rechaza()
    {
        var prod = SeedProducto(TipoInventario.Ventas, "VEN-005");
        var compra = SeedCompraConfirmada((prod, 1m, 10m));
        compra.Origen = "DTE";
        await _context.SaveChangesAsync();
        await _context.Entry(compra).Collection(c => c.Detalles).LoadAsync();
        var dto = DtoDesde(compra);

        var act = async () => await _service.EditarConfirmadaAsync(compra.Id, dto, _emisor.Id);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*DTE*");
    }

    [Fact]
    public async Task OtroEmisor_Rechaza()
    {
        var prod = SeedProducto(TipoInventario.Ventas, "VEN-006");
        var compra = SeedCompraConfirmada((prod, 1m, 10m));
        await _context.Entry(compra).Collection(c => c.Detalles).LoadAsync();
        var dto = DtoDesde(compra);

        var act = async () => await _service.EditarConfirmadaAsync(compra.Id, dto, _emisor.Id + 999);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task ItemsCambian_SmartInventoryActiva_EncolaCorreccionPorDelta()
    {
        _emisor.TieneSmartInventoryActiva = true;
        await _context.SaveChangesAsync();
        var prod = SeedProducto(TipoInventario.Ventas, "VEN-007");
        var compra = SeedCompraConfirmada((prod, 10m, 50m));
        await _context.Entry(compra).Collection(c => c.Detalles).LoadAsync();
        var dto = DtoDesde(compra, detalles: new()
        {
            new CrearCompraDetalleDto
            {
                ProductoId = prod.Id, BodegaId = _bodega.Id, Cantidad = 8m, CostoUnitario = 50m,
                Subtotal = 400m, IVA = 52m, Total = 452m, EsParaInventario = true
            }
        });

        await _service.EditarConfirmadaAsync(compra.Id, dto, _emisor.Id);

        var job = await _context.IntegracionInventarioPendientes.SingleAsync();
        job.MovimientoIdExterno.Should().StartWith($"compra-{compra.Id}-edit");
        var payload = JsonSerializer.Deserialize<SmartInventoryMovimientoRequestDto>(job.PayloadJson)!;
        payload.Items.Should().HaveCount(1);
        payload.Items[0].ProductoIdExterno.Should().Be(prod.Id);
        payload.Items[0].Cantidad.Should().Be(-2m);
    }

    [Fact]
    public async Task ItemsCambian_SmartInventoryInactiva_NoEncolaPeroCorrigeStock()
    {
        var prod = SeedProducto(TipoInventario.Ventas, "VEN-008");
        var compra = SeedCompraConfirmada((prod, 10m, 50m));
        await _context.Entry(compra).Collection(c => c.Detalles).LoadAsync();
        var dto = DtoDesde(compra, detalles: new()
        {
            new CrearCompraDetalleDto
            {
                ProductoId = prod.Id, BodegaId = _bodega.Id, Cantidad = 8m, CostoUnitario = 50m,
                Subtotal = 400m, IVA = 52m, Total = 452m, EsParaInventario = true
            }
        });

        await _service.EditarConfirmadaAsync(compra.Id, dto, _emisor.Id);

        (await _context.IntegracionInventarioPendientes.AnyAsync()).Should().BeFalse();
        (await _context.StocksBodega.SingleAsync(s => s.ProductoId == prod.Id)).CantidadDisponible.Should().Be(8m);
    }

    public void Dispose() => _context.Dispose();
}
