using System.Net.Http.Json;
using System.Text.Json;
using FraFactu.Application.Common.Settings;
using FraFactu.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FraFactu.Infrastructure.Jobs;

/// <summary>
/// BackgroundService que registra el catálogo de roles de Smartix en SmartHub al arrancar.
/// Habilita la asignación cross-app de usuario→rol desde la UI del Hub (Fase 1 del plan
/// de centralización de usuarios). El Hub almacena los roles como JSON en
/// Aplicaciones.RolesDisponibles y los expone a la UI para asignaciones futuras.
/// </summary>
public class HubRoleRegistrationService : BackgroundService
{
    private const string AppCodigo = "facturacion";
    private const string RolAdminGeneral = "EmisorAdmin";
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(5);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IServiceProvider _serviceProvider;
    private readonly SmartHubSettings _settings;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HubRoleRegistrationService> _logger;

    public HubRoleRegistrationService(
        IServiceProvider serviceProvider,
        IOptions<SmartHubSettings> settings,
        IHttpClientFactory httpClientFactory,
        ILogger<HubRoleRegistrationService> logger)
    {
        _serviceProvider = serviceProvider;
        _settings = settings.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(StartupDelay, stoppingToken);
            await RegistrarRolesEnHubAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[HubRoleRegistration] Error al registrar roles en el Hub");
        }
    }

    public async Task RegistrarRolesEnHubAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.BaseUrl) || string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            _logger.LogWarning("[HubRoleRegistration] SmartHub.BaseUrl o SmartHub.ApiKey no configurados. Omitiendo registro.");
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var roles = await context.Roles
            .OrderBy(r => r.Id)
            .Select(r => new RolPayload { Nombre = r.Nombre })
            .ToListAsync(stoppingToken);

        if (roles.Count == 0)
        {
            _logger.LogWarning("[HubRoleRegistration] No hay roles en la BD. Omitiendo registro.");
            return;
        }

        var body = new RegistrarRolesPayload
        {
            Roles = roles,
            RolAdminGeneral = RolAdminGeneral,
            NotifyUrl = ResolveNotifyUrl()
        };

        var url = $"{_settings.BaseUrl.TrimEnd('/')}/api/aplicaciones/{AppCodigo}/roles";
        var client = _httpClientFactory.CreateClient(nameof(HubRoleRegistrationService));

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body, options: JsonOptions)
        };
        request.Headers.Add("X-Api-Key", _settings.ApiKey);

        var response = await client.SendAsync(request, stoppingToken);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation(
                "[HubRoleRegistration] Roles registrados en el Hub: {Count} roles para app '{Codigo}'",
                roles.Count, AppCodigo);
        }
        else
        {
            var errorBody = await response.Content.ReadAsStringAsync(stoppingToken);
            _logger.LogWarning(
                "[HubRoleRegistration] Hub respondió {StatusCode} al registrar roles: {Body}",
                response.StatusCode, errorBody);
        }
    }

    private string ResolveNotifyUrl()
    {
        if (!string.IsNullOrWhiteSpace(_settings.SelfUrl))
            return _settings.SelfUrl.TrimEnd('/');

        _logger.LogWarning("[HubRoleRegistration] SmartHub.SelfUrl no configurada. Hub no podrá disparar webhooks de gestión de usuarios.");
        return string.Empty;
    }

    private sealed class RegistrarRolesPayload
    {
        public List<RolPayload> Roles { get; set; } = new();
        public string RolAdminGeneral { get; set; } = string.Empty;
        public string NotifyUrl { get; set; } = string.Empty;
    }

    private sealed class RolPayload
    {
        public string Nombre { get; set; } = string.Empty;
    }
}
