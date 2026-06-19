namespace FraFactu.Application.DTOs.Auth
{
    /// <summary>
    /// DTO para sincronizar usuario autenticado con Supabase
    /// </summary>
    public class SupabaseSyncDto
    {
        /// <summary>
        /// ID del usuario en Supabase (UUID)
        /// </summary>
        public string SupabaseUserId { get; set; } = string.Empty;

        /// <summary>
        /// Email del usuario (proporcionado por Google)
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Nombre completo del usuario (proporcionado por Google)
        /// </summary>
        public string FullName { get; set; } = string.Empty;
    }
}
