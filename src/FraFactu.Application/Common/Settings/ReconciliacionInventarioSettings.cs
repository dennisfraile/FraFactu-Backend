namespace FraFactu.Application.Common.Settings;

/// <summary>
/// F4 (Plan inventario desde DTE): configuracion del job periodico de
/// reconciliacion Smartix &lt;-&gt; SmartInventory. Independiente de
/// <see cref="SmartInventorySettings"/> para poder habilitar/deshabilitar
/// reconciliacion sin tocar el outbox de movimientos.
///
/// Si <see cref="Habilitado"/> es false o <see cref="SmartInventorySettings.EstaConfigurado"/>
/// es false, el job no corre (entornos dev sin SmartInventory desplegado).
/// </summary>
public class ReconciliacionInventarioSettings
{
    /// <summary>Cuando false, el BackgroundService no se loopea (queda inactivo).</summary>
    public bool Habilitado { get; set; } = true;

    /// <summary>Periodo entre corridas. Default 6h, suficiente para detectar drift dia a dia.</summary>
    public int IntervaloHoras { get; set; } = 6;
}
