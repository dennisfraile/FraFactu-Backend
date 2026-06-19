namespace FraFactu.Application.Interfaces;

/// <summary>
/// Ejecuta una lectura de correo Gmail para un <c>LecturaCorreoJob</c> ya
/// marcado como <c>EN_PROGRESO</c>. Paginación completa con <c>pageToken</c>
/// (no hay tope fijo de mensajes). Actualiza los contadores en vivo del job
/// para que la UI pueda hacer polling y avanza
/// <c>Emisor.UltimaLecturaCorreo</c> solo al finalizar correctamente.
///
/// Una excepción aquí debe ser capturada por el consumidor para marcar
/// el job como FALLIDO con el mensaje correspondiente.
/// </summary>
public interface IEmailLectorWorker
{
    Task EjecutarAsync(int jobId, CancellationToken ct = default);
}
