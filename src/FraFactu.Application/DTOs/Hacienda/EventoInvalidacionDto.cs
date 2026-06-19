using System.ComponentModel.DataAnnotations;

using System.Text.Json.Serialization;

namespace FraFactu.Application.DTOs.Hacienda
{
    /// <summary>
    /// Evento de invalidación de DTE según schema v3 del MH
    /// </summary>
    public class EventoInvalidacionDto
    {
        [Required]
        [JsonPropertyName("identificacion")]
        public IdentificacionInvalidacionDto Identificacion { get; set; } = new();

        [Required]
        [JsonPropertyName("emisor")]
        public EmisorInvalidacionDto Emisor { get; set; } = new();

        [Required]
        [JsonPropertyName("documento")]
        public DocumentoInvalidacionDto Documento { get; set; } = new();

        [Required]
        [JsonPropertyName("motivo")]
        public MotivoInvalidacionDto Motivo { get; set; } = new();
    }

    public class IdentificacionInvalidacionDto
    {
        /// <summary>
        /// Versión del esquema (valor fijo: 3)
        /// </summary>
        [Required]
        [JsonPropertyName("version")]
        public int Version { get; set; } = 3;

        /// <summary>
        /// Ambiente de destino: 00 - Pruebas, 01 - Produccion
        /// </summary>
        [Required]
        [RegularExpression("^(00|01)$")]
        [JsonPropertyName("ambiente")]
        public string Ambiente { get; set; } = "00";

        /// <summary>
        /// UUID v4 único del evento de invalidación (36 caracteres con guiones)
        /// </summary>
        [Required]
        [StringLength(36, MinimumLength = 36)]
        [RegularExpression(@"^[A-F0-9]{8}-[A-F0-9]{4}-[A-F0-9]{4}-[A-F0-9]{4}-[A-F0-9]{12}$")]
        [JsonPropertyName("codigoGeneracion")]
        public string CodigoGeneracion { get; set; } = string.Empty;

        /// <summary>
        /// Fecha del evento (formato yyyy-MM-dd). En v3 se renombró fecAnula → fecEmi.
        /// </summary>
        [Required]
        [JsonPropertyName("fecEmi")]
        public string FecEmi { get; set; } = string.Empty;

        /// <summary>
        /// Hora del evento (formato HH:mm:ss). En v3 se renombró horAnula → horEmi.
        /// </summary>
        [Required]
        [RegularExpression(@"^(0[0-9]|1[0-9]|2[0-3]):[0-5][0-9]:[0-5][0-9]$")]
        [JsonPropertyName("horEmi")]
        public string HorEmi { get; set; } = string.Empty;

        /// <summary>
        /// Fusiones y otros (NIT de 14 o 9 dígitos). Campo nuevo en v3, requerido y anulable.
        /// </summary>
        [RegularExpression(@"^([0-9]{14}|[0-9]{9})$")]
        [JsonPropertyName("fusion")]
        public string? Fusion { get; set; }
    }

    public class EmisorInvalidacionDto
    {
        /// <summary>
        /// NIT sin guiones (9 o 14 dígitos)
        /// </summary>
        [Required]
        [RegularExpression(@"^([0-9]{14}|[0-9]{9})$")]
        [JsonPropertyName("nit")]
        public string Nit { get; set; } = string.Empty;

        /// <summary>
        /// Nombre, denominación o razón social del emisor
        /// </summary>
        [Required]
        [StringLength(250, MinimumLength = 3)]
        [JsonPropertyName("nombre")]
        public string Nombre { get; set; } = string.Empty;

        /// <summary>
        /// Código de establecimiento asignado por MH (4 dígitos)
        /// </summary>
        [StringLength(4, MinimumLength = 4)]
        [JsonPropertyName("codEstableMH")]
        public string? CodEstableMH { get; set; }

        /// <summary>
        /// Código de establecimiento del contribuyente
        /// </summary>
        [Required]
        [StringLength(10, MinimumLength = 1)]
        [JsonPropertyName("codEstable")]
        public string CodEstable { get; set; } = string.Empty;

        /// <summary>
        /// Código de punto de venta asignado por MH (4 dígitos)
        /// </summary>
        [StringLength(4, MinimumLength = 4)]
        [JsonPropertyName("codPuntoVentaMH")]
        public string? CodPuntoVentaMH { get; set; }

        /// <summary>
        /// Código de punto de venta del contribuyente
        /// </summary>
        [Required]
        [StringLength(15, MinimumLength = 1)]
        [JsonPropertyName("codPuntoVenta")]
        public string CodPuntoVenta { get; set; } = string.Empty;

        /// <summary>
        /// Número de teléfono del emisor
        /// </summary>
        [StringLength(26, MinimumLength = 8)]
        [RegularExpression(@"^[0-9+;]{8,26}$")]
        [JsonPropertyName("telefono")]
        public string? Telefono { get; set; }

        /// <summary>
        /// Correo electrónico del emisor
        /// </summary>
        [Required]
        [EmailAddress]
        [StringLength(100, MinimumLength = 3)]
        [JsonPropertyName("correo")]
        public string Correo { get; set; } = string.Empty;
    }

    public class DocumentoInvalidacionDto
    {
        /// <summary>
        /// Tipo de DTE a invalidar (01-15)
        /// </summary>
        [Required]
        [RegularExpression(@"^0[0-9]|1[0-5]$")]
        [JsonPropertyName("tipoDte")]
        public string TipoDte { get; set; } = string.Empty;

        /// <summary>
        /// Código de generación del DTE a invalidar (UUID v4)
        /// </summary>
        [Required]
        [StringLength(36, MinimumLength = 36)]
        [RegularExpression(@"^[A-F0-9]{8}-[A-F0-9]{4}-[A-F0-9]{4}-[A-F0-9]{4}-[A-F0-9]{12}$")]
        [JsonPropertyName("codigoGeneracion")]
        public string CodigoGeneracion { get; set; } = string.Empty;

        /// <summary>
        /// Sello de recepción del DTE (40 caracteres alfanuméricos)
        /// </summary>
        [Required]
        [StringLength(40, MinimumLength = 40)]
        [RegularExpression(@"^[A-Z0-9]{40}$")]
        [JsonPropertyName("selloRecibido")]
        public string SelloRecibido { get; set; } = string.Empty;

        /// <summary>
        /// Número de control del DTE
        /// </summary>
        [Required]
        [StringLength(31, MinimumLength = 31)]
        [RegularExpression(@"^DTE-0[0-9]|1[0-2]-[A-Z0-9]{8}-[0-9]{15}$")]
        [JsonPropertyName("numeroControl")]
        public string NumeroControl { get; set; } = string.Empty;

        /// <summary>
        /// Fecha de emisión del DTE (formato yyyy-MM-dd)
        /// </summary>
        [Required]
        [JsonPropertyName("fecEmi")]
        public string FecEmi { get; set; } = string.Empty;

        /// <summary>
        /// Código de generación del DTE que reemplaza (solo si tipoAnulacion != 2)
        /// </summary>
        [StringLength(36, MinimumLength = 36)]
        [RegularExpression(@"^[A-F0-9]{8}-[A-F0-9]{4}-[A-F0-9]{4}-[A-F0-9]{4}-[A-F0-9]{12}$")]
        [JsonPropertyName("codigoGeneracionR")]
        public string? CodigoGeneracionR { get; set; }

        /// <summary>
        /// Tipo de documento del receptor (CAT-22): 36-NIT, 13-DUI, 02-Carnet, 03-Pasaporte, 37-Otro
        /// </summary>
        [Required]
        [RegularExpression("^(36|13|02|03|37)$")]
        [JsonPropertyName("tipoDocumento")]
        public string TipoDocumento { get; set; } = string.Empty;

        /// <summary>
        /// Número de documento del receptor
        /// </summary>
        [Required]
        [StringLength(20, MinimumLength = 3)]
        [JsonPropertyName("numDocumento")]
        public string NumDocumento { get; set; } = string.Empty;

        /// <summary>
        /// Nombre del receptor
        /// </summary>
        [Required]
        [StringLength(200, MinimumLength = 5)]
        [JsonPropertyName("nombre")]
        public string Nombre { get; set; } = string.Empty;

        /// <summary>
        /// Teléfono del receptor
        /// </summary>
        [StringLength(50, MinimumLength = 8)]
        [RegularExpression(@"^[0-9+;]{8,50}$")]
        [JsonPropertyName("telefono")]
        public string? Telefono { get; set; }

        /// <summary>
        /// Correo electrónico del receptor
        /// </summary>
        [EmailAddress]
        [StringLength(100)]
        [JsonPropertyName("correo")]
        public string? Correo { get; set; }
    }

    public class MotivoInvalidacionDto
    {
        /// <summary>
        /// Tipo de anulación: 1, 2, 3
        /// </summary>
        [Required]
        [Range(1, 3)]
        [JsonPropertyName("tipoAnulacion")]
        public int TipoAnulacion { get; set; }

        /// <summary>
        /// Motivo de anulación (obligatorio si TipoAnulacion = 3)
        /// </summary>
        [StringLength(250, MinimumLength = 5)]
        [JsonPropertyName("motivoAnulacion")]
        public string? MotivoAnulacion { get; set; }

        /// <summary>
        /// Nombre del responsable de invalidar
        /// </summary>
        [Required]
        [StringLength(100, MinimumLength = 5)]
        [JsonPropertyName("nombreResponsable")]
        public string NombreResponsable { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de documento del responsable (CAT-22)
        /// </summary>
        [Required]
        [RegularExpression("^(36|13|02|03|37)$")]
        [JsonPropertyName("tipDocResponsable")]
        public string TipDocResponsable { get; set; } = string.Empty;

        /// <summary>
        /// Número de documento del responsable
        /// </summary>
        [Required]
        [StringLength(20, MinimumLength = 3)]
        [JsonPropertyName("numDocResponsable")]
        public string NumDocResponsable { get; set; } = string.Empty;

        /// <summary>
        /// Nombre de quien solicita la invalidación
        /// </summary>
        [Required]
        [StringLength(100, MinimumLength = 5)]
        [JsonPropertyName("nombreSolicita")]
        public string NombreSolicita { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de documento del solicitante (CAT-22)
        /// </summary>
        [Required]
        [RegularExpression("^(36|13|02|03|37)$")]
        [JsonPropertyName("tipDocSolicita")]
        public string TipDocSolicita { get; set; } = string.Empty;

        /// <summary>
        /// Número de documento del solicitante
        /// </summary>
        [Required]
        [StringLength(20, MinimumLength = 3)]
        [JsonPropertyName("numDocSolicita")]
        public string NumDocSolicita { get; set; } = string.Empty;
    }
}
