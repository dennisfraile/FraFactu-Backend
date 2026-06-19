namespace FraFactu.Domain.Enums;

/// <summary>
/// Ciclo de vida de una solicitud de lectura de correo. Persistido como
/// string vía <c>HasConversion&lt;string&gt;()</c>.
/// </summary>
public enum EstadoLecturaCorreoJob
{
    /// <summary>El usuario solicitó la lectura; aún no la toma el consumidor.</summary>
    ENCOLADO,

    /// <summary>El consumidor en background está procesando la bandeja Gmail.</summary>
    EN_PROGRESO,

    /// <summary>La lectura terminó correctamente (los contadores reflejan el resultado).</summary>
    COMPLETADO,

    /// <summary>La lectura abortó por un error; ver <c>MensajeError</c>.</summary>
    FALLIDO
}
