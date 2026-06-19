using System.Net;
using System.Net.Http.Json;
using FraFactu.Application.Common.Settings;
using FraFactu.Application.DTOs.Integraciones;
using FraFactu.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FraFactu.Infrastructure.Services;

/// <summary>
/// F3 (Plan inventario desde DTE): implementacion HTTP del cliente que
/// habla con SmartInventory. Polly se aplica via el handler registrado en
/// <c>Program.cs</c> (<c>HttpPolicies.CrossAppRetry</c>) para que los
/// 5xx/429/timeouts reintenten antes de bubblear al consumidor del outbox.
/// </summary>
public class SmartInventoryClient : ISmartInventoryClient
{
    private readonly HttpClient _httpClient;
    private readonly SmartInventorySettings _settings;
    private readonly ILogger<SmartInventoryClient> _logger;

    public SmartInventoryClient(
        HttpClient httpClient,
        IOptions<SmartInventorySettings> settings,
        ILogger<SmartInventoryClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        if (_settings.EstaConfigurado)
        {
            // BaseAddress se asigna acá y no en el handler para que sea
            // tolerante a settings con o sin trailing slash.
            _httpClient.BaseAddress = new Uri(_settings.BaseUrl.TrimEnd('/') + "/");
            if (!_httpClient.DefaultRequestHeaders.Contains("X-Api-Key"))
                _httpClient.DefaultRequestHeaders.Add("X-Api-Key", _settings.ApiKey);
        }
    }

    public bool EstaConfigurado => _settings.EstaConfigurado;

    public async Task<SmartInventoryEnvioResultado> RegistrarEntradaAsync(
        SmartInventoryMovimientoRequestDto request,
        CancellationToken ct)
    {
        if (!EstaConfigurado)
            throw new InvalidOperationException(
                "SmartInventory no esta configurado (BaseUrl/ApiKey vacios).");

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsJsonAsync(
                "api/movimientos/entrada", request, ct);
        }
        catch (HttpRequestException ex)
        {
            // Network-level: la politica Polly ya reintento 3 veces, asi que
            // si llegamos aca el error es persistente. El consumidor del
            // outbox decidira si reintentar mas tarde o marcar como FALLIDO.
            _logger.LogWarning(ex,
                "[SmartInventoryClient] Error de red enviando movimiento {Id}",
                request.MovimientoIdExterno);
            throw;
        }

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation(
                "[SmartInventoryClient] Movimiento {Id} enviado OK ({Status})",
                request.MovimientoIdExterno, (int)response.StatusCode);
            return SmartInventoryEnvioResultado.Exito();
        }

        var body = await response.Content.ReadAsStringAsync(ct);

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            // Idempotencia: SmartInventory ya proceso este movimientoIdExterno.
            // Tratamos como exito; no hay que reintentar.
            _logger.LogInformation(
                "[SmartInventoryClient] Movimiento {Id} ya procesado en SmartInventory (409)",
                request.MovimientoIdExterno);
            return SmartInventoryEnvioResultado.Duplicado(body);
        }

        // 400, 401, 403, 404: error de cliente. Reintentar no va a cambiar
        // el resultado; marcamos permanente para que el outbox no insista.
        if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 500)
        {
            _logger.LogError(
                "[SmartInventoryClient] Error permanente {Status} enviando {Id}: {Body}",
                (int)response.StatusCode, request.MovimientoIdExterno, body);
            return SmartInventoryEnvioResultado.ErrorPermanente(
                $"HTTP {(int)response.StatusCode}: {Truncar(body, 500)}");
        }

        // 5xx que sobrevivio a Polly: tratamos como transitorio (lanzar excepcion).
        _logger.LogWarning(
            "[SmartInventoryClient] {Status} transitorio enviando {Id}: {Body}",
            (int)response.StatusCode, request.MovimientoIdExterno, body);
        throw new HttpRequestException(
            $"SmartInventory respondio {(int)response.StatusCode}: {Truncar(body, 200)}");
    }

    public async Task<SmartInventorySnapshotResponseDto> GetSnapshotAsync(
        int organizacionId,
        int? continuationToken,
        int? limit,
        CancellationToken ct)
    {
        if (!EstaConfigurado)
            throw new InvalidOperationException(
                "SmartInventory no esta configurado (BaseUrl/ApiKey vacios).");

        var query = $"api/stock-snapshot?organizacionId={organizacionId}";
        if (continuationToken.HasValue)
            query += $"&continuationToken={continuationToken.Value}";
        if (limit.HasValue)
            query += $"&limit={limit.Value}";

        var response = await _httpClient.GetAsync(query, ct);

        if (response.IsSuccessStatusCode)
        {
            var payload = await response.Content.ReadFromJsonAsync<SmartInventorySnapshotResponseDto>(ct)
                ?? throw new InvalidOperationException(
                    "SmartInventory devolvio 200 con cuerpo vacio en snapshot.");
            return payload;
        }

        var body = await response.Content.ReadAsStringAsync(ct);

        if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 500)
        {
            _logger.LogError(
                "[SmartInventoryClient] Snapshot {Status} org={Org}: {Body}",
                (int)response.StatusCode, organizacionId, body);
            throw new HttpRequestException(
                $"SmartInventory snapshot HTTP {(int)response.StatusCode}: {Truncar(body, 200)}");
        }

        _logger.LogWarning(
            "[SmartInventoryClient] Snapshot {Status} transitorio org={Org}: {Body}",
            (int)response.StatusCode, organizacionId, body);
        throw new HttpRequestException(
            $"SmartInventory snapshot respondio {(int)response.StatusCode}: {Truncar(body, 200)}");
    }

    private static string Truncar(string s, int max) =>
        string.IsNullOrEmpty(s) ? "" : (s.Length <= max ? s : s[..max] + "...");
}
