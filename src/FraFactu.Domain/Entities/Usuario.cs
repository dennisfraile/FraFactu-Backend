using FraFactu.Domain.Common;
using FraFactu.Domain.Enums;

namespace FraFactu.Domain.Entities
{
    public class Usuario : BaseEntity
    {
        // DATOS PERSONALES
        public string NombreCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty; // Login
        public string? PasswordHash { get; set; } // Nullable para usuarios con OAuth

        // AUTENTICACIÓN EXTERNA (OAuth)
        /// <summary>
        /// Proveedor de autenticación del usuario (Local, Google, etc.)
        /// </summary>
        public ProveedorAutenticacion ProveedorAuth { get; set; } = ProveedorAutenticacion.Local;

        /// <summary>
        /// ID del usuario en el proveedor externo (ej: Google User ID)
        /// </summary>
        public string? ProveedorExternoId { get; set; }

        /// <summary>
        /// Token de acceso del proveedor externo (para operaciones futuras)
        /// </summary>
        public string? ProveedorExternoAccessToken { get; set; }

        /// <summary>
        /// Fecha de expiración del token del proveedor externo
        /// </summary>
        public DateTime? ProveedorExternoTokenExpiracion { get; set; }

        // SEGURIDAD DE CONTRASEÑA
        public bool RequiereCambioPwd { get; set; } = false; // True si es temporal
        public DateTime? ExpiracionPwdTemporal { get; set; }
        public bool PermiteCambioPwd { get; set; } = true;   // False para el Admin de Hacienda
        public int IntentosFallidos { get; set; } = 0;       // Contador de intentos fallidos

        /// <summary>
        /// Instante (UTC) hasta el cual la cuenta está bloqueada por exceso de
        /// intentos fallidos. Null cuando no hay bloqueo. Se auto-libera al pasar.
        /// </summary>
        public DateTime? BloqueadoHasta { get; set; }
        public DateTime? UltimoAcceso { get; set; }          // Fecha y hora del último acceso exitoso
        public EstadoUsuario Estado { get; set; } = EstadoUsuario.Activo;

        // REVOCACIÓN LOCAL DE SESIONES (F2)
        /// <summary>
        /// Versión del token. Se incrementa en logout / cambio de contraseña /
        /// cambio de rol / desactivación para invalidar los JWT emitidos antes.
        /// El middleware OnTokenValidated compara el claim "token_version" del JWT
        /// con este valor y rechaza el token si no coinciden.
        /// </summary>
        public int TokenVersion { get; set; } = 0;

        // RESET DE CONTRASEÑA POR CORREO (F2)
        /// <summary>
        /// Hash (SHA-256) del token de reset de contraseña. Se guarda el hash,
        /// nunca el token en claro. Null cuando no hay reset pendiente.
        /// </summary>
        public string? PasswordResetTokenHash { get; set; }

        /// <summary>
        /// Expiración (UTC) del token de reset de contraseña. Token de un solo uso.
        /// </summary>
        public DateTime? PasswordResetTokenExpira { get; set; }

        // RELACIÓN CON EMPRESA (TENANT)
        // RELACIÓN CON EMPRESA (TENANT)
        public int? EmisorId { get; set; }
        public Emisor? Emisor { get; set; }

        /// <summary>
        /// Si es true, el usuario tiene acceso a todas las sucursales del emisor.
        /// Si es false, solo tiene acceso a las sucursales asignadas en UsuarioSucursales.
        /// </summary>
        public bool AccesoTodasSucursales { get; set; } = false;

        /// <summary>
        /// Sucursales asignadas al usuario (many-to-many)
        /// </summary>
        public ICollection<UsuarioSucursal> UsuarioSucursales { get; set; } = new List<UsuarioSucursal>();

        // RELACIÓN CON ROL (N:1 - Un usuario tiene un rol principal)
        public int RolId { get; set; }
        public Rol Rol { get; set; } = null!;

        // CAJAS ASIGNADAS (many-to-many, aplica a usuarios con rol Cajero)
        public ICollection<UsuarioCaja> UsuarioCajas { get; set; } = new List<UsuarioCaja>();
    }
}