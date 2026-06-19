using FraFactu.Domain.Common;
using FraFactu.Domain.Enums;

namespace FraFactu.Domain.Entities;

/// <summary>
/// F3 (Plan inventario desde DTE): outbox que persiste los eventos a
/// replicar en SmartInventory. Cuando Smartix confirma una compra externa
/// y el Emisor tiene SmartInventory activo, se encolan registros aqui;
/// el <c>IntegracionInventarioConsumer</c> los procesa en background.
///
/// Sobrevive reinicios del servidor: si el consumidor crashea o
/// SmartInventory esta caido, los eventos siguen en BD hasta que se
/// procesen. Para idempotencia, el endpoint de SmartInventory acepta
/// <c>MovimientoIdExterno</c> y responde 409 si lo recibe dos veces;
/// el consumidor de Smartix interpreta 409 como exito.
/// </summary>
public class IntegracionInventarioPendiente : BaseEntity
{
    /// <summary>
    /// Tipo logico del evento. Por ahora solo <c>MOVIMIENTO_ENTRADA</c>,
    /// pero queda como string para poder agregar SALIDA/AJUSTE en F4 sin
    /// migracion de schema.
    /// </summary>
    public string TipoEvento { get; set; } = "MOVIMIENTO_ENTRADA";

    /// <summary>
    /// Payload serializado que se envia a SmartInventory tal como esta;
    /// el consumidor no lo modifica. Cualquier cambio de contrato implica
    /// versionar el campo (no implementado todavia).
    /// </summary>
    public string PayloadJson { get; set; } = string.Empty;

    public EstadoIntegracionInventario Estado { get; set; } = EstadoIntegracionInventario.ENCOLADO;

    /// <summary>Cantidad acumulada de intentos (cuenta el actual cuando esta EN_PROCESO).</summary>
    public int Intentos { get; set; }

    /// <summary>Mensaje del ultimo error si Estado=FALLIDO o el reintento previo fallo.</summary>
    public string? UltimoError { get; set; }

    /// <summary>
    /// Cuando el consumidor debe volver a intentar este job. Para
    /// <c>ENCOLADO</c> nuevos vale null (=ahora). Para reintentos despues
    /// de un fallo transitorio, el consumidor calcula
    /// <c>now + backoffExponencial(intentos)</c>.
    /// </summary>
    public DateTime? FechaProximoIntento { get; set; }

    /// <summary>UTC cuando se cerro el job (COMPLETADO o FALLIDO definitivo).</summary>
    public DateTime? FechaProcesado { get; set; }

    /// <summary>
    /// Identificador opaco del movimiento del lado Smartix
    /// (formato <c>{compraId}-{detalleId}</c>). Se envia a SmartInventory
    /// como <c>MovimientoIdExterno</c> para idempotencia.
    /// </summary>
    public string MovimientoIdExterno { get; set; } = string.Empty;

    public int EmisorId { get; set; }
    public Emisor Emisor { get; set; } = null!;
}
