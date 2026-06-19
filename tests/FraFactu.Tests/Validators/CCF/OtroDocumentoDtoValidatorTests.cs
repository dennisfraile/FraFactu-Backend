using FluentValidation.TestHelper;
using Xunit;
using FraFactu.Application.DTOs.Facturas;
using FraFactu.Application.Validators.Facturas;

namespace FraFactu.Tests.Validators.CCF
{
    /// <summary>
    /// Tests para OtroDocumentoDtoValidator
    /// </summary>
    public class OtroDocumentoDtoValidatorTests
    {
        private readonly OtroDocumentoDtoValidator _validator;

        public OtroDocumentoDtoValidatorTests()
        {
            _validator = new OtroDocumentoDtoValidator();
        }

        [Fact]
        public void Validate_DocumentoGeneral_NoDeberiaGenerarErrores()
        {
            // Arrange - CodDocAsociado != 3 (documento general)
            var dto = new OtroDocumentoDto
            {
                CodDocAsociado = 1,
                DescDocumento = "Orden de Compra",
                DetalleDocumento = "OC-2025-001 del 10/12/2025",
                Medico = null
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_ServicioMedico_NoDeberiaGenerarErrores()
        {
            // Arrange - CodDocAsociado = 3 (servicio médico)
            var dto = new OtroDocumentoDto
            {
                CodDocAsociado = 3,
                DescDocumento = null,
                DetalleDocumento = null,
                Medico = new MedicoDto
                {
                    Nombre = "Dr. Juan Pérez",
                    Nit = "012345678",
                    TipoServicio = 1
                }
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_CodDocAsociadoFueraDeRango_DeberiaGenerarError()
        {
            // Arrange
            var dto = new OtroDocumentoDto
            {
                CodDocAsociado = 5, // Fuera de rango (debe ser 1-4)
                DescDocumento = "Test",
                DetalleDocumento = "Detalle test"
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.CodDocAsociado);
        }

        [Fact]
        public void Validate_ServicioMedicoSinMedico_DeberiaGenerarError()
        {
            // Arrange - CodDocAsociado = 3 pero sin Medico
            var dto = new OtroDocumentoDto
            {
                CodDocAsociado = 3,
                DescDocumento = null,
                DetalleDocumento = null,
                Medico = null
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Medico)
                .WithErrorMessage("El campo Medico es requerido cuando CodDocAsociado es 3");
        }

        [Fact]
        public void Validate_ServicioMedicoConDescDocumento_DeberiaGenerarError()
        {
            // Arrange - CodDocAsociado = 3 con DescDocumento (debe ser null)
            var dto = new OtroDocumentoDto
            {
                CodDocAsociado = 3,
                DescDocumento = "No debería estar",
                DetalleDocumento = null,
                Medico = new MedicoDto
                {
                    Nombre = "Dr. Juan",
                    Nit = "012345678",
                    TipoServicio = 1
                }
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.DescDocumento);
        }

        [Fact]
        public void Validate_DocumentoGeneralSinDescDocumento_DeberiaGenerarError()
        {
            // Arrange - CodDocAsociado != 3 sin DescDocumento
            var dto = new OtroDocumentoDto
            {
                CodDocAsociado = 1,
                DescDocumento = null,
                DetalleDocumento = "Detalle",
                Medico = null
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.DescDocumento);
        }

        [Fact]
        public void Validate_DocumentoGeneralSinDetalleDocumento_DeberiaGenerarError()
        {
            // Arrange - CodDocAsociado != 3 sin DetalleDocumento
            var dto = new OtroDocumentoDto
            {
                CodDocAsociado = 2,
                DescDocumento = "Descripción",
                DetalleDocumento = null,
                Medico = null
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.DetalleDocumento);
        }

        [Fact]
        public void Validate_DocumentoGeneralConMedico_DeberiaGenerarError()
        {
            // Arrange - CodDocAsociado != 3 con Medico (debe ser null)
            var dto = new OtroDocumentoDto
            {
                CodDocAsociado = 1,
                DescDocumento = "Descripción",
                DetalleDocumento = "Detalle",
                Medico = new MedicoDto
                {
                    Nombre = "Dr. Juan",
                    Nit = "012345678",
                    TipoServicio = 1
                }
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Medico);
        }

        [Fact]
        public void Validate_DescDocumentoMuyLargo_DeberiaGenerarError()
        {
            // Arrange
            var dto = new OtroDocumentoDto
            {
                CodDocAsociado = 1,
                DescDocumento = new string('A', 101), // 101 caracteres (máximo 100)
                DetalleDocumento = "Detalle válido",
                Medico = null
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.DescDocumento);
        }

        [Fact]
        public void Validate_DetalleDocumentoMuyLargo_DeberiaGenerarError()
        {
            // Arrange
            var dto = new OtroDocumentoDto
            {
                CodDocAsociado = 2,
                DescDocumento = "Descripción válida",
                DetalleDocumento = new string('A', 301), // 301 caracteres (máximo 300)
                Medico = null
            };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.DetalleDocumento);
        }
    }
}
