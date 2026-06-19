using FluentValidation.TestHelper;
using Xunit;
using FraFactu.Application.DTOs.Facturas;
using FraFactu.Application.Validators.Facturas;

namespace FraFactu.Tests.Validators.CCF
{
    /// <summary>
    /// Tests para MedicoDtoValidator
    /// </summary>
    public class MedicoDtoValidatorTests
    {
        private readonly MedicoDtoValidator _validator;

        public MedicoDtoValidatorTests()
        {
            _validator = new MedicoDtoValidator();
        }

        [Fact]
        public void Validate_MedicoValidoConNit_NoDeberiaGenerarErrores()
        {
            // Arrange
            var dto = new MedicoDto
            {
                Nombre = "Dr. Juan Pérez",
                Nit = "012345678",
                TipoServicio = 1
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_MedicoValidoConDocIdentificacion_NoDeberiaGenerarErrores()
        {
            // Arrange
            var dto = new MedicoDto
            {
                Nombre = "Dr. Juan Pérez",
                DocIdentificacion = "PASS12345",
                TipoServicio = 2
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_NombreVacio_DeberiaGenerarError()
        {
            // Arrange
            var dto = new MedicoDto
            {
                Nombre = "",
                Nit = "012345678",
                TipoServicio = 1
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
            var dto = new MedicoDto
            {
                Nombre = new string('A', 101), // 101 caracteres (máximo 100)
                Nit = "012345678",
                TipoServicio = 1
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Nombre);
        }

        [Fact]
        public void Validate_NitConLongitudInvalida_DeberiaGenerarError()
        {
            // Arrange
            var dto = new MedicoDto
            {
                Nombre = "Dr. Juan Pérez",
                Nit = "12345", // Solo 5 dígitos
                TipoServicio = 1
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Nit);
        }

        [Fact]
        public void Validate_SinNitNiDocIdentificacion_DeberiaGenerarError()
        {
            // Arrange
            var dto = new MedicoDto
            {
                Nombre = "Dr. Juan Pérez",
                Nit = null,
                DocIdentificacion = null,
                TipoServicio = 1
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor("Identificación");
        }

        [Fact]
        public void Validate_DocIdentificacionMuyCorto_DeberiaGenerarError()
        {
            // Arrange
            var dto = new MedicoDto
            {
                Nombre = "Dr. Juan Pérez",
                DocIdentificacion = "A", // Solo 1 carácter (mínimo 2)
                TipoServicio = 1
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.DocIdentificacion);
        }

        [Fact]
        public void Validate_TipoServicioFueraDeRango_DeberiaGenerarError()
        {
            // Arrange
            var dto = new MedicoDto
            {
                Nombre = "Dr. Juan Pérez",
                Nit = "012345678",
                TipoServicio = 7 // Fuera de rango (debe ser 1-6)
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.TipoServicio);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        public void Validate_TipoServicioValido_NoDeberiaGenerarErrores(int tipoServicio)
        {
            // Arrange
            var dto = new MedicoDto
            {
                Nombre = "Dr. Juan Pérez",
                Nit = "012345678",
                TipoServicio = tipoServicio
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.TipoServicio);
        }
    }
}
