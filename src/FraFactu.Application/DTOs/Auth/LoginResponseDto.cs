namespace FraFactu.Application.DTOs.Auth
{
    /// <summary>
    /// DTO para respuesta de inicio de sesión exitoso
    /// </summary>
    public class LoginResponseDto
    {
        /// <summary>
        /// Token JWT
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de token (Bearer)
        /// </summary>
        public string TokenType { get; set; } = "Bearer";

        /// <summary>
        /// Duración del token en segundos
        /// </summary>
        public int ExpiresIn { get; set; }

        /// <summary>
        /// ID del usuario
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Nombre completo del usuario
        /// </summary>
        public string NombreCompleto { get; set; } = string.Empty;

        /// <summary>
        /// Email del usuario
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// ID del rol del usuario
        /// </summary>
        public int RolId { get; set; }

        /// <summary>
        /// Nombre del rol del usuario
        /// </summary>
        public string RolNombre { get; set; } = string.Empty;

        /// <summary>
        /// ID del emisor (tenant) al que pertenece
        /// </summary>
        public int? EmisorId { get; set; }

        /// <summary>
        /// Nombre del emisor
        /// </summary>
        public string? EmisorNombre { get; set; }

        /// <summary>
        /// Si tiene acceso a todas las sucursales del emisor
        /// </summary>
        public bool AccesoTodasSucursales { get; set; }

        /// <summary>
        /// IDs de las sucursales asignadas al usuario
        /// </summary>
        public List<int> SucursalIds { get; set; } = new();

        /// <summary>
        /// Lista de permisos del usuario
        /// </summary>
        public List<string> Permisos { get; set; } = new List<string>();

        /// <summary>
        /// Indica si requiere cambio de contraseña
        /// </summary>
        public bool RequiereCambioPwd { get; set; }
    }
}
