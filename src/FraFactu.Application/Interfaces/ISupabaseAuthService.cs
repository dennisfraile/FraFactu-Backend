using FraFactu.Application.DTOs.Auth;

namespace FraFactu.Application.Interfaces
{
    /// <summary>
    /// Servicio para integración con Supabase Auth
    /// </summary>
    public interface ISupabaseAuthService
    {
        /// <summary>
        /// Valida un token de Supabase (JWT access token)
        /// </summary>
        /// <param name="token">Token de sesión de Supabase</param>
        /// <returns>True si el token es válido</returns>
        Task<bool> ValidateSupabaseToken(string token);

        /// <summary>
        /// Sincroniza un usuario autenticado con Supabase con la base de datos local
        /// Crea el usuario si no existe, o actualiza su información si ya existe
        /// </summary>
        /// <param name="dto">Datos del usuario de Supabase</param>
        /// <returns>Respuesta con token JWT y datos del usuario para la aplicación</returns>
        Task<LoginResponseDto?> SyncSupabaseUser(SupabaseSyncDto dto);
    }
}
