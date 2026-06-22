using FraFactu.Domain.Entities;
using FraFactu.Domain.Enums;
using FraFactu.Infrastructure.Jobs;
using FluentAssertions;
using Xunit;

namespace FraFactu.Tests.UnitTests.Jobs;

/// <summary>
/// F3.3 (G1): lógica de devaluación anual de mobiliario/equipo. Cubre la función
/// pura Devaluar() (sobre el item en memoria) y NextRunUtc() (programación del cron).
/// EjecutarAsync() depende de DI/EF y se valida por integración/cierre.
/// </summary>
public class DevaluacionAnualTests
{
    private static ProductoServicio Mobiliario(
        DateTime fechaAdquisicion,
        int aniosVidaUtil,
        decimal porcentaje,
        decimal valorActual,
        decimal? valorResidual = null,
        DateTime? ultimaDevaluacion = null) => new()
    {
        Id = 1,
        Codigo = "MOB-001",
        Nombre = "Escritorio",
        TipoInventario = TipoInventario.MobiliarioEquipo,
        PrecioCosto = valorActual,
        FechaAdquisicion = fechaAdquisicion,
        AniosVidaUtil = aniosVidaUtil,
        PorcentajeDevaluacionAnual = porcentaje,
        ValorActual = valorActual,
        ValorResidual = valorResidual,
        FechaUltimaDevaluacion = ultimaDevaluacion,
    };

    [Fact]
    public void Devaluar_SinFechaAdquisicion_NoModifica()
    {
        var p = Mobiliario(new DateTime(2025, 1, 1), 5, 10m, 1000m);
        p.FechaAdquisicion = null;

        var modificado = DevaluacionAnualBackgroundService.Devaluar(p, new DateTime(2027, 1, 1));

        modificado.Should().BeFalse();
        p.ValorActual.Should().Be(1000m);
    }

    [Fact]
    public void Devaluar_AnioMismoDeAdquisicion_NoModifica()
    {
        var p = Mobiliario(new DateTime(2026, 7, 15), 5, 10m, 1000m);

        var modificado = DevaluacionAnualBackgroundService.Devaluar(p, new DateTime(2026, 12, 31));

        modificado.Should().BeFalse();
        p.ValorActual.Should().Be(1000m);
    }

    [Fact]
    public void Devaluar_PrimerAnio_AplicaDecrecienteYMarcaFecha()
    {
        var p = Mobiliario(new DateTime(2025, 6, 1), 5, 10m, 1000m);
        var hoy = new DateTime(2026, 1, 1);

        var modificado = DevaluacionAnualBackgroundService.Devaluar(p, hoy);

        modificado.Should().BeTrue();
        p.ValorActual.Should().Be(900m);
        p.FechaUltimaDevaluacion.Should().Be(hoy);
    }

    [Fact]
    public void Devaluar_AnioYaProcesado_NoModifica()
    {
        var p = Mobiliario(new DateTime(2025, 1, 1), 5, 10m, 900m,
            ultimaDevaluacion: new DateTime(2026, 1, 1));

        var modificado = DevaluacionAnualBackgroundService.Devaluar(p, new DateTime(2026, 6, 15));

        modificado.Should().BeFalse();
        p.ValorActual.Should().Be(900m);
    }

    [Fact]
    public void Devaluar_FueraDeVidaUtil_NoModifica()
    {
        var p = Mobiliario(new DateTime(2022, 1, 1), 3, 10m, 729m);

        var modificado = DevaluacionAnualBackgroundService.Devaluar(p, new DateTime(2026, 1, 1));

        modificado.Should().BeFalse();
        p.ValorActual.Should().Be(729m);
    }

    [Fact]
    public void Devaluar_UltimoAnioDeVidaUtil_AunAplica()
    {
        var p = Mobiliario(new DateTime(2023, 6, 1), 3, 10m, 810m);
        var hoy = new DateTime(2026, 1, 1);

        var modificado = DevaluacionAnualBackgroundService.Devaluar(p, hoy);

        modificado.Should().BeTrue();
        p.ValorActual.Should().Be(729m);
    }

    [Fact]
    public void Devaluar_RespetaValorResidualComoPiso()
    {
        var p = Mobiliario(new DateTime(2025, 1, 1), 5, 90m, 100m, valorResidual: 200m);

        var modificado = DevaluacionAnualBackgroundService.Devaluar(p, new DateTime(2026, 1, 1));

        modificado.Should().BeTrue();
        p.ValorActual.Should().Be(200m);
    }

    [Fact]
    public void Devaluar_ResidualNullSeTrataComoCero()
    {
        var p = Mobiliario(new DateTime(2025, 1, 1), 5, 100m, 500m);

        var modificado = DevaluacionAnualBackgroundService.Devaluar(p, new DateTime(2026, 1, 1));

        modificado.Should().BeTrue();
        p.ValorActual.Should().Be(0m);
    }

    [Fact]
    public void Devaluar_ProductoNoMobiliario_NoModifica()
    {
        var p = Mobiliario(new DateTime(2025, 1, 1), 5, 10m, 1000m);
        p.TipoInventario = TipoInventario.Ventas;

        var modificado = DevaluacionAnualBackgroundService.Devaluar(p, new DateTime(2026, 1, 1));

        modificado.Should().BeFalse();
        p.ValorActual.Should().Be(1000m);
    }

    [Fact]
    public void NextRunUtc_AntesDelTrigger_DevuelveEsteAnio()
    {
        var ahora = new DateTime(2026, 1, 1, 2, 30, 0, DateTimeKind.Utc);

        var next = DevaluacionAnualBackgroundService.NextRunUtc(ahora);

        next.Should().Be(new DateTime(2026, 1, 1, 3, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void NextRunUtc_DespuesDelTrigger_DevuelveAnioSiguiente()
    {
        var ahora = new DateTime(2026, 5, 11, 14, 0, 0, DateTimeKind.Utc);

        var next = DevaluacionAnualBackgroundService.NextRunUtc(ahora);

        next.Should().Be(new DateTime(2027, 1, 1, 3, 0, 0, DateTimeKind.Utc));
    }
}
