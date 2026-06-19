namespace FraFactu.Application.Interfaces.Hacienda
{
    public interface IHaciendaAuthService
    {
        /// <summary>
        /// Obtiene un token válido para comunicarse con el Ministerio de Hacienda.
        /// Maneja el caché y la renovación automática si ha expirado.
        /// </summary>
        /// <param name="emisorId">ID del emisor en base de datos para recuperar sus credenciales</param>
        Task<string> ObtenerTokenAsync(int emisorId);
    }
}
