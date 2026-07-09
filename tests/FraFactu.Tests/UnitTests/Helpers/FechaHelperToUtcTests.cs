using FraFactu.Infrastructure.Helpers;

namespace FraFactu.Tests.UnitTests.Helpers;

/// <summary>
/// Pruebas unitarias del helper compartido FechaHelper.ToUtc.
/// Verifica que los tres casos de DateTimeKind se normalicen a UTC antes de
/// llegar a EF/Npgsql (que rechaza Kind=Unspecified con timestamptz).
/// </summary>
public class FechaHelperToUtcTests
{
    [Fact]
    public void ToUtc_CuandoKindEsUtc_DevuelveElMismoValor()
    {
        var original = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        var result = FechaHelper.ToUtc(original);

        Assert.Equal(DateTimeKind.Utc, result.Kind);
        Assert.Equal(original, result);
    }

    [Fact]
    public void ToUtc_CuandoKindEsUnspecified_AsignaKindUtcSinCambiarElValor()
    {
        // Simula lo que ASP.NET devuelve al bindear "?fechaInicio=2026-06-01" desde query string
        var unspecified = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Unspecified);

        var result = FechaHelper.ToUtc(unspecified);

        Assert.Equal(DateTimeKind.Utc, result.Kind);
        Assert.Equal(unspecified.Ticks, result.Ticks);
    }

    [Fact]
    public void ToUtc_CuandoKindEsLocal_ConvierteAUtc()
    {
        var local = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Local);

        var result = FechaHelper.ToUtc(local);

        Assert.Equal(DateTimeKind.Utc, result.Kind);
        Assert.Equal(local.ToUniversalTime(), result);
    }

    [Fact]
    public void ToUtc_EsIdempotente_SiYaEsUtc()
    {
        var utc = DateTime.UtcNow;

        var result1 = FechaHelper.ToUtc(utc);
        var result2 = FechaHelper.ToUtc(result1);

        Assert.Equal(result1, result2);
        Assert.Equal(DateTimeKind.Utc, result2.Kind);
    }
}
