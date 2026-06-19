using FraFactu.Application.DTOs.Integraciones;
using FraFactu.Domain.Entities;
using FraFactu.Domain.Enums;
using FraFactu.Infrastructure.Services;
using FraFactu.Tests.Fixtures;
using FluentAssertions;
using Xunit;

namespace FraFactu.Tests.IntegrationTests.Services;

/// <summary>
/// F4 (Plan inventario desde DTE): cubre el listado paginado con filtros del
/// endpoint <c>GET /api/inventario/divergencias</c>. El service solo lee; las
/// transiciones de estado las hace <c>ReconciliacionInventarioService</c>.
/// </summary>
public class DivergenciaInventarioServiceTests : IDisposable
{
    private readonly FraFactu.Infrastructure.Persistence.ApplicationDbContext _context;
    private readonly DivergenciaInventarioService _service;
    private readonly int _emisorId;

    public DivergenciaInventarioServiceTests()
    {
        _context = TestDatabaseHelper.CreateInMemoryContext();
        TestDatabaseHelper.SeedTestData(_context);
        _service = new DivergenciaInventarioService(_context);
        _emisorId = _context.Emisores.First().Id;
    }

    private void Seed(string codigo, EstadoDivergenciaInventario estado, DateTime ultima, int emisorId = 0)
    {
        _context.DivergenciasInventario.Add(new DivergenciaInventario
        {
            EjecucionId = Guid.NewGuid(),
            EmisorId = emisorId == 0 ? _emisorId : emisorId,
            CodigoProducto = codigo,
            NombreBodega = "Bodega Principal",
            StockSmartix = 10,
            StockSmartInventory = 5,
            Diff = 5,
            Estado = estado,
            UltimaDeteccion = ultima,
            FechaCreacion = ultima,
            FechaResolucion = estado == EstadoDivergenciaInventario.RESUELTA ? ultima : null
        });
        _context.SaveChanges();
    }

    [Fact]
    public async Task Listar_SinFiltros_OrdenaPorUltimaDeteccionDesc()
    {
        Seed("P-001", EstadoDivergenciaInventario.DETECTADA, new DateTime(2026, 5, 21, 9, 0, 0, DateTimeKind.Utc));
        Seed("P-002", EstadoDivergenciaInventario.DETECTADA, new DateTime(2026, 5, 21, 12, 0, 0, DateTimeKind.Utc));
        Seed("P-003", EstadoDivergenciaInventario.RESUELTA, new DateTime(2026, 5, 21, 6, 0, 0, DateTimeKind.Utc));

        var resp = await _service.ListarAsync(_emisorId, new DivergenciasFiltroDto());

        resp.Total.Should().Be(3);
        resp.Items.Select(i => i.CodigoProducto).Should().Equal("P-002", "P-001", "P-003");
    }

    [Fact]
    public async Task Listar_FiltroEstado_SoloDevuelveDetectadas()
    {
        Seed("P-001", EstadoDivergenciaInventario.DETECTADA, DateTime.UtcNow);
        Seed("P-002", EstadoDivergenciaInventario.RESUELTA, DateTime.UtcNow);

        var resp = await _service.ListarAsync(_emisorId, new DivergenciasFiltroDto
        {
            Estado = EstadoDivergenciaInventario.DETECTADA
        });

        resp.Total.Should().Be(1);
        resp.Items.Should().ContainSingle().Which.CodigoProducto.Should().Be("P-001");
    }

    [Fact]
    public async Task Listar_ScopeTenant_NoMezclaEmisores()
    {
        Seed("MIO", EstadoDivergenciaInventario.DETECTADA, DateTime.UtcNow);
        Seed("AJENO", EstadoDivergenciaInventario.DETECTADA, DateTime.UtcNow, emisorId: 9999);

        var resp = await _service.ListarAsync(_emisorId, new DivergenciasFiltroDto());

        resp.Total.Should().Be(1);
        resp.Items.Should().ContainSingle().Which.CodigoProducto.Should().Be("MIO");
    }

    [Fact]
    public async Task Listar_Paginacion_RespetaTamanoYPagina()
    {
        for (var i = 1; i <= 5; i++)
        {
            Seed($"P-{i:D3}", EstadoDivergenciaInventario.DETECTADA,
                new DateTime(2026, 5, 21, 10, 0, 0, DateTimeKind.Utc).AddMinutes(i));
        }

        var pag1 = await _service.ListarAsync(_emisorId, new DivergenciasFiltroDto
        {
            Pagina = 1, TamanoPagina = 2
        });
        var pag2 = await _service.ListarAsync(_emisorId, new DivergenciasFiltroDto
        {
            Pagina = 2, TamanoPagina = 2
        });

        pag1.Total.Should().Be(5);
        pag1.Items.Select(i => i.CodigoProducto).Should().Equal("P-005", "P-004");
        pag2.Items.Select(i => i.CodigoProducto).Should().Equal("P-003", "P-002");
    }

    public void Dispose() => _context.Dispose();
}
