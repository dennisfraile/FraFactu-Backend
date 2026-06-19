using FraFactu.Application.Common.Settings;
using FraFactu.Application.DTOs.DtesRecibidos;
using FraFactu.Application.Validators.DtesRecibidos;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace FraFactu.Tests.UnitTests.Validators;

/// <summary>
/// F4: cubre el validator del body de POST /leer-correo y el helper que
/// convierte el rango a UTC para encolar el job.
/// </summary>
public class EncolarLecturaCorreoRequestDtoValidatorTests
{
    private static EncolarLecturaCorreoRequestDtoValidator NuevoValidator(int limite = 12)
        => new(Options.Create(new DtesRecibidosSettings { LimiteMesesManual = limite }));

    [Fact]
    public void BodyVacio_EsValido_DefaultMesEnCurso()
    {
        var v = NuevoValidator();
        var resultado = v.Validate(new EncolarLecturaCorreoRequestDto());

        resultado.IsValid.Should().BeTrue();
    }

    [Fact]
    public void AmbosMesesPresentes_RangoValido_EsValido()
    {
        var v = NuevoValidator();
        var resultado = v.Validate(new EncolarLecturaCorreoRequestDto
        {
            MesInicio = "2026-03",
            MesFin = "2026-05"
        });

        resultado.IsValid.Should().BeTrue();
    }

    [Fact]
    public void SoloMesInicio_Invalido()
    {
        var v = NuevoValidator();
        var resultado = v.Validate(new EncolarLecturaCorreoRequestDto { MesInicio = "2026-05" });

        resultado.IsValid.Should().BeFalse();
        resultado.Errors[0].ErrorMessage.Should().Contain("juntos");
    }

    [Fact]
    public void FormatoMalo_Invalido()
    {
        var v = NuevoValidator();
        var resultado = v.Validate(new EncolarLecturaCorreoRequestDto
        {
            MesInicio = "2026/05",
            MesFin = "2026-06"
        });

        resultado.IsValid.Should().BeFalse();
        resultado.Errors[0].ErrorMessage.Should().Contain("YYYY-MM");
    }

    [Fact]
    public void RangoInvertido_Invalido()
    {
        var v = NuevoValidator();
        var resultado = v.Validate(new EncolarLecturaCorreoRequestDto
        {
            MesInicio = "2026-06",
            MesFin = "2026-03"
        });

        resultado.IsValid.Should().BeFalse();
        resultado.Errors[0].ErrorMessage.Should().Contain("anterior");
    }

    [Fact]
    public void RangoSuperaLimite_Invalido()
    {
        var v = NuevoValidator(limite: 6);
        var resultado = v.Validate(new EncolarLecturaCorreoRequestDto
        {
            MesInicio = "2026-01",
            MesFin = "2026-12"
        });

        resultado.IsValid.Should().BeFalse();
        resultado.Errors[0].ErrorMessage.Should().Contain("no puede superar 6");
    }

    [Fact]
    public void AMesesUtc_BodyVacio_DevuelveNulls()
    {
        var (desde, hasta) = EncolarLecturaCorreoRequestDtoValidator.AMesesUtc(
            new EncolarLecturaCorreoRequestDto());

        desde.Should().BeNull();
        hasta.Should().BeNull();
    }

    [Fact]
    public void AMesesUtc_RangoValido_DevuelveInicioMesInclusivoYInicioMesPosteriorExclusivo()
    {
        var (desde, hasta) = EncolarLecturaCorreoRequestDtoValidator.AMesesUtc(
            new EncolarLecturaCorreoRequestDto { MesInicio = "2026-03", MesFin = "2026-05" });

        desde.Should().Be(new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc));
        hasta.Should().Be(new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void AMesesUtc_MismoMes_RangoDeUnMes()
    {
        var (desde, hasta) = EncolarLecturaCorreoRequestDtoValidator.AMesesUtc(
            new EncolarLecturaCorreoRequestDto { MesInicio = "2026-05", MesFin = "2026-05" });

        desde.Should().Be(new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc));
        hasta.Should().Be(new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));
    }
}
