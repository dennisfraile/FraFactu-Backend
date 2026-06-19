using FluentValidation.TestHelper;
using Xunit;
using FraFactu.Application.DTOs.Facturas;
using FraFactu.Application.Validators.Facturas;

namespace FraFactu.Tests.Validators.CCF
{
    /// <summary>
    /// Tests para TributoResumenDtoValidator
    /// </summary>
    public class TributoResumenDtoValidatorTests
    {
        private readonly TributoResumenDtoValidator _validator;

        public TributoResumenDtoValidatorTests()
        {
            _validator = new TributoResumenDtoValidator();
        }

        [Fact]
        public void Validate_TributoValido_NoDeberiaGenerarErrores()
        {
            // Arrange
            var dto = new TributoResumenDto
            {
                Codigo = "20",
                Descripcion = "Impuesto al Valor Agregado 13%",
                Valor = 13.50m
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_CodigoVacio_DeberiaGenerarError()
        {
            // Arrange
            var dto = new TributoResumenDto
            {
                Codigo = "",
                Descripcion = "IVA 13%",
                Valor = 10.00m
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Codigo);
        }

        [Fact]
        public void Validate_CodigoConUnCaracter_DeberiaGenerarError()
        {
            // Arrange
            var dto = new TributoResumenDto
            {
                Codigo = "2", // Solo 1 carácter (debe ser exactamente 2)
                Descripcion = "IVA 13%",
                Valor = 10.00m
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Codigo);
        }

        [Fact]
        public void Validate_CodigoConTresCaracteres_DeberiaGenerarError()
        {
            // Arrange
            var dto = new TributoResumenDto
            {
                Codigo = "ABC", // 3 caracteres (debe ser exactamente 2)
                Descripcion = "IVA 13%",
                Valor = 10.00m
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Codigo);
        }

        [Fact]
        public void Validate_DescripcionVacia_DeberiaGenerarError()
        {
            // Arrange
            var dto = new TributoResumenDto
            {
                Codigo = "20",
                Descripcion = "",
                Valor = 10.00m
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Descripcion);
        }

        [Fact]
        public void Validate_DescripcionMuyCorta_DeberiaGenerarError()
        {
            // Arrange
            var dto = new TributoResumenDto
            {
                Codigo = "20",
                Descripcion = "A", // Solo 1 carácter (mínimo 2)
                Valor = 10.00m
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Descripcion);
        }

        [Fact]
        public void Validate_DescripcionMuyLarga_DeberiaGenerarError()
        {
            // Arrange
            var dto = new TributoResumenDto
            {
                Codigo = "20",
                Descripcion = new string('A', 151), // 151 caracteres (máximo 150)
                Valor = 10.00m
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Descripcion);
        }

        [Fact]
        public void Validate_ValorNegativo_DeberiaGenerarError()
        {
            // Arrange
            var dto = new TributoResumenDto
            {
                Codigo = "20",
                Descripcion = "IVA 13%",
                Valor = -10.00m
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Valor);
        }

        [Fact]
        public void Validate_ValorCero_NoDeberiaGenerarErrores()
        {
            // Arrange
            var dto = new TributoResumenDto
            {
                Codigo = "20",
                Descripcion = "IVA 13%",
                Valor = 0m
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Valor);
        }

        [Fact]
        public void Validate_ValorMuyGrande_DeberiaGenerarError()
        {
            // Arrange
            var dto = new TributoResumenDto
            {
                Codigo = "20",
                Descripcion = "IVA 13%",
                Valor = 100000000000m // 100 billones (excede límite)
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Valor);
        }

        [Theory]
        [InlineData("20", "IVA 13%", 13.50)]
        [InlineData("C3", "FOVIAL", 0.20)]
        [InlineData("D1", "COTRANS", 0.10)]
        public void Validate_TributosComunes_NoDeberiaGenerarErrores(string codigo, string descripcion, decimal valor)
        {
            // Arrange
            var dto = new TributoResumenDto
            {
                Codigo = codigo,
                Descripcion = descripcion,
                Valor = valor
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}
