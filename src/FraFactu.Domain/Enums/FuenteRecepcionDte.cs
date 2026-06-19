namespace FraFactu.Domain.Enums;

/// <summary>
/// Origen por el que un DTE recibido entró al sistema. Persistido como string
/// vía <c>HasConversion&lt;string&gt;()</c>. <see cref="CORREO"/> es el valor
/// por defecto para preservar las filas existentes, que provienen todas de la
/// lectura del buzón de Gmail del emisor.
/// </summary>
public enum FuenteRecepcionDte
{
    /// <summary>Extraído de un adjunto del correo del emisor (Gmail).</summary>
    CORREO,

    /// <summary>Subido manualmente por el usuario (carga masiva JSON/JWT).</summary>
    CARGA_MANUAL
}
