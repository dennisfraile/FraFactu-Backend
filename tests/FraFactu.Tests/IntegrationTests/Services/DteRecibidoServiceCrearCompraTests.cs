using AutoMapper;
using FraFactu.Application.DTOs.DtesRecibidos;
using FraFactu.Application.Interfaces;
using FraFactu.Domain.Entities;
using FraFactu.Domain.Enums;
using FraFactu.Infrastructure.Persistence;
using FraFactu.Infrastructure.Services;
using FraFactu.Tests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FraFactu.Tests.IntegrationTests.Services;

/// <summary>
/// F4: cubre <see cref="DteRecibidoService.CrearCompraDesdeAsync"/> tras
/// envolver el flujo en una transaccion (proveedor + compra + gastos +
/// vinculacion DTE). Tests con EF InMemory: las transacciones son no-op
/// (InMemoryEventId.TransactionIgnoredWarning configurado en el helper),
/// asi que cubrimos la logica feliz y las invariantes de estado; el
/// rollback real solo se valida en BD real.
/// </summary>
public class DteRecibidoServiceCrearCompraTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly DteRecibidoService _service;
    private readonly Emisor _emisor;
    private readonly Sucursal _sucursal;
    private readonly IDteParserService _parser;

    public DteRecibidoServiceCrearCompraTests()
    {
        _context = TestDatabaseHelper.CreateInMemoryContext();
        TestDatabaseHelper.SeedTestData(_context);
        _emisor = _context.Emisores.First();
        _sucursal = _context.Sucursales.First(s => s.EmisorId == _emisor.Id);

        _parser = new DteParserService(Mock.Of<ILogger<DteParserService>>());
        var ingesta = new DteIngestaService(_context, _parser, Mock.Of<ILogger<DteIngestaService>>());
        _service = new DteRecibidoService(
            _context, Mock.Of<IMapper>(), ingesta, _parser, Mock.Of<ILogger<DteRecibidoService>>());
    }

    private DteRecibido SeedDte(string codigoGen, string? jsonOverride = null)
    {
        var json = jsonOverride ?? $$"""
        {
          "identificacion": {
            "version": 3, "ambiente": "00", "tipoDte": "03",
            "numeroControl": "DTE-03-0001-000000000000001",
            "codigoGeneracion": "{{codigoGen}}", "fecEmi": "2026-05-01"
          },
          "emisor": { "nit": "06141804941035", "nrc": "1234567", "nombre": "PROVEEDOR SA" },
          "receptor": { "nit": "{{_emisor.Nit}}", "nombre": "MI EMPRESA" },
          "resumen": {
            "totalGravada": 200.00, "totalExenta": 0, "totalNoSuj": 0,
            "subTotal": 200.00, "totalPagar": 226.00,
            "tributos": [ { "codigo": "20", "valor": 26.00 } ]
          },
          "cuerpoDocumento": [
            { "numItem": 1, "descripcion": "Item A", "cantidad": 2, "precioUni": 50.00, "ventaGravada": 100.00 },
            { "numItem": 2, "descripcion": "Item B", "cantidad": 1, "precioUni": 100.00, "ventaGravada": 100.00 }
          ]
        }
        """;

        var dte = new DteRecibido
        {
            EmisorId = _emisor.Id,
            CodigoGeneracion = codigoGen,
            TipoDte = "03",
            NumeroControl = "DTE-03-0001-000000000000001",
            FechaEmision = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            EmisorNit = "06141804941035",
            EmisorNombre = "PROVEEDOR SA",
            ReceptorNit = _emisor.Nit,
            ReceptorNombre = "MI EMPRESA",
            JsonDte = json,
            SubTotal = 200.00m,
            IVA = 26.00m,
            Total = 226.00m,
            Estado = EstadoDteRecibido.PENDIENTE,
            FuenteRecepcion = FuenteRecepcionDte.CORREO,
            FechaCreacion = DateTime.UtcNow
        };
        _context.DtesRecibidos.Add(dte);
        _context.SaveChanges();
        return dte;
    }

    [Fact]
    public async Task CrearCompra_HappyPath_CreaProveedorCompraGastosYVinculaDte()
    {
        var dte = SeedDte("CREA-0001");

        var compraId = await _service.CrearCompraDesdeAsync(dte.Id, _emisor.Id, _sucursal.Id);

        compraId.Should().BeGreaterThan(0);

        var proveedor = await _context.Proveedores.SingleAsync(p => p.NIT == dte.EmisorNit);
        proveedor.EmisorId.Should().Be(_emisor.Id);

        var compra = await _context.ComprasExternas.SingleAsync(c => c.Id == compraId);
        compra.ProveedorId.Should().Be(proveedor.Id);
        compra.SucursalId.Should().Be(_sucursal.Id);
        compra.CodigoGeneracionDte.Should().Be(dte.CodigoGeneracion);

        var gastos = await _context.GastosAdministrativos
            .Where(g => g.CompraExternaId == compraId).ToListAsync();
        gastos.Should().HaveCount(2);
        gastos.Sum(g => g.Monto).Should().Be(200m);

        var dteRefrescado = await _context.DtesRecibidos.SingleAsync(d => d.Id == dte.Id);
        dteRefrescado.Estado.Should().Be(EstadoDteRecibido.VINCULADO);
        dteRefrescado.CompraExternaId.Should().Be(compraId);
    }

    [Fact]
    public async Task CrearCompra_ReusaProveedorExistente()
    {
        var dte = SeedDte("REU-0001");
        _context.Proveedores.Add(new Proveedor
        {
            NIT = dte.EmisorNit,
            Nombre = "VIEJO NOMBRE",
            EmisorId = _emisor.Id,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        await _service.CrearCompraDesdeAsync(dte.Id, _emisor.Id, _sucursal.Id);

        var proveedores = await _context.Proveedores
            .Where(p => p.NIT == dte.EmisorNit && p.EmisorId == _emisor.Id).ToListAsync();
        proveedores.Should().HaveCount(1, "no se debe duplicar el proveedor");
    }

    [Fact]
    public async Task CrearCompra_DteYaVinculado_LanzaInvalidOperation()
    {
        var dte = SeedDte("YAV-0001");
        dte.Estado = EstadoDteRecibido.VINCULADO;
        await _context.SaveChangesAsync();

        var act = async () => await _service.CrearCompraDesdeAsync(dte.Id, _emisor.Id, _sucursal.Id);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*ya está vinculado*");
    }

    [Fact]
    public async Task CrearCompra_DteDescartado_LanzaInvalidOperation()
    {
        var dte = SeedDte("DES-0001");
        dte.Estado = EstadoDteRecibido.DESCARTADO;
        await _context.SaveChangesAsync();

        var act = async () => await _service.CrearCompraDesdeAsync(dte.Id, _emisor.Id, _sucursal.Id);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*DTE descartado*");
    }

    [Fact]
    public async Task CrearCompra_CompraConMismoCodigoGeneracionDteYaExiste_LanzaInvalidOperation()
    {
        var dte = SeedDte("DUP-0001");

        // Sembrar una compra con el mismo CodigoGeneracionDte (creada por otro flujo).
        var proveedor = new Proveedor
        {
            NIT = "ANOTHER", Nombre = "Otro", EmisorId = _emisor.Id, Activo = true
        };
        _context.Proveedores.Add(proveedor);
        await _context.SaveChangesAsync();

        _context.ComprasExternas.Add(new CompraExterna
        {
            ProveedorId = proveedor.Id,
            SucursalId = _sucursal.Id,
            NumeroFactura = "X",
            FechaEmision = DateTime.UtcNow,
            CodigoGeneracionDte = dte.CodigoGeneracion,
            Estado = "BORRADOR",
            Origen = "MANUAL"
        });
        await _context.SaveChangesAsync();

        var act = async () => await _service.CrearCompraDesdeAsync(dte.Id, _emisor.Id, _sucursal.Id);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Ya existe una compra*");

        var dteRefrescado = await _context.DtesRecibidos.SingleAsync(d => d.Id == dte.Id);
        dteRefrescado.Estado.Should().Be(EstadoDteRecibido.PENDIENTE);
    }

    [Fact]
    public async Task CrearCompra_JsonInvalido_LanzaYNoDejaCompraOrfana()
    {
        var dte = SeedDte("INV-0001", jsonOverride: "no es json");

        var act = async () => await _service.CrearCompraDesdeAsync(dte.Id, _emisor.Id, _sucursal.Id);
        await act.Should().ThrowAsync<Exception>();

        // F4: el DTE no debe quedar vinculado si fallo el parseo de items.
        var dteRefrescado = await _context.DtesRecibidos.SingleAsync(d => d.Id == dte.Id);
        dteRefrescado.Estado.Should().Be(EstadoDteRecibido.PENDIENTE);
        dteRefrescado.CompraExternaId.Should().BeNull();
    }

    [Fact]
    public async Task CrearCompra_DteInexistente_LanzaKeyNotFound()
    {
        var act = async () => await _service.CrearCompraDesdeAsync(
            dteRecibidoId: 99999, _emisor.Id, _sucursal.Id);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    public void Dispose() => _context.Dispose();
}
