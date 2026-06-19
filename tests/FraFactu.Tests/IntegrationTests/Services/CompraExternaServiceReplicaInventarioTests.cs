using System.Text.Json;
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
/// F3 (Plan inventario desde DTE): cuando una compra externa se confirma y
/// el Emisor tiene la app SmartInventory activa, debe quedar encolado un
/// <c>IntegracionInventarioPendiente</c> con todos los detalles. Cuando la
/// app NO esta activa, no debe encolarse nada.
/// </summary>
public class CompraExternaServiceReplicaInventarioTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly CompraExternaService _service;
    private readonly Emisor _emisor;
    private readonly Sucursal _sucursal;
    private readonly Bodega _bodega;
    private readonly Mock<ICurrentUserService> _currentUser = new();

    public CompraExternaServiceReplicaInventarioTests()
    {
        _context = TestDatabaseHelper.CreateInMemoryContext();
        TestDatabaseHelper.SeedTestData(_context);
        _emisor = _context.Emisores.First();
        _emisor.HubId = 99;  // F3: HubId es obligatorio para encolar
        _sucursal = _context.Sucursales.First(s => s.EmisorId == _emisor.Id);
        _sucursal.HubSucursalId = 77;  // sucursal vinculada a SmartHub (mapea a Bodega.SucursalId)
        _bodega = _context.Bodegas.First(b => b.SucursalId == _sucursal.Id);
        _currentUser.Setup(c => c.GetUsuarioId()).Returns((int?)null);
        _context.SaveChanges();

        _service = new CompraExternaService(_context, _currentUser.Object);
    }

    private ProductoServicio SeedProducto(
        TipoInventario tipo,
        string codigo,
        bool conActivoFijo = false)
    {
        var unidad = _context.CatUnidadesMedida.First();
        var tipoItem = _context.CatTiposItem.First(t => t.Codigo == "1");
        var p = new ProductoServicio
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
            FechaCreacion = DateTime.UtcNow,
            FechaAdquisicion = conActivoFijo ? new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc) : null,
            AniosVidaUtil = conActivoFijo ? 5 : null,
            ValorActual = conActivoFijo ? 1200m : null,
            ValorResidual = conActivoFijo ? 100m : null
        };
        _context.ProductosServicios.Add(p);
        _context.SaveChanges();
        return p;
    }

    private CompraExterna SeedCompraBorrador(
        params (ProductoServicio prod, decimal cantidad, decimal costo)[] items)
    {
        var proveedor = new Proveedor
        {
            NIT = "00000000000099",
            Nombre = "Proveedor",
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
                Total = cant * costo,
                EsParaInventario = true,
                FechaCreacion = DateTime.UtcNow
            });
        }
        _context.SaveChanges();
        return compra;
    }

    [Fact]
    public async Task Confirmar_SmartInventoryActiva_EncolaJobEnOutbox()
    {
        _emisor.TieneSmartInventoryActiva = true;
        await _context.SaveChangesAsync();
        var prod = SeedProducto(TipoInventario.Ventas, "VEN-001");
        var compra = SeedCompraBorrador((prod, 10m, 50m));

        await _service.ConfirmarAsync(compra.Id, _emisor.Id);

        var job = await _context.IntegracionInventarioPendientes.SingleAsync();
        job.Estado.Should().Be(EstadoIntegracionInventario.ENCOLADO);
        job.MovimientoIdExterno.Should().Be($"compra-{compra.Id}");
        job.EmisorId.Should().Be(_emisor.Id);

        var payload = JsonSerializer.Deserialize<SmartInventoryMovimientoRequestDto>(job.PayloadJson);
        payload.Should().NotBeNull();
        payload!.OrganizacionId.Should().Be(99);
        payload.DocumentoOrigenId.Should().Be(compra.Id);
        payload.Items.Should().HaveCount(1);
        payload.Items[0].CodigoProducto.Should().Be("VEN-001");
        payload.Items[0].BodegaNombre.Should().Be(_bodega.Nombre);
        // El HubSucursalId de la sucursal de la compra viaja para que SmartInventory
        // cree la bodega con sucursal (si no, el filtro Sub-plan 8 la oculta).
        payload.Items[0].SucursalIdExterno.Should().Be(77);
        payload.Items[0].Cantidad.Should().Be(10m);
        payload.Items[0].CostoUnitario.Should().Be(50m);
        payload.Items[0].TipoInventario.Should().Be("Ventas");
        // Para Ventas no se envian campos de activo fijo.
        payload.Items[0].FechaAdquisicion.Should().BeNull();
    }

    [Fact]
    public async Task Confirmar_SmartInventoryNoActiva_NoEncolaNada()
    {
        // Default: TieneSmartInventoryActiva == false.
        var prod = SeedProducto(TipoInventario.Ventas, "VEN-002");
        var compra = SeedCompraBorrador((prod, 5m, 20m));

        await _service.ConfirmarAsync(compra.Id, _emisor.Id);

        (await _context.IntegracionInventarioPendientes.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task Confirmar_MobiliarioEquipo_IncluyeCamposDeActivoFijoEnPayload()
    {
        _emisor.TieneSmartInventoryActiva = true;
        await _context.SaveChangesAsync();
        var activo = SeedProducto(TipoInventario.MobiliarioEquipo, "ACT-001", conActivoFijo: true);
        var compra = SeedCompraBorrador((activo, 1m, 1200m));

        await _service.ConfirmarAsync(compra.Id, _emisor.Id);

        var job = await _context.IntegracionInventarioPendientes.SingleAsync();
        var payload = JsonSerializer.Deserialize<SmartInventoryMovimientoRequestDto>(job.PayloadJson)!;
        payload.Items.Should().HaveCount(1);
        payload.Items[0].TipoInventario.Should().Be("MobiliarioEquipo");
        payload.Items[0].FechaAdquisicion.Should().Be(new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc));
        payload.Items[0].AniosVidaUtil.Should().Be(5);
        payload.Items[0].ValorActual.Should().Be(1200m);
        payload.Items[0].ValorResidual.Should().Be(100m);
    }

    [Fact]
    public async Task Confirmar_DosVecesMismaCompra_NoEncolaDuplicado()
    {
        // En la practica una compra solo se confirma una vez (porque despues
        // queda CONFIRMADA), pero el filtro defensivo en el codigo previene
        // duplicar el job si alguien forzara el estado.
        _emisor.TieneSmartInventoryActiva = true;
        await _context.SaveChangesAsync();
        var prod = SeedProducto(TipoInventario.Ventas, "VEN-003");
        var compra = SeedCompraBorrador((prod, 1m, 10m));

        await _service.ConfirmarAsync(compra.Id, _emisor.Id);

        // Simulamos re-encolar regresando a BORRADOR y confirmando otra vez.
        compra.Estado = "BORRADOR";
        compra.FechaConfirmacion = null;
        await _context.SaveChangesAsync();

        await _service.ConfirmarAsync(compra.Id, _emisor.Id);

        var jobs = await _context.IntegracionInventarioPendientes.ToListAsync();
        jobs.Should().HaveCount(1);
    }

    [Fact]
    public async Task Confirmar_SinHubId_NoEncolaAunqueFlagActivo()
    {
        // F3: HubId del Emisor es la OrganizacionId de SmartInventory. Sin
        // HubId no podemos armar el payload, asi que el encolado se salta.
        _emisor.TieneSmartInventoryActiva = true;
        _emisor.HubId = null;
        await _context.SaveChangesAsync();
        var prod = SeedProducto(TipoInventario.Ventas, "VEN-004");
        var compra = SeedCompraBorrador((prod, 1m, 10m));

        await _service.ConfirmarAsync(compra.Id, _emisor.Id);

        (await _context.IntegracionInventarioPendientes.AnyAsync()).Should().BeFalse();
        var compraActualizada = await _context.ComprasExternas.FindAsync(compra.Id);
        compraActualizada!.Estado.Should().Be("CONFIRMADA");
    }

    public void Dispose() => _context.Dispose();
}
