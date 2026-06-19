namespace FraFactu.Domain.Enums;

/// <summary>
/// F2 (Plan inventario desde DTE): clasificacion contable del producto.
/// Espejo simplificado del enum equivalente de SmartInventory para que el
/// sync futuro sea trivial.
///
/// <list type="bullet">
/// <item><see cref="Ventas"/>: mercaderia destinada a la venta. Stock circulante
///   normal en <c>StockBodega</c>.</item>
/// <item><see cref="MobiliarioEquipo"/>: activo fijo (mobiliario, equipo, vehiculos).
///   NO entra a <c>StockBodega</c>: la entrada se registra en <c>MovimientoInventario</c>
///   pero el "stock circulante" queda en cero. Smartix guarda los datos basicos
///   (fecha adquisicion, valor actual, vida util) pero NO devalua automaticamente;
///   para eso el cliente activa SmartInventory desde el Hub.</item>
/// <item><see cref="Insumos"/>: materiales que se consumen (papeleria, repuestos).
///   Stock circulante como <see cref="Ventas"/> pero filtrable para reportes.</item>
/// </list>
/// </summary>
public enum TipoInventario
{
    Ventas = 0,
    MobiliarioEquipo = 1,
    Insumos = 2
}
