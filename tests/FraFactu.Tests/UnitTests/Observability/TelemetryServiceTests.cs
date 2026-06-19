using FraFactu.Infrastructure.Observability;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;

namespace FraFactu.Tests.UnitTests.Observability;

/// <summary>
/// F7: <see cref="TelemetryService"/> debe atachar el correlationId desde
/// HttpContext.Items a cada customEvent. Si el caller pasa correlationId
/// explicito en properties (background tasks), respeta ese y no lo sobreescribe.
/// </summary>
public class TelemetryServiceTests
{
    private static (TelemetryService svc, CapturingChannel channel, HttpContextAccessor accessor) Build()
    {
        var channel = new CapturingChannel();
        var config = new TelemetryConfiguration
        {
            ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000",
            TelemetryChannel = channel
        };
        var client = new TelemetryClient(config);
        var accessor = new HttpContextAccessor();
        var svc = new TelemetryService(client, accessor);
        return (svc, channel, accessor);
    }

    [Fact]
    public void AtachaCorrelationIdDesdeHttpContext()
    {
        var (svc, channel, accessor) = Build();
        var ctx = new DefaultHttpContext();
        ctx.Items[TelemetryService.CorrelationIdItemKey] = "cid-from-context";
        accessor.HttpContext = ctx;

        svc.TrackEvent("smartix.test.evento",
            properties: new Dictionary<string, string> { ["foo"] = "bar" });

        var evt = Assert.Single(channel.Events);
        Assert.Equal("smartix.test.evento", evt.Name);
        Assert.Equal("cid-from-context", evt.Properties["correlationId"]);
        Assert.Equal("bar", evt.Properties["foo"]);
    }

    [Fact]
    public void NoSobreescribeCorrelationIdExplicito()
    {
        var (svc, channel, accessor) = Build();
        var ctx = new DefaultHttpContext();
        ctx.Items[TelemetryService.CorrelationIdItemKey] = "cid-from-context";
        accessor.HttpContext = ctx;

        svc.TrackEvent("smartix.test.evento",
            properties: new Dictionary<string, string> { ["correlationId"] = "cid-explicit" });

        var evt = Assert.Single(channel.Events);
        Assert.Equal("cid-explicit", evt.Properties["correlationId"]);
    }

    [Fact]
    public void EmiteSinCorrelationIdSiNoHayHttpContext()
    {
        var (svc, channel, _) = Build();

        svc.TrackEvent("smartix.test.background",
            properties: new Dictionary<string, string> { ["foo"] = "bar" });

        var evt = Assert.Single(channel.Events);
        Assert.False(evt.Properties.ContainsKey("correlationId"));
        Assert.Equal("bar", evt.Properties["foo"]);
    }

    [Fact]
    public void IncluyeMeasurementsCuandoVienen()
    {
        var (svc, channel, accessor) = Build();
        accessor.HttpContext = new DefaultHttpContext();

        svc.TrackEvent("smartix.factura.emitida",
            measurements: new Dictionary<string, double> { ["totalPagar"] = 49.99 });

        var evt = Assert.Single(channel.Events);
        Assert.Equal(49.99, evt.Metrics["totalPagar"]);
    }

    private class CapturingChannel : ITelemetryChannel
    {
        public List<EventTelemetry> Events { get; } = new();
        public bool? DeveloperMode { get; set; }
        public string? EndpointAddress { get; set; }

        public void Send(ITelemetry item)
        {
            if (item is EventTelemetry evt) Events.Add(evt);
        }

        public void Flush() { }
        public void Dispose() { }
    }
}
