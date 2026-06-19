using FraFactu.Application.DTOs.Integraciones;
using FraFactu.Application.Interfaces;
using FraFactu.Domain.Entities;
using FraFactu.Infrastructure.Jobs;
using FraFactu.Infrastructure.Persistence;
using FraFactu.Tests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace FraFactu.Tests.IntegrationTests.Jobs;

/// <summary>
/// F4: cubre el cuerpo testeable del BackgroundService
/// <see cref="ReconciliacionInventarioJob"/>.EjecutarCorridaAsync. Valida que
/// solo procesa emisores con SmartInventory activo + HubId, que un fallo
/// puntual no bloquea el resto y que sin emisores no llama al cliente.
/// </summary>
public class ReconciliacionInventarioJobTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ISmartInventoryClient> _client = new();

    public ReconciliacionInventarioJobTests()
    {
        _context = TestDatabaseHelper.CreateInMemoryContext();
        TestDatabaseHelper.SeedTestData(_context);

        _client.SetupGet(c => c.EstaConfigurado).Returns(true);
        // Snapshot vacio por defecto: si llega a un emisor, no produce divergencia.
        _client.Setup(c => c.GetSnapshotAsync(
                It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmartInventorySnapshotResponseDto
            {
                OrganizacionId = 0,
                ContinuationToken = null,
                TotalEnviado = 0,
                Items = new()
            });
    }

    [Fact]
    public async Task EjecutarCorrida_SinEmisoresActivos_NoLlamaCliente()
    {
        // El seed por defecto crea un Emisor SIN TieneSmartInventoryActiva.
        await ReconciliacionInventarioJob.EjecutarCorridaAsync(
            _context, _client.Object, NullLogger.Instance, CancellationToken.None);

        _client.Verify(c => c.GetSnapshotAsync(
            It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task EjecutarCorrida_SoloProcesaEmisoresConSmartInventoryActiva()
    {
        // Activamos un emisor; otro queda inactivo.
        var emisor1 = _context.Emisores.First();
        emisor1.HubId = 1;
        emisor1.TieneSmartInventoryActiva = true;

        _context.Emisores.Add(new Emisor
        {
            Nit = "00000000000000",
            Nrc = "x",
            NombreRazonSocial = "OTRA EMPRESA",
            CodigoActividad = "00000",
            DescripcionActividad = "x",
            CorreoElectronico = "x@x",
            Telefono = "0",
            CatDepartamentoId = emisor1.CatDepartamentoId,
            CatMunicipioId = emisor1.CatMunicipioId,
            Direccion = "x",
            HubId = 2,
            TieneSmartInventoryActiva = false  // no debe procesarse
        });
        await _context.SaveChangesAsync();

        await ReconciliacionInventarioJob.EjecutarCorridaAsync(
            _context, _client.Object, NullLogger.Instance, CancellationToken.None);

        // Una sola llamada al cliente (por el emisor1).
        _client.Verify(c => c.GetSnapshotAsync(
            It.Is<int>(o => o == 1), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
        _client.Verify(c => c.GetSnapshotAsync(
            It.Is<int>(o => o == 2), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task EjecutarCorrida_EmisorSinHubId_SeIgnora()
    {
        var emisor = _context.Emisores.First();
        emisor.HubId = null;
        emisor.TieneSmartInventoryActiva = true;
        await _context.SaveChangesAsync();

        await ReconciliacionInventarioJob.EjecutarCorridaAsync(
            _context, _client.Object, NullLogger.Instance, CancellationToken.None);

        _client.Verify(c => c.GetSnapshotAsync(
            It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task EjecutarCorrida_UnEmisorFalla_NoBloqueaLosDemas()
    {
        var emisor1 = _context.Emisores.First();
        emisor1.HubId = 100;
        emisor1.TieneSmartInventoryActiva = true;

        _context.Emisores.Add(new Emisor
        {
            Nit = "00000000000000",
            Nrc = "x",
            NombreRazonSocial = "OTRA EMPRESA",
            CodigoActividad = "00000",
            DescripcionActividad = "x",
            CorreoElectronico = "x@x",
            Telefono = "0",
            CatDepartamentoId = emisor1.CatDepartamentoId,
            CatMunicipioId = emisor1.CatMunicipioId,
            Direccion = "x",
            HubId = 200,
            TieneSmartInventoryActiva = true
        });
        await _context.SaveChangesAsync();

        // El primer emisor (HubId=100) crashea; el segundo (200) debe seguir.
        _client.Setup(c => c.GetSnapshotAsync(
                It.Is<int>(o => o == 100), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Boom"));

        await ReconciliacionInventarioJob.EjecutarCorridaAsync(
            _context, _client.Object, NullLogger.Instance, CancellationToken.None);

        // El segundo emisor recibio al menos una llamada al snapshot.
        _client.Verify(c => c.GetSnapshotAsync(
            It.Is<int>(o => o == 200), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    public void Dispose() => _context.Dispose();
}
