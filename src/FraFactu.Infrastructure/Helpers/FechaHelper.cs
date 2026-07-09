namespace FraFactu.Infrastructure.Helpers;

/// <summary>
/// Helpers de fecha/hora para consultas contra columnas timestamptz (Npgsql).
/// </summary>
public static class FechaHelper
{
    /// <summary>
    /// Normaliza un DateTime a UTC preservando la hora.
    /// Kind=Utc → sin cambio; Kind=Local → convierte a UTC; Kind=Unspecified → asume UTC
    /// (fechas de query string sin offset; evita el 500 de Npgsql contra timestamptz).
    /// </summary>
    public static DateTime ToUtc(DateTime d) => d.Kind switch
    {
        DateTimeKind.Utc => d,
        DateTimeKind.Local => d.ToUniversalTime(),
        _ => DateTime.SpecifyKind(d, DateTimeKind.Utc)
    };
}
