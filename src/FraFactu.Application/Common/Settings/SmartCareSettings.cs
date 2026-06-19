namespace FraFactu.Application.Common.Settings;

public class SmartCareSettings
{
    /// <summary>
    /// API key que SmartCare envía en el header X-Api-Key al llamar endpoints internos de Smartix.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// API key que Smartix envía en el header X-Smartix-Api-Key al enviar webhooks a SmartCare.
    /// Debe coincidir con SMARTIX_WEBHOOK_API_KEY en SmartCare.
    /// </summary>
    public string WebhookApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Base URL del frontend Vue de Smartix. Usado para construir el redirect del
    /// wizard pre-cargado tras crear un Prefill. Default http://localhost:5180.
    /// </summary>
    public string SmartixFrontendBaseUrl { get; set; } = "http://localhost:5180";
}
