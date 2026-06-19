namespace FraFactu.Domain.Enums
{
    /// <summary>
    /// Proveedor de autenticación del usuario
    /// </summary>
    public enum ProveedorAutenticacion
    {
        /// <summary>
        /// Autenticación local con email/contraseña
        /// </summary>
        Local = 1,

        /// <summary>
        /// Autenticación mediante Google OAuth 2.0
        /// </summary>
        Google = 2,

        /// <summary>
        /// SSO desde SmartHub (exchange code via /api/Auth/hub-login).
        /// </summary>
        SmartHub = 3
    }
}
