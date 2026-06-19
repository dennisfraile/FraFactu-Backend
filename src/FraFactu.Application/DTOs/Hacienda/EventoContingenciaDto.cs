using System.ComponentModel.DataAnnotations;

using System.Text.Json.Serialization;

namespace FraFactu.Application.DTOs.Hacienda
{
    /// <summary>
    /// Reporte de contingencia según schema v4 del MH
    /// </summary>
    public class EventoContingenciaDto
    {
        [Required]
        [JsonPropertyName("identificacion")]
        public IdentificacionContingenciaDto Identificacion { get; set; } = new();

        [Required]
        [JsonPropertyName("emisor")]
        public EmisorContingenciaDto Emisor { get; set; } = new();

        [Required]
        [JsonPropertyName("detalleDTE")]
        public List<DetalleDteContingenciaDto> DetalleDTE { get; set; } = new();

        [Required]
        [JsonPropertyName("motivo")]
        public MotivoContingenciaDto Motivo { get; set; } = new();
    }

    public class IdentificacionContingenciaDto
    {
        /// <summary>
        /// Version del esquema del DTE (valor fijo: 4)
        /// </summary>
        [Required]
        [JsonPropertyName("version")]
        public int Version { get; set; } = 4;

        /// <summary>
        /// Ambiente de destino: 00 - Pruebas, 01 - Produccion
        /// </summary>
        [Required]
        [RegularExpression("^(00|01)$")]
        [JsonPropertyName("ambiente")]
        public string Ambiente { get; set; } = "00";

        /// <summary>
        /// UUID v4 único por evento de contingencia (36 caracteres con guiones)
        /// </summary>
        [Required]
        [StringLength(36, MinimumLength = 36)]
        [RegularExpression(@"^[A-F0-9]{8}-[A-F0-9]{4}-[A-F0-9]{4}-[A-F0-9]{4}-[A-F0-9]{12}$")]
        [JsonPropertyName("codigoGeneracion")]
        public string CodigoGeneracion { get; set; } = string.Empty;

        /// <summary>
        /// Fecha de transmision (formato yyyy-MM-dd)
        /// </summary>
        [Required]
        [JsonPropertyName("fTransmision")]
        public string FTransmision { get; set; } = string.Empty;

        /// <summary>
        /// Hora de transmision (formato HH:mm:ss)
        /// </summary>
        [Required]
        [RegularExpression(@"^(0[0-9]|1[0-9]|2[0-3]):[0-5][0-9]:[0-5][0-9]$")]
        [JsonPropertyName("hTransmision")]
        public string HTransmision { get; set; } = string.Empty;
    }

    public class EmisorContingenciaDto
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
        [StringLength(250, MinimumLength = 5)]
        [JsonPropertyName("nombre")]
        public string Nombre { get; set; } = string.Empty;

        /// <summary>
        /// Nombre del responsable del establecimiento
        /// </summary>
        [Required]
        [StringLength(100, MinimumLength = 5)]
        [JsonPropertyName("nombreResponsable")]
        public string NombreResponsable { get; set; } = string.Empty;

        /// <summary>
        /// Tipo documento del responsable (CAT-22): 36-NIT, 13-DUI, 02-Carnet, 03-Pasaporte, 37-Otro
        /// </summary>
        [Required]
        [RegularExpression("^(36|13|02|03|37)$")]
        [JsonPropertyName("tipoDocResponsable")]
        public string TipoDocResponsable { get; set; } = string.Empty;

        /// <summary>
        /// Número de documento del responsable
        /// </summary>
        [Required]
        [StringLength(25, MinimumLength = 5)]
        [JsonPropertyName("numeroDocResponsable")]
        public string NumeroDocResponsable { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de establecimiento: 01, 02, 04, 07, 20
        /// </summary>
        [Required]
        [RegularExpression("^(01|02|04|07|20)$")]
        [JsonPropertyName("tipoEstablecimiento")]
        public string TipoEstablecimiento { get; set; } = string.Empty;

        /// <summary>
        /// Código de establecimiento asignado por MH (4 dígitos)
        /// </summary>
        [StringLength(4, MinimumLength = 4)]
        [JsonPropertyName("codEstableMH")]
        public string? CodEstableMH { get; set; }

        /// <summary>
        /// Código de punto de venta asignado por MH (4 dígitos). En v4 se renombró codPuntoVenta → codPuntoVentaMH.
        /// </summary>
        [StringLength(4, MinimumLength = 4)]
        [JsonPropertyName("codPuntoVentaMH")]
        public string? CodPuntoVentaMH { get; set; }

        /// <summary>
        /// Número de teléfono del emisor
        /// </summary>
        [Required]
        [StringLength(30, MinimumLength = 8)]
        [JsonPropertyName("telefono")]
        public string Telefono { get; set; } = string.Empty;

        /// <summary>
        /// Correo electrónico del emisor
        /// </summary>
        [Required]
        [EmailAddress]
        [StringLength(100, MinimumLength = 3)]
        [JsonPropertyName("correo")]
        public string Correo { get; set; } = string.Empty;
    }

    public class DetalleDteContingenciaDto
    {
        /// <summary>
        /// Número correlativo del item
        /// </summary>
        [Required]
        [Range(1, 1000)]
        [JsonPropertyName("noItem")]
        public int NoItem { get; set; }

        /// <summary>
        /// Código de generación del DTE reportado (UUID v4)
        /// </summary>
        [Required]
        [RegularExpression(@"^[A-F0-9]{8}-[A-F0-9]{4}-[A-F0-9]{4}-[A-F0-9]{4}-[A-F0-9]{12}$")]
        [JsonPropertyName("codigoGeneracion")]
        public string CodigoGeneracion { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de Documento Tributario Electrónico (01-15)
        /// </summary>
        [Required]
        [RegularExpression(@"^0[1-9]|1[0-5]$")]
        [JsonPropertyName("tipoDoc")]
        public string TipoDoc { get; set; } = string.Empty;
    }

    public class MotivoContingenciaDto
    {
        /// <summary>
        /// Fecha inicio de la contingencia (yyyy-MM-dd)
        /// </summary>
        [Required]
        [JsonPropertyName("fInicio")]
        public string FInicio { get; set; } = string.Empty;

        /// <summary>
        /// Fecha fin de la contingencia (yyyy-MM-dd)
        /// </summary>
        [Required]
        [JsonPropertyName("fFin")]
        public string FFin { get; set; } = string.Empty;

        /// <summary>
        /// Hora inicio de la contingencia (HH:mm:ss)
        /// </summary>
        [Required]
        [RegularExpression(@"^(0[0-9]|1[0-9]|2[0-3]):[0-5][0-9]:[0-5][0-9]$")]
        [JsonPropertyName("hInicio")]
        public string HInicio { get; set; } = string.Empty;

        /// <summary>
        /// Hora fin de la contingencia (HH:mm:ss)
        /// </summary>
        [Required]
        [RegularExpression(@"^(0[0-9]|1[0-9]|2[0-3]):[0-5][0-9]:[0-5][0-9]$")]
        [JsonPropertyName("hFin")]
        public string HFin { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de contingencia (1-5)
        /// </summary>
        [Required]
        [Range(1, 5)]
        [JsonPropertyName("tipoContingencia")]
        public int TipoContingencia { get; set; }

        /// <summary>
        /// Descripción del motivo (obligatorio si TipoContingencia = 5)
        /// </summary>
        [StringLength(500)]
        [JsonPropertyName("motivoContingencia")]
        public string? MotivoContingencia { get; set; }
    }
}
