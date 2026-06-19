namespace FraFactu.Application.Common.Settings;

/// <summary>
/// F3 (Plan inventario desde DTE): configuracion del cliente HTTP que
/// replica movimientos hacia SmartInventory. Se inyecta via
/// <c>IOptions&lt;SmartInventorySettings&gt;</c>.
///
/// En entornos sin SmartInventory desplegado (dev sin docker-compose,
/// tests sin BG service) basta con dejar la seccion vacia: el cliente
/// expone <see cref="EstaConfigurado"/> para que el consumidor decida
/// si saltarse el envio.
/// </summary>
public class SmartInventorySettings
{
    /// <summary>URL base de SmartInventory (incluye trailing slash o no).</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>API key compartida; viaja en el header <c>X-Api-Key</c>.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Polling del consumidor en segundos. Default 5s; bajar a 1-2s en dev
    /// para iterar rapido. Subir a 30s+ en prod si el volumen es bajo.
    /// </summary>
    public int PollingSeconds { get; set; } = 5;

    /// <summary>
    /// Tope de reintentos antes de marcar el job como FALLIDO permanente
    /// (sin volver a encolar). Default 10 (~3.5 dias con backoff exponencial).
    /// </summary>
    public int MaxIntentos { get; set; } = 10;

    /// <summary>True cuando hay BaseUrl y ApiKey configurados.</summary>
    public bool EstaConfigurado =>
        !string.IsNullOrWhiteSpace(BaseUrl) && !string.IsNullOrWhiteSpace(ApiKey);
}
