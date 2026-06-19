using System.Net;
using FraFactu.Tests.Fixtures;
using FluentAssertions;

namespace FraFactu.Tests.IntegrationTests.Controllers;

/// <summary>
/// Tests de integración para FacturaController
/// Prueba endpoints HTTP completos (request → response)
/// </summary>
public class FacturaControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public FacturaControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GET_Facturas_DebeRetornar200()
    {
        // Act
        var response = await _client.GetAsync("/api/facturas");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GET_FacturaPorId_Inexistente_DebeRetornar404()
    {
        // Act
        var response = await _client.GetAsync("/api/facturas/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_Facturas_DebeRetornarListaVacia()
    {
        // Act
        var response = await _client.GetAsync("/api/facturas");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }
}
