using FraFactu.Application.Common.Interfaces;
using FraFactu.Application.Common.Settings;
using FraFactu.Application.DTOs;
using FraFactu.Application.Interfaces;
using FraFactu.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;

namespace FraFactu.Infrastructure.Services;

public class SmartCareWebhookService : ISmartCareWebhookService
{
    private readonly HttpClient _http;
    private readonly SmartCareSettings _settings;
    private readonly ILogger<SmartCareWebhookService> _logger;
    private readonly ITelemetryService _telemetry;

    // PascalCase forzado para el payload del webhook. Smartix.API esta configurado
    // global con CamelCase (Program.cs), pero SmartCare espera los campos en
    // PascalCase (CorrelationId, Estado, ...) — sin estas options el webhook
    // serializa camelCase, SmartCare lee undefined y responde 400.
    // Bug reproducido en UAT 2026-05-14 visit 5b9a5c30-...; AI confirmaba
    // statusCode=400 en customEvents.smartix.webhook.enviado.
    private static readonly JsonSerializerOptions PascalCaseOptions = new()
    {
        PropertyNamingPolicy = null
    };

    public SmartCareWebhookService(
        HttpClient http,
        IOptions<SmartCareSettings> settings,
        ILogger<SmartCareWebhookService> logger,
        ITelemetryService telemetry)
    {
        _http = http;
        _settings = settings.Value;
        _logger = logger;
        _telemetry = telemetry;
    }

    public async Task NotificarCambioEstadoAsync(FacturaElectronica factura)
    {
        if (string.IsNullOrEmpty(factura.SmartCareWebhookUrl) ||
            string.IsNullOrEmpty(factura.SmartCareCorrelationId))
            return;

        var payload = new SmartCareWebhookPayloadDto
        {
            CorrelationId = factura.SmartCareCorrelationId,
            VisitId = factura.SmartCareVisitId,
            ClinicId = factura.SmartCareClinicId,
            Estado = factura.EstadoHacienda,
            CodigoGeneracion = factura.CodigoGeneracion,
            NumeroControl = factura.NumeroControl,
            SelloRecibido = factura.SelloRecibido,
            Timestamp = DateTime.UtcNow,
            InvoiceId = factura.Id,
            ViewUrl = BuildViewUrl(factura.EstadoHacienda, factura.NumeroControl, factura.EventoContingenciaId)
        };

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, factura.SmartCareWebhookUrl);
            request.Headers.Add("X-Smartix-Api-Key", _settings.WebhookApiKey);
            request.Content = JsonContent.Create(payload, options: PascalCaseOptions);

            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Webhook SmartCare retornó {Status} para CorrelationId={CorrelationId}.",
                    (int)response.StatusCode, payload.CorrelationId);
            }
            else
            {
                _logger.LogInformation(
                    "Webhook SmartCare enviado. Estado={Estado} CorrelationId={CorrelationId}.",
                    payload.Estado, payload.CorrelationId);
            }

            _telemetry.TrackEvent("smartix.webhook.enviado",
                properties: new Dictionary<string, string>
                {
                    ["correlationId"] = payload.CorrelationId ?? string.Empty,
                    ["destino"] = "smartcare",
                    ["estado"] = payload.Estado ?? string.Empty,
                    ["resultado"] = response.IsSuccessStatusCode ? "ok" : "error",
                    ["statusCode"] = ((int)response.StatusCode).ToString(),
                    ["facturaId"] = factura.Id.ToString()
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error enviando webhook a SmartCare. Url={Url} CorrelationId={CorrelationId}.",
                factura.SmartCareWebhookUrl, payload.CorrelationId);

            _telemetry.TrackEvent("smartix.webhook.enviado",
                properties: new Dictionary<string, string>
                {
                    ["correlationId"] = payload.CorrelationId ?? string.Empty,
                    ["destino"] = "smartcare",
                    ["estado"] = payload.Estado ?? string.Empty,
                    ["resultado"] = "exception",
                    ["error"] = ex.Message,
                    ["facturaId"] = factura.Id.ToString()
                });
        }
    }

    // Genera el deep-link al frontend de Smartix según el estado. SmartCare lo
    // persiste para que el botón "Ver en Smartix" lleve a la pantalla correcta
    // (Historial / Pendientes / Contingencia) — antes apuntaba al wizard de
    // emisión y se podía crear una factura duplicada por accidente.
    //
    // Reglas de mapeo:
    //  - PROCESADO / RECHAZADO  → /historial (Smartix-FE no tiene vista
    //    dedicada de rechazados; el filtro por NumeroControl basta).
    //  - PENDIENTE_ENVIO        → /facturas-pendientes (queue normal).
    //  - PENDIENTE_LOTE         → depende: si la factura tiene un
    //    EventoContingenciaId, está esperando en un lote de contingencia y
    //    el usuario espera verla en /contingencia (tab Diferidos). Si no, es
    //    un lote regular (transmisión diferida, caso raro) y se ve en
    //    /facturas-pendientes con el filtro de estado.
    //  - EN_CONTINGENCIA es un estado de Evento, no de Factura, pero lo
    //    dejamos como red de seguridad por si llegara a aparecer.
    public string? BuildViewUrl(string estadoHacienda, string? numeroControl, int? eventoContingenciaId)
    {
        if (string.IsNullOrWhiteSpace(numeroControl)) return null;

        var baseUrl = (_settings.SmartixFrontendBaseUrl ?? string.Empty).TrimEnd('/');
        if (string.IsNullOrEmpty(baseUrl)) return null;

        var encoded = Uri.EscapeDataString(numeroControl);

        return estadoHacienda switch
        {
            "PROCESADO" or "RECHAZADO" => $"{baseUrl}/historial?numeroControl={encoded}",
            "PENDIENTE_ENVIO" => $"{baseUrl}/facturas-pendientes?numeroControl={encoded}",
            "PENDIENTE_LOTE" when eventoContingenciaId.HasValue
                => $"{baseUrl}/contingencia?numeroControl={encoded}",
            "PENDIENTE_LOTE" => $"{baseUrl}/facturas-pendientes?numeroControl={encoded}",
            "EN_CONTINGENCIA" => $"{baseUrl}/contingencia?numeroControl={encoded}",
            _ => null
        };
    }
}
