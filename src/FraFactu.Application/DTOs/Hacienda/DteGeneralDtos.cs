using System.Text.Json.Serialization;

namespace FraFactu.Application.DTOs.Hacienda
{
    /// <summary>
    /// DTO genérico para Documentos Tributarios Electrónicos (DTE).
    /// Soporta todos los tipos de DTE según especificaciones del Ministerio de Hacienda de El Salvador.
    /// </summary>
    /// <remarks>
    /// Este DTO utiliza una estructura flexible que se adapta a todos los tipos de DTE.
    /// Para validaciones específicas por tipo, consultar los JSON schemas oficiales en:
    /// - Factura de Consumidor (01): fe-fc-v1.json
    /// - Factura de Exportación (11): fe-fex-v1.json
    /// - Comprobante de Crédito Fiscal (03): fe-ccf-v3.json
    /// - Nota de Remisión (04): fe-nr-v1.json
    /// - Nota de Crédito (05): fe-nc-v1.json
    /// - Nota de Débito (06): fe-nd-v1.json
    /// - Comprobante de Retención (07): fe-cr-v1.json
    /// - Comprobante de Liquidación (08): fe-cl-v1.json
    /// - Documento Contable de Liquidación (09): fe-dcl-v1.json
    /// - Factura Sujeto Excluido (14): fe-fse-v1.json
    /// - Comprobante de Donación (15): fe-cd-v1.json
    /// 
    /// Ubicación de schemas: /Documentacion/svfe-json-schemas/
    /// </remarks>
    public class DteBaseDto
    {
        /// <summary>
        /// Información de identificación del DTE (obligatorio para todos los tipos)
        /// </summary>
        [JsonPropertyName("identificacion")]
        public IdentificacionDto Identificacion { get; set; } = new();

        /// <summary>
        /// Documentos relacionados (opcional - usado en notas de crédito/débito)
        /// </summary>
        [JsonPropertyName("documentoRelacionado")]
        public DocumentoRelacionadoDto? DocumentoRelacionado { get; set; }

        /// <summary>
        /// Información del emisor del documento (obligatorio)
        /// </summary>
        [JsonPropertyName("emisor")]
        public EmisorDto Emisor { get; set; } = new();

        /// <summary>
        /// Información del receptor del documento (obligatorio)
        /// </summary>
        [JsonPropertyName("receptor")]
        public ReceptorDto Receptor { get; set; } = new();

        /// <summary>
        /// Otros documentos asociados (opcional - ej: prescripciones médicas)
        /// </summary>
        [JsonPropertyName("otrosDocumentos")]
        public List<OtrosDocumentosDto>? OtrosDocumentos { get; set; }

        /// <summary>
        /// Información para ventas por cuenta de terceros (opcional)
        /// </summary>
        [JsonPropertyName("ventaTercero")]
        public VentaTerceroDto? VentaTercero { get; set; }

        /// <summary>
        /// Detalle de items/servicios del documento (obligatorio - máx 2000 items)
        /// </summary>
        [JsonPropertyName("cuerpoDocumento")]
        public List<CuerpoDocumentoDto> CuerpoDocumento { get; set; } = new();

        /// <summary>
        /// Resumen de totales, impuestos y condiciones de pago (obligatorio)
        /// </summary>
        [JsonPropertyName("resumen")]
        public ResumenDto Resumen { get; set; } = new();

        /// <summary>
        /// Información adicional de entrega y observaciones (opcional)
        /// </summary>
        [JsonPropertyName("extension")]
        public ExtensionDto? Extension { get; set; }

        /// <summary>
        /// Campos personalizados adicionales (opcional - máx 10 campos)
        /// </summary>
        [JsonPropertyName("apendice")]
        public ApendiceDto? Apendice { get; set; }
    }

    /// <summary>
    /// Identificación del Documento Tributario Electrónico
    /// </summary>
    public class IdentificacionDto
    {
        /// <summary>
        /// Versión del esquema (1, 2 o 3 según tipo de DTE)
        /// </summary>
        [JsonPropertyName("version")]
        public int Version { get; set; } = 1;

        /// <summary>
        /// Ambiente de destino: 00 = Pruebas, 01 = Producción
        /// </summary>
        [JsonPropertyName("ambiente")]
        public string Ambiente { get; set; } = "00";

        /// <summary>
        /// Tipo de DTE: 01=Factura, 03=CCF, 04=NotaRemisión, 05=NotaCrédito, 06=NotaDébito,
        /// 07=CompRetencion, 08=CompLiquidacion, 09=DocContable, 11=FacturaExportación,
        /// 14=FacturaSujetoExcluido, 15=CompDonación
        /// </summary>
        [JsonPropertyName("tipoDte")]
        public string TipoDte { get; set; } = string.Empty;

        /// <summary>
        /// Número de control único del DTE (31 caracteres) - Formato: DTE-{tipo}-{serie}-{correlativo}
        /// </summary>
        [JsonPropertyName("numeroControl")]
        public string NumeroControl { get; set; } = string.Empty;

        /// <summary>
        /// Código de generación único (UUID v4 en mayúsculas - 36 caracteres con guiones)
        /// </summary>
        [JsonPropertyName("codigoGeneracion")]
        public string CodigoGeneracion { get; set; } = string.Empty;

        /// <summary>
        /// Modelo de facturación: 1 = Previo (envío inmediato), 2 = Diferido/Contingencia
        /// </summary>
        [JsonPropertyName("tipoModelo")]
        public int TipoModelo { get; set; } = 1;

        /// <summary>
        /// Tipo de transmisión: 1 = Normal, 2 = Contingencia
        /// </summary>
        [JsonPropertyName("tipoOperacion")]
        public int TipoOperacion { get; set; } = 1;

        /// <summary>
        /// Fecha de emisión del documento (formato: YYYY-MM-DD)
        /// </summary>
        [JsonPropertyName("fecEmi")]
        public string FecEmi { get; set; } = string.Empty;

        /// <summary>
        /// Hora de emisión del documento (formato: HH:MM:SS)
        /// </summary>
        [JsonPropertyName("horEmi")]
        public string HorEmi { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de moneda (actualmente solo USD soportado)
        /// </summary>
        [JsonPropertyName("tipoMoneda")]
        public string TipoMoneda { get; set; } = "USD";

        /// <summary>
        /// Tipo de contingencia (nullable): 1-5 según catálogo MH. Solo requerido cuando TipoOperacion=2
        /// </summary>
        [JsonPropertyName("tipoContingencia")]
        public int? TipoContingencia { get; set; }

        /// <summary>
        /// Motivo de contingencia (opcional, maxLength: 150). Requerido cuando TipoContingencia=5
        /// </summary>
        [JsonPropertyName("motivoContin")]
        public string? MotivoContin { get; set; }
    }

    /// <summary>
    /// Información del emisor del DTE
    /// </summary>
    public class EmisorDto
    {
        /// <summary>
        /// NIT del emisor sin guiones (9 o 14 dígitos)
        /// </summary>
        [JsonPropertyName("nit")]
        public string Nit { get; set; } = string.Empty;

        /// <summary>
        /// NRC del emisor (1-8 dígitos)
        /// </summary>
        [JsonPropertyName("nrc")]
        public string Nrc { get; set; } = string.Empty;

        /// <summary>
        /// Nombre, denominación o razón social del emisor (máx 200 caracteres)
        /// </summary>
        [JsonPropertyName("nombre")]
        public string Nombre { get; set; } = string.Empty;

        /// <summary>
        /// Código de actividad económica (2-6 dígitos)
        /// </summary>
        [JsonPropertyName("codActividad")]
        public string CodActividad { get; set; } = string.Empty;

        /// <summary>
        /// Descripción de la actividad económica (máx 150 caracteres)
        /// </summary>
        [JsonPropertyName("descActividad")]
        public string DescActividad { get; set; } = string.Empty;

        /// <summary>
        /// Nombre comercial del emisor (opcional, máx 150 caracteres)
        /// </summary>
        [JsonPropertyName("nombreComercial")]
        public string? NombreComercial { get; set; }

        /// <summary>
        /// Tipo de establecimiento: 01=Casa matriz, 02=Sucursal, 04=Punto de venta, 07=Otro, 20=Quiosco
        /// </summary>
        [JsonPropertyName("tipoEstablecimiento")]
        public string TipoEstablecimiento { get; set; } = string.Empty;

        /// <summary>
        /// Dirección completa del emisor (departamento, municipio, complemento)
        /// </summary>
        [JsonPropertyName("direccion")]
        public DireccionDto Direccion { get; set; } = new();

        /// <summary>
        /// Teléfono del emisor (8-30 caracteres)
        /// </summary>
        [JsonPropertyName("telefono")]
        public string Telefonos { get; set; } = string.Empty;

        /// <summary>
        /// Correo electrónico del emisor (máx 100 caracteres, formato email válido)
        /// </summary>
        [JsonPropertyName("correo")]
        public string Correo { get; set; } = string.Empty;

        /// <summary>
        /// Código de establecimiento asignado por MH (4 dígitos, opcional)
        /// </summary>
        [JsonPropertyName("codEstableMH")]
        public string? CodEstableMH { get; set; }

        /// <summary>
        /// Código interno de establecimiento del contribuyente (1-10 caracteres, opcional)
        /// </summary>
        [JsonPropertyName("codEstable")]
        public string? CodEstable { get; set; }

        /// <summary>
        /// Código de punto de venta asignado por MH (4 dígitos, opcional)
        /// </summary>
        [JsonPropertyName("codPuntoVentaMH")]
        public string? CodPuntoVentaMH { get; set; }

        /// <summary>
        /// Código interno de punto de venta del contribuyente (1-15 caracteres, opcional)
        /// </summary>
        [JsonPropertyName("codPuntoVenta")]
        public string? CodPuntoVenta { get; set; }
    }
}
