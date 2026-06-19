using FraFactu.Application.Common.Settings;
using FraFactu.Application.Interfaces;
using FraFactu.Domain.Entities;
using FraFactu.Domain.Enums;
using FraFactu.Infrastructure.Persistence;
using FraFactu.Infrastructure.Services;
using FraFactu.Tests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace FraFactu.Tests.IntegrationTests.Services;

/// <summary>
/// F2: cubre la fachada <see cref="EmailReaderService"/> tras el refactor a
/// flujo asíncrono — solo encolar y consultar estado del job más reciente.
/// </summary>
public class EmailReaderServiceEncolarTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly EmailReaderService _service;
    private readonly Emisor _emisor;

    public EmailReaderServiceEncolarTests()
    {
        _context = TestDatabaseHelper.CreateInMemoryContext();
        TestDatabaseHelper.SeedTestData(_context);
        _emisor = _context.Emisores.First();
        _emisor.LecturaCorreoHabilitada = true;
        _emisor.GmailConectado = true;
        _emisor.GmailRefreshToken = "ENC:dummy-token";
        _context.SaveChanges();

        var encryption = new Mock<IEncryptionService>();
        encryption.Setup(e => e.Decrypt(It.IsAny<string>())).Returns("decrypted");
        var settings = Options.Create(new GoogleAuthSettings
        {
            ClientId = "dummy", ClientSecret = "dummy"
        });

        _service = new EmailReaderService(
            _context, encryption.Object, settings, Mock.Of<ILogger<EmailReaderService>>());
    }

    [Fact]
    public async Task EncolarLectura_EmisorValido_CreaJobEnEstadoEncolado()
    {
        var resp = await _service.EncolarLecturaAsync(_emisor.Id, usuarioId: 7);

        resp.JobId.Should().BeGreaterThan(0);
        resp.Estado.Should().Be("ENCOLADO");

        var job = await _context.LecturaCorreoJobs.SingleAsync();
        job.EmisorId.Should().Be(_emisor.Id);
        job.Estado.Should().Be(EstadoLecturaCorreoJob.ENCOLADO);
        job.IniciadoPorUsuarioId.Should().Be(7);
        job.FechaInicio.Should().BeNull();
        job.FechaFin.Should().BeNull();
    }

    [Fact]
    public async Task EncolarLectura_ConJobActivoExistente_ReutilizaSinDuplicar()
    {
        var existente = new LecturaCorreoJob
        {
            EmisorId = _emisor.Id,
            Estado = EstadoLecturaCorreoJob.EN_PROGRESO,
            FechaCreacion = DateTime.UtcNow.AddMinutes(-1)
        };
        _context.LecturaCorreoJobs.Add(existente);
        await _context.SaveChangesAsync();

        var resp = await _service.EncolarLecturaAsync(_emisor.Id);

        resp.JobId.Should().Be(existente.Id);
        resp.Estado.Should().Be("EN_PROGRESO");
        (await _context.LecturaCorreoJobs.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task EncolarLectura_LecturaDeshabilitada_LanzaInvalidOperation()
    {
        _emisor.LecturaCorreoHabilitada = false;
        await _context.SaveChangesAsync();

        var act = async () => await _service.EncolarLecturaAsync(_emisor.Id);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*lectura de correo no está habilitada*");
    }

    [Fact]
    public async Task EncolarLectura_GmailNoConectado_LanzaInvalidOperation()
    {
        _emisor.GmailConectado = false;
        _emisor.GmailRefreshToken = null;
        await _context.SaveChangesAsync();

        var act = async () => await _service.EncolarLecturaAsync(_emisor.Id);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Gmail OAuth2 no está configurado*");
    }

    [Fact]
    public async Task EncolarLectura_EmisorInexistente_LanzaKeyNotFound()
    {
        var act = async () => await _service.EncolarLecturaAsync(emisorId: 99999);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task ObtenerEstadoLectura_SinJobs_DevuelveNull()
    {
        var estado = await _service.ObtenerEstadoLecturaAsync(_emisor.Id);
        estado.Should().BeNull();
    }

    [Fact]
    public async Task ObtenerEstadoLectura_VariosJobs_DevuelveElMasReciente()
    {
        _context.LecturaCorreoJobs.AddRange(
            new LecturaCorreoJob
            {
                EmisorId = _emisor.Id,
                Estado = EstadoLecturaCorreoJob.COMPLETADO,
                FechaCreacion = DateTime.UtcNow.AddHours(-2),
                DtesNuevos = 3
            },
            new LecturaCorreoJob
            {
                EmisorId = _emisor.Id,
                Estado = EstadoLecturaCorreoJob.EN_PROGRESO,
                FechaCreacion = DateTime.UtcNow.AddMinutes(-1),
                CorreosProcesados = 12,
                DtesNuevos = 5
            });
        await _context.SaveChangesAsync();

        var estado = await _service.ObtenerEstadoLecturaAsync(_emisor.Id);

        estado.Should().NotBeNull();
        estado!.Estado.Should().Be("EN_PROGRESO");
        estado.CorreosProcesados.Should().Be(12);
        estado.DtesNuevos.Should().Be(5);
        estado.Terminado.Should().BeFalse();
    }

    [Fact]
    public async Task ObtenerEstadoLectura_JobCompletado_TerminadoEsTrue()
    {
        _context.LecturaCorreoJobs.Add(new LecturaCorreoJob
        {
            EmisorId = _emisor.Id,
            Estado = EstadoLecturaCorreoJob.COMPLETADO,
            FechaCreacion = DateTime.UtcNow,
            FechaFin = DateTime.UtcNow,
            DtesNuevos = 7
        });
        await _context.SaveChangesAsync();

        var estado = await _service.ObtenerEstadoLecturaAsync(_emisor.Id);

        estado!.Terminado.Should().BeTrue();
        estado.Estado.Should().Be("COMPLETADO");
    }

    [Fact]
    public async Task EncolarLectura_SinRango_UsaMesEnCurso()
    {
        var resp = await _service.EncolarLecturaAsync(_emisor.Id);

        var job = await _context.LecturaCorreoJobs.SingleAsync();
        job.RangoDesde.Should().NotBeNull();
        job.RangoHasta.Should().NotBeNull();

        var hoy = DateTime.UtcNow;
        var inicioMes = new DateTime(hoy.Year, hoy.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        job.RangoDesde!.Value.Should().Be(inicioMes);
        job.RangoHasta!.Value.Should().Be(inicioMes.AddMonths(1));
        job.EsAutomatico.Should().BeFalse();
    }

    [Fact]
    public async Task EncolarLectura_ConRangoExplicito_LoPersisteTalCual()
    {
        var desde = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var hasta = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        await _service.EncolarLecturaAsync(_emisor.Id,
            usuarioId: null, rangoDesde: desde, rangoHasta: hasta);

        var job = await _context.LecturaCorreoJobs.SingleAsync();
        job.RangoDesde.Should().Be(desde);
        job.RangoHasta.Should().Be(hasta);
    }

    [Fact]
    public async Task EncolarLectura_FlagAutomatico_SePersiste()
    {
        await _service.EncolarLecturaAsync(_emisor.Id, esAutomatico: true);

        var job = await _context.LecturaCorreoJobs.SingleAsync();
        job.EsAutomatico.Should().BeTrue();
    }

    [Fact]
    public async Task ObtenerEstadoLectura_IncluyeRangoYEsAutomatico()
    {
        var desde = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        var hasta = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        _context.LecturaCorreoJobs.Add(new LecturaCorreoJob
        {
            EmisorId = _emisor.Id,
            Estado = EstadoLecturaCorreoJob.COMPLETADO,
            FechaCreacion = DateTime.UtcNow,
            FechaFin = DateTime.UtcNow,
            RangoDesde = desde,
            RangoHasta = hasta,
            EsAutomatico = true
        });
        await _context.SaveChangesAsync();

        var estado = await _service.ObtenerEstadoLecturaAsync(_emisor.Id);

        estado!.RangoDesde.Should().Be(desde);
        estado.RangoHasta.Should().Be(hasta);
        estado.EsAutomatico.Should().BeTrue();
    }

    [Fact]
    public async Task ObtenerEstadoLectura_IncluyeDtesIgnorados()
    {
        _context.LecturaCorreoJobs.Add(new LecturaCorreoJob
        {
            EmisorId = _emisor.Id,
            Estado = EstadoLecturaCorreoJob.COMPLETADO,
            FechaCreacion = DateTime.UtcNow,
            FechaFin = DateTime.UtcNow,
            DtesIgnorados = 4
        });
        await _context.SaveChangesAsync();

        var estado = await _service.ObtenerEstadoLecturaAsync(_emisor.Id);

        estado!.DtesIgnorados.Should().Be(4);
    }

    public void Dispose() => _context.Dispose();
}
