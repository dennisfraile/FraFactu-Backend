using Xunit;
using FluentAssertions;
using FraFactu.Application.DTOs.Invalidacion;
using FraFactu.Application.Validators.Invalidacion;

namespace FraFactu.Tests.Validators;

/// <summary>
/// Tests unitarios simplificados para el validador de invalidación
/// Actualizado para usar IDs de catálogos
/// </summary>
public class InvalidacionValidatorTests
{
    private readonly AnularFacturaDtoValidator _validator;

    public InvalidacionValidatorTests()
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
            NombreResponsable = "Juan Pérez",
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
    public void Validar_TipoInvalido_DebeSerInvalido(int tipo)
    {
        // Arrange
        var dto = new AnularFacturaDto
        {
            TipoAnulacion = tipo,
            NombreResponsable = "Test",
            CatTipoDocResponsableId = 2,
            NumDocResponsable = "12345678-9",
            NombreSolicita = "Test",
            CatTipoDocSolicitaId = 2,
            NumDocSolicita = "98765432-1"
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validar_Tipo3SinMotivo_DebeSerInvalido()
    {
        // Arrange
        var dto = new AnularFacturaDto
        {
            TipoAnulacion = 3,
            MotivoAnulacion = null,
            NombreResponsable = "Test",
            CatTipoDocResponsableId = 2,
            NumDocResponsable = "12345678-9",
            NombreSolicita = "Test",
            CatTipoDocSolicitaId = 2,
            NumDocSolicita = "98765432-1"
        };

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
        var dto = new AnularFacturaDto
        {
            TipoAnulacion = 1,
            FacturaReemplazoId = null,
            NombreResponsable = "Test",
            CatTipoDocResponsableId = 2,
            NumDocResponsable = "12345678-9",
            NombreSolicita = "Test",
            CatTipoDocSolicitaId = 2,
            NumDocSolicita = "98765432-1"
        };

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
        var dto = new AnularFacturaDto
        {
            TipoAnulacion = 2,
            FacturaReemplazoId = 123,
            NombreResponsable = "Test",
            CatTipoDocResponsableId = 2,
            NumDocResponsable = "12345678-9",
            NombreSolicita = "Test",
            CatTipoDocSolicitaId = 2,
            NumDocSolicita = "98765432-1"
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validar_NombreMuyCorto_DebeSerInvalido()
    {
        // Arrange
        var dto = new AnularFacturaDto
        {
            TipoAnulacion = 2,
            NombreResponsable = "AB",
            CatTipoDocResponsableId = 2,
            NumDocResponsable = "12345678-9",
            NombreSolicita = "Test User",
            CatTipoDocSolicitaId = 2,
            NumDocSolicita = "98765432-1"
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(1)]  // NIT
    [InlineData(2)]  // DUI
    [InlineData(3)]  // Carnet
    [InlineData(4)]  // Pasaporte
    [InlineData(5)]  // Otro
    public void Validar_TiposDocumentoValidosIds_DebeSerValido(int tipoId)
    {
        // Arrange
        var dto = new AnularFacturaDto
        {
            TipoAnulacion = 2,
            NombreResponsable = "Test User",
            CatTipoDocResponsableId = tipoId,
            NumDocResponsable = "12345678-9",
            NombreSolicita = "Test User",
            CatTipoDocSolicitaId = tipoId,
            NumDocSolicita = "98765432-1"
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
