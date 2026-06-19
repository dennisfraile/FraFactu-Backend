

namespace FraFactu.Application.DTOs.Emisores
{
    /// <summary>
    /// DTO para actualización de emisor
    /// </summary>
    public class UpdateEmisorDto
    {
        public string Nit { get; set; } = string.Empty;
        public string Nrc { get; set; } = string.Empty;
        public string NombreRazonSocial { get; set; } = string.Empty;
        public string? NombreComercial { get; set; }
        public string CodigoActividad { get; set; } = string.Empty;
        public string DescripcionActividad { get; set; } = string.Empty;
        public int? CatTipoEstablecimientoId { get; set; }
        public string CorreoElectronico { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public int CatDepartamentoId { get; set; }
        public int CatMunicipioId { get; set; }
        public int? CatDistritoId { get; set; } // CAT-008 (obligatorio para emitir V2.0)
        public string Direccion { get; set; } = string.Empty;


        // Configuración MH
        public int? CatAmbienteDestinoId { get; set; }
        public string? MhUsuario { get; set; }
        public string? MhClaveApi { get; set; }
        public string? MhLlavePrivada { get; set; }
        public string? MhLlavePublica { get; set; }
        public string? MhPassPrivada { get; set; }

        // Credenciales de Produccion MH
        public string? MhUsuarioProd { get; set; }
        public string? MhClaveApiProd { get; set; }
        public string? MhLlavePrivadaProd { get; set; }
        public string? MhLlavePublicaProd { get; set; }
        public string? MhPassPrivadaProd { get; set; }

        // Configuracion SMTP
        public string? SmtpHost { get; set; }
        public int? SmtpPort { get; set; }
        public string? SmtpUser { get; set; }
        public string? SmtpPassword { get; set; }
        public string? EmailRemitente { get; set; }
        public bool? EmailHabilitado { get; set; }

        // Lectura de DTEs desde correo
        public bool? LecturaCorreoHabilitada { get; set; }

        // Estado
        public bool Activo { get; set; }
    }
}
