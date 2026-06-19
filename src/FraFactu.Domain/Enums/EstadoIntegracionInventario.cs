namespace FraFactu.Domain.Enums;

/// <summary>
/// F3 (Plan inventario desde DTE): estados del outbox que sincroniza
/// movimientos de inventario hacia SmartInventory cuando la app esta
/// activa en el Hub. Ciclo:
/// <c>ENCOLADO -&gt; EN_PROCESO -&gt; (COMPLETADO | FALLIDO)</c>. <c>FALLIDO</c>
/// puede volver a <c>ENCOLADO</c> si el consumidor decide reintentar
/// segun la politica de backoff.
/// </summary>
public enum EstadoIntegracionInventario
{
    ENCOLADO = 0,
    EN_PROCESO = 1,
    COMPLETADO = 2,
    FALLIDO = 3
}
