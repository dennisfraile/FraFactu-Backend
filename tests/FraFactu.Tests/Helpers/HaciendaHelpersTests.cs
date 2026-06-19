using Xunit;
using FluentAssertions;
using FraFactu.Infrastructure.Helpers;

namespace FraFactu.Tests.Helpers
{
    /// <summary>
    /// Tests para Helpers oficiales de Hacienda
    /// </summary>
    public class HaciendaHelpersTests
    {
        #region FacturaCalculosHelper Tests

        [Fact]
        public void CalcularIVA_Con1000Gravado_DeberiaRetornar130()
        {
            // Arrange
            decimal montoGravado = 1000.00m;

            // Act
            decimal iva = FacturaCalculosHelper.CalcularIVA(montoGravado);

            // Assert
            iva.Should().Be(130.00m);
        }

        [Theory]
        [InlineData(100.00, 13.00)]
        [InlineData(500.00, 65.00)]
        [InlineData(1000.00, 130.00)]
        [InlineData(0.00, 0.00)]
        public void CalcularIVA_ConDistintosMontos_DeberiaCalcularCorrectamente(decimal monto, decimal ivaEsperado)
        {
            // Act
            decimal iva = FacturaCalculosHelper.CalcularIVA(monto);

            // Assert
            iva.Should().Be(ivaEsperado);
        }

        [Fact]
        public void ConvertirMontoALetras_Con1130_DeberiaRetornarTextoCorrecto()
        {
            // Arrange
            decimal monto = 1130.00m;

            // Act
            string letras = FacturaCalculosHelper.ConvertirMontoALetras(monto);

            // Assert
            letras.Should().Contain("MIL");
            letras.Should().Contain("CIEN"); // 130 = CIEN TREINTA
            letras.Should().Contain("TREINTA");
            letras.Should().Contain("DÓLARES");
            letras.Should().Contain("00/100");
        }

        [Theory]
        [InlineData(0, "CERO DÓLARES CON 00/100")]
        [InlineData(1, "UN DÓLARES CON 00/100")]
        [InlineData(100, "CIEN DÓLARES CON 00/100")]
        public void ConvertirMontoALetras_ConMontosBasicos_DeberiaConvertirCorrectamente(decimal monto, string esperado)
        {
            // Act
            string letras = FacturaCalculosHelper.ConvertirMontoALetras(monto);

            // Assert
            letras.Should().Be(esperado);
        }

        [Fact]
        public void CalcularPorcentajeDescuento_Con100TotalY20Descuento_DeberiaRetornar20Porciento()
        {
            // Arrange
            decimal totalSinDescuento = 100.00m;
            decimal totalDescuento = 20.00m;

            // Act
            decimal porcentaje = FacturaCalculosHelper.CalcularPorcentajeDescuento(totalSinDescuento, totalDescuento);

            // Assert
            porcentaje.Should().Be(20.00m);
        }

        [Fact]
        public void ObtenerNombreFormaPago_Con1_DeberiaRetornarEfectivo()
        {
            // Act
            string nombre = FacturaCalculosHelper.ObtenerNombreFormaPago(1);

            // Assert
            nombre.Should().Be("Efectivo");
        }

        #endregion

        #region DireccionHelper Tests

        [Theory]
        [InlineData("01", "Ahuachapán")]
        [InlineData("06", "San Salvador")]
        [InlineData("12", "San Miguel")]
        public void ObtenerNombreDepartamento_ConCodigosValidos_DeberiaRetornarNombre(string codigo, string nombreEsperado)
        {
            // Act
            string nombre = DireccionHelper.ObtenerNombreDepartamento(codigo);

            // Assert
            nombre.Should().Contain(nombreEsperado);
        }

        [Fact]
        public void ObtenerNombreDepartamento_ConCodigoInvalido_DeberiaRetornarDesconocido()
        {
            // Act
            string nombre = DireccionHelper.ObtenerNombreDepartamento("99");

            // Assert
            nombre.Should().Be("Desconocido");
        }

        [Fact]
        public void ValidarDepartamento_ConCodigoValido_DeberiaRetornarTrue()
        {
            // Act
            bool esValido = DireccionHelper.ValidarDepartamento("06");

            // Assert
            esValido.Should().BeTrue();
        }

        [Fact]
        public void ValidarDepartamento_ConCodigoInvalido_DeberiaRetornarFalse()
        {
            // Act
            bool esValido = DireccionHelper.ValidarDepartamento("99");

            // Assert
            esValido.Should().BeFalse();
        }

        [Fact]
        public void ValidarComplemento_ConTextoValido_DeberiaRetornarTrue()
        {
            // Arrange
            string complemento = "Calle Principal #123, Colonia Centro";

            // Act
            bool esValido = DireccionHelper.ValidarComplemento(complemento);

            // Assert
            esValido.Should().BeTrue();
        }

        [Fact]
        public void ValidarComplemento_ConTextoMuyCorto_DeberiaRetornarFalse()
        {
            // Arrange
            string complemento = "123";

            // Act
            bool esValido = DireccionHelper.ValidarComplemento(complemento);

            // Assert
            esValido.Should().BeFalse();
        }

        #endregion

        #region IdentificacionHelper Tests

        [Theory]
        [InlineData("06142812991015")] // 14 dígitos
        [InlineData("061428129")] // 9 dígitos
        [InlineData("0614-281299-101-5")] // Con guiones (14 dígitos)
        public void ValidarNIT_ConFormatosValidos_DeberiaRetornarTrue(string nit)
        {
            // Act
            bool esValido = IdentificacionHelper.ValidarNIT(nit);

            // Assert
            esValido.Should().BeTrue();
        }

        [Theory]
        [InlineData("12345")] // Muy corto
        [InlineData("ABC123456789")] // Con letras
        [InlineData("")] // Vacío
        public void ValidarNIT_ConFormatosInvalidos_DeberiaRetornarFalse(string nit)
        {
            // Act
            bool esValido = IdentificacionHelper.ValidarNIT(nit);

            // Assert
            esValido.Should().BeFalse();
        }

        [Theory]
        [InlineData("123456")]
        [InlineData("1234567")]
        [InlineData("12345678")]
        public void ValidarNRC_ConFormatosValidos_DeberiaRetornarTrue(string nrc)
        {
            // Act
            bool esValido = IdentificacionHelper.ValidarNRC(nrc);

            // Assert
            esValido.Should().BeTrue();
        }

        [Fact]
        public void ValidarDUI_ConFormatoValido_DeberiaRetornarTrue()
        {
            // Arrange
            string dui = "03845678-9";

            // Act
            bool esValido = IdentificacionHelper.ValidarDUI(dui);

            // Assert
            esValido.Should().BeTrue();
        }

        [Fact]
        public void GenerarCodigoGeneracion_DeberiaRetornarGUIDValido()
        {
            // Act
            string codigo = IdentificacionHelper.GenerarCodigoGeneracion();

            // Assert
            codigo.Should().HaveLength(36);
            Guid.TryParse(codigo, out _).Should().BeTrue();
            codigo.Should().MatchRegex("^[A-F0-9]{8}-[A-F0-9]{4}-[A-F0-9]{4}-[A-F0-9]{4}-[A-F0-9]{12}$");
        }

        [Fact]
        public void ValidarCodigoGeneracion_ConGUIDValido_DeberiaRetornarTrue()
        {
            // Arrange
            string codigo = "12345678-1234-1234-1234-123456789012";

            // Act
            bool esValido = IdentificacionHelper.ValidarCodigoGeneracion(codigo);

            // Assert
            esValido.Should().BeTrue();
        }

        [Fact]
        public void GenerarNumeroControl_ConNitYCorrelativo_DeberiaGenerarFormatoCorrecto()
        {
            // Arrange
            string nit = "0614-281299-101-5";
            int correlativo = 1;

            // Act
            string numeroControl = IdentificacionHelper.GenerarNumeroControl(nit, correlativo);

            // Assert
            numeroControl.Should().StartWith("DTE-01-");
            numeroControl.Should().HaveLength(31);
            numeroControl.Should().MatchRegex(@"^DTE-01-\w{8}-\d{15}$");
        }

        [Fact]
        public void ValidarNumeroControl_ConFormatoValido_DeberiaRetornarTrue()
        {
            // Arrange
            string numeroControl = "DTE-01-06142812-000000000000001";

            // Act
            bool esValido = IdentificacionHelper.ValidarNumeroControl(numeroControl);

            // Assert
            esValido.Should().BeTrue();
        }

        [Theory]
        [InlineData("DTE-01-06142812")] // Muy corto
        [InlineData("DTE-02-06142812-000000000000001")] // Tipo incorrecto
        [InlineData("FACTURA-01-06142812-000000000000001")] // Prefijo incorrecto
        public void ValidarNumeroControl_ConFormatosInvalidos_DeberiaRetornarFalse(string numeroControl)
        {
            // Act
            bool esValido = IdentificacionHelper.ValidarNumeroControl(numeroControl);

            // Assert
            esValido.Should().BeFalse();
        }

        [Fact]
        public void FormatearNIT_DeberiaAgregarGuiones()
        {
            // Arrange
            string nit = "06142812991015";

            // Act
            string nitFormateado = IdentificacionHelper.FormatearNIT(nit);

            // Assert
            nitFormateado.Should().Contain("-");
        }

        [Fact]
        public void FormatearDUI_DeberiaAgregarGuion()
        {
            // Arrange
            string dui = "038456789";

            // Act
            string duiFormateado = IdentificacionHelper.FormatearDUI(dui);

            // Assert
            duiFormateado.Should().Be("03845678-9");
        }

        [Theory]
        [InlineData("test@example.com", true)]
        [InlineData("usuario@dominio.co.sv", true)]
        [InlineData("invalido", false)]
        [InlineData("@ejemplo.com", false)]
        public void ValidarCorreo_ConDistintosFormatos_DeberiaValidarCorrectamente(string correo, bool esperado)
        {
            // Act
            bool esValido = IdentificacionHelper.ValidarCorreo(correo);

            // Assert
            esValido.Should().Be(esperado);
        }

        [Theory]
        [InlineData("2222-2222", true)]
        [InlineData("7777-8888", true)]
        [InlineData("22222222", true)]
        [InlineData("123", false)]
        public void ValidarTelefono_ConDistintosFormatos_DeberiaValidarCorrectamente(string telefono, bool esperado)
        {
            // Act
            bool esValido = IdentificacionHelper.ValidarTelefono(telefono);

            // Assert
            esValido.Should().Be(esperado);
        }

        #endregion
    }
}
