namespace FraFactu.Application.Common.Settings;

/// <summary>
/// Configuración de la funcionalidad de DTEs Recibidos (F3): scheduler
/// automático + límites de la carga manual con rango de meses.
/// </summary>
public class DtesRecibidosSettings
{
    public RecepcionAutomaticaSettings RecepcionAutomatica { get; set; } = new();

    /// <summary>
    /// Máximo de meses que el usuario puede pedir en una lectura manual
    /// (diferencia entre <c>MesInicio</c> y <c>MesFin</c>, ambos inclusivos).
    /// Default 12: cubre un año completo, evita pedir rangos enormes.
    /// </summary>
    public int LimiteMesesManual { get; set; } = 12;
}

public class RecepcionAutomaticaSettings
{
    /// <summary>Toggle global del scheduler. Apagarlo deshabilita la recepción automática.</summary>
    public bool Habilitada { get; set; } = true;

    /// <summary>Cada cuántos minutos el scheduler recorre los emisores habilitados.</summary>
    public int IntervaloMinutos { get; set; } = 30;

    /// <summary>
    /// Cooldown por emisor: no encolar otra lectura automática si la última
    /// terminó hace menos de este tiempo. Evita martillar Gmail cuando el
    /// intervalo de barrido es más corto que el tiempo real entre lecturas.
    /// </summary>
    public int CooldownHoras { get; set; } = 2;

    /// <summary>Espera inicial antes del primer barrido (deja que la app levante limpia).</summary>
    public int EsperaInicialSegundos { get; set; } = 120;
}
