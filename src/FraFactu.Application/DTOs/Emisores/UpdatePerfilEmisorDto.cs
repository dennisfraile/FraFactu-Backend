namespace FraFactu.Application.DTOs.Emisores
{
    /// <summary>
    /// DTO para actualizar el perfil del emisor actual (EmisorAdmin)
    /// </summary>
    public class UpdatePerfilEmisorDto
    {
        /// <summary>
        /// Nombre comercial (opcional)
        /// </summary>
        public string? NombreComercial { get; set; }

        /// <summary>
        /// Correo electrónico principal
        /// </summary>
        public string CorreoElectronico { get; set; } = string.Empty;

        /// <summary>
        /// Teléfono de contacto
        /// </summary>
        public string Telefono { get; set; } = string.Empty;

        /// <summary>
        /// Dirección completa (complemento)
        /// </summary>
        public string Direccion { get; set; } = string.Empty;

        /// <summary>
        /// NRC (Número de Registro de Contribuyente)
        /// </summary>
        public string? Nrc { get; set; }

        /// <summary>
        /// Código de Actividad Económica (5-6 dígitos)
        /// </summary>
        public string? CodigoActividad { get; set; }

        /// <summary>
        /// Descripción de la Actividad Económica
        /// </summary>
        public string? DescripcionActividad { get; set; }

        /// <summary>
        /// ID del departamento donde se ubica
        /// </summary>
        public int? CatDepartamentoId { get; set; }

        /// <summary>
        /// ID del municipio donde se ubica
        /// </summary>
        public int? CatMunicipioId { get; set; }

        /// <summary>
        /// ID del distrito (CAT-008) donde se ubica. Obligatorio para emitir V2.0.
        /// </summary>
        public int? CatDistritoId { get; set; }

        /// <summary>
        /// ID del tipo de establecimiento
        /// </summary>
        public int? CatTipoEstablecimientoId { get; set; }

        // ==========================================
        // CONFIGURACIÓN HACIENDA (Solo Admin Emisor)
        // ==========================================

        /// <summary>
        /// ID del ambiente destino (1=Pruebas, 2=Producción)
        /// </summary>
        public int? CatAmbienteDestinoId { get; set; }

        /// <summary>
        /// Usuario API Hacienda
        /// </summary>
        public string? MhUsuario { get; set; }

        /// <summary>
        /// Clave API Hacienda
        /// </summary>
        public string? MhClaveApi { get; set; }

        /// <summary>
        /// Llave Privada (PEM)
        /// </summary>
        public string? MhLlavePrivada { get; set; }

        /// <summary>
        /// Llave Pública (PEM)
        /// </summary>
        public string? MhLlavePublica { get; set; }

        /// <summary>
        /// Contraseña de la Llave Privada
        /// </summary>
        public string? MhPassPrivada { get; set; }

        // Credenciales de Produccion MH (Solo Admin Emisor)
        public string? MhUsuarioProd { get; set; }
        public string? MhClaveApiProd { get; set; }
        public string? MhLlavePrivadaProd { get; set; }
        public string? MhLlavePublicaProd { get; set; }
        public string? MhPassPrivadaProd { get; set; }

        // ==========================================
        // CONFIGURACION SMTP (Solo Admin Emisor)
        // ==========================================

        public string? SmtpHost { get; set; }
        public int? SmtpPort { get; set; }
        public string? SmtpUser { get; set; }
        public string? SmtpPassword { get; set; }
        public string? EmailRemitente { get; set; }
        public bool? EmailHabilitado { get; set; }

        // Lectura de DTEs desde correo
        public bool? LecturaCorreoHabilitada { get; set; }
    }
}
