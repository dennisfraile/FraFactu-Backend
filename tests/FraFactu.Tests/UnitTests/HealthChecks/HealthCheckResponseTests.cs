using System.Text.Json;
using FluentAssertions;
using FraFactu.API.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FraFactu.Tests.UnitTests.HealthChecks;

public class HealthCheckResponseTests
{
    [Fact]
    public async Task WritesJson_WithStatusAndPerCheckEntries()
    {
        var entries = new Dictionary<string, HealthReportEntry>
        {
            ["database"] = new HealthReportEntry(
                HealthStatus.Healthy, description: null, duration: TimeSpan.Zero,
                exception: null, data: null),
            ["background-services"] = new HealthReportEntry(
                HealthStatus.Unhealthy, description: "Job 'X' caído", duration: TimeSpan.Zero,
                exception: null, data: null),
        };
        var report = new HealthReport(entries, HealthStatus.Unhealthy, TimeSpan.Zero);

        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new MemoryStream();

        await HealthCheckResponse.WriteJsonAsync(ctx, report);

        ctx.Response.ContentType.Should().StartWith("application/json");
        ctx.Response.Body.Position = 0;
        using var doc = JsonDocument.Parse(ctx.Response.Body);
        var root = doc.RootElement;
        root.GetProperty("status").GetString().Should().Be("Unhealthy");
        var checks = root.GetProperty("checks").EnumerateArray().ToList();
        checks.Should().HaveCount(2);
        checks.Should().Contain(c => c.GetProperty("name").GetString() == "database"
                                     && c.GetProperty("status").GetString() == "Healthy");
        checks.Should().Contain(c => c.GetProperty("name").GetString() == "background-services"
                                     && c.GetProperty("description").GetString() == "Job 'X' caído");
    }
}
