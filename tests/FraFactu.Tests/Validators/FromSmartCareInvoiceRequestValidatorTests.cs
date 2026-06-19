using FluentValidation.TestHelper;
using FraFactu.Application.DTOs;
using FraFactu.Application.Validators.FromSmartCare;
using Xunit;

namespace FraFactu.Tests.Validators;

public class FromSmartCareInvoiceRequestValidatorTests
{
    private readonly FromSmartCareInvoiceRequestValidator _validator = new();

    [Fact]
    public void Should_Fail_When_CorrelationId_Empty()
    {
        var dto = ValidRequest();
        dto.CorrelationId = "";
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.CorrelationId);
    }

    [Fact]
    public void Should_Fail_When_WebhookUrl_NotHttpOrHttps()
    {
        var dto = ValidRequest();
        dto.WebhookUrl = "ftp://invalido.com/x";
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.WebhookUrl);
    }

    [Fact]
    public void Should_Pass_When_WebhookUrl_IsHttp()
    {
        var dto = ValidRequest();
        dto.WebhookUrl = "http://localhost:5001/api/v1/smartix-webhooks/invoice-status";
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.WebhookUrl);
    }

    [Fact]
    public void Should_Fail_When_TipoDte_Invalid()
    {
        var dto = ValidRequest();
        dto.TipoDte = "99";
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.TipoDte);
    }

    [Fact]
    public void Should_Fail_When_Lineas_Empty()
    {
        var dto = ValidRequest();
        dto.Lineas = new();
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Lineas);
    }

    [Fact]
    public void Should_Fail_When_CCF_Without_Nrc()
    {
        var dto = ValidRequest();
        dto.TipoDte = "03";
        dto.Receptor.Nrc = null;
        dto.Receptor.NumeroDocumento = "06141511101012";
        dto.Receptor.CodigoActividad = "62010";
        dto.Receptor.DepartamentoId = 6;
        dto.Receptor.MunicipioId = 12;
        dto.Receptor.Direccion = "x";
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor("Receptor.Nrc");
    }

    [Fact]
    public void Should_Pass_When_Valid_CF()
    {
        var result = _validator.TestValidate(ValidRequest());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData(true)]
    [InlineData(false)]
    public void Should_Pass_With_Any_PrecioIncluyeIva_State(bool? precioIncluyeIva)
    {
        // Plan C1: el flag es opcional sin reglas — los 3 estados deben pasar.
        // Pin para que un futuro .NotNull() no se cuele silencioso.
        var dto = ValidRequest();
        dto.Lineas[0].PrecioIncluyeIva = precioIncluyeIva;
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validator_CCF_sin_DistritoId_falla_con_mensaje_CAT008()
    {
        var validator = new FromSmartCareInvoiceRequestValidator();
        var request = new FromSmartCareInvoiceRequestDto
        {
            CorrelationId = Guid.NewGuid().ToString(),
            WebhookUrl = "https://x.test/webhook",
            SucursalSmartixId = 1,
            TipoDte = "03",
            Receptor = new FromSmartCareReceptorDto
            {
                Nombre = "Test",
                Nrc = "12345",
                NumeroDocumento = "12345678901234",
                CodigoActividad = "12345",
                DepartamentoId = 7,
                MunicipioId = 24,
                DistritoId = null,
                Direccion = "calle x",
            },
            Lineas = new List<FromSmartCareInvoiceLineDto>
            {
                new() { Descripcion = "x", Cantidad = 1, PrecioUnitario = 10, TipoItem = 2 }
            }
        };

        var result = validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == "Receptor.DistritoId"
            && e.ErrorMessage.Contains("CCF requiere distrito (CAT-008)"));
    }

    [Fact]
    public void Validator_CCF_con_DistritoId_no_levanta_error_de_distrito()
    {
        var validator = new FromSmartCareInvoiceRequestValidator();
        var request = new FromSmartCareInvoiceRequestDto
        {
            CorrelationId = Guid.NewGuid().ToString(),
            WebhookUrl = "https://x.test/webhook",
            SucursalSmartixId = 1,
            TipoDte = "03",
            Receptor = new FromSmartCareReceptorDto
            {
                Nombre = "Test",
                Nrc = "12345",
                NumeroDocumento = "12345678901234",
                CodigoActividad = "12345",
                DepartamentoId = 7,
                MunicipioId = 24,
                DistritoId = 111,
                Direccion = "calle x",
            },
            Lineas = new List<FromSmartCareInvoiceLineDto>
            {
                new() { Descripcion = "x", Cantidad = 1, PrecioUnitario = 10, TipoItem = 2 }
            }
        };

        var result = validator.Validate(request);

        Assert.DoesNotContain(result.Errors, e => e.PropertyName == "Receptor.DistritoId");
    }

    [Fact]
    public void Validator_FC_sin_DistritoId_no_levanta_error_de_distrito()
    {
        var validator = new FromSmartCareInvoiceRequestValidator();
        var request = new FromSmartCareInvoiceRequestDto
        {
            CorrelationId = Guid.NewGuid().ToString(),
            WebhookUrl = "https://x.test/webhook",
            SucursalSmartixId = 1,
            TipoDte = "01",   // FC: distrito NO exigido por el validador (schema FC v2 permite direccion null entera)
            Receptor = new FromSmartCareReceptorDto { Nombre = "CF" },
            Lineas = new List<FromSmartCareInvoiceLineDto>
            {
                new() { Descripcion = "x", Cantidad = 1, PrecioUnitario = 10, TipoItem = 2 }
            }
        };

        var result = validator.Validate(request);

        Assert.DoesNotContain(result.Errors, e => e.PropertyName == "Receptor.DistritoId");
    }

    private FromSmartCareInvoiceRequestDto ValidRequest() => new()
    {
        CorrelationId = Guid.NewGuid().ToString(),
        WebhookUrl = "https://smartcare.dev/webhooks/invoice-status",
        ClinicId = "1", VisitId = "2",
        SucursalSmartixId = 1,
        TipoDte = "01",
        Receptor = new() { Nombre = "Consumidor Final" },
        Lineas = new() { new() { Descripcion = "Consulta", Cantidad = 1, PrecioUnitario = 25m, TipoItem = 2 } },
        FormaPagoSugerida = "01",
    };
}
