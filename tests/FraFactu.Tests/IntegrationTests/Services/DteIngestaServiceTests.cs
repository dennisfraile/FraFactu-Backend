using FraFactu.Application.Interfaces;
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
/// Cubre el pipeline único de ingesta (F0): validación es-DTE → es-CCF →
/// receptor==emisor, deduplicación idempotente y persistencia como PENDIENTE.
/// Nota: el provider InMemory no aplica el índice único, por lo que el camino
/// de duplicado se valida vía el fast-path (AnyAsync). El catch de
/// DbUpdateException (race real) requiere un provider relacional y se valida
/// en UAT.
/// </summary>
public class DteIngestaServiceTests : IDisposable
{
    private const string EmisorNit = "06140000000000";

    private readonly ApplicationDbContext _context;
    private readonly DteIngestaService _service;

    public DteIngestaServiceTests()
    {
        _context = TestDatabaseHelper.CreateInMemoryContext();
        var parser = new DteParserService(new Mock<ILogger<DteParserService>>().Object);
        _service = new DteIngestaService(_context, parser, new Mock<ILogger<DteIngestaService>>().Object);
    }

    private static string Dte(string codigoGen, string tipoDte = "03", string receptorNit = EmisorNit)
        => $$"""
        {
          "identificacion": {
            "version": 3, "ambiente": "00", "tipoDte": "{{tipoDte}}",
            "numeroControl": "DTE-{{tipoDte}}-0001-000000000000001",
            "codigoGeneracion": "{{codigoGen}}", "fecEmi": "2026-05-01"
          },
          "emisor": { "nit": "06141804941035", "nrc": "1234567", "nombre": "PROVEEDOR TEST SA DE CV" },
          "receptor": { "nit": "{{receptorNit}}", "nombre": "MI EMPRESA SA DE CV" },
          "resumen": {
            "totalGravada": 100.00, "totalExenta": 0, "totalNoSuj": 0,
            "subTotal": 100.00, "totalPagar": 113.00,
            "tributos": [ { "codigo": "20", "valor": 13.00 } ]
          },
          "cuerpoDocumento": [
            { "numItem": 1, "descripcion": "Producto X", "cantidad": 1, "precioUni": 100.00, "ventaGravada": 100.00 }
          ]
        }
        """;

    private static IngestaContexto Ctx(FuenteRecepcionDte fuente = FuenteRecepcionDte.CORREO, int? usuarioId = null)
        => new()
        {
            EmisorId = 1,
            EmisorNit = EmisorNit,
            Fuente = fuente,
            CargadoPorUsuarioId = usuarioId,
            EmailOrigen = fuente == FuenteRecepcionDte.CORREO ? "proveedor@correo.com" : null
        };

    [Fact]
    public async Task Ingestar_CcfValido_DevuelveCargadoYPersistePendiente()
    {
        var codigo = "A1B2C3D4-E5F6-7890-ABCD-EF1234567890";

        var res = await _service.IngestarAsync(Dte(codigo), Ctx());

        res.Resultado.Should().Be(ResultadoIngestaDte.Cargado);
        res.EsExito.Should().BeTrue();
        res.CodigoGeneracion.Should().Be(codigo);
        res.DteRecibidoId.Should().NotBeNull();

        var fila = await _context.DtesRecibidos.SingleAsync();
        fila.Estado.Should().Be(EstadoDteRecibido.PENDIENTE);
        fila.FuenteRecepcion.Should().Be(FuenteRecepcionDte.CORREO);
        fila.TipoDte.Should().Be("03");
        fila.EmisorId.Should().Be(1);
    }

    [Fact]
    public async Task Ingestar_CargaManual_GuardaFuenteYUsuario()
    {
        var res = await _service.IngestarAsync(
            Dte("CARGA-001"), Ctx(FuenteRecepcionDte.CARGA_MANUAL, usuarioId: 42));

        res.Resultado.Should().Be(ResultadoIngestaDte.Cargado);
        var fila = await _context.DtesRecibidos.SingleAsync();
        fila.FuenteRecepcion.Should().Be(FuenteRecepcionDte.CARGA_MANUAL);
        fila.CargadoPorUsuarioId.Should().Be(42);
        fila.EmailOrigen.Should().BeNull();
    }

    [Fact]
    public async Task Ingestar_NoEsCcf_DevuelveNoEsCCF()
    {
        var res = await _service.IngestarAsync(Dte("FAC-001", tipoDte: "01"), Ctx());

        res.Resultado.Should().Be(ResultadoIngestaDte.NoEsCCF);
        (await _context.DtesRecibidos.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task Ingestar_ReceptorDistintoDelEmisor_DevuelveReceptorInvalido()
    {
        var res = await _service.IngestarAsync(
            Dte("REC-001", receptorNit: "99999999999999"), Ctx());

        res.Resultado.Should().Be(ResultadoIngestaDte.ReceptorInvalido);
        (await _context.DtesRecibidos.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task Ingestar_ContenidoBasura_DevuelveContenidoInvalido()
    {
        var res = await _service.IngestarAsync("esto no es un dte", Ctx());

        res.Resultado.Should().Be(ResultadoIngestaDte.ContenidoInvalido);
        (await _context.DtesRecibidos.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task Ingestar_MismoCodigoDosVeces_SegundaEsDuplicado()
    {
        var codigo = "DUP-1234-5678";

        var primera = await _service.IngestarAsync(Dte(codigo), Ctx());
        var segunda = await _service.IngestarAsync(Dte(codigo), Ctx());

        primera.Resultado.Should().Be(ResultadoIngestaDte.Cargado);
        segunda.Resultado.Should().Be(ResultadoIngestaDte.Duplicado);
        (await _context.DtesRecibidos.CountAsync()).Should().Be(1);
    }

    public void Dispose() => _context.Dispose();
}
