using System.Text.RegularExpressions;
using Xunit;
using FluentAssertions;
using FraFactu.Infrastructure.Helpers;

namespace FraFactu.Tests.Helpers
{
    /// <summary>
    /// Tests del generador de Número de Control según la Normativa DTE V2.0.
    /// </summary>
    public class NumeroControlHelperTests
    {
        // Patrón oficial del bloque V2.0 (sin el prefijo anti-saltos que añade la ND v4).
        private const string PatronV2 = @"^DTE-\d{2}-(M|B|S|P)[0-9]{3}P[0-9]{3}-[0-9]{15}$";

        [Theory]
        [InlineData("01", 'M')] // Casa Matriz
        [InlineData("02", 'S')] // Sucursal
        [InlineData("04", 'B')] // Bodega
        [InlineData("07", 'P')] // Patio
        [InlineData("20", 'M')] // Oficina Administrativa -> default M
        [InlineData("99", 'M')] // Otros -> default M
        [InlineData(null, 'M')] // sin tipo -> default M
        [InlineData("xx", 'M')] // desconocido -> default M
        public void LetraTipoEstablecimiento_MapeaSegunCat009(string? codigo, char esperada)
        {
            NumeroControlHelper.LetraTipoEstablecimiento(codigo).Should().Be(esperada);
        }

        [Theory]
        [InlineData("0001", "001")] // 4 dígitos legacy -> últimos 3
        [InlineData("001", "001")]  // ya 3 dígitos
        [InlineData("P001", "001")] // con prefijo P -> quita no numérico
        [InlineData("", "001")]     // vacío -> 001
        [InlineData(null, "001")]   // nulo -> 001
        [InlineData("12", "012")]   // rellena con ceros
        [InlineData("1234", "234")] // más de 3 -> últimos 3
        public void Normalizar3_DerivaTresDigitos(string? codigo, string esperado)
        {
            NumeroControlHelper.Normalizar3(codigo).Should().Be(esperado);
        }

        [Fact]
        public void Generar_ConCasaMatriz_ProduceFormatoEsperado()
        {
            var nc = NumeroControlHelper.Generar("01", "01", "0001", "P001", 399);
            nc.Should().Be("DTE-01-M001P001-000000000000399");
        }

        [Theory]
        [InlineData("01", "01")]
        [InlineData("03", "02")]
        [InlineData("05", "04")]
        [InlineData("06", "07")]
        [InlineData("14", "20")]
        public void Generar_SiempreCumplePatronYLongitud(string tipoDte, string tipoEstab)
        {
            var nc = NumeroControlHelper.Generar(tipoDte, tipoEstab, "001", "001", 1);

            nc.Length.Should().Be(31);
            Regex.IsMatch(nc, PatronV2).Should().BeTrue($"'{nc}' debe cumplir el patrón V2.0");
        }

        [Fact]
        public void Generar_RellenaCorrelativoA15Digitos()
        {
            var nc = NumeroControlHelper.Generar("01", "01", "001", "001", 7);
            nc.Should().EndWith("-000000000000007");
        }

        [Fact]
        public void Generar_ConCodigosVacios_UsaDefault001()
        {
            var nc = NumeroControlHelper.Generar("01", null, null, null, 1);
            nc.Should().Be("DTE-01-M001P001-000000000000001");
        }
    }
}
