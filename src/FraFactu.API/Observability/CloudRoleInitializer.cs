using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;

namespace FraFactu.API.Observability;

/// <summary>
/// F7: identifica este backend en el App Map de Application Insights.
/// Hardcoded a "smartix-backend" para que los workbooks cross-app filtren
/// por <c>cloud_RoleName</c> sin depender de env vars en Azure App Settings.
/// </summary>
public class CloudRoleInitializer : ITelemetryInitializer
{
    private readonly string _roleName;

    public CloudRoleInitializer(string roleName)
    {
        _roleName = roleName;
    }

    public void Initialize(ITelemetry telemetry)
    {
        if (string.IsNullOrEmpty(telemetry.Context.Cloud.RoleName))
        {
            telemetry.Context.Cloud.RoleName = _roleName;
        }
    }
}
