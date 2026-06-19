using FraFactu.Domain.Common;
using FraFactu.Domain.Entities.Catalogos;
using FraFactu.Domain.Enums;

namespace FraFactu.Domain.Entities
{
    public class Emisor : BaseEntity
    {
        // DATOS BÁSICOS
        public string Nit { get; set; } = string.Empty; // NIT (9 o 14 dígitos)
        public string Nrc { get; set; } = string.Empty; // NRC (1-8 dígitos)
        public string NombreRazonSocial { get; set; } = string.Empty;
        public string? NombreComercial { get; set; }
        public string? LogoUrl { get; set; } // URL del logo en Supabase

        // ACTIVIDAD ECONÓMICA (requerido para DTE)
        public string CodigoActividad { get; set; } = string.Empty; // 5-6 dígitos
        public string DescripcionActividad { get; set; } = string.Empty; // Descripción de la actividad

        // ESTABLECIMIENTO (requerido para DTE)
        /// <summary>
        /// FK al catálogo de tipo de establecimiento (CAT-09)
        /// Por defecto 1 = Casa Matriz (01)
        /// </summary>
        public int? CatTipoEstablecimientoId { get; set; }
        public CatTipoEstablecimiento? TipoEstablecimiento { get; set; }

        // CONTACTO
        public string CorreoElectronico { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty; // Requerido para DTE


        // DIRECCIÓN (detalle completo)
        /// <summary>
        /// FK al catálogo de departamentos (CAT-12)
        /// </summary>
        public int CatDepartamentoId { get; set; }
        public CatDepartamento Departamento { get; set; } = null!;

        /// <summary>
        /// FK al catálogo de municipios (CAT-13)
        /// </summary>
        public int CatMunicipioId { get; set; }
        public CatMunicipio Municipio { get; set; } = null!;

        /// <summary>
        /// FK al catálogo de distritos (CAT-008). Obligatorio en la Normativa DTE V2.0.
        /// Nullable a nivel de BD hasta poblar el catálogo y migrar las direcciones existentes.
        /// </summary>
        public int? CatDistritoId { get; set; }
        public CatDistrito? Distrito { get; set; }

        public string Direccion { get; set; } = string.Empty; // Complemento de dirección

        // RELACIÓN 1:N (Una empresa tiene muchos usuarios)
        public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();

        // RELACIÓN 1:N (Una empresa tiene muchas facturas)
        public ICollection<FacturaElectronica> Facturas { get; set; } = new List<FacturaElectronica>();

        // RELACIÓN 1:N (Una empresa tiene muchas sucursales)
        public ICollection<Sucursal> Sucursales { get; set; } = new List<Sucursal>();

        // RELACIÓN 1:N (Una empresa tiene muchos productos/servicios)
        public ICollection<ProductoServicio> ProductosServicios { get; set; } = new List<ProductoServicio>();

        // RELACIÓN 1:N (Una empresa tiene muchos clientes/receptores)
        public ICollection<Receptor> Receptores { get; set; } = new List<Receptor>();

        // ============================================
        // CONFIGURACIÓN MINISTERIO DE HACIENDA (DTE)
        // ============================================

        /// <summary>
        /// FK al catálogo de ambiente de destino (CAT-002)
        /// 1 = Modo prueba ("00"), 2 = Modo producción ("01")
        /// </summary>
        public int CatAmbienteDestinoId { get; set; } = 1;
        public CatAmbienteDestino AmbienteDestino { get; set; } = null!;

        /// <summary>
        /// Usuario para obtener Token de Seguridad (Auth)
        /// </summary>
        public string MhUsuario { get; set; } = string.Empty;

        /// <summary>
        /// Clave/Password para obtener Token de Seguridad
        /// IMPORTANTE: Considerar encriptar este campo
        /// </summary>
        public string MhClaveApi { get; set; } = string.Empty;

        /// <summary>
        /// Llave Privada para firmar documentos (Formato PEM o JSON Web Key)
        /// IMPORTANTE: Debe estar protegida/encriptada
        /// </summary>
        public string MhLlavePrivada { get; set; } = string.Empty;

        /// <summary>
        /// Llave Pública asociada (para validaciones)
        /// </summary>
        public string MhLlavePublica { get; set; } = string.Empty;

        /// <summary>
        /// Contraseña de la llave privada (si la llave esta encriptada)
        /// </summary>
        public string MhPassPrivada { get; set; } = string.Empty;

        // ============================================
        // CREDENCIALES DE PRODUCCION (MH)
        // ============================================

        /// <summary>
        /// Usuario para Token de Seguridad en ambiente PRODUCCION
        /// </summary>
        public string? MhUsuarioProd { get; set; }

        /// <summary>
        /// Clave/Password API Hacienda en ambiente PRODUCCION (encriptada)
        /// </summary>
        public string? MhClaveApiProd { get; set; }

        /// <summary>
        /// Llave Privada para firma en ambiente PRODUCCION (encriptada)
        /// </summary>
        public string? MhLlavePrivadaProd { get; set; }

        /// <summary>
        /// Llave Publica en ambiente PRODUCCION
        /// </summary>
        public string? MhLlavePublicaProd { get; set; }

        /// <summary>
        /// Contrasena de la llave privada en ambiente PRODUCCION (encriptada)
        /// </summary>
        public string? MhPassPrivadaProd { get; set; }

        // ============================================
        // CONFIGURACION SMTP (Correo Electronico)
        // ============================================

        /// <summary>
        /// Host SMTP (ej: smtp.gmail.com, smtp.office365.com)
        /// </summary>
        public string? SmtpHost { get; set; }

        /// <summary>
        /// Puerto SMTP (587 para TLS, 465 para SSL)
        /// </summary>
        public int? SmtpPort { get; set; }

        /// <summary>
        /// Usuario SMTP (normalmente el email)
        /// </summary>
        public string? SmtpUser { get; set; }

        /// <summary>
        /// Contraseña SMTP
        /// </summary>
        public string? SmtpPassword { get; set; }

        /// <summary>
        /// Email remitente (From) para los correos enviados
        /// </summary>
        public string? EmailRemitente { get; set; }

        /// <summary>
        /// Indica si el envio de correos esta habilitado para este emisor
        /// </summary>
        public bool EmailHabilitado { get; set; } = false;

        // ============================================
        // CONFIGURACION GMAIL API (OAuth2)
        // ============================================

        /// <summary>
        /// Refresh token de Gmail OAuth2 (encriptado AES-256-GCM)
        /// </summary>
        public string? GmailRefreshToken { get; set; }

        /// <summary>
        /// Email de la cuenta Gmail conectada
        /// </summary>
        public string? GmailEmail { get; set; }

        /// <summary>
        /// Indica si OAuth2 de Gmail está activo para envío de correos
        /// </summary>
        public bool GmailConectado { get; set; } = false;

        // ============================================
        // CONFIGURACION LECTURA DE CORREOS (DTEs)
        // ============================================

        /// <summary>
        /// Indica si la lectura automática de DTEs desde correo está habilitada
        /// </summary>
        public bool LecturaCorreoHabilitada { get; set; } = false;

        /// <summary>
        /// Fecha/hora de la última lectura de correos para DTEs
        /// </summary>
        public DateTime? UltimaLecturaCorreo { get; set; }

        // ============================================
        // INTEGRACIÓN SMARTHUB
        // ============================================

        /// <summary>
        /// ID del Hub en SmartHub que corresponde a este Emisor (1:1).
        /// Null si el emisor no está vinculado a SmartHub.
        /// </summary>
        public int? HubId { get; set; }

        // Nota: el campo Activo (bool) lo provee BaseEntity. SmartHub lo controla
        // via /api/sync/emisor/toggle-active cuando se (des)activa el Hub 1:1.

        /// <summary>
        /// F3 (Plan inventario desde DTE): indica si el Hub asociado tiene la
        /// app SmartInventory activa. Lo controla SmartHub via webhook
        /// (<c>PUT /api/internal/emisores/{hubId}/inventory-app-toggle</c>)
        /// cuando se (des)activa la suscripcion correspondiente. Cuando es
        /// true, al confirmar una compra externa Smartix encola un evento al
        /// outbox para replicar el movimiento en SmartInventory.
        /// </summary>
        public bool TieneSmartInventoryActiva { get; set; }
    }
}