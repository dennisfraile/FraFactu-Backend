namespace FraFactu.Application.Interfaces;

/// <summary>
/// Servicio para obtener información del usuario actual autenticado
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Obtiene el ID del usuario actual desde el contexto HTTP (JWT Claims)
    /// </summary>
    /// <returns>ID del usuario autenticado</returns>
    int? GetUsuarioId();

    /// <summary>
    /// Obtiene el nombre o email del usuario actual
    /// </summary>
    /// <returns>Nombre o email del usuario</returns>
    string? GetUsuarioNombre();

    /// <summary>
    /// Obtiene el ID del Emisor asociado al usuario actual
    /// </summary>
    /// <returns>ID del emisor</returns>
    int GetEmisorId();

    /// <summary>
    /// Indica si existe un usuario autenticado en el contexto actual
    /// </summary>
    bool IsAuthenticated();
}
