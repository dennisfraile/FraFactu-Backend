using FluentAssertions;
using FraFactu.API.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace FraFactu.Tests.UnitTests.HealthChecks;

public class BackgroundServicesHealthCheckTests
{
    // Fake que permite controlar el cuerpo (y por tanto el estado de ExecuteTask).
    private sealed class FakeBackgroundService : BackgroundService
    {
        private readonly Func<CancellationToken, Task> _body;
        public FakeBackgroundService(Func<CancellationToken, Task> body) => _body = body;
        protected override Task ExecuteAsync(CancellationToken stoppingToken) => _body(stoppingToken);
    }

    private static async Task<FakeBackgroundService> RunningAsync()
    {
        var svc = new FakeBackgroundService(ct => Task.Delay(Timeout.Infinite, ct));
        await svc.StartAsync(CancellationToken.None);
        return svc; // ExecuteTask corriendo (nunca completa)
    }

    private static async Task<FakeBackgroundService> FaultedAsync()
    {
        var svc = new FakeBackgroundService(_ => Task.FromException(new InvalidOperationException("boom")));
        try { await svc.StartAsync(CancellationToken.None); } catch { /* la tarea faulted se propaga por StartAsync */ }
        return svc; // ExecuteTask.IsFaulted == true
    }

    private static async Task<FakeBackgroundService> CompletedAsync()
    {
        var svc = new FakeBackgroundService(_ => Task.CompletedTask);
        await svc.StartAsync(CancellationToken.None);
        return svc; // ExecuteTask.IsCompletedSuccessfully == true
    }

    private static Task<HealthCheckResult> Check(params IHostedService[] services)
        => new BackgroundServicesHealthCheck(services).CheckHealthAsync(new HealthCheckContext());

    [Fact]
    public async Task Healthy_WhenAllRunning()
    {
        var result = await Check(await RunningAsync(), await RunningAsync());
        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task Unhealthy_WhenOneFaulted()
    {
        var faulted = await FaultedAsync();
        var result = await Check(await RunningAsync(), faulted);
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Data.Should().ContainKey(nameof(FakeBackgroundService));
    }

    [Fact]
    public async Task Unhealthy_WhenOneCompleted()
    {
        var result = await Check(await RunningAsync(), await CompletedAsync());
        result.Status.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task Ignores_NotStartedService_TreatsAsHealthy()
    {
        // Sin StartAsync => ExecuteTask == null => se ignora.
        var notStarted = new FakeBackgroundService(ct => Task.Delay(Timeout.Infinite, ct));
        var result = await Check(await RunningAsync(), notStarted);
        result.Status.Should().Be(HealthStatus.Healthy);
    }
}
