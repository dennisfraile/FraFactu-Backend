using AutoMapper;
using FraFactu.Application.Interfaces;
using FraFactu.Domain.Entities;
using FraFactu.Domain.Enums;
using FraFactu.Infrastructure.Persistence;
using FraFactu.Infrastructure.Services;
using FraFactu.Tests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FraFactu.Tests.IntegrationTests.Services;

/// <summary>
/// F5: cubre el endpoint de estadisticas tras aceptar los mismos filtros que
/// el listado. Antes devolvia totales globales; ahora las tarjetas de la UI
/// quedan sincronizadas con la tabla cuando hay filtros activos.
/// </summary>
public class DteRecibidoServiceEstadisticasTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly DteRecibidoService _service;
    private readonly Emisor _emisor;

    public DteRecibidoServiceEstadisticasTests()
    {
        _context = TestDatabaseHelper.CreateInMemoryContext();
        TestDatabaseHelper.SeedTestData(_context);
        _emisor = _context.Emisores.First();

        var parser = new DteParserService(Mock.Of<ILogger<DteParserService>>());
        var ingesta = new DteIngestaService(_context, parser, Mock.Of<ILogger<DteIngestaService>>());
        _service = new DteRecibidoService(
            _context, Mock.Of<IMapper>(), ingesta, parser, Mock.Of<ILogger<DteRecibidoService>>());
    }

    private DteRecibido SeedDte(
        string codigoGen,
        EstadoDteRecibido estado = EstadoDteRecibido.PENDIENTE,
        decimal total = 100m,
        DateTime? fechaEmision = null,
        string emisorNitProveedor = "06141804941035",
        string emisorNombreProveedor = "PROVEEDOR SA")
    {
        var fecha = fechaEmision ?? new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var dte = new DteRecibido
        {
            EmisorId = _emisor.Id,
            CodigoGeneracion = codigoGen,
            TipoDte = "03",
            NumeroControl = "DTE-03-0001-000000000000001",
            FechaEmision = DateTime.SpecifyKind(fecha, DateTimeKind.Utc),
            EmisorNit = emisorNitProveedor,
            EmisorNombre = emisorNombreProveedor,
            ReceptorNit = _emisor.Nit,
            JsonDte = "{}",
            SubTotal = total,
            Total = total,
            Estado = estado,
            FuenteRecepcion = FuenteRecepcionDte.CORREO,
            FechaCreacion = DateTime.UtcNow
        };
        _context.DtesRecibidos.Add(dte);
        _context.SaveChanges();
        return dte;
    }

    [Fact]
    public async Task SinFiltros_DevuelveTotalesGlobales()
    {
        SeedDte("E-1", EstadoDteRecibido.PENDIENTE, total: 100m);
        SeedDte("E-2", EstadoDteRecibido.PENDIENTE, total: 50m);
        SeedDte("E-3", EstadoDteRecibido.VINCULADO, total: 200m);
        SeedDte("E-4", EstadoDteRecibido.DESCARTADO, total: 25m);

        var stats = await _service.ObtenerEstadisticasAsync(_emisor.Id);

        stats.TotalPendientes.Should().Be(2);
        stats.TotalVinculados.Should().Be(1);
        stats.TotalDescartados.Should().Be(1);
        stats.Total.Should().Be(4);
        stats.MontoTotalPendientes.Should().Be(150m);
    }

    [Fact]
    public async Task FiltroPorEstado_SoloCuentaEseEstado()
    {
        SeedDte("F-1", EstadoDteRecibido.PENDIENTE);
        SeedDte("F-2", EstadoDteRecibido.PENDIENTE);
        SeedDte("F-3", EstadoDteRecibido.VINCULADO);

        var stats = await _service.ObtenerEstadisticasAsync(_emisor.Id, estado: "VINCULADO");

        stats.TotalPendientes.Should().Be(0);
        stats.TotalVinculados.Should().Be(1);
        stats.Total.Should().Be(1);
    }

    [Fact]
    public async Task FiltroPorRangoFechas_SoloDtesEnRango()
    {
        SeedDte("R-1", fechaEmision: new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc));
        SeedDte("R-2", fechaEmision: new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc));
        SeedDte("R-3", fechaEmision: new DateTime(2026, 5, 20, 0, 0, 0, DateTimeKind.Utc));

        var stats = await _service.ObtenerEstadisticasAsync(
            _emisor.Id,
            fechaDesde: new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            fechaHasta: new DateTime(2026, 4, 30, 0, 0, 0, DateTimeKind.Utc));

        stats.TotalPendientes.Should().Be(1);
        stats.Total.Should().Be(1);
    }

    [Fact]
    public async Task FiltroPorSearch_BuscaEnEmisorNombreNitYCodigo()
    {
        SeedDte("S-1", emisorNombreProveedor: "Distribuidora ABC");
        SeedDte("S-2", emisorNombreProveedor: "Comercial XYZ");

        var stats = await _service.ObtenerEstadisticasAsync(_emisor.Id, search: "abc");

        stats.Total.Should().Be(1);
        stats.TotalPendientes.Should().Be(1);
    }

    [Fact]
    public async Task FiltrosCombinados_AplicanTodos()
    {
        SeedDte("C-1", EstadoDteRecibido.PENDIENTE,
            fechaEmision: new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc));
        SeedDte("C-2", EstadoDteRecibido.VINCULADO,
            fechaEmision: new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc));
        SeedDte("C-3", EstadoDteRecibido.PENDIENTE,
            fechaEmision: new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc));

        var stats = await _service.ObtenerEstadisticasAsync(
            _emisor.Id,
            estado: "PENDIENTE",
            fechaDesde: new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            fechaHasta: new DateTime(2026, 5, 31, 0, 0, 0, DateTimeKind.Utc));

        stats.Total.Should().Be(1);
        stats.TotalPendientes.Should().Be(1);
    }

    public void Dispose() => _context.Dispose();
}
