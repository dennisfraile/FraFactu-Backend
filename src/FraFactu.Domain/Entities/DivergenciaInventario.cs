using FraFactu.Domain.Common;
using FraFactu.Domain.Enums;

namespace FraFactu.Domain.Entities;

/// <summary>
/// F4 (Plan inventario desde DTE): registro de una divergencia entre el
/// stock que mantiene Smartix internamente (<c>StockBodega</c>) y el que
/// SmartInventory expone via snapshot. Una corrida del job que vuelve a
/// observar la misma divergencia <i>actualiza la fila existente</i>
/// (no duplica): asi <see cref="FechaCreacion"/> es la primera deteccion
/// y <see cref="UltimaDeteccion"/> avanza con cada corrida que la vuelve
/// a ver. Cuando una corrida no la observa mas, se marca
/// <c>RESUELTA</c> con <see cref="FechaResolucion"/>.
/// </summary>
public class DivergenciaInventario : BaseEntity
{
    /// <summary>
    /// Id de la corrida del job que toco esta divergencia mas recientemente.
    /// Se renueva en cada update (DETECTADA reaparece, o RESUELTA).
    /// Permite agrupar el reporte de una corrida especifica.
    /// </summary>
    public Guid EjecucionId { get; set; }

    public int EmisorId { get; set; }
    public Emisor Emisor { get; set; } = null!;

    /// <summary>Codigo del producto en Smartix; mismo valor enviado al snapshot.</summary>
    public string CodigoProducto { get; set; } = string.Empty;

    public string NombreBodega { get; set; } = string.Empty;

    /// <summary>Cantidad observada del lado Smartix (StockBodega.Cantidad).</summary>
    public int StockSmartix { get; set; }

    /// <summary>Cantidad observada del lado SmartInventory (snapshot.Cantidad).</summary>
    public int StockSmartInventory { get; set; }

    /// <summary>
    /// <c>StockSmartix - StockSmartInventory</c>. Positivo = Smartix tiene mas;
    /// negativo = SmartInventory tiene mas. Se persiste para facilitar consultas
    /// sin recalcular.
    /// </summary>
    public int Diff { get; set; }

    public EstadoDivergenciaInventario Estado { get; set; } = EstadoDivergenciaInventario.DETECTADA;

    /// <summary>Ultima corrida que vio esta divergencia abierta (incluye la primera).</summary>
    public DateTime UltimaDeteccion { get; set; } = DateTime.UtcNow;

    /// <summary>UTC cuando una corrida observo que la divergencia ya no existe.</summary>
    public DateTime? FechaResolucion { get; set; }
}
