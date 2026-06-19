using FraFactu.Application.Services;
using FluentAssertions;
using Xunit;

namespace FraFactu.Tests.Services;

public class MoraServiceTests
{
    private readonly MoraService _sut = new();

    [Fact]
    public void SinAtraso_NoCobraMora()
    {
        var r = _sut.CalcularMora(113m, new DateTime(2026, 6, 10), new DateTime(2026, 6, 10), 0.03m, 3, true);
        r.Should().Be(0m);
    }

    [Fact]
    public void DentroDeGracia_NoCobraMora()
    {
        // 2 días de atraso, gracia 3 → 0
        var r = _sut.CalcularMora(113m, new DateTime(2026, 6, 10), new DateTime(2026, 6, 12), 0.03m, 3, true);
        r.Should().Be(0m);
    }

    [Fact]
    public void MoraDeshabilitada_NoCobra()
    {
        var r = _sut.CalcularMora(113m, new DateTime(2026, 6, 10), new DateTime(2026, 7, 10), 0.03m, 3, false);
        r.Should().Be(0m);
    }

    [Fact]
    public void ConAtraso_CalculaInteresProporcional()
    {
        // 13 días de atraso, gracia 3 → 10 días efectivos
        // tasaDiaria = 0.03/30 = 0.001; interes = 113 * 0.001 * 10 = 1.13
        var r = _sut.CalcularMora(113m, new DateTime(2026, 6, 10), new DateTime(2026, 6, 23), 0.03m, 3, true);
        r.Should().Be(1.13m);
    }
}
