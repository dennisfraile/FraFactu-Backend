using FraFactu.Application.DTOs.Integraciones;

namespace FraFactu.Application.Interfaces;

/// <summary>
/// F3 (Plan inventario desde DTE): cliente HTTP que Smartix usa para
/// notificar movimientos a SmartInventory. Implementacion via
/// <c>HttpClientFactory</c> con politica Polly compartida
/// (<c>HttpPolicies.CrossAppRetry</c>).
/// </summary>
public interface ISmartInventoryClient
{
    /// <summary>
    /// True si hay configuracion (URL + API key). El consumidor del outbox
    /// usa este flag para saltarse el ciclo cuando SmartInventory no esta
    /// desplegado (entornos dev sin la app).
    /// </summary>
    bool EstaConfigurado { get; }

    /// <summary>
    /// Envia una entrada de inventario a SmartInventory. Devuelve el resultado
    /// procesado por SmartInventory o lanza una excepcion si la llamada falla.
    /// El consumidor distingue entre exception transitoria (reintentar) e
    /// idempotente 409 (tratar como exito).
    /// </summary>
    Task<SmartInventoryEnvioResultado> RegistrarEntradaAsync(
        SmartInventoryMovimientoRequestDto request,
        CancellationToken ct);

    /// <summary>
    /// F4: consulta una pagina del snapshot de stock de un Emisor en
    /// SmartInventory. Read-only; sin reintento persistente (Polly cubre
    /// transitorios y si falla bubblea para que el job reagende).
    /// <paramref name="continuationToken"/> null en la primera llamada;
    /// pasar luego el valor recibido en la respuesta hasta que sea null.
    /// </summary>
    Task<SmartInventorySnapshotResponseDto> GetSnapshotAsync(
        int organizacionId,
        int? continuationToken,
        int? limit,
        CancellationToken ct);
}

/// <summary>Resultado del envio para que el consumidor decida transicion del job.</summary>
public class SmartInventoryEnvioResultado
{
    /// <summary>True si SmartInventory respondio 2xx o 409 (idempotencia exitosa).</summary>
    public bool EsExito { get; init; }

    /// <summary>True si SmartInventory respondio 409 (movimiento ya procesado antes).</summary>
    public bool EsDuplicado { get; init; }

    /// <summary>
    /// True si SmartInventory respondio 4xx (no 409); el error es permanente
    /// y el consumidor debe marcar el job como FALLIDO sin reintentar.
    /// </summary>
    public bool EsErrorPermanente { get; init; }

    /// <summary>Mensaje del servidor (cuerpo de la respuesta de error o detalle).</summary>
    public string? Mensaje { get; init; }

    public static SmartInventoryEnvioResultado Exito() =>
        new() { EsExito = true };

    public static SmartInventoryEnvioResultado Duplicado(string? mensaje = null) =>
        new() { EsExito = true, EsDuplicado = true, Mensaje = mensaje };

    public static SmartInventoryEnvioResultado ErrorPermanente(string mensaje) =>
        new() { EsExito = false, EsErrorPermanente = true, Mensaje = mensaje };
}
