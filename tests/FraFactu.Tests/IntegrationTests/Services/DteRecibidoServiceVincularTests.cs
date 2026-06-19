using AutoMapper;
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
/// F6: cubre <see cref="DteRecibidoService.VincularACompraAsync"/>. Valida la
/// transición PENDIENTE → VINCULADO con FK a la compra, las guardas contra
/// re-vincular y contra usar una compra que pertenece a otro emisor (proveedor
/// de otro tenant).
/// </summary>
public class DteRecibidoServiceVincularTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly DteRecibidoService _service;
    private readonly Emisor _emisor;
    private readonly Sucursal _sucursal;

    public DteRecibidoServiceVincularTests()
    {
        _context = TestDatabaseHelper.CreateInMemoryContext();
        TestDatabaseHelper.SeedTestData(_context);
        _emisor = _context.Emisores.First();
        _sucursal = _context.Sucursales.First(s => s.EmisorId == _emisor.Id);

        var parser = new DteParserService(Mock.Of<ILogger<DteParserService>>());
        var ingesta = new DteIngestaService(_context, parser, Mock.Of<ILogger<DteIngestaService>>());
        _service = new DteRecibidoService(
            _context, Mock.Of<IMapper>(), ingesta, parser, Mock.Of<ILogger<DteRecibidoService>>());
    }

    private DteRecibido SeedDte(EstadoDteRecibido estado, int? compraExternaId = null)
    {
        var dte = new DteRecibido
        {
            EmisorId = _emisor.Id,
            CodigoGeneracion = $"DTE-{Guid.NewGuid()}",
            TipoDte = "03",
            FechaEmision = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            EmisorNit = "06141804941035",
            EmisorNombre = "PROVEEDOR SA",
            JsonDte = "{}",
            Estado = estado,
            CompraExternaId = compraExternaId,
            FuenteRecepcion = FuenteRecepcionDte.CORREO,
            FechaCreacion = DateTime.UtcNow
        };
        _context.DtesRecibidos.Add(dte);
        _context.SaveChanges();
        return dte;
    }

    private CompraExterna SeedCompra(int? emisorIdOverride = null)
    {
        var proveedor = new Proveedor
        {
            NIT = "06141804941035",
            Nombre = "PROVEEDOR SA",
            EmisorId = emisorIdOverride ?? _emisor.Id,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };
        _context.Proveedores.Add(proveedor);
        _context.SaveChanges();

        var compra = new CompraExterna
        {
            ProveedorId = proveedor.Id,
            SucursalId = _sucursal.Id,
            NumeroFactura = "FAC-001",
            FechaEmision = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            FechaRegistro = DateTime.UtcNow,
            Total = 100,
            Estado = "BORRADOR",
            Origen = "MANUAL",
            FechaCreacion = DateTime.UtcNow
        };
        _context.ComprasExternas.Add(compra);
        _context.SaveChanges();
        return compra;
    }

    [Fact]
    public async Task Vincular_DtePendienteConCompraValida_TransicionaAVinculado()
    {
        var dte = SeedDte(EstadoDteRecibido.PENDIENTE);
        var compra = SeedCompra();

        await _service.VincularACompraAsync(dte.Id, compra.Id, _emisor.Id);

        var persistido = await _context.DtesRecibidos.SingleAsync(d => d.Id == dte.Id);
        persistido.Estado.Should().Be(EstadoDteRecibido.VINCULADO);
        persistido.CompraExternaId.Should().Be(compra.Id);
        persistido.FechaActualizacion.Should().NotBeNull();
    }

    [Fact]
    public async Task Vincular_DteYaVinculado_LanzaInvalidOperation()
    {
        var compraExistente = SeedCompra();
        var dte = SeedDte(EstadoDteRecibido.VINCULADO, compraExternaId: compraExistente.Id);
        var otraCompra = SeedCompra();

        var act = async () => await _service.VincularACompraAsync(dte.Id, otraCompra.Id, _emisor.Id);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*ya está vinculado*");

        var persistido = await _context.DtesRecibidos.SingleAsync(d => d.Id == dte.Id);
        persistido.CompraExternaId.Should().Be(compraExistente.Id);
    }

    [Fact]
    public async Task Vincular_CompraDeOtroEmisor_LanzaKeyNotFound()
    {
        // Aislamiento cross-tenant: la compra existe pero pertenece (vía
        // proveedor) a otro emisor; el endpoint debe responder 404 sin tocar
        // el DTE del tenant actual.
        var otroEmisor = new Emisor
        {
            Nit = "00000000000002",
            Nrc = "2",
            NombreRazonSocial = "OTRO EMISOR",
            CodigoActividad = "00000",
            DescripcionActividad = "x",
            CorreoElectronico = "x@x.com",
            Telefono = "0",
            CatDepartamentoId = _emisor.CatDepartamentoId,
            CatMunicipioId = _emisor.CatMunicipioId,
            Direccion = "x"
        };
        _context.Emisores.Add(otroEmisor);
        await _context.SaveChangesAsync();
        var dte = SeedDte(EstadoDteRecibido.PENDIENTE);
        var compraAjena = SeedCompra(emisorIdOverride: otroEmisor.Id);

        var act = async () => await _service.VincularACompraAsync(dte.Id, compraAjena.Id, _emisor.Id);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Compra*");

        var persistido = await _context.DtesRecibidos.SingleAsync(d => d.Id == dte.Id);
        persistido.Estado.Should().Be(EstadoDteRecibido.PENDIENTE);
        persistido.CompraExternaId.Should().BeNull();
    }

    [Fact]
    public async Task Vincular_DteInexistente_LanzaKeyNotFound()
    {
        var compra = SeedCompra();

        var act = async () => await _service.VincularACompraAsync(
            dteRecibidoId: 99999, compra.Id, _emisor.Id);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*DTE recibido*");
    }

    public void Dispose() => _context.Dispose();
}
