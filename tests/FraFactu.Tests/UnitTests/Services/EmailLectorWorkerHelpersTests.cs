using FraFactu.Application.Interfaces;
using FraFactu.Domain.Entities;
using FraFactu.Infrastructure.Services;
using FluentAssertions;
using Xunit;

namespace FraFactu.Tests.UnitTests.Services;

/// <summary>
/// F6: cubre los helpers <c>ResolverRango</c> y <c>MapearResultadoIngesta</c>
/// del <see cref="EmailLectorWorker"/>. El bucle Gmail (paginación con pageToken,
/// llamadas a la API y manejo de adjuntos) no se prueba en unit tests porque
/// requeriría un mock pesado del SDK de Google; se valida en UAT con un buzón
/// real.
/// </summary>
public class EmailLectorWorkerHelpersTests
{
    #region ResolverRango

    [Fact]
    public void ResolverRango_JobConRango_DevuelveEseRango()
    {
        var desde = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var hasta = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        var job = new LecturaCorreoJob { RangoDesde = desde, RangoHasta = hasta };

        var (resDesde, resHasta) = EmailLectorWorker.ResolverRango(job);

        resDesde.Should().Be(desde);
        resHasta.Should().Be(hasta);
    }

    [Fact]
    public void ResolverRango_JobSinRango_DevuelveMesEnCurso()
    {
        var job = new LecturaCorreoJob { RangoDesde = null, RangoHasta = null };

        var (desde, hasta) = EmailLectorWorker.ResolverRango(job);

        var hoy = DateTime.UtcNow;
        var inicioMes = new DateTime(hoy.Year, hoy.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        desde.Should().Be(inicioMes);
        hasta.Should().Be(inicioMes.AddMonths(1));
        desde.Kind.Should().Be(DateTimeKind.Utc);
        hasta.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void ResolverRango_JobConSoloDesde_CaePorDefaultAMesEnCurso()
    {
        // El método exige que ambos estén seteados; si solo viene uno, se trata
        // como sin rango (compatibilidad con jobs pre-F3 mal poblados).
        var job = new LecturaCorreoJob
        {
            RangoDesde = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            RangoHasta = null
        };

        var (desde, _) = EmailLectorWorker.ResolverRango(job);

        var hoy = DateTime.UtcNow;
        var inicioMes = new DateTime(hoy.Year, hoy.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        desde.Should().Be(inicioMes);
    }

    #endregion

    #region MapearResultadoIngesta

    [Fact]
    public void MapearResultado_Cargado_IncrementaEncontradosYNuevos()
    {
        var job = new LecturaCorreoJob();

        EmailLectorWorker.MapearResultadoIngesta(ResultadoIngestaDte.Cargado, job);

        job.DtesEncontrados.Should().Be(1);
        job.DtesNuevos.Should().Be(1);
        job.DtesDuplicados.Should().Be(0);
        job.DtesIgnorados.Should().Be(0);
    }

    [Fact]
    public void MapearResultado_Duplicado_IncrementaEncontradosYDuplicados()
    {
        var job = new LecturaCorreoJob();

        EmailLectorWorker.MapearResultadoIngesta(ResultadoIngestaDte.Duplicado, job);

        job.DtesEncontrados.Should().Be(1);
        job.DtesDuplicados.Should().Be(1);
        job.DtesNuevos.Should().Be(0);
        job.DtesIgnorados.Should().Be(0);
    }

    [Theory]
    [InlineData(ResultadoIngestaDte.NoEsCCF)]
    [InlineData(ResultadoIngestaDte.ReceptorInvalido)]
    [InlineData(ResultadoIngestaDte.ContenidoInvalido)]
    public void MapearResultado_Ignorados_NoSumanAEncontradosNiAErrores(ResultadoIngestaDte resultado)
    {
        var job = new LecturaCorreoJob();

        EmailLectorWorker.MapearResultadoIngesta(resultado, job);

        job.DtesIgnorados.Should().Be(1);
        // F4: ignorados NO deben contar como "encontrados" (no son del receptor)
        // ni como "errores" (no son técnicos), para no contaminar la UI.
        job.DtesEncontrados.Should().Be(0);
        job.DtesNuevos.Should().Be(0);
        job.DtesDuplicados.Should().Be(0);
        job.Errores.Should().Be(0);
    }

    [Fact]
    public void MapearResultado_MultiplesLlamadas_AcumulanCorrectamente()
    {
        var job = new LecturaCorreoJob();

        EmailLectorWorker.MapearResultadoIngesta(ResultadoIngestaDte.Cargado, job);
        EmailLectorWorker.MapearResultadoIngesta(ResultadoIngestaDte.Cargado, job);
        EmailLectorWorker.MapearResultadoIngesta(ResultadoIngestaDte.Duplicado, job);
        EmailLectorWorker.MapearResultadoIngesta(ResultadoIngestaDte.NoEsCCF, job);
        EmailLectorWorker.MapearResultadoIngesta(ResultadoIngestaDte.NoEsCCF, job);
        EmailLectorWorker.MapearResultadoIngesta(ResultadoIngestaDte.ContenidoInvalido, job);

        job.DtesEncontrados.Should().Be(3); // 2 cargados + 1 duplicado
        job.DtesNuevos.Should().Be(2);
        job.DtesDuplicados.Should().Be(1);
        job.DtesIgnorados.Should().Be(3); // 2 NoEsCCF + 1 ContenidoInvalido
    }

    #endregion
}
