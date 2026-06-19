using Xunit;
using FluentValidation.TestHelper;
using FraFactu.Application.Validators.Facturas;
using FraFactu.Application.DTOs.Facturas;

namespace FraFactu.Tests.UnitTests.Formulas
{
    /// <summary>
    /// Pruebas unitarias para fórmulas de cálculo de ítems de factura
    /// </summary>
    public class CalculosFacturaTests
    {
        private readonly ItemDocumentoDtoValidator _validator;

        public CalculosFacturaTests()
        {
            _validator = new ItemDocumentoDtoValidator();
        }

        #region Fórmula 1: VentaGravada = (PrecioUni × Cantidad) - Descuento

        [Fact]
        public void VentaGravada_FormulaStandard_CalculaCorrectamente()
        {
            // Arrange
            var item = new ItemDocumentoDto
            {
                NumItem = 1,
                TipoItem = 1,
                Cantidad = 10,
                UniMedida = 59,
                Descripcion = "Producto de prueba",
                PrecioUni = 5.00m,
                MontoDescuento = 0m,
                VentaGravada = 50.00m, // (5.00 × 10) - 0 = 50.00
                VentaExenta = 0m,
                VentaNoSuj = 0m,
                IvaItem = 5.75m // Ejemplo
            };

            // Act
            var result = _validator.TestValidate(item);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.VentaGravada);
        }

        [Fact]
        public void VentaGravada_ConDescuento_AplicaDescuentoCorrectamente()
        {
            // Arrange
            var item = new ItemDocumentoDto
            {
                NumItem = 1,
                TipoItem = 1,
                Cantidad = 10,
                UniMedida = 59,
                Descripcion = "Producto con descuento",
                PrecioUni = 10.00m,
                MontoDescuento = 20.00m,
                VentaGravada = 80.00m, // (10.00 × 10) - 20.00 = 80.00
                VentaExenta = 0m,
                VentaNoSuj = 0m,
                IvaItem = 9.20m
            };

            // Act
            var result = _validator.TestValidate(item);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.VentaGravada);
        }

        [Fact]
        public void VentaGravada_CalculoIncorrecto_GeneraError()
        {
            // Arrange
            var item = new ItemDocumentoDto
            {
                NumItem = 1,
                TipoItem = 1,
                Cantidad = 10,
                UniMedida = 59,
                Descripcion = "Producto mal calculado",
                PrecioUni = 5.00m,
                MontoDescuento = 0m,
                VentaGravada = 45.00m, // INCORRECTO: debería ser 50.00
                VentaExenta = 0m,
                VentaNoSuj = 0m,
                IvaItem = 5.18m
            };

            // Act
            var result = _validator.TestValidate(item);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.VentaGravada);
        }

        [Theory]
        [InlineData(10, 5.00, 0, 50.00)]      // Sin descuento
        [InlineData(5, 10.00, 10.00, 40.00)]  // Con descuento
        [InlineData(2.5, 20.00, 5.00, 45.00)] // Cantidad decimal
        [InlineData(1, 100.00, 0, 100.00)]    // Cantidad unitaria
        public void VentaGravada_DiferentesEscenarios_CalculaCorrectamente(
            decimal cantidad, decimal precioUni, decimal descuento, decimal ventaEsperada)
        {
            // Arrange
            var item = new ItemDocumentoDto
            {
                NumItem = 1,
                TipoItem = 1,
                Cantidad = cantidad,
                UniMedida = 59,
                Descripcion = "Producto",
                PrecioUni = precioUni,
                MontoDescuento = descuento,
                VentaGravada = ventaEsperada,
                VentaExenta = 0m,
                VentaNoSuj = 0m,
                IvaItem = ventaEsperada * 0.13m // Simplificado para esta prueba
            };

            // Act
            var result = _validator.TestValidate(item);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.VentaGravada);
        }

        #endregion

        #region Fórmula 2: IVA según Tipo de Documento

        [Fact]
        public void VentaExenta_ConIva_GeneraError()
        {
            // Arrange
            var item = new ItemDocumentoDto
            {
                NumItem = 1,
                TipoItem = 1,
                Cantidad = 10,
                UniMedida = 59,
                Descripcion = "Producto exento con IVA (incorrecto)",
                PrecioUni = 5.00m,
                MontoDescuento = 0m,
                VentaGravada = 0m,
                VentaExenta = 50.00m,
                VentaNoSuj = 0m,
                IvaItem = 6.50m // ERROR: No puede tener IVA
            };

            // Act
            var result = _validator.TestValidate(item);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.IvaItem)
                .WithErrorMessage("Los ítems Exentos o No Sujetos no pueden tener IVA");
        }

        [Fact]
        public void VentaNoSujeta_ConIva_GeneraError()
        {
            // Arrange
            var item = new ItemDocumentoDto
            {
                NumItem = 1,
                TipoItem = 1,
                Cantidad = 10,
                UniMedida = 59,
                Descripcion = "Producto no sujeto con IVA (incorrecto)",
                PrecioUni = 5.00m,
                MontoDescuento = 0m,
                VentaGravada = 0m,
                VentaExenta = 0m,
                VentaNoSuj = 50.00m,
                IvaItem = 6.50m // ERROR: No puede tener IVA
            };

            // Act
            var result = _validator.TestValidate(item);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.IvaItem)
                .WithErrorMessage("Los ítems Exentos o No Sujetos no pueden tener IVA");
        }

        [Fact]
        public void VentaGravada_ConIvaCero_EsValido()
        {
            // Arrange
            var item = new ItemDocumentoDto
            {
                NumItem = 1,
                TipoItem = 1,
                Cantidad = 10,
                UniMedida = 59,
                Descripcion = "Producto gravado sin IVA",
                PrecioUni = 5.00m,
                MontoDescuento = 0m,
                VentaGravada = 50.00m,
                VentaExenta = 0m,
                VentaNoSuj = 0m,
                IvaItem = 0m // Válido en ciertos casos especiales
            };

            // Act
            var result = _validator.TestValidate(item);

            // Assert - No debe haber error de IVA negativo
            result.ShouldNotHaveValidationErrorFor(x => x.IvaItem);
        }

        #endregion

        #region Validación de Tributos

        [Fact]
        public void VentaExenta_ConTributos_GeneraError()
        {
            // Arrange
            var item = new ItemDocumentoDto
            {
                NumItem = 1,
                TipoItem = 1,
                Cantidad = 10,
                UniMedida = 59,
                Descripcion = "Producto exento con tributos (incorrecto)",
                PrecioUni = 5.00m,
                MontoDescuento = 0m,
                VentaGravada = 0m,
                VentaExenta = 50.00m,
                VentaNoSuj = 0m,
                IvaItem = 0m,
                Tributos = new List<string> { "20" } // ERROR: No puede tener tributos
            };

            // Act
            var result = _validator.TestValidate(item);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Tributos)
                .WithErrorMessage("Los ítems Exentos, No Sujetos o No Afectos NO pueden tener tributos asociados según regulación del MH");
        }

        [Fact]
        public void VentaNoSujeta_ConTributos_GeneraError()
        {
            // Arrange
            var item = new ItemDocumentoDto
            {
                NumItem = 1,
                TipoItem = 1,
                Cantidad = 10,
                UniMedida = 59,
                Descripcion = "Producto no sujeto con tributos (incorrecto)",
                PrecioUni = 5.00m,
                MontoDescuento = 0m,
                VentaGravada = 0m,
                VentaExenta = 0m,
                VentaNoSuj = 50.00m,
                IvaItem = 0m,
                Tributos = new List<string> { "20" } // ERROR: No puede tener tributos
            };

            // Act
            var result = _validator.TestValidate(item);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Tributos)
                .WithErrorMessage("Los ítems Exentos, No Sujetos o No Afectos NO pueden tener tributos asociados según regulación del MH");
        }

        #endregion

        #region Validación de al menos un tipo de venta

        [Fact]
        public void SinNingunTipoDeVenta_GeneraError()
        {
            // Arrange
            var item = new ItemDocumentoDto
            {
                NumItem = 1,
                TipoItem = 1,
                Cantidad = 10,
                UniMedida = 59,
                Descripcion = "Producto sin ventas",
                PrecioUni = 5.00m,
                MontoDescuento = 0m,
                VentaGravada = 0m,
                VentaExenta = 0m,
                VentaNoSuj = 0m,
                IvaItem = 0m
            };

            // Act
            var result = _validator.TestValidate(item);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.VentaGravada)
                .WithErrorMessage("Al menos uno de los tipos de venta (Gravada, Exenta, No Sujeta) o Compra (FSE) debe tener un valor mayor a 0");
        }

        #endregion

        #region Casos extremos y tolerancia

        [Fact]
        public void VentaGravada_ConToleranciaRedondeo_NoGeneraError()
        {
            // Arrange - Diferencia de 0.01 (dentro de tolerancia)
            var item = new ItemDocumentoDto
            {
                NumItem = 1,
                TipoItem = 1,
                Cantidad = 3,
                UniMedida = 59,
                Descripcion = "Producto con redondeo",
                PrecioUni = 0.333m,
                MontoDescuento = 0m,
                VentaGravada = 1.00m, // Calculado: 0.999, redondeado a 1.00
                VentaExenta = 0m,
                VentaNoSuj = 0m,
                IvaItem = 0.11m
            };

            // Act
            var result = _validator.TestValidate(item);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.VentaGravada);
        }

        [Fact]
        public void VentaGravada_FueraDeTolerancia_GeneraError()
        {
            // Arrange - Diferencia de 0.05 (fuera de tolerancia de 0.01)
            var item = new ItemDocumentoDto
            {
                NumItem = 1,
                TipoItem = 1,
                Cantidad = 10,
                UniMedida = 59,
                Descripcion = "Producto con error de cálculo",
                PrecioUni = 5.00m,
                MontoDescuento = 0m,
                VentaGravada = 50.05m, // INCORRECTO: debería ser 50.00
                VentaExenta = 0m,
                VentaNoSuj = 0m,
                IvaItem = 5.75m
            };

            // Act
            var result = _validator.TestValidate(item);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.VentaGravada);
        }

        #endregion
    }
}
