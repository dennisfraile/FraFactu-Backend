using FraFactu.Application.DTOs.DtesRecibidos;

namespace FraFactu.Application.Interfaces;

/// <summary>
/// Servicio para gestionar la lectura asíncrona de DTEs desde Gmail.
///
/// Desde F2 el flujo es:
/// <list type="number">
///   <item>El cliente llama <see cref="EncolarLecturaAsync"/> y recibe un <c>jobId</c>.</item>
///   <item>Un consumidor en background toma el job y procesa la bandeja paginando Gmail.</item>
///   <item>El cliente hace polling a <see cref="ObtenerEstadoLecturaAsync"/> con el jobId.</item>
/// </list>
/// </summary>
public interface IEmailReaderService
{
    /// <summary>
    /// Encola una solicitud de lectura para el emisor. Si ya existe un job
    /// ENCOLADO o EN_PROGRESO para ese emisor, devuelve ese mismo en vez de
    /// crear uno nuevo (idempotencia: no acumular trabajo duplicado).
    /// </summary>
    /// <param name="rangoDesde">Inicio del rango UTC a leer (inclusivo). Si es null, default = mes en curso.</param>
    /// <param name="rangoHasta">Fin del rango UTC a leer (exclusivo). Si es null, default = primer día del mes siguiente al actual.</param>
    /// <param name="esAutomatico"><c>true</c> cuando lo dispara el scheduler en background; <c>false</c> para manual.</param>
    Task<EncolarLecturaCorreoResponseDto> EncolarLecturaAsync(
        int emisorId,
        int? usuarioId = null,
        DateTime? rangoDesde = null,
        DateTime? rangoHasta = null,
        bool esAutomatico = false,
        CancellationToken ct = default);

    /// <summary>
    /// Devuelve el job de lectura más reciente del emisor (cualquiera sea su
    /// estado) o <c>null</c> si nunca se ha leído. Pensado para polling de la UI.
    /// </summary>
    Task<EstadoLecturaCorreoJobDto?> ObtenerEstadoLecturaAsync(int emisorId, CancellationToken ct = default);

    /// <summary>
    /// Configura la lectura de correos para un emisor.
    /// </summary>
    Task ConfigurarLecturaCorreoAsync(int emisorId, ConfiguracionLecturaCorreoDto dto);

    /// <summary>
    /// Prueba la conexión OAuth2 de Gmail para lectura.
    /// </summary>
    Task<(bool exitoso, string mensaje)> ProbarConexionGmailAsync(int emisorId);
}
