using Xunit;
using FluentValidation.TestHelper;
using FraFactu.Application.Validators.Facturas;
using FraFactu.Application.DTOs.Facturas;

namespace FraFactu.Tests.UnitTests.Formulas
{
    /// <summary>
    /// Pruebas unitarias para fórmulas de cálculo de resumen de factura
    /// </summary>
    public class CalculosResumenTests
    {
        private readonly ResumenDtoValidator _validator;

        public CalculosResumenTests()
        {
            _validator = new ResumenDtoValidator();
        }

        #region Validación de Suma de Pagos

        [Fact]
        public void SumaPagos_IgualTotalPagar_ValidacionCorrecta()
        {
            // Arrange
            var resumen = new ResumenDto
            {
                TotalNoSuj = 0m,
                TotalExenta = 0m,
                TotalGravada = 100.00m,
                TotalPagar = 100.00m,
                TotalIva = 11.50m,
                SubTotal = 100.00m,
                TotalLetras = "CIEN DOLARES",
                CondicionOperacion = 1,
                Pagos = new List<PagoDto>
                {
                    new PagoDto { CatFormaPagoId = 1, Monto = 100.00m }
                }
            };

            // Act
            var result = _validator.TestValidate(resumen);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Pagos);
        }

        [Fact]
        public void SumaPagos_DiferenteTotalPagar_GeneraError()
        {
            // Arrange
            var resumen = new ResumenDto
            {
                TotalNoSuj = 0m,
                TotalExenta = 0m,
                TotalGravada = 100.00m,
                TotalPagar = 100.00m,
                TotalIva = 11.50m,
                SubTotal = 100.00m,
                TotalLetras = "CIEN DOLARES",
                CondicionOperacion = 1,
                Pagos = new List<PagoDto>
                {
                    new PagoDto { CatFormaPagoId = 1, Monto = 95.00m } // ERROR: Solo suma 95
                }
            };

            // Act
            var result = _validator.TestValidate(resumen);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Pagos);
        }

        [Fact]
        public void SumaPagos_MultiplesPagos_CalculaSumaCorrectamente()
        {
            // Arrange
            var resumen = new ResumenDto
            {
                TotalNoSuj = 0m,
                TotalExenta = 0m,
                TotalGravada = 100.00m,
                TotalPagar = 100.00m,
                TotalIva = 11.50m,
                SubTotal = 100.00m,
                TotalLetras = "CIEN DOLARES",
                CondicionOperacion = 1,
                Pagos = new List<PagoDto>
                {
                    new PagoDto { CatFormaPagoId = 1, Monto = 50.00m },
                    new PagoDto { CatFormaPagoId = 2, Monto = 30.00m },
                    new PagoDto { CatFormaPagoId = 3, Monto = 20.00m }
                }
            };

            // Act
            var result = _validator.TestValidate(resumen);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Pagos);
        }

        [Fact]
        public void SumaPagos_ConToleranciaRedondeo_NoGeneraError()
        {
            // Arrange - Diferencia de 0.01 (dentro de tolerancia)
            var resumen = new ResumenDto
            {
                TotalNoSuj = 0m,
                TotalExenta = 0m,
                TotalGravada = 100.00m,
                TotalPagar = 100.00m,
                TotalIva = 11.50m,
                SubTotal = 100.00m,
                TotalLetras = "CIEN DOLARES",
                CondicionOperacion = 1,
                Pagos = new List<PagoDto>
                {
                    new PagoDto { CatFormaPagoId = 1, Monto = 99.99m }
                }
            };

            // Act
            var result = _validator.TestValidate(resumen);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Pagos);
        }

        [Fact]
        public void SumaPagos_FueraDeTolerancia_GeneraError()
        {
            // Arrange - Diferencia de 0.05 (fuera de tolerancia)
            var resumen = new ResumenDto
            {
                TotalNoSuj = 0m,
                TotalExenta = 0m,
                TotalGravada = 100.00m,
                TotalPagar = 100.00m,
                TotalIva = 11.50m,
                SubTotal = 100.00m,
                TotalLetras = "CIEN DOLARES",
                CondicionOperacion = 1,
                Pagos = new List<PagoDto>
                {
                    new PagoDto { CatFormaPagoId = 1, Monto = 99.95m }
                }
            };

            // Act
            var result = _validator.TestValidate(resumen);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Pagos);
        }

        #endregion

        #region Validaciones Básicas

        [Fact]
        public void TotalPagar_Negativo_GeneraError()
        {
            // Arrange
            var resumen = new ResumenDto
            {
                TotalNoSuj = 0m,
                TotalExenta = 0m,
                TotalGravada = 100.00m,
                TotalPagar = -10.00m, // ERROR: No puede ser negativo
                TotalIva = 11.50m,
                SubTotal = 100.00m,
                TotalLetras = "CIEN DOLARES",
                CondicionOperacion = 1,
                Pagos = new List<PagoDto>
                {
                    new PagoDto { CatFormaPagoId = 1, Monto = 100.00m }
                }
            };

            // Act
            var result = _validator.TestValidate(resumen);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.TotalPagar)
                .WithErrorMessage("Total a pagar no puede ser negativo");
        }

        [Fact]
        public void CondicionOperacion_FueraDeRango_GeneraError()
        {
            // Arrange
            var resumen = new ResumenDto
            {
                TotalNoSuj = 0m,
                TotalExenta = 0m,
                TotalGravada = 100.00m,
                TotalPagar = 100.00m,
                TotalIva = 11.50m,
                SubTotal = 100.00m,
                TotalLetras = "CIEN DOLARES",
                CondicionOperacion = 5, // ERROR: Debe ser 1, 2 o 3
                Pagos = new List<PagoDto>
                {
                    new PagoDto { CatFormaPagoId = 1, Monto = 100.00m }
                }
            };

            // Act
            var result = _validator.TestValidate(resumen);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.CondicionOperacion)
                .WithErrorMessage("Condición de operación debe ser 1 (Contado), 2 (Crédito) o 3 (Otro)");
        }

        [Theory]
        [InlineData(1)] // Contado
        [InlineData(2)] // Crédito
        [InlineData(3)] // Otro
        public void CondicionOperacion_ValoresValidos_NoGeneraError(int condicion)
        {
            // Arrange
            var resumen = new ResumenDto
            {
                TotalNoSuj = 0m,
                TotalExenta = 0m,
                TotalGravada = 100.00m,
                TotalPagar = 100.00m,
                TotalIva = 11.50m,
                SubTotal = 100.00m,
                TotalLetras = "CIEN DOLARES",
                CondicionOperacion = condicion,
                Pagos = new List<PagoDto>
                {
                    new PagoDto { CatFormaPagoId = 1, Monto = 100.00m }
                }
            };

            // Act
            var result = _validator.TestValidate(resumen);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.CondicionOperacion);
        }

        [Fact]
        public void TotalLetras_Vacio_GeneraError()
        {
            // Arrange
            var resumen = new ResumenDto
            {
                TotalNoSuj = 0m,
                TotalExenta = 0m,
                TotalGravada = 100.00m,
                TotalPagar = 100.00m,
                TotalIva = 11.50m,
                SubTotal = 100.00m,
                TotalLetras = "", // ERROR: Requerido
                CondicionOperacion = 1,
                Pagos = new List<PagoDto>
                {
                    new PagoDto { CatFormaPagoId = 1, Monto = 100.00m }
                }
            };

            // Act
            var result = _validator.TestValidate(resumen);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.TotalLetras);
        }

        [Fact]
        public void SinPagos_GeneraError()
        {
            // Arrange
            var resumen = new ResumenDto
            {
                TotalNoSuj = 0m,
                TotalExenta = 0m,
                TotalGravada = 100.00m,
                TotalPagar = 100.00m,
                TotalIva = 11.50m,
                SubTotal = 100.00m,
                TotalLetras = "CIEN DOLARES",
                CondicionOperacion = 1,
                Pagos = new List<PagoDto>() // ERROR: Vacío
            };

            // Act
            var result = _validator.TestValidate(resumen);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Pagos)
                .WithErrorMessage("Debe especificar al menos una forma de pago");
        }

        #endregion

        #region Escenarios Reales

        [Fact]
        public void Resumen_FacturaCompleta_ValidacionCorrecta()
        {
            // Arrange - Factura real con todos los campos
            var resumen = new ResumenDto
            {
                TotalNoSuj = 0m,
                TotalExenta = 25.00m,
                TotalGravada = 100.00m,
                SubTotalVentas = 125.00m,
                DescuNoSuj = 0m,
                DescuExenta = 0m,
                DescuGravada = 5.00m,
                PorcentajeDescuento = 5m,
                TotalDescu = 5.00m,
                SubTotal = 120.00m,
                TotalIva = 10.62m,
                IvaPerci1 = 0m,
                IvaRete1 = 0m,
                ReteRenta = 0m,
                MontoTotalOperacion = 130.62m,
                TotalNoGravado = 0m,
                TotalPagar = 130.62m,
                SaldoFavor = 0m,
                TotalLetras = "CIENTO TREINTA DOLARES CON SESENTA Y DOS CENTAVOS",
                CondicionOperacion = 1,
                Pagos = new List<PagoDto>
                {
                    new PagoDto { CatFormaPagoId = 1, Monto = 100.00m },
                    new PagoDto { CatFormaPagoId = 3, Monto = 30.62m }
                }
            };

            // Act
            var result = _validator.TestValidate(resumen);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Resumen_FacturaMixta_ConTodosLosTiposDeVenta()
        {
            // Arrange
            var resumen = new ResumenDto
            {
                TotalNoSuj = 10.00m,
                TotalExenta = 20.00m,
                TotalGravada = 70.00m,
                SubTotal = 100.00m,
                TotalIva = 8.05m,
                TotalPagar = 100.00m,
                TotalLetras = "CIEN DOLARES",
                CondicionOperacion = 1,
                Pagos = new List<PagoDto>
                {
                    new PagoDto { CatFormaPagoId = 1, Monto = 100.00m }
                }
            };

            // Act
            var result = _validator.TestValidate(resumen);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Pagos);
        }

        #endregion
    }
}
