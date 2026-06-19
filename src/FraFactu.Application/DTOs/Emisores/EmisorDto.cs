namespace FraFactu.Application.DTOs.Emisores
{
    /// <summary>
    /// DTO completo de Emisor (para lectura)
    /// </summary>
    public class EmisorDto
    {
        public int Id { get; set; }
        public string Nit { get; set; } = string.Empty;
        public string NombreRazonSocial { get; set; } = string.Empty;
        public string? NombreComercial { get; set; }
        public string CorreoElectronico { get; set; } = string.Empty;
        public string? Nrc { get; set; }
        public string? Telefono { get; set; }
        public string Direccion { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }

        // Actividad Económica
        public string? CodigoActividad { get; set; }
        public string? DescripcionActividad { get; set; }

        // Ubicación (IDs y nombres de catálogos)
        public int CatDepartamentoId { get; set; }
        public string DepartamentoNombre { get; set; } = string.Empty;
        public int CatMunicipioId { get; set; }
        public string MunicipioNombre { get; set; } = string.Empty;
        public int? CatDistritoId { get; set; }
        public string? DistritoNombre { get; set; }
        public int? CatTipoEstablecimientoId { get; set; }
        public string? TipoEstablecimientoNombre { get; set; }


        // Configuración MH (solo campos públicos - NO devolver credenciales sensibles)
        public int CatAmbienteDestinoId { get; set; }
        public string AmbienteDestinoCodigo { get; set; } = string.Empty; // "00" o "01"
        public string? MhUsuario { get; set; }
        public string? MhLlavePublica { get; set; }

        // Credenciales Produccion (solo campos publicos)
        public string? MhUsuarioProd { get; set; }
        public string? MhLlavePublicaProd { get; set; }

        public bool Activo { get; set; }
        public DateTime FechaCreacion { get; set; }

        // Configuracion SMTP (NO exponer SmtpPassword)
        public string? SmtpHost { get; set; }
        public int? SmtpPort { get; set; }
        public string? SmtpUser { get; set; }
        public string? EmailRemitente { get; set; }
        public bool EmailHabilitado { get; set; }
        public bool SmtpConfigurado { get; set; }

        // Gmail OAuth2 (NO exponer RefreshToken)
        public string? GmailEmail { get; set; }
        public bool GmailConectado { get; set; }

        // Lectura de DTEs desde correo
        public bool LecturaCorreoHabilitada { get; set; }
        public DateTime? UltimaLecturaCorreo { get; set; }

        // Estadísticas opcionales
        public int? TotalUsuarios { get; set; }
        public int? TotalFacturas { get; set; }

        /// <summary>
        /// Plan B Hub-as-Emisor — Fase 3 Task 18.
        /// Id del Hub en SmartHub vinculado a este Emisor (1:1). Si está poblado,
        /// las UIs deben mostrar los datos fiscales identitarios como read-only y
        /// redirigir al admin a SmartHub para editar (Smartix los descarta server-
        /// side, ver UpdateMiPerfilAsync). Null para emisores legacy.
        /// </summary>
        public int? HubId { get; set; }
    }
}
