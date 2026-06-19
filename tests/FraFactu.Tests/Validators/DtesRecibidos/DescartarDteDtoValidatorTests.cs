using Xunit;
using FluentAssertions;
using FraFactu.Application.DTOs.DtesRecibidos;
using FraFactu.Application.Validators.DtesRecibidos;

namespace FraFactu.Tests.Validators.DtesRecibidos;

public class DescartarDteDtoValidatorTests
{
    private readonly DescartarDteDtoValidator _validator;

    public DescartarDteDtoValidatorTests()
    {
        _validator = new DescartarDteDtoValidator();
    }

    [Fact]
    public void Validar_MotivoValido_DebeSerValido()
    {
        // Arrange
        var dto = new DescartarDteDto
        {
            Motivo = "El DTE no corresponde a esta empresa"
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validar_MotivoVacio_DebeSerInvalido(string? motivo)
    {
        // Arrange
        var dto = new DescartarDteDto
        {
            Motivo = motivo!
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Motivo");
    }

    [Theory]
    [InlineData("A")]
    [InlineData("AB")]
    [InlineData("ABCD")]
    public void Validar_MotivoMuyCorto_DebeSerInvalido(string motivo)
    {
        // Arrange
        var dto = new DescartarDteDto
        {
            Motivo = motivo
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Motivo");
    }

    [Fact]
    public void Validar_MotivoMuyLargo_DebeSerInvalido()
    {
        // Arrange
        var dto = new DescartarDteDto
        {
            Motivo = new string('X', 501)
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Motivo");
    }

    [Fact]
    public void Validar_MotivoConExactamente5Caracteres_DebeSerValido()
    {
        // Arrange
        var dto = new DescartarDteDto
        {
            Motivo = "ABCDE"
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validar_MotivoConExactamente500Caracteres_DebeSerValido()
    {
        // Arrange
        var dto = new DescartarDteDto
        {
            Motivo = new string('X', 500)
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
