namespace FraFactu.Application.Common.Settings;

public class SmartHubSettings
{
    /// <summary>
    /// API key que SmartHub envía en el header X-Api-Key para endpoints internos
    /// (incoming) y que Smartix envía a SmartHub al consumir endpoints como
    /// /api/auth/validate-code (outgoing).
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// URL base del backend SmartHub (sin slash final). Usado por SmartHubApiService
    /// para llamadas outgoing (SSO validate-code, etc.). Configurar via
    /// SmartHub__BaseUrl en cada entorno: UAT/Demo/Prod apuntan a su Hub correspondiente.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// URL pública del backend de Smartix (sin slash final). Se envía al Hub como
    /// notifyUrl al registrar roles, para que el Hub pueda dispararnos webhooks de
    /// gestión de usuarios. Configurar via SmartHub__SelfUrl por entorno.
    /// </summary>
    public string SelfUrl { get; set; } = string.Empty;
}
