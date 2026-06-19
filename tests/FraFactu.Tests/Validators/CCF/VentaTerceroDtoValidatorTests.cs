using FluentValidation.TestHelper;
using Xunit;
using FraFactu.Application.DTOs.Facturas;
using FraFactu.Application.Validators.Facturas;

namespace FraFactu.Tests.Validators.CCF
{
    /// <summary>
    /// Tests para VentaTerceroDtoValidator
    /// </summary>
    public class VentaTerceroDtoValidatorTests
    {
        private readonly VentaTerceroDtoValidator _validator;

        public VentaTerceroDtoValidatorTests()
        {
            _validator = new VentaTerceroDtoValidator();
        }

        [Fact]
        public void Validate_VentaTerceroValido_NoDeberiaGenerarErrores()
        {
            // Arrange
            var dto = new VentaTerceroDto
            {
                Nit = "012345678", // 9 dígitos
                Nombre = "Empresa Tercero SA de CV"
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_VentaTerceroConNit14Digitos_NoDeberiaGenerarErrores()
        {
            // Arrange
            var dto = new VentaTerceroDto
            {
                Nit = "01234567890123", // 14 dígitos
                Nombre = "Empresa Tercero SA de CV"
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_NitVacio_DeberiaGenerarError()
        {
            // Arrange
            var dto = new VentaTerceroDto
            {
                Nit = "",
                Nombre = "Empresa Tercero"
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Nit)
                .WithErrorMessage("El NIT del tercero es requerido");
        }

        [Fact]
        public void Validate_NitConLongitudInvalida_DeberiaGenerarError()
        {
            // Arrange
            var dto = new VentaTerceroDto
            {
                Nit = "12345", // Solo 5 dígitos (debe ser 9 o 14)
                Nombre = "Empresa Tercero"
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Nit)
                .WithErrorMessage("El NIT debe tener 9 o 14 dígitos");
        }

        [Fact]
        public void Validate_NitConLetras_DeberiaGenerarError()
        {
            // Arrange
            var dto = new VentaTerceroDto
            {
                Nit = "ABC123456",
                Nombre = "Empresa Tercero"
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Nit);
        }

        [Fact]
        public void Validate_NombreVacio_DeberiaGenerarError()
        {
            // Arrange
            var dto = new VentaTerceroDto
            {
                Nit = "012345678",
                Nombre = ""
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Nombre)
                .WithErrorMessage("El nombre del tercero es requerido");
        }

        [Fact]
        public void Validate_NombreMuyCorto_DeberiaGenerarError()
        {
            // Arrange
            var dto = new VentaTerceroDto
            {
                Nit = "012345678",
                Nombre = "AB" // Solo 2 caracteres (mínimo 3)
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Nombre);
        }

        [Fact]
        public void Validate_NombreMuyLargo_DeberiaGenerarError()
        {
            // Arrange
            var dto = new VentaTerceroDto
            {
                Nit = "012345678",
                Nombre = new string('A', 201) // 201 caracteres (máximo 200)
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Nombre);
        }
    }
}
