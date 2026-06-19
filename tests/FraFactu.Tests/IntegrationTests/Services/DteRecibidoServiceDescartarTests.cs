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
/// F6: cubre <see cref="DteRecibidoService.DescartarAsync"/>. Valida la
/// transición PENDIENTE → DESCARTADO con motivo, las guardas contra
/// re-descartar o descartar un DTE ya vinculado, y el aislamiento por
/// emisor (no se puede tocar el DTE de otro tenant).
/// </summary>
public class DteRecibidoServiceDescartarTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly DteRecibidoService _service;
    private readonly Emisor _emisor;

    public DteRecibidoServiceDescartarTests()
    {
        _context = TestDatabaseHelper.CreateInMemoryContext();
        TestDatabaseHelper.SeedTestData(_context);
        _emisor = _context.Emisores.First();

        var parser = new DteParserService(Mock.Of<ILogger<DteParserService>>());
        var ingesta = new DteIngestaService(_context, parser, Mock.Of<ILogger<DteIngestaService>>());
        _service = new DteRecibidoService(
            _context, Mock.Of<IMapper>(), ingesta, parser, Mock.Of<ILogger<DteRecibidoService>>());
    }

    private DteRecibido SeedDte(EstadoDteRecibido estado, int? emisorId = null)
    {
        var dte = new DteRecibido
        {
            EmisorId = emisorId ?? _emisor.Id,
            CodigoGeneracion = $"DTE-{Guid.NewGuid()}",
            TipoDte = "03",
            FechaEmision = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            EmisorNit = "06141804941035",
            EmisorNombre = "PROVEEDOR SA",
            JsonDte = "{}",
            Estado = estado,
            FuenteRecepcion = FuenteRecepcionDte.CORREO,
            FechaCreacion = DateTime.UtcNow
        };
        _context.DtesRecibidos.Add(dte);
        _context.SaveChanges();
        return dte;
    }

    [Fact]
    public async Task Descartar_DtePendiente_TransicionaADescartadoYGuardaMotivo()
    {
        var dte = SeedDte(EstadoDteRecibido.PENDIENTE);
        var motivo = "DTE no corresponde a esta empresa";

        await _service.DescartarAsync(dte.Id, new DescartarDteDto { Motivo = motivo }, _emisor.Id);

        var persistido = await _context.DtesRecibidos.SingleAsync();
        persistido.Estado.Should().Be(EstadoDteRecibido.DESCARTADO);
        persistido.MotivoDescarte.Should().Be(motivo);
        persistido.FechaActualizacion.Should().NotBeNull();
    }

    [Fact]
    public async Task Descartar_DteYaVinculado_LanzaInvalidOperation()
    {
        var dte = SeedDte(EstadoDteRecibido.VINCULADO);

        var act = async () => await _service.DescartarAsync(
            dte.Id, new DescartarDteDto { Motivo = "intento" }, _emisor.Id);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*vinculado*");

        var persistido = await _context.DtesRecibidos.SingleAsync();
        persistido.Estado.Should().Be(EstadoDteRecibido.VINCULADO);
        persistido.MotivoDescarte.Should().BeNull();
    }

    [Fact]
    public async Task Descartar_DteYaDescartado_LanzaInvalidOperation()
    {
        var dte = SeedDte(EstadoDteRecibido.DESCARTADO);
        dte.MotivoDescarte = "motivo original";
        await _context.SaveChangesAsync();

        var act = async () => await _service.DescartarAsync(
            dte.Id, new DescartarDteDto { Motivo = "nuevo" }, _emisor.Id);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*ya fue descartado*");

        var persistido = await _context.DtesRecibidos.SingleAsync();
        persistido.MotivoDescarte.Should().Be("motivo original");
    }

    [Fact]
    public async Task Descartar_DteInexistente_LanzaKeyNotFound()
    {
        var act = async () => await _service.DescartarAsync(
            id: 99999, new DescartarDteDto { Motivo = "cualquiera" }, _emisor.Id);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Descartar_DteDeOtroEmisor_LanzaKeyNotFound()
    {
        // Aislamiento cross-tenant: el DTE existe pero pertenece a otro emisor;
        // debe responder como si no existiera (404), no como 400 ni 403.
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
        var dte = SeedDte(EstadoDteRecibido.PENDIENTE, emisorId: otroEmisor.Id);

        var act = async () => await _service.DescartarAsync(
            dte.Id, new DescartarDteDto { Motivo = "intento cross-tenant" }, _emisor.Id);

        await act.Should().ThrowAsync<KeyNotFoundException>();

        var persistido = await _context.DtesRecibidos.SingleAsync();
        persistido.Estado.Should().Be(EstadoDteRecibido.PENDIENTE);
    }

    public void Dispose() => _context.Dispose();
}
