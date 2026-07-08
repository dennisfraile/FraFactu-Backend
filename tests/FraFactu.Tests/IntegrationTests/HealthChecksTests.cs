using System.Net;
using FluentAssertions;
using FraFactu.Tests.Fixtures;

namespace FraFactu.Tests.IntegrationTests;

public class HealthChecksTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    public HealthChecksTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Liveness_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/health");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Readiness_ReturnsOk_WithDatabaseCheckInBody()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/health/ready");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        resp.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        var body = await resp.Content.ReadAsStringAsync();
        body.Should().Contain("\"database\"");
        body.Should().Contain("\"background-services\"");
        body.Should().Contain("\"status\":\"Healthy\"");
    }
}
