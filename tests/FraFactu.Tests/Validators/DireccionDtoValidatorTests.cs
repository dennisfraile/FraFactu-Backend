using FluentValidation.TestHelper;
using Xunit;
using FraFactu.Application.DTOs.Facturas;
using FraFactu.Application.Validators.Facturas;

namespace FraFactu.Tests.Validators
{
    /// <summary>
    /// Tests para DireccionDtoValidator. Verifican P0-B (Normativa DTE V2.0):
    /// toda dirección de receptor debe incluir distrito (CAT-008).
    /// </summary>
    public class DireccionDtoValidatorTests
    {
        private readonly DireccionDtoValidator _validator = new();

        private static DireccionDto DireccionValida() => new()
        {
            Departamento = "06",
            Municipio = "14",
            Distrito = "13",
            Complemento = "Colonia Escalón, Calle 1"
        };

        [Fact]
        public void Validate_DireccionCompletaConDistrito_NoDeberiaGenerarErrores()
        {
            var result = _validator.TestValidate(DireccionValida());
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_SinDistrito_DeberiaGenerarError()
        {
            var dto = DireccionValida();
            dto.Distrito = null;

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.Distrito);
        }

        [Fact]
        public void Validate_DistritoVacio_DeberiaGenerarError()
        {
            var dto = DireccionValida();
            dto.Distrito = "";

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.Distrito);
        }

        [Theory]
        [InlineData("1")]    // un solo dígito
        [InlineData("123")]  // tres dígitos
        [InlineData("AB")]   // no numérico
        public void Validate_DistritoConFormatoInvalido_DeberiaGenerarError(string distrito)
        {
            var dto = DireccionValida();
            dto.Distrito = distrito;

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.Distrito);
        }
    }
}
