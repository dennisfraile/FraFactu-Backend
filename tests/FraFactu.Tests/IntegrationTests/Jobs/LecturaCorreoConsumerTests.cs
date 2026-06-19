using FraFactu.Application.Interfaces;
using FraFactu.Domain.Entities;
using FraFactu.Domain.Enums;
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
/// F6: cubre el método estático <c>ProcesarSiguienteJobAsync</c> del
/// <see cref="LecturaCorreoConsumer"/>. El bucle del BackgroundService no se
/// prueba acá; la unidad de trabajo (un pickup → un job procesado) se invoca
/// directo con un <c>IEmailLectorWorker</c> mockeado para validar transiciones
/// de estado y manejo de excepciones.
/// </summary>
public class LecturaCorreoConsumerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IEmailLectorWorker> _worker = new();
    private readonly Emisor _emisor;

    public LecturaCorreoConsumerTests()
    {
        _context = TestDatabaseHelper.CreateInMemoryContext();
        TestDatabaseHelper.SeedTestData(_context);
        _emisor = _context.Emisores.First();
    }

    private LecturaCorreoJob SeedJob(
        EstadoLecturaCorreoJob estado = EstadoLecturaCorreoJob.ENCOLADO,
        DateTime? fechaCreacion = null)
    {
        var job = new LecturaCorreoJob
        {
            EmisorId = _emisor.Id,
            Estado = estado,
            FechaCreacion = fechaCreacion ?? DateTime.UtcNow
        };
        _context.LecturaCorreoJobs.Add(job);
        _context.SaveChanges();
        return job;
    }

    [Fact]
    public async Task ProcesarSiguienteJob_SinJobsEncolados_NoInvocaWorker()
    {
        await LecturaCorreoConsumer.ProcesarSiguienteJobAsync(
            _context, _worker.Object, NullLogger.Instance, CancellationToken.None);

        _worker.Verify(w => w.EjecutarAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcesarSiguienteJob_JobEncolado_TransicionaACompletado()
    {
        var job = SeedJob();
        _worker.Setup(w => w.EjecutarAsync(job.Id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await LecturaCorreoConsumer.ProcesarSiguienteJobAsync(
            _context, _worker.Object, NullLogger.Instance, CancellationToken.None);

        var persistido = await _context.LecturaCorreoJobs.SingleAsync();
        persistido.Estado.Should().Be(EstadoLecturaCorreoJob.COMPLETADO);
        persistido.FechaInicio.Should().NotBeNull();
        persistido.FechaFin.Should().NotBeNull();
        persistido.MensajeError.Should().BeNull();
        _worker.Verify(w => w.EjecutarAsync(job.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcesarSiguienteJob_VariosEncolados_ProcesaElMasAntiguo()
    {
        var viejo = SeedJob(fechaCreacion: DateTime.UtcNow.AddMinutes(-10));
        var nuevo = SeedJob(fechaCreacion: DateTime.UtcNow);
        _worker.Setup(w => w.EjecutarAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await LecturaCorreoConsumer.ProcesarSiguienteJobAsync(
            _context, _worker.Object, NullLogger.Instance, CancellationToken.None);

        _worker.Verify(w => w.EjecutarAsync(viejo.Id, It.IsAny<CancellationToken>()), Times.Once);
        _worker.Verify(w => w.EjecutarAsync(nuevo.Id, It.IsAny<CancellationToken>()), Times.Never);

        var nuevoPersistido = await _context.LecturaCorreoJobs.SingleAsync(j => j.Id == nuevo.Id);
        nuevoPersistido.Estado.Should().Be(EstadoLecturaCorreoJob.ENCOLADO);
    }

    [Fact]
    public async Task ProcesarSiguienteJob_NoEncolado_NoSeProcesa()
    {
        // EN_PROGRESO, COMPLETADO y FALLIDO no deben re-procesarse.
        SeedJob(EstadoLecturaCorreoJob.EN_PROGRESO);
        SeedJob(EstadoLecturaCorreoJob.COMPLETADO);
        SeedJob(EstadoLecturaCorreoJob.FALLIDO);

        await LecturaCorreoConsumer.ProcesarSiguienteJobAsync(
            _context, _worker.Object, NullLogger.Instance, CancellationToken.None);

        _worker.Verify(w => w.EjecutarAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcesarSiguienteJob_WorkerLanzaExcepcion_MarcaFallidoConMensaje()
    {
        var job = SeedJob();
        _worker.Setup(w => w.EjecutarAsync(job.Id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Gmail expiró"));

        await LecturaCorreoConsumer.ProcesarSiguienteJobAsync(
            _context, _worker.Object, NullLogger.Instance, CancellationToken.None);

        var persistido = await _context.LecturaCorreoJobs.SingleAsync();
        persistido.Estado.Should().Be(EstadoLecturaCorreoJob.FALLIDO);
        persistido.FechaInicio.Should().NotBeNull();
        persistido.FechaFin.Should().NotBeNull();
        persistido.MensajeError.Should().Be("Gmail expiró");
    }

    [Fact]
    public async Task ProcesarSiguienteJob_MensajeErrorMuyLargo_LoTrunca()
    {
        var job = SeedJob();
        var mensajeLargo = new string('x', 1500);
        _worker.Setup(w => w.EjecutarAsync(job.Id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(mensajeLargo));

        await LecturaCorreoConsumer.ProcesarSiguienteJobAsync(
            _context, _worker.Object, NullLogger.Instance, CancellationToken.None);

        var persistido = await _context.LecturaCorreoJobs.SingleAsync();
        persistido.Estado.Should().Be(EstadoLecturaCorreoJob.FALLIDO);
        persistido.MensajeError.Should().HaveLength(1000);
        persistido.MensajeError.Should().Be(new string('x', 1000));
    }

    [Fact]
    public async Task ProcesarSiguienteJob_CancellationDuranteWorker_NoMarcaFallido()
    {
        var job = SeedJob();
        var cts = new CancellationTokenSource();

        // Shutdown realista: el worker ya empezó (job EN_PROGRESO) y a mitad
        // de ejecución se cancela el token. El consumer debe re-lanzar y NO
        // marcar el job como FALLIDO, para que se note al reanudar.
        _worker.Setup(w => w.EjecutarAsync(job.Id, It.IsAny<CancellationToken>()))
            .Callback(() => cts.Cancel())
            .ThrowsAsync(new OperationCanceledException(cts.Token));

        var act = async () => await LecturaCorreoConsumer.ProcesarSiguienteJobAsync(
            _context, _worker.Object, NullLogger.Instance, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();

        var persistido = await _context.LecturaCorreoJobs.SingleAsync();
        persistido.Estado.Should().Be(EstadoLecturaCorreoJob.EN_PROGRESO);
        persistido.MensajeError.Should().BeNull();
        persistido.FechaFin.Should().BeNull();
    }

    public void Dispose() => _context.Dispose();
}
