using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using FraFactu.Application.Common.Interfaces;
using FraFactu.Application.Common.Settings;
using FraFactu.Domain.Entities;
using FraFactu.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace FraFactu.Tests.Services;

/// <summary>
/// Tests del SmartCareWebhookService. Cubre el bug observado en UAT 2026-05-14
/// (visit 5b9a5c30-...): el payload se serializaba camelCase por la config global
/// de Smartix.API (Program.cs:274), pero SmartCare lee con PascalCase y respondia
/// 400 "Faltan campos requeridos". El fix fuerza PascalCase localmente en el
/// HttpContent del webhook sin afectar la config global de la API.
/// </summary>
public class SmartCareWebhookServiceTests
{
    private static IOptions<SmartCareSettings> Settings() =>
        Options.Create(new SmartCareSettings
        {
            ApiKey = "ignored",
            WebhookApiKey = "test-webhook-key",
            SmartixFrontendBaseUrl = "http://localhost:5180"
        });

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? CapturedRequest { get; private set; }
        public string? CapturedBody { get; private set; }
        public HttpStatusCode ResponseStatus { get; set; } = HttpStatusCode.OK;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CapturedRequest = request;
            CapturedBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(ResponseStatus);
        }
    }

    private static (SmartCareWebhookService Service, CapturingHandler Handler, Mock<ITelemetryService> Telemetry) BuildService()
    {
        var handler = new CapturingHandler();
        var http = new HttpClient(handler);
        var telemetry = new Mock<ITelemetryService>();
        var service = new SmartCareWebhookService(
            http,
            Settings(),
            NullLogger<SmartCareWebhookService>.Instance,
            telemetry.Object);
        return (service, handler, telemetry);
    }

    private static FacturaElectronica FacturaProcesada() => new()
    {
        Id = 35,
        SmartCareCorrelationId = "ebde3f85-17e7-447a-95da-61c9a454dc5a",
        SmartCareWebhookUrl = "https://api-smartcare-uat.jdsmartcode.com/api/v1/smartix-webhooks/invoice-status",
        SmartCareClinicId = "861e9f5b-cacb-4fe4-a114-66c8a04e8b41",
        SmartCareVisitId = "5b9a5c30-5ce4-42af-bbd9-80b4e1aeca13",
        EstadoHacienda = "PROCESADO",
        CodigoGeneracion = "1884F153-38F8-4980-AA63-D68C6ABB09FD",
        NumeroControl = "DTE-01-M001P001-000000000000218",
        SelloRecibido = "202610E2154E14BD435896F288BED1EC1562QOQ9"
    };

    [Fact]
    public async Task NotificarCambioEstadoAsync_SerializaPayloadEnPascalCase()
    {
        var (service, handler, _) = BuildService();
        await service.NotificarCambioEstadoAsync(FacturaProcesada());

        handler.CapturedBody.Should().NotBeNullOrEmpty();
        using var doc = JsonDocument.Parse(handler.CapturedBody!);
        var root = doc.RootElement;

        // SmartCare lee con PascalCase (req.body.CorrelationId etc.). Garantizamos
        // que los nombres clave existan en esa forma — bug de UAT 2026-05-14.
        root.TryGetProperty("CorrelationId", out _).Should().BeTrue("SmartCare lee CorrelationId");
        root.TryGetProperty("Estado", out _).Should().BeTrue("SmartCare lee Estado");
        root.TryGetProperty("CodigoGeneracion", out _).Should().BeTrue();
        root.TryGetProperty("NumeroControl", out _).Should().BeTrue();
        root.TryGetProperty("SelloRecibido", out _).Should().BeTrue();
        root.TryGetProperty("Timestamp", out _).Should().BeTrue();

        // Y que NO se serialice en camelCase, para que el contrato quede claro.
        root.TryGetProperty("correlationId", out _).Should().BeFalse("evita ambiguedad PascalCase/camelCase");
        root.TryGetProperty("estado", out _).Should().BeFalse();
    }

    [Fact]
    public async Task NotificarCambioEstadoAsync_IncluyeApiKeyHeader()
    {
        var (service, handler, _) = BuildService();
        await service.NotificarCambioEstadoAsync(FacturaProcesada());

        handler.CapturedRequest!.Headers.TryGetValues("X-Smartix-Api-Key", out var values)
            .Should().BeTrue();
        values!.Single().Should().Be("test-webhook-key");
    }

    [Fact]
    public async Task NotificarCambioEstadoAsync_NoLlamaSiFaltaWebhookUrl()
    {
        var (service, handler, _) = BuildService();
        var factura = FacturaProcesada();
        factura.SmartCareWebhookUrl = null;

        await service.NotificarCambioEstadoAsync(factura);

        handler.CapturedRequest.Should().BeNull("sin webhook url no hay nada que llamar");
    }

    [Fact]
    public async Task NotificarCambioEstadoAsync_NoLlamaSiFaltaCorrelationId()
    {
        var (service, handler, _) = BuildService();
        var factura = FacturaProcesada();
        factura.SmartCareCorrelationId = null;

        await service.NotificarCambioEstadoAsync(factura);

        handler.CapturedRequest.Should().BeNull("sin correlation no hay con qué correlacionar en SmartCare");
    }

    [Fact]
    public async Task NotificarCambioEstadoAsync_CuandoResponseEsNoExitoso_TrackeaTelemetryConStatusCode()
    {
        // Reproduce el escenario del bug original (HTTP 400 PascalCase mismatch):
        // el webhook responde no-2xx, el catch silencioso traga el error, pero
        // queda registro en Application Insights con resultado=error y statusCode
        // — exactamente la query que se uso para diagnosticar el bug UAT.
        var (service, handler, telemetry) = BuildService();
        handler.ResponseStatus = HttpStatusCode.BadRequest;

        await service.NotificarCambioEstadoAsync(FacturaProcesada());

        // No throw — fire-and-forget tragó el error pero...
        handler.CapturedRequest.Should().NotBeNull("el webhook se llamo");

        // ...el evento de telemetry tiene que registrar el statusCode + resultado=error
        telemetry.Verify(t => t.TrackEvent(
            "smartix.webhook.enviado",
            It.Is<IDictionary<string, string>>(d =>
                d["resultado"] == "error" &&
                d["statusCode"] == "400" &&
                d["destino"] == "smartcare"),
            It.IsAny<IDictionary<string, double>?>()),
            Times.Once,
            "El telemetry debe registrar el statusCode para que se pueda diagnosticar el bug en AI.");
    }

    [Fact]
    public async Task NotificarCambioEstadoAsync_CuandoResponseEsOk_TrackeaResultadoOk()
    {
        var (service, handler, telemetry) = BuildService();
        handler.ResponseStatus = HttpStatusCode.OK;

        await service.NotificarCambioEstadoAsync(FacturaProcesada());

        telemetry.Verify(t => t.TrackEvent(
            "smartix.webhook.enviado",
            It.Is<IDictionary<string, string>>(d =>
                d["resultado"] == "ok" &&
                d["statusCode"] == "200"),
            It.IsAny<IDictionary<string, double>?>()),
            Times.Once);
    }

    [Fact]
    public async Task NotificarCambioEstadoAsync_IncluyeInvoiceIdYViewUrlEnPayload()
    {
        // Fix bug "Ver en Smartix → wizard" (2026-05-18). SmartCare necesita el
        // InvoiceId para descolar el fallback de polling huérfano y la ViewUrl
        // para que el botón lleve a la pantalla correcta en vez de al wizard.
        var (service, handler, _) = BuildService();
        await service.NotificarCambioEstadoAsync(FacturaProcesada());

        using var doc = JsonDocument.Parse(handler.CapturedBody!);
        var root = doc.RootElement;

        root.GetProperty("InvoiceId").GetInt32().Should().Be(35);
        root.GetProperty("ViewUrl").GetString()
            .Should().Be("http://localhost:5180/historial?numeroControl=DTE-01-M001P001-000000000000218",
                "PROCESADO mapea a /historial con numeroControl url-encoded");
    }

    [Theory]
    [InlineData("PROCESADO", "DTE-01", null, "http://localhost:5180/historial?numeroControl=DTE-01")]
    [InlineData("RECHAZADO", "DTE-01", null, "http://localhost:5180/historial?numeroControl=DTE-01")]
    [InlineData("PENDIENTE_ENVIO", "DTE-02", null, "http://localhost:5180/facturas-pendientes?numeroControl=DTE-02")]
    [InlineData("PENDIENTE_LOTE", "DTE-03", null, "http://localhost:5180/facturas-pendientes?numeroControl=DTE-03")]
    [InlineData("PENDIENTE_LOTE", "DTE-04", 42, "http://localhost:5180/contingencia?numeroControl=DTE-04")] // En lote de contingencia
    [InlineData("EN_CONTINGENCIA", "DTE-05", null, "http://localhost:5180/contingencia?numeroControl=DTE-05")]
    public void BuildViewUrl_ConEstadoMapeadoYNumeroControl_DevuelveUrlCorrecta(
        string estado, string numeroControl, int? eventoContingenciaId, string expected)
    {
        var (service, _, _) = BuildService();
        service.BuildViewUrl(estado, numeroControl, eventoContingenciaId).Should().Be(expected);
    }

    [Theory]
    [InlineData("PROCESADO", null)]
    [InlineData("PROCESADO", "")]
    [InlineData("PROCESADO", "   ")]
    [InlineData("INICIADO", "DTE-01")]      // Estado intermedio sin pantalla
    [InlineData("DESCONOCIDO", "DTE-01")]   // Estado no mapeado
    public void BuildViewUrl_SinDestinoValido_DevuelveNull(string estado, string? numeroControl)
    {
        var (service, _, _) = BuildService();
        service.BuildViewUrl(estado, numeroControl, null).Should().BeNull();
    }
}
