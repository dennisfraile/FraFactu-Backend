using AutoMapper;
using FraFactu.Application.DTOs.DtesRecibidos;
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
/// F1: cubre la carga manual masiva de DTEs JSON/JWT (CCF) que delega
/// archivo por archivo en el pipeline único de ingesta (F0).
/// </summary>
public class DteRecibidoServiceCargaManualTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly DteRecibidoService _service;
    private readonly int _emisorId;
    private readonly string _emisorNit;

    public DteRecibidoServiceCargaManualTests()
    {
        _context = TestDatabaseHelper.CreateInMemoryContext();
        TestDatabaseHelper.SeedTestData(_context);
        var emisor = _context.Emisores.First();
        _emisorId = emisor.Id;
        _emisorNit = emisor.Nit;

        var parser = new DteParserService(Mock.Of<ILogger<DteParserService>>());
        var ingesta = new DteIngestaService(_context, parser, Mock.Of<ILogger<DteIngestaService>>());
        _service = new DteRecibidoService(
            _context, Mock.Of<IMapper>(), ingesta, parser, Mock.Of<ILogger<DteRecibidoService>>());
    }

    private string Dte(string codigoGen, string tipoDte = "03", string? receptorNit = null)
    {
        receptorNit ??= _emisorNit;
        return $$"""
        {
          "identificacion": {
            "version": 3, "ambiente": "00", "tipoDte": "{{tipoDte}}",
            "numeroControl": "DTE-{{tipoDte}}-0001-000000000000001",
            "codigoGeneracion": "{{codigoGen}}", "fecEmi": "2026-05-01"
          },
          "emisor": { "nit": "06141804941035", "nrc": "1234567", "nombre": "PROVEEDOR SA" },
          "receptor": { "nit": "{{receptorNit}}", "nombre": "MI EMPRESA" },
          "resumen": {
            "totalGravada": 100.00, "totalExenta": 0, "totalNoSuj": 0,
            "subTotal": 100.00, "totalPagar": 113.00,
            "tributos": [ { "codigo": "20", "valor": 13.00 } ]
          },
          "cuerpoDocumento": [
            { "numItem": 1, "descripcion": "X", "cantidad": 1, "precioUni": 100.00, "ventaGravada": 100.00 }
          ]
        }
        """;
    }

    private static ArchivoAIngestar Arc(string nombre, string contenido)
        => new() { NombreArchivo = nombre, Contenido = contenido };

    [Fact]
    public async Task Cargar_BatchMixto_ClasificaPorArchivoYPersisteSoloCargados()
    {
        var archivos = new[]
        {
            Arc("ccf-1.json", Dte("AAA-1111")),
            Arc("ccf-2.json", Dte("AAA-2222")),
            Arc("factura.json", Dte("BBB-3333", tipoDte: "01")),
            Arc("ajeno.json", Dte("CCC-4444", receptorNit: "99999999999999")),
            Arc("basura.json", "esto no es un dte"),
        };

        var res = await _service.CargarDtesManualmenteAsync(_emisorId, archivos);

        res.TotalArchivos.Should().Be(5);
        res.Cargados.Should().Be(2);
        res.Duplicados.Should().Be(0);
        res.Rechazados.Should().Be(3);

        res.Resultados.Should().HaveCount(5);
        res.Resultados.Single(r => r.Archivo == "ccf-1.json").Resultado.Should().Be("Cargado");
        res.Resultados.Single(r => r.Archivo == "ccf-2.json").Resultado.Should().Be("Cargado");
        res.Resultados.Single(r => r.Archivo == "factura.json").Resultado.Should().Be("NoEsCCF");
        res.Resultados.Single(r => r.Archivo == "ajeno.json").Resultado.Should().Be("ReceptorInvalido");
        res.Resultados.Single(r => r.Archivo == "basura.json").Resultado.Should().Be("ContenidoInvalido");

        var persistidos = await _context.DtesRecibidos.ToListAsync();
        persistidos.Should().HaveCount(2);
        persistidos.Should().OnlyContain(d => d.FuenteRecepcion == FuenteRecepcionDte.CARGA_MANUAL
            && d.Estado == EstadoDteRecibido.PENDIENTE);
    }

    [Fact]
    public async Task Cargar_MismoCodigoDosVecesEnElBatch_SegundaEsDuplicado()
    {
        var archivos = new[]
        {
            Arc("dup-a.json", Dte("DUP-0001")),
            Arc("dup-b.json", Dte("DUP-0001")),
        };

        var res = await _service.CargarDtesManualmenteAsync(_emisorId, archivos);

        res.Cargados.Should().Be(1);
        res.Duplicados.Should().Be(1);
        res.Resultados.Single(r => r.Archivo == "dup-a.json").Resultado.Should().Be("Cargado");
        res.Resultados.Single(r => r.Archivo == "dup-b.json").Resultado.Should().Be("Duplicado");

        (await _context.DtesRecibidos.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Cargar_RegistraUsuarioQueSube_CuandoSeProvee()
    {
        await _service.CargarDtesManualmenteAsync(
            _emisorId, new[] { Arc("u.json", Dte("USR-0001")) }, cargadoPorUsuarioId: 77);

        var fila = await _context.DtesRecibidos.SingleAsync();
        fila.CargadoPorUsuarioId.Should().Be(77);
        fila.FuenteRecepcion.Should().Be(FuenteRecepcionDte.CARGA_MANUAL);
    }

    [Fact]
    public async Task Cargar_EmisorInexistente_LanzaKeyNotFound()
    {
        var act = async () => await _service.CargarDtesManualmenteAsync(
            emisorId: 99999, new[] { Arc("x.json", Dte("ZZZ-9999")) });

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    public void Dispose() => _context.Dispose();
}
