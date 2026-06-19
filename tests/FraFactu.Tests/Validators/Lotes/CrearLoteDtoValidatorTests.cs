using Xunit;
using FluentAssertions;
using FraFactu.Application.DTOs.Lotes;
using FraFactu.Application.Validators.Lotes;

namespace FraFactu.Tests.Validators.Lotes;

/// <summary>
/// Tests unitarios para CrearLoteDtoValidator
/// </summary>
public class CrearLoteDtoValidatorTests
{
    private readonly CrearLoteDtoValidator _validator;

    public CrearLoteDtoValidatorTests()
    {
        _validator = new CrearLoteDtoValidator();
    }

    [Fact]
    public void Validar_LoteVacio_DebeSerInvalido()
    {
        // Arrange
        var dto = new CrearLoteDto
        {
            FacturaIds = new List<int>()
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "FacturaIds" &&
            e.ErrorMessage.Contains("al menos una factura"));
    }

    [Fact]
    public void Validar_LoteConMasDe100Facturas_DebeSerInvalido()
    {
        // Arrange
        var dto = new CrearLoteDto
        {
            FacturaIds = Enumerable.Range(1, 101).ToList()
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "FacturaIds" &&
            e.ErrorMessage.Contains("no puede exceder 100"));
    }

    [Fact]
    public void Validar_LoteConFacturasDuplicadas_DebeSerInvalido()
    {
        // Arrange
        var dto = new CrearLoteDto
        {
            FacturaIds = new List<int> { 1, 2, 3, 2, 4 } // 2 duplicado
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "FacturaIds" &&
            e.ErrorMessage.Contains("duplicadas"));
    }

    [Fact]
    public void Validar_LoteConIdsInvalidos_DebeSerInvalido()
    {
        // Arrange
        var dto = new CrearLoteDto
        {
            FacturaIds = new List<int> { 1, 2, -1, 0, 5 } // -1 y 0 inválidos
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "FacturaIds" &&
            e.ErrorMessage.Contains("válidos"));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(100)]
    public void Validar_LoteConCantidadValida_DebeSerValido(int cantidad)
    {
        // Arrange
        var dto = new CrearLoteDto
        {
            FacturaIds = Enumerable.Range(1, cantidad).ToList()
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validar_LoteParaContingencia_DebeSerValido()
    {
        // Arrange
        var dto = new CrearLoteDto
        {
            FacturaIds = new List<int> { 1, 2, 3 },
            EsContingencia = true
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
        dto.EsContingencia.Should().BeTrue();
    }
}
