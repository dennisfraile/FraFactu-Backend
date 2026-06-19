namespace FraFactu.Application.Common.Settings
{
    /// <summary>
    /// Configuración de Supabase para autenticación
    /// </summary>
    public class SupabaseSettings
    {
        /// <summary>
        /// URL del proyecto de Supabase
        /// Ejemplo: https://xxxxxxxxxxx.supabase.co
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Service Role Key (clave secreta para operaciones de admin)
        /// Se usa para validar tokens de usuario desde el backend
        /// </summary>
        public string ServiceRoleKey { get; set; } = string.Empty;

        /// <summary>
        /// Anon Key (clave pública)
        /// Se usa para llamadas HTTP a Supabase Auth API
        /// </summary>
        public string AnonKey { get; set; } = string.Empty;
    }
}
