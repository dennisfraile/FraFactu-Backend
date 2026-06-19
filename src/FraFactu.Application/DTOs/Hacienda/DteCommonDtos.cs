using System.Text.Json.Serialization;

namespace FraFactu.Application.DTOs.Hacienda
{
    public class ReceptorDto
    {
        [JsonPropertyName("nit")]
        public string? Nit { get; set; }
        [JsonPropertyName("nrc")]
        public string? Nrc { get; set; }
        [JsonPropertyName("nombre")]
        public string Nombre { get; set; } = string.Empty;
        [JsonPropertyName("codActividad")]
        public string? CodActividad { get; set; }
        [JsonPropertyName("descActividad")]
        public string? DescActividad { get; set; }
        [JsonPropertyName("nombreComercial")]
        public string? NombreComercial { get; set; }
        [JsonPropertyName("direccion")]
        public DireccionDto? Direccion { get; set; }
        [JsonPropertyName("telefono")]
        public string? Telefonos { get; set; }
        [JsonPropertyName("correo")]
        public string? Correo { get; set; }
    }

    public class DireccionDto
    {
        [JsonPropertyName("departamento")]
        public string Departamento { get; set; } = string.Empty; // Código
        [JsonPropertyName("municipio")]
        public string Municipio { get; set; } = string.Empty; // Código
        [JsonPropertyName("distrito")]
        public string Distrito { get; set; } = string.Empty; // Código (CAT-008, obligatorio en V2.0)
        [JsonPropertyName("complemento")]
        public string Complemento { get; set; } = string.Empty;
    }

    /// <summary>
    /// Documento relacionado (usado en Notas de Crédito/Débito y otros DTEs)
    /// </summary>
    public class DocumentoRelacionadoDto
    {
        /// <summary>
        /// Tipo de documento relacionado (04=Nota Remisión, 08=Comp Liquidación, 09=Doc Contable para CCF)
        /// </summary>
        [JsonPropertyName("tipoDocumento")]
        public string TipoDocumento { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de generación: 1=Interno (número de control), 2=Electrónico (UUID)
        /// </summary>
        [JsonPropertyName("tipoGeneracion")]
        public int TipoGeneracion { get; set; }

        /// <summary>
        /// Número del documento relacionado (maxLength: 36)
        /// </summary>
        [JsonPropertyName("numeroDocumento")]
        public string NumeroDocumento { get; set; } = string.Empty;

        /// <summary>
        /// Fecha de emisión del documento relacionado (formato: YYYY-MM-DD)
        /// </summary>
        [JsonPropertyName("fechaEmision")]
        public string FechaEmision { get; set; } = string.Empty;
    }

    /// <summary>
    /// Otros documentos asociados (prescripciones médicas, etc.) - máximo 10 items
    /// </summary>
    public class OtrosDocumentosDto
    {
        /// <summary>
        /// Código del documento asociado: 1=Otro, 2=DUI, 3=Prescripción médica, 4=Carnet minoridad
        /// </summary>
        [JsonPropertyName("codDocAsociado")]
        public int CodDocAsociado { get; set; }

        /// <summary>
        /// Descripción del documento (nullable, maxLength: 100). Requerido cuando CodDocAsociado != 3
        /// </summary>
        [JsonPropertyName("descDocumento")]
        public string? DescDocumento { get; set; }

        /// <summary>
        /// Detalle del documento (nullable, maxLength: 300). Requerido cuando CodDocAsociado != 3
        /// </summary>
        [JsonPropertyName("detalleDocumento")]
        public string? DetalleDocumento { get; set; }

        /// <summary>
        /// Información del médico (requerido solo cuando CodDocAsociado = 3)
        /// </summary>
        [JsonPropertyName("medico")]
        public MedicoDto? Medico { get; set; }
    }

    /// <summary>
    /// Información de ventas realizadas por cuenta de terceros
    /// </summary>
    public class VentaTerceroDto
    {
        /// <summary>
        /// NIT del tercero (9 o 14 dígitos sin guiones)
        /// </summary>
        [JsonPropertyName("nit")]
        public string Nit { get; set; } = string.Empty;

        /// <summary>
        /// Nombre, denominación o razón social del tercero (maxLength: 200)
        /// </summary>
        [JsonPropertyName("nombre")]
        public string Nombre { get; set; } = string.Empty;
    }

    /// <summary>
    /// Información de extensión - entrega, recepción y observaciones
    /// </summary>
    public class ExtensionDto
    {
        /// <summary>
        /// Nombre de quien entrega (opcional)
        /// </summary>
        [JsonPropertyName("nombEntrega")]
        public string? NombEntrega { get; set; }

        /// <summary>
        /// Documento de identificación de quien entrega (opcional)
        /// </summary>
        [JsonPropertyName("docuEntrega")]
        public string? DocuEntrega { get; set; }

        /// <summary>
        /// Nombre de quien recibe (opcional)
        /// </summary>
        [JsonPropertyName("nombRecibe")]
        public string? NombRecibe { get; set; }

        /// <summary>
        /// Documento de identificación de quien recibe (opcional)
        /// </summary>
        [JsonPropertyName("docuRecibe")]
        public string? DocuRecibe { get; set; }

        /// <summary>
        /// Observaciones adicionales (opcional, maxLength: 3000)
        /// </summary>
        [JsonPropertyName("observaciones")]
        public string? Observaciones { get; set; }

        /// <summary>
        /// Placa del vehículo utilizado para entrega (opcional)
        /// </summary>
        [JsonPropertyName("placaVehiculo")]
        public string? PlacaVehiculo { get; set; }
    }

    /// <summary>
    /// Campo personalizado del apéndice - máximo 10 items por DTE
    /// </summary>
    public class ApendiceDto
    {
        /// <summary>
        /// Nombre del campo (maxLength: 25)
        /// </summary>
        [JsonPropertyName("campo")]
        public string Campo { get; set; } = string.Empty;

        /// <summary>
        /// Etiqueta descriptiva del campo (maxLength: 50)
        /// </summary>
        [JsonPropertyName("etiqueta")]
        public string Etiqueta { get; set; } = string.Empty;

        /// <summary>
        /// Valor del campo (maxLength: 150)
        /// </summary>
        [JsonPropertyName("valor")]
        public string Valor { get; set; } = string.Empty;
    }

    /// <summary>
    /// Información del médico que prestó el servicio (usado en OtrosDocumentosDto)
    /// </summary>
    public class MedicoDto
    {
        /// <summary>
        /// Nombre del médico (maxLength: 100)
        /// </summary>
        [JsonPropertyName("nombre")]
        public string Nombre { get; set; } = string.Empty;

        /// <summary>
        /// NIT del médico (9 o 14 dígitos, nullable - requerido si DocIdentificacion es null)
        /// </summary>
        [JsonPropertyName("nit")]
        public string? Nit { get; set; }

        /// <summary>
        /// Documento de identificación para médicos no domiciliados (maxLength: 25, nullable)
        /// </summary>
        [JsonPropertyName("docIdentificacion")]
        public string? DocIdentificacion { get; set; }

        /// <summary>
        /// Código del tipo de servicio realizado (1-6 según catálogo MH)
        /// </summary>
        [JsonPropertyName("tipoServicio")]
        public decimal TipoServicio { get; set; }
    }
}
