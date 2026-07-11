using System.Net;
using FraFactu.Tests.Fixtures;
using FluentAssertions;

namespace FraFactu.Tests.IntegrationTests;

/// <summary>
/// Verifica que el versionado de API quedó cableado: un endpoint de controller
/// sin versión explícita se resuelve bajo la versión default (v1.0) y la
/// respuesta reporta las versiones soportadas (ReportApiVersions).
/// </summary>
public class ApiVersioningTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ApiVersioningTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ControllerResponse_ReportaVersionSoportada_1_0()
    {
        // Act: endpoint de controller SIN especificar versión → asume default v1.0
        var response = await _client.GetAsync("/api/facturas");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey("api-supported-versions");
        response.Headers.GetValues("api-supported-versions").Should().Contain("1.0");
    }
}
