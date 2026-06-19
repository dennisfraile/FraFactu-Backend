using System.Text;
using System.Text.Json;
using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using FraFactu.Infrastructure.Services;

namespace FraFactu.Tests.UnitTests.Services;

public class DteParserServiceTests
{
    private readonly DteParserService _service;

    public DteParserServiceTests()
    {
        _service = new DteParserService(new Mock<ILogger<DteParserService>>().Object);
    }

    #region JSON de prueba

    private const string FacturaJson = """
    {
      "identificacion": {
        "version": 1,
        "ambiente": "00",
        "tipoDte": "01",
        "numeroControl": "DTE-01-0001-000000000000001",
        "codigoGeneracion": "A1B2C3D4-E5F6-7890-ABCD-EF1234567890",
        "tipoModelo": 1,
        "tipoOperacion": 1,
        "fecEmi": "2026-03-15",
        "horEmi": "10:30:00",
        "tipoMoneda": "USD"
      },
      "emisor": {
        "nit": "06141804941035",
        "nrc": "1234567",
        "nombre": "Proveedor Test S.A. de C.V.",
        "codActividad": "46100",
        "descActividad": "Comercio al por mayor",
        "tipoEstablecimiento": "01",
        "direccion": {
          "departamento": "06",
          "municipio": "14",
          "complemento": "Calle Principal #123"
        },
        "telefono": "22001234",
        "correo": "proveedor@test.com"
      },
      "receptor": {
        "nit": "06142505671038",
        "nombre": "Mi Empresa S.A.",
        "correo": "miempresa@test.com"
      },
      "cuerpoDocumento": [
        {
          "numItem": 1,
          "tipoItem": 1,
          "codigo": "PROD-001",
          "uniMedida": 59,
          "descripcion": "Producto de prueba",
          "cantidad": 10.0,
          "precioUni": 25.00,
          "montoDescu": 0.00,
          "ventaNoSuj": 0.00,
          "ventaExenta": 0.00,
          "ventaGravada": 250.00,
          "psv": 0.00,
          "noGravado": 0.00
        }
      ],
      "resumen": {
        "totalNoSuj": 0.00,
        "totalExenta": 0.00,
        "totalGravada": 250.00,
        "subTotalVentas": 250.00,
        "descuNoSuj": 0.00,
        "descuExenta": 0.00,
        "descuGravada": 0.00,
        "totalDescu": 0.00,
        "tributos": [
          {
            "codigo": "20",
            "descripcion": "Impuesto al Valor Agregado 13%",
            "valor": 32.50
          }
        ],
        "subTotal": 250.00,
        "montoTotalOperacion": 282.50,
        "totalPagar": 282.50,
        "totalLetras": "DOSCIENTOS OCHENTA Y DOS 50/100 USD",
        "condicionOperacion": 1,
        "pagos": [
          { "codigo": "01", "montoPago": 282.50 }
        ]
      }
    }
    """;

    private const string CcfJson = """
    {
      "identificacion": {
        "version": 3,
        "ambiente": "00",
        "tipoDte": "03",
        "numeroControl": "DTE-03-0001-000000000000001",
        "codigoGeneracion": "B2C3D4E5-F6A7-8901-BCDE-F12345678901",
        "tipoModelo": 1,
        "tipoOperacion": 1,
        "fecEmi": "2026-03-20",
        "horEmi": "14:00:00",
        "tipoMoneda": "USD"
      },
      "emisor": {
        "nit": "06141804941035",
        "nrc": "1234567",
        "nombre": "Proveedor CCF S.A. de C.V.",
        "codActividad": "46100",
        "descActividad": "Comercio al por mayor",
        "tipoEstablecimiento": "01",
        "direccion": {
          "departamento": "06",
          "municipio": "14",
          "complemento": "Calle Principal #123"
        },
        "telefono": "22001234",
        "correo": "proveedor@test.com"
      },
      "receptor": {
        "nit": "06142505671038",
        "nrc": "7654321",
        "nombre": "Mi Empresa CCF S.A. de C.V.",
        "correo": "miempresa@test.com"
      },
      "cuerpoDocumento": [
        {
          "numItem": 1,
          "tipoItem": 1,
          "codigo": "SERV-001",
          "uniMedida": 99,
          "descripcion": "Servicio de consultoría",
          "cantidad": 1.0,
          "precioUni": 500.00,
          "montoDescu": 0.00,
          "ventaNoSuj": 0.00,
          "ventaExenta": 0.00,
          "ventaGravada": 500.00,
          "psv": 0.00,
          "noGravado": 0.00
        }
      ],
      "resumen": {
        "totalNoSuj": 0.00,
        "totalExenta": 0.00,
        "totalGravada": 500.00,
        "subTotalVentas": 500.00,
        "descuNoSuj": 0.00,
        "descuExenta": 0.00,
        "descuGravada": 0.00,
        "totalDescu": 0.00,
        "tributos": [
          {
            "codigo": "20",
            "descripcion": "Impuesto al Valor Agregado 13%",
            "valor": 65.00
          }
        ],
        "subTotal": 500.00,
        "ivaPerci1": 0.00,
        "ivaRete1": 0.00,
        "reteRenta": 0.00,
        "montoTotalOperacion": 565.00,
        "totalPagar": 565.00,
        "totalLetras": "QUINIENTOS SESENTA Y CINCO 00/100 USD",
        "condicionOperacion": 1,
        "pagos": [
          { "codigo": "01", "montoPago": 565.00 }
        ]
      }
    }
    """;

    #endregion

    #region ParsearDteJson - Factura (01)

    [Fact]
    public void ParsearDteJson_ConFacturaValida_ExtraeTodosLosCamposCorrectamente()
    {
        // Act
        var result = _service.ParsearDteJson(FacturaJson);

        // Assert
        result.Should().NotBeNull();
        result.CodigoGeneracion.Should().Be("A1B2C3D4-E5F6-7890-ABCD-EF1234567890");
        result.TipoDte.Should().Be("01");
        result.NumeroControl.Should().Be("DTE-01-0001-000000000000001");
        result.FechaEmision.Should().Be(new DateTime(2026, 3, 15));
        result.EmisorNit.Should().Be("06141804941035");
        result.EmisorNombre.Should().Be("Proveedor Test S.A. de C.V.");
        result.EmisorNrc.Should().Be("1234567");
        result.ReceptorNit.Should().Be("06142505671038");
        result.ReceptorNombre.Should().Be("Mi Empresa S.A.");
        result.JsonOriginal.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region ParsearDteJson - CCF (03)

    [Fact]
    public void ParsearDteJson_ConCcfValido_ExtraeTodosLosCamposCorrectamente()
    {
        // Act
        var result = _service.ParsearDteJson(CcfJson);

        // Assert
        result.Should().NotBeNull();
        result.CodigoGeneracion.Should().Be("B2C3D4E5-F6A7-8901-BCDE-F12345678901");
        result.TipoDte.Should().Be("03");
        result.NumeroControl.Should().Be("DTE-03-0001-000000000000001");
        result.FechaEmision.Should().Be(new DateTime(2026, 3, 20));
        result.EmisorNit.Should().Be("06141804941035");
        result.EmisorNombre.Should().Be("Proveedor CCF S.A. de C.V.");
        result.EmisorNrc.Should().Be("1234567");
        result.ReceptorNit.Should().Be("06142505671038");
        result.ReceptorNombre.Should().Be("Mi Empresa CCF S.A. de C.V.");
    }

    #endregion

    #region ParsearDteJson - Errores

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ParsearDteJson_ConCadenaVacia_LanzaArgumentException(string? json)
    {
        // Act
        var act = () => _service.ParsearDteJson(json!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ParsearDteJson_ConJsonInvalido_LanzaInvalidOperationException()
    {
        // Arrange
        var jsonInvalido = "esto no es json {{{";

        // Act
        var act = () => _service.ParsearDteJson(jsonInvalido);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*no es un JSON válido*");
    }

    [Fact]
    public void ParsearDteJson_SinSeccionIdentificacion_LanzaInvalidOperationException()
    {
        // Arrange
        var json = """
        {
          "emisor": { "nit": "12345", "nombre": "Test" },
          "receptor": { "nit": "67890", "nombre": "Receptor" }
        }
        """;

        // Act
        var act = () => _service.ParsearDteJson(json);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*identificacion*");
    }

    [Fact]
    public void ParsearDteJson_SinSeccionEmisor_LanzaInvalidOperationException()
    {
        // Arrange
        var json = """
        {
          "identificacion": {
            "tipoDte": "01",
            "codigoGeneracion": "TEST-UUID",
            "fecEmi": "2026-03-15"
          },
          "receptor": { "nit": "67890", "nombre": "Receptor" }
        }
        """;

        // Act
        var act = () => _service.ParsearDteJson(json);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*emisor*");
    }

    #endregion

    #region ParsearDteJson - Montos

    [Fact]
    public void ParsearDteJson_ExtraeMontosCorrectamenteDesdeResumen()
    {
        // Act
        var result = _service.ParsearDteJson(FacturaJson);

        // Assert
        result.MontoGravado.Should().Be(250.00m);
        result.MontoExento.Should().Be(0.00m);
        result.MontoNoSujeto.Should().Be(0.00m);
        result.SubTotal.Should().Be(250.00m);
        result.IVA.Should().Be(32.50m);
        result.Total.Should().Be(282.50m);
    }

    #endregion

    #region ParsearDteJson - Items

    [Fact]
    public void ParsearDteJson_ExtraeItemsDeCuerpoDocumento()
    {
        // Act
        var result = _service.ParsearDteJson(FacturaJson);

        // Assert
        result.Items.Should().HaveCount(1);

        var item = result.Items[0];
        item.NumItem.Should().Be(1);
        item.Codigo.Should().Be("PROD-001");
        item.Descripcion.Should().Be("Producto de prueba");
        item.Cantidad.Should().Be(10.0m);
        item.PrecioUnitario.Should().Be(25.00m);
        item.MontoDescuento.Should().Be(0.00m);
        item.VentaGravada.Should().Be(250.00m);
        item.VentaExenta.Should().Be(0.00m);
        item.VentaNoSujeta.Should().Be(0.00m);
        item.UnidadMedida.Should().Be(59);
    }

    #endregion

    #region ParsearDteJwt

    [Fact]
    public void ParsearDteJwt_ConJwtValido_DecodificaYExtraeCorrectamente()
    {
        // Arrange - crear un JWT con el payload del DTE de factura
        var header = Base64UrlEncode("""{"alg":"RS256","typ":"JWT"}""");
        var payload = Base64UrlEncode(FacturaJson);
        var signature = Base64UrlEncode("fake-signature-data");
        var jwt = $"{header}.{payload}.{signature}";

        // Act
        var result = _service.ParsearDteJwt(jwt);

        // Assert
        result.Should().NotBeNull();
        result.CodigoGeneracion.Should().Be("A1B2C3D4-E5F6-7890-ABCD-EF1234567890");
        result.TipoDte.Should().Be("01");
        result.EmisorNit.Should().Be("06141804941035");
        result.EmisorNombre.Should().Be("Proveedor Test S.A. de C.V.");
        result.Total.Should().Be(282.50m);
    }

    [Fact]
    public void ParsearDteJwt_ConFormatoInvalido_LanzaInvalidOperationException()
    {
        // Arrange - JWT sin las 3 partes
        var jwtInvalido = "parte1.parte2";

        // Act
        var act = () => _service.ParsearDteJwt(jwtInvalido);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*JWT*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ParsearDteJwt_ConCadenaVacia_LanzaArgumentException(string? jwt)
    {
        // Act
        var act = () => _service.ParsearDteJwt(jwt!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region EsDteValido

    [Fact]
    public void EsDteValido_ConDteJsonValido_RetornaTrue()
    {
        // Act
        var result = _service.EsDteValido(FacturaJson);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void EsDteValido_ConCadenaVacia_RetornaFalse(string? contenido)
    {
        // Act
        var result = _service.EsDteValido(contenido!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void EsDteValido_ConJsonSinCamposDte_RetornaFalse()
    {
        // Arrange
        var jsonNoDte = """{ "nombre": "test", "valor": 123 }""";

        // Act
        var result = _service.EsDteValido(jsonNoDte);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Helpers

    private static string Base64UrlEncode(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    #endregion
}
