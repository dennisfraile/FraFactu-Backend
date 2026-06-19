using Xunit;
using FraFactu.Domain.Entities;
using FraFactu.Domain.Entities.Catalogos;

namespace FraFactu.Tests.UnitTests.Entities
{
    /// <summary>
    /// Pruebas unitarias para las propiedades calculadas de FacturaElectronicaDetalle
    /// </summary>
    public class FacturaElectronicaDetalleTests
    {
        [Fact]
        public void CostoTotal_ConCantidadYCostoUnitario_CalculaCorrectamente()
        {
            // Arrange
            var detalle = new FacturaElectronicaDetalle
            {
                Cantidad = 10,
                CostoUnitario = 5.50m
            };

            // Act
            var costoTotal = detalle.CostoTotal;

            // Assert
            Assert.Equal(55.00m, costoTotal);
        }

        [Fact]
        public void CostoTotal_ConCantidadDecimal_CalculaCorrectamente()
        {
            // Arrange
            var detalle = new FacturaElectronicaDetalle
            {
                Cantidad = 2.5m,
                CostoUnitario = 10.00m
            };

            // Act
            var costoTotal = detalle.CostoTotal;

            // Assert
            Assert.Equal(25.00m, costoTotal);
        }

        [Fact]
        public void CostoTotal_ConValoresCero_RetornaCero()
        {
            // Arrange
            var detalle = new FacturaElectronicaDetalle
            {
                Cantidad = 0,
                CostoUnitario = 0
            };

            // Act
            var costoTotal = detalle.CostoTotal;

            // Assert
            Assert.Equal(0m, costoTotal);
        }

        [Fact]
        public void Utilidad_ConVentaGravadaYCostoTotal_CalculaCorrectamente()
        {
            // Arrange
            var detalle = new FacturaElectronicaDetalle
            {
                VentaGravada = 100.00m,
                Cantidad = 10,
                CostoUnitario = 6.00m // CostoTotal = 60.00
            };

            // Act
            var utilidad = detalle.Utilidad;

            // Assert
            Assert.Equal(40.00m, utilidad); // 100 - 60 = 40
        }

        [Fact]
        public void Utilidad_ConPerdida_RetornaValorNegativo()
        {
            // Arrange
            var detalle = new FacturaElectronicaDetalle
            {
                VentaGravada = 50.00m,
                Cantidad = 10,
                CostoUnitario = 8.00m // CostoTotal = 80.00
            };

            // Act
            var utilidad = detalle.Utilidad;

            // Assert
            Assert.Equal(-30.00m, utilidad); // 50 - 80 = -30
        }

        [Fact]
        public void Utilidad_ConVentaExenta_NoCalculaUtilidad()
        {
            // Arrange
            var detalle = new FacturaElectronicaDetalle
            {
                VentaGravada = 0m,  // No hay venta gravada
                VentaExenta = 100.00m,
                Cantidad = 10,
                CostoUnitario = 6.00m
            };

            // Act
            var utilidad = detalle.Utilidad;

            // Assert
            Assert.Equal(-60.00m, utilidad); // 0 - 60 = -60 (solo se calcula sobre gravada)
        }

        [Fact]
        public void PorcentajeUtilidad_ConUtilidad_CalculaPorcentajeCorrectamente()
        {
            // Arrange
            var detalle = new FacturaElectronicaDetalle
            {
                VentaGravada = 150.00m,
                Cantidad = 10,
                CostoUnitario = 10.00m // CostoTotal = 100, Utilidad = 50
            };

            // Act
            var porcentajeUtilidad = detalle.PorcentajeUtilidad;

            // Assert
            Assert.Equal(50.00m, porcentajeUtilidad); // (50 / 100) * 100 = 50%
        }

        [Fact]
        public void PorcentajeUtilidad_ConCostoTotalCero_RetornaCero()
        {
            // Arrange
            var detalle = new FacturaElectronicaDetalle
            {
                VentaGravada = 100.00m,
                Cantidad = 0,
                CostoUnitario = 0 // CostoTotal = 0
            };

            // Act
            var porcentajeUtilidad = detalle.PorcentajeUtilidad;

            // Assert
            Assert.Equal(0m, porcentajeUtilidad); // Evita división por cero
        }

        [Fact]
        public void PorcentajeUtilidad_ConPerdida_RetornaPorcentajeNegativo()
        {
            // Arrange
            var detalle = new FacturaElectronicaDetalle
            {
                VentaGravada = 80.00m,
                Cantidad = 10,
                CostoUnitario = 10.00m // CostoTotal = 100, Utilidad = -20
            };

            // Act
            var porcentajeUtilidad = detalle.PorcentajeUtilidad;

            // Assert
            Assert.Equal(-20.00m, porcentajeUtilidad); // (-20 / 100) * 100 = -20%
        }

        [Theory]
        [InlineData(5, 10.00, 50.00)]
        [InlineData(12.5, 8.00, 100.00)]
        [InlineData(1, 25.75, 25.75)]
        [InlineData(100, 0.50, 50.00)]
        public void CostoTotal_ConDiferentesCombinaciones_CalculaCorrectamente(
            decimal cantidad, decimal costoUnitario, decimal esperado)
        {
            // Arrange
            var detalle = new FacturaElectronicaDetalle
            {
                Cantidad = cantidad,
                CostoUnitario = costoUnitario
            };

            // Act
            var costoTotal = detalle.CostoTotal;

            // Assert
            Assert.Equal(esperado, costoTotal);
        }

        [Theory]
        [InlineData(100.00, 80.00, 25.00)]    // 25% utilidad
        [InlineData(200.00, 100.00, 100.00)]  // 100% utilidad
        [InlineData(50.00, 100.00, -50.00)]   // -50% pérdida
        [InlineData(113.00, 100.00, 13.00)]   // 13% utilidad (similar al IVA)
        public void PorcentajeUtilidad_EscenariosDiferentes_CalculaCorrectamente(
            decimal ventaGravada, decimal costoTotal, decimal porcentajeEsperado)
        {
            // Arrange
            var cantidad = 1m;
            var costoUnitario = costoTotal; // Para que CostoTotal = costoTotal
            var detalle = new FacturaElectronicaDetalle
            {
                VentaGravada = ventaGravada,
                Cantidad = cantidad,
                CostoUnitario = costoUnitario
            };

            // Act
            var porcentajeUtilidad = detalle.PorcentajeUtilidad;

            // Assert
            Assert.Equal(porcentajeEsperado, porcentajeUtilidad, 2); // 2 decimales de precisión
        }
    }
}
