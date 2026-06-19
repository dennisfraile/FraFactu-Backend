using Xunit;
using FluentAssertions;
using FraFactu.Application.DTOs.Invalidacion;
using FraFactu.Application.Validators.Invalidacion;

namespace FraFactu.Tests.Validators;

/// <summary>
/// Tests unitarios para AnularFacturaDto
/// Actualizado para usar IDs de catálogos
/// </summary>
public class AnularFacturaDtoValidatorTests
{
    private readonly AnularFacturaDtoValidator _validator;

    public AnularFacturaDtoValidatorTests()
    {
        _validator = new AnularFacturaDtoValidator();
    }

    [Fact]
    public void Validar_DatosCompletos_DebeSerValido()
    {
        // Arrange
        var dto = new AnularFacturaDto
        {
            TipoAnulacion = 2,
            NombreResponsable = "Juan Pérez López",
            CatTipoDocResponsableId = 2, // DUI
            NumDocResponsable = "12345678-9",
            NombreSolicita = "María González",
            CatTipoDocSolicitaId = 2, // DUI
            NumDocSolicita = "98765432-1"
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(-1)]
    public void Validar_TipoAnulacionInvalido_DebeSerInvalido(int tipo)
    {
        // Arrange
        var dto = CrearDtoValido();
        dto.TipoAnulacion = tipo;

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validar_Tipo3SinMotivo_DebeSerInvalido()
    {
        // Arrange
        var dto = CrearDtoValido();
        dto.TipoAnulacion = 3;
        dto.MotivoAnulacion = null;

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MotivoAnulacion");
    }

    [Fact]
    public void Validar_Tipo1SinReemplazo_DebeSerInvalido()
    {
        // Arrange
        var dto = CrearDtoValido();
        dto.TipoAnulacion = 1;
        dto.FacturaReemplazoId = null;

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FacturaReemplazoId");
    }

    [Fact]
    public void Validar_Tipo2ConReemplazo_DebeSerInvalido()
    {
        // Arrange
        var dto = CrearDtoValido();
        dto.TipoAnulacion = 2;
        dto.FacturaReemplazoId = 123;

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("A")]
    [InlineData("AB")]
    [InlineData("ABC")]
    [InlineData("ABCD")]
    public void Validar_NombreMuyCorto_DebeSerInvalido(string nombre)
    {
        // Arrange
        var dto = CrearDtoValido();
        dto.NombreResponsable = nombre;

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(1)]  // NIT
    [InlineData(2)]  // DUI
    [InlineData(3)]  // Carnet residente
    [InlineData(4)]  // Pasaporte
    [InlineData(5)]  // Otro  
    public void Validar_TiposDocumentoValidosIds_DebeSerValido(int tipoId)
    {
        // Arrange
        var dto = CrearDtoValido();
        dto.CatTipoDocResponsableId = tipoId;
        dto.CatTipoDocSolicitaId = tipoId;

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(99)]
    public void Validar_TipoDocumentoInvalidoId_DebeSerInvalido(int tipoId)
    {
        // Arrange
        var dto = CrearDtoValido();
        dto.CatTipoDocResponsableId = tipoId;

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CatTipoDocResponsableId");
    }

    // Helper
    private AnularFacturaDto CrearDtoValido()
    {
        return new AnularFacturaDto
        {
            TipoAnulacion = 2,
            NombreResponsable = "Juan Pérez",
            CatTipoDocResponsableId = 2, // DUI por defecto
            NumDocResponsable = "12345678-9",
            NombreSolicita = "María González",
            CatTipoDocSolicitaId = 2, // DUI por defecto
            NumDocSolicita = "98765432-1"
        };
    }
}
