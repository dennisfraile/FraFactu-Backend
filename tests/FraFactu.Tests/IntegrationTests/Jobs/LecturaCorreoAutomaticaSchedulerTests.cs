using FraFactu.Application.DTOs.DtesRecibidos;
using FraFactu.Application.Interfaces;
using FraFactu.Domain.Entities;
using FraFactu.Domain.Enums;
using FraFactu.Infrastructure.Jobs;
using FraFactu.Infrastructure.Persistence;
using FraFactu.Tests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace FraFactu.Tests.IntegrationTests.Jobs;

/// <summary>
/// F3: cubre el núcleo del scheduler (<c>BarrerYEncolarAsync</c>). El bucle
/// del hosted service no se prueba acá; el método estático se invoca directo
/// con un <c>IEmailReaderService</c> mockeado para asegurar que solo encole
/// emisores válidos y respete cooldown + jobs activos.
/// </summary>
public class LecturaCorreoAutomaticaSchedulerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IEmailReaderService> _emailReader = new();

    public LecturaCorreoAutomaticaSchedulerTests()
    {
        _context = TestDatabaseHelper.CreateInMemoryContext();
        TestDatabaseHelper.SeedTestData(_context);

        _emailReader
            .Setup(s => s.EncolarLecturaAsync(
                It.IsAny<int>(),
                It.IsAny<int?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EncolarLecturaCorreoResponseDto
            {
                JobId = 1,
                Estado = "ENCOLADO",
                Mensaje = "ok"
            });
    }

    private Emisor SeedEmisor(
        bool habilitada = true,
        bool conectado = true,
        bool activo = true,
        string? refreshToken = "ENC:tok",
        DateTime? ultimaLectura = null)
    {
        var existente = _context.Emisores.First();
        existente.LecturaCorreoHabilitada = habilitada;
        existente.GmailConectado = conectado;
        existente.Activo = activo;
        existente.GmailRefreshToken = refreshToken;
        existente.UltimaLecturaCorreo = ultimaLectura;
        _context.SaveChanges();
        return existente;
    }

    [Fact]
    public async Task Barrido_EmisorHabilitadoYConectado_LoEncola()
    {
        var emisor = SeedEmisor();

        await LecturaCorreoAutomaticaScheduler.BarrerYEncolarAsync(
            _context, _emailReader.Object, cooldownHoras: 2,
            NullLogger.Instance, CancellationToken.None);

        _emailReader.Verify(s => s.EncolarLecturaAsync(
            emisor.Id, null, null, null, true, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Barrido_LecturaDeshabilitada_NoEncola()
    {
        SeedEmisor(habilitada: false);

        await LecturaCorreoAutomaticaScheduler.BarrerYEncolarAsync(
            _context, _emailReader.Object, cooldownHoras: 2,
            NullLogger.Instance, CancellationToken.None);

        _emailReader.Verify(s => s.EncolarLecturaAsync(
            It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Barrido_GmailDesconectado_NoEncola()
    {
        SeedEmisor(conectado: false);

        await LecturaCorreoAutomaticaScheduler.BarrerYEncolarAsync(
            _context, _emailReader.Object, cooldownHoras: 2,
            NullLogger.Instance, CancellationToken.None);

        _emailReader.Verify(s => s.EncolarLecturaAsync(
            It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Barrido_EmisorInactivo_NoEncola()
    {
        SeedEmisor(activo: false);

        await LecturaCorreoAutomaticaScheduler.BarrerYEncolarAsync(
            _context, _emailReader.Object, cooldownHoras: 2,
            NullLogger.Instance, CancellationToken.None);

        _emailReader.Verify(s => s.EncolarLecturaAsync(
            It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Barrido_SinRefreshToken_NoEncola()
    {
        SeedEmisor(refreshToken: null);

        await LecturaCorreoAutomaticaScheduler.BarrerYEncolarAsync(
            _context, _emailReader.Object, cooldownHoras: 2,
            NullLogger.Instance, CancellationToken.None);

        _emailReader.Verify(s => s.EncolarLecturaAsync(
            It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Barrido_UltimaLecturaDentroDelCooldown_NoEncola()
    {
        SeedEmisor(ultimaLectura: DateTime.UtcNow.AddMinutes(-30));

        await LecturaCorreoAutomaticaScheduler.BarrerYEncolarAsync(
            _context, _emailReader.Object, cooldownHoras: 2,
            NullLogger.Instance, CancellationToken.None);

        _emailReader.Verify(s => s.EncolarLecturaAsync(
            It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Barrido_UltimaLecturaFueraDelCooldown_Encola()
    {
        var emisor = SeedEmisor(ultimaLectura: DateTime.UtcNow.AddHours(-3));

        await LecturaCorreoAutomaticaScheduler.BarrerYEncolarAsync(
            _context, _emailReader.Object, cooldownHoras: 2,
            NullLogger.Instance, CancellationToken.None);

        _emailReader.Verify(s => s.EncolarLecturaAsync(
            emisor.Id, null, null, null, true, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Barrido_ConJobEnProgreso_NoEncola()
    {
        var emisor = SeedEmisor();
        _context.LecturaCorreoJobs.Add(new LecturaCorreoJob
        {
            EmisorId = emisor.Id,
            Estado = EstadoLecturaCorreoJob.EN_PROGRESO,
            FechaCreacion = DateTime.UtcNow.AddMinutes(-1)
        });
        await _context.SaveChangesAsync();

        await LecturaCorreoAutomaticaScheduler.BarrerYEncolarAsync(
            _context, _emailReader.Object, cooldownHoras: 2,
            NullLogger.Instance, CancellationToken.None);

        _emailReader.Verify(s => s.EncolarLecturaAsync(
            It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Barrido_ConJobEncolado_NoEncola()
    {
        var emisor = SeedEmisor();
        _context.LecturaCorreoJobs.Add(new LecturaCorreoJob
        {
            EmisorId = emisor.Id,
            Estado = EstadoLecturaCorreoJob.ENCOLADO,
            FechaCreacion = DateTime.UtcNow.AddMinutes(-1)
        });
        await _context.SaveChangesAsync();

        await LecturaCorreoAutomaticaScheduler.BarrerYEncolarAsync(
            _context, _emailReader.Object, cooldownHoras: 2,
            NullLogger.Instance, CancellationToken.None);

        _emailReader.Verify(s => s.EncolarLecturaAsync(
            It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Barrido_ConJobCompletado_SiEncola()
    {
        var emisor = SeedEmisor();
        _context.LecturaCorreoJobs.Add(new LecturaCorreoJob
        {
            EmisorId = emisor.Id,
            Estado = EstadoLecturaCorreoJob.COMPLETADO,
            FechaCreacion = DateTime.UtcNow.AddHours(-3),
            FechaFin = DateTime.UtcNow.AddHours(-3)
        });
        await _context.SaveChangesAsync();

        await LecturaCorreoAutomaticaScheduler.BarrerYEncolarAsync(
            _context, _emailReader.Object, cooldownHoras: 2,
            NullLogger.Instance, CancellationToken.None);

        _emailReader.Verify(s => s.EncolarLecturaAsync(
            emisor.Id, null, null, null, true, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Barrido_ExcepcionEnUnEmisor_NoFrenaAlSiguiente()
    {
        // Dos emisores: el primero lanza al encolar, el segundo debe encolarse igual.
        var emisor1 = _context.Emisores.First();
        emisor1.LecturaCorreoHabilitada = true;
        emisor1.GmailConectado = true;
        emisor1.Activo = true;
        emisor1.GmailRefreshToken = "ENC:t1";
        emisor1.UltimaLecturaCorreo = null;

        var emisor2 = new Emisor
        {
            Nit = "00000000000002",
            Nrc = "2",
            NombreRazonSocial = "E2",
            CodigoActividad = "00000",
            DescripcionActividad = "Test",
            CorreoElectronico = "e2@x.com",
            Telefono = "0",
            CatDepartamentoId = emisor1.CatDepartamentoId,
            CatMunicipioId = emisor1.CatMunicipioId,
            Direccion = "x",
            LecturaCorreoHabilitada = true,
            GmailConectado = true,
            Activo = true,
            GmailRefreshToken = "ENC:t2"
        };
        _context.Emisores.Add(emisor2);
        await _context.SaveChangesAsync();

        _emailReader
            .Setup(s => s.EncolarLecturaAsync(
                emisor1.Id, It.IsAny<int?>(), It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        await LecturaCorreoAutomaticaScheduler.BarrerYEncolarAsync(
            _context, _emailReader.Object, cooldownHoras: 2,
            NullLogger.Instance, CancellationToken.None);

        _emailReader.Verify(s => s.EncolarLecturaAsync(
            emisor2.Id, null, null, null, true, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    public void Dispose() => _context.Dispose();
}
