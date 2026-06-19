using FraFactu.Domain.Common;
using FraFactu.Domain.Enums;

namespace FraFactu.Domain.Entities;

/// <summary>
/// Solicitud de lectura de correo Gmail para un emisor. Se persiste en BD
/// para sobrevivir reinicios del servidor y dejar trazabilidad histórica
/// de cuándo y con qué resultado se leyó la bandeja.
///
/// Ciclo: ENCOLADO → EN_PROGRESO → (COMPLETADO | FALLIDO).
/// </summary>
public class LecturaCorreoJob : BaseEntity
{
    public int EmisorId { get; set; }
    public Emisor Emisor { get; set; } = null!;

    public EstadoLecturaCorreoJob Estado { get; set; } = EstadoLecturaCorreoJob.ENCOLADO;

    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }

    // Contadores en vivo: el consumidor los actualiza durante la ejecución
    // para que la UI pueda mostrarlos via polling.
    public int CorreosProcesados { get; set; }
    public int DtesEncontrados { get; set; }
    public int DtesNuevos { get; set; }
    public int DtesDuplicados { get; set; }
    public int Errores { get; set; }

    /// <summary>
    /// F4: adjuntos que parseaban pero se descartaron por no ser CCF (tipo "03"),
    /// por receptor que no coincide con el emisor, o por contenido invalido.
    /// Se separa de <c>Errores</c> (excepciones tecnicas) para no contaminar el
    /// indicador rojo en la UI; es informativo, no un fallo.
    /// </summary>
    public int DtesIgnorados { get; set; }

    /// <summary>Mensaje de error si el job terminó en estado FALLIDO.</summary>
    public string? MensajeError { get; set; }

    /// <summary>Usuario que disparó la lectura (opcional, para auditoría).</summary>
    public int? IniciadoPorUsuarioId { get; set; }

    /// <summary>
    /// Rango de fechas de Gmail que el worker debe leer (UTC, inclusivo).
    /// En F3 lo asigna el servicio al encolar; null en jobs anteriores a F3
    /// (se interpretan como "mes actual" por compatibilidad).
    /// </summary>
    public DateTime? RangoDesde { get; set; }

    /// <summary>
    /// Rango de fechas de Gmail que el worker debe leer (UTC, exclusivo).
    /// </summary>
    public DateTime? RangoHasta { get; set; }

    /// <summary>
    /// <c>true</c> si el job fue disparado por el scheduler de recepción
    /// automática; <c>false</c> si fue manual desde la UI.
    /// </summary>
    public bool EsAutomatico { get; set; }
}
