using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FraFactu.Application.DTOs;
using FraFactu.Application.Interfaces;
using FraFactu.Tests.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace FraFactu.Tests.IntegrationTests.Controllers;

/// <summary>
/// Tests de integración para los endpoints
///   POST /api/from-smartcare/prefills
///   GET  /api/from-smartcare/prefills/{id}
///
/// El servicio se mockea para evitar dependencias de SqlQueryRaw (sintaxis Postgres
/// no soportada por InMemory). Aquí solo verificamos contrato HTTP del controller:
/// auth, validación, idempotencia observable a través del mock, y el shape de la
/// respuesta.
/// </summary>
public class FromSmartCareControllerTests : IClassFixture<FromSmartCareWebApplicationFactory>
{
    private readonly HttpClient _client;
    private const string ValidApiKey = "test-smartcare-api-key";

    public FromSmartCareControllerTests(FromSmartCareWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ───────────────────────────────────────────────────────────────────────
    // Helpers
    // ───────────────────────────────────────────────────────────────────────

    private static FromSmartCareInvoiceRequestDto BuildValidCfPayload(string? correlationId = null) =>
        new()
        {
            CorrelationId = correlationId ?? Guid.NewGuid().ToString(),
            WebhookUrl = "https://smartcare.example.com/webhook",
            SucursalSmartixId = 1,
            TipoDte = "01",
            FormaPagoSugerida = "01",
            Receptor = new FromSmartCareReceptorDto { Nombre = "Paciente de Prueba" },
            Lineas = new List<FromSmartCareInvoiceLineDto>
            {
                new()
                {
                    Descripcion = "Consulta general",
                    Cantidad = 1,
                    PrecioUnitario = 50.00m,
                    TipoItem = 2
                }
            }
        };

    private HttpRequestMessage BuildPostRequest(FromSmartCareInvoiceRequestDto? payload, string? apiKey)
    {
        var msg = new HttpRequestMessage(HttpMethod.Post, "/api/from-smartcare/prefills");

        if (payload is not null)
            msg.Content = JsonContent.Create(payload);

        if (apiKey is not null)
            msg.Headers.Add("X-Api-Key", apiKey);

        return msg;
    }

    // ───────────────────────────────────────────────────────────────────────
    // POST: 401 sin X-Api-Key
    // ───────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Post_Prefills_ReturnsUnauthorized_WhenApiKeyMissing()
    {
        var request = BuildPostRequest(BuildValidCfPayload(), apiKey: null);
        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ───────────────────────────────────────────────────────────────────────
    // POST: 401 con X-Api-Key incorrecta
    // ───────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Post_Prefills_ReturnsUnauthorized_WhenApiKeyWrong()
    {
        var request = BuildPostRequest(BuildValidCfPayload(), apiKey: "wrong-key");
        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ───────────────────────────────────────────────────────────────────────
    // POST: 400 con payload nulo (ApiController lo rechaza automáticamente)
    // ───────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Post_Prefills_ReturnsBadRequest_WhenPayloadNull()
    {
        var msg = new HttpRequestMessage(HttpMethod.Post, "/api/from-smartcare/prefills");
        msg.Content = new StringContent("null", System.Text.Encoding.UTF8, "application/json");
        msg.Headers.Add("X-Api-Key", ValidApiKey);

        var response = await _client.SendAsync(msg);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ───────────────────────────────────────────────────────────────────────
    // POST: 400 con CorrelationId inválido (no es UUID)
    // ───────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Post_Prefills_ReturnsBadRequest_WhenCorrelationIdNotGuid()
    {
        var payload = BuildValidCfPayload();
        payload.CorrelationId = "not-a-guid";

        var response = await _client.SendAsync(BuildPostRequest(payload, ValidApiKey));
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ───────────────────────────────────────────────────────────────────────
    // POST: 200 con payload válido → shape correcto y redirectUrl bien armado
    // ───────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Post_Prefills_ReturnsOk_WithRedirectUrlContainingPrefillId()
    {
        var payload = BuildValidCfPayload();

        var response = await _client.SendAsync(BuildPostRequest(payload, ValidApiKey));
        var responseBody = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, because: $"Body: {responseBody}");

        var body = JsonSerializer.Deserialize<FromSmartCarePrefillResponseDto>(responseBody,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        body.Should().NotBeNull();
        body!.PrefillId.Should().BeGreaterThan(0);
        body.CorrelationId.Should().Be(payload.CorrelationId);
        body.RedirectUrl.Should().Contain("/emision/factura");
        body.RedirectUrl.Should().Contain($"prefillId={body.PrefillId}");
        body.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    // ───────────────────────────────────────────────────────────────────────
    // POST: idempotencia — mismo CorrelationId dos veces → mismo PrefillId
    // ───────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Post_Prefills_IsIdempotent_OnSameCorrelationId()
    {
        var correlationId = Guid.NewGuid().ToString();
        var payload = BuildValidCfPayload(correlationId);

        var r1 = await _client.SendAsync(BuildPostRequest(payload, ValidApiKey));
        var b1Str = await r1.Content.ReadAsStringAsync();
        r1.StatusCode.Should().Be(HttpStatusCode.OK, because: $"Body: {b1Str}");
        var b1 = JsonSerializer.Deserialize<FromSmartCarePrefillResponseDto>(b1Str,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        var r2 = await _client.SendAsync(BuildPostRequest(payload, ValidApiKey));
        var b2Str = await r2.Content.ReadAsStringAsync();
        r2.StatusCode.Should().Be(HttpStatusCode.OK, because: $"Body: {b2Str}");
        var b2 = JsonSerializer.Deserialize<FromSmartCarePrefillResponseDto>(b2Str,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        b1.Should().NotBeNull();
        b2.Should().NotBeNull();
        b2!.PrefillId.Should().Be(b1!.PrefillId);
        b2.CorrelationId.Should().Be(correlationId);
    }

    // ───────────────────────────────────────────────────────────────────────
    // GET: 404 si el prefill no existe
    // ───────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Get_Prefill_ReturnsNotFound_WhenMissing()
    {
        var response = await _client.GetAsync("/api/from-smartcare/prefills/999999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ───────────────────────────────────────────────────────────────────────
    // GET: 200 con shape de FromSmartCarePrefillReadDto
    // ───────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Get_Prefill_ReturnsOk_WithReadDto()
    {
        var response = await _client.GetAsync("/api/from-smartcare/prefills/42");
        var responseBody = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, because: $"Body: {responseBody}");

        var body = JsonSerializer.Deserialize<FromSmartCarePrefillReadDto>(responseBody,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        body.Should().NotBeNull();
        body!.Id.Should().Be(42);
        body.TipoDte.Should().Be("01");
    }

    // ───────────────────────────────────────────────────────────────────────
    // POST CCF: distritoId camelCase en JSON → se deserializa en DistritoId
    //           → validator CCF (NotNull) pasa → mock recibe valor correcto
    // ───────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Post_Prefills_CCF_camelCase_distritoId_deserializa_y_pasa_validacion()
    {
        // Arrange: payload CCF con distritoId en camelCase tal como lo enviaría SmartCare.
        // Se serializa con PropertyNamingPolicy.CamelCase para forzar explícitamente
        // el wire-format camelCase y verificar que el endpoint lo deserializa.
        var payload = new FromSmartCareInvoiceRequestDto
        {
            CorrelationId = Guid.NewGuid().ToString(),
            WebhookUrl = "https://smartcare.example.com/webhook",
            SucursalSmartixId = 1,
            TipoDte = "03",
            Receptor = new FromSmartCareReceptorDto
            {
                TipoDocumento = "36",
                NumeroDocumento = "06141802231019",
                Nombre = "Receptor Test Distrito",
                Nrc = "12345-6",
                CodigoActividad = "47111",
                DepartamentoId = 7,
                MunicipioId = 24,
                DistritoId = 111,
                Direccion = "Calle Demo #1, San Salvador"
            },
            Lineas = new List<FromSmartCareInvoiceLineDto>
            {
                new() { Descripcion = "Consulta general", Cantidad = 1, PrecioUnitario = 50m, TipoItem = 2 }
            }
        };

        var camelCaseOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        var msg = new HttpRequestMessage(HttpMethod.Post, "/api/from-smartcare/prefills")
        {
            Content = JsonContent.Create(payload, options: camelCaseOptions)
        };
        msg.Headers.Add("X-Api-Key", ValidApiKey);

        // Act
        var response = await _client.SendAsync(msg);
        var responseBody = await response.Content.ReadAsStringAsync();

        // Assert: 200 prueba que (a) camelCase fue deserializado, (b) el validador
        // CCF que exige DistritoId != null no rebotó con 400, y (c) el mock respondió.
        response.StatusCode.Should().Be(HttpStatusCode.OK, because:
            $"distritoId=111 debe pasar la validación CCF y llegar al servicio. Body: {responseBody}");
    }
}

/// <summary>
/// Fábrica derivada de <see cref="CustomWebApplicationFactory"/> que:
///  1. Añade <c>SmartCare:ApiKey</c> = "test-smartcare-api-key" a la configuración in-memory.
///  2. Reemplaza <see cref="IFromSmartCarePrefillService"/> con un Mock que devuelve
///     respuestas predecibles, evitando dependencias de SqlQueryRaw que no funciona
///     con el proveedor InMemory de EF.
/// </summary>
public class FromSmartCareWebApplicationFactory : CustomWebApplicationFactory
{
    public const string TestApiKey = "test-smartcare-api-key";

    /// <summary>
    /// Mock del servicio. Por defecto devuelve un PrefillId fijo (42) tanto para
    /// crear como para obtener, lo que permite verificar idempotencia observable.
    /// </summary>
    public Mock<IFromSmartCarePrefillService> ServiceMock { get; } = new();

    private const int MockPrefillId = 42;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SmartCare:ApiKey"] = TestApiKey,
                ["SmartCare:SmartixFrontendBaseUrl"] = "https://smartix.test"
            });
        });

        ServiceMock
            .Setup(s => s.CrearAsync(It.IsAny<FromSmartCareInvoiceRequestDto>()))
            .ReturnsAsync((FromSmartCareInvoiceRequestDto req) => new FromSmartCarePrefillResponseDto
            {
                PrefillId = MockPrefillId,
                CorrelationId = req.CorrelationId,
                RedirectUrl = $"https://smartix.test/emision/factura?prefillId={MockPrefillId}",
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            });

        ServiceMock
            .Setup(s => s.ObtenerAsync(MockPrefillId))
            .ReturnsAsync(new FromSmartCarePrefillReadDto
            {
                Id = MockPrefillId,
                CorrelationId = Guid.NewGuid().ToString(),
                SucursalSmartixId = 1,
                TipoDte = "01",
                Receptor = new FromSmartCareReceptorDto { Nombre = "Paciente de Prueba" },
                Lineas = new List<FromSmartCareInvoiceLineDto>
                {
                    new() { Descripcion = "Consulta", Cantidad = 1, PrecioUnitario = 50m, TipoItem = 2 }
                },
                FormaPagoSugerida = "01",
                Consumed = false
            });

        // Por defecto: cualquier otro Id devuelve null (NotFound).
        ServiceMock
            .Setup(s => s.ObtenerAsync(It.Is<int>(i => i != MockPrefillId)))
            .ReturnsAsync((FromSmartCarePrefillReadDto?)null);

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IFromSmartCarePrefillService));
            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddScoped<IFromSmartCarePrefillService>(_ => ServiceMock.Object);
        });
    }
}
