using System.Text.Json.Serialization;

namespace FraFactu.Application.DTOs.Hacienda
{
    /// <summary>
    /// Evento de Operaciones Especiales (tipoEvento "17") según el esquema fe-eop-v1.json del MH.
    /// Se serializa con nulls explícitos (el esquema marca como required varios campos anulables).
    /// </summary>
    public class EventoOperacionesEspecialesDto
    {
        [JsonPropertyName("identificacion")]
        public IdentificacionOperacionEspecialDto Identificacion { get; set; } = new();

        [JsonPropertyName("emisor")]
        public EmisorOperacionEspecialDto Emisor { get; set; } = new();

        [JsonPropertyName("cuerpoDocumento")]
        public List<CuerpoOperacionEspecialDto> CuerpoDocumento { get; set; } = new();

        [JsonPropertyName("resumen")]
        public ResumenOperacionEspecialDto Resumen { get; set; } = new();

        [JsonPropertyName("apendice")]
        public List<ApendiceOperacionEspecialDto>? Apendice { get; set; }
    }

    public class IdentificacionOperacionEspecialDto
    {
        [JsonPropertyName("version")]
        public int Version { get; set; } = 1;

        [JsonPropertyName("ambiente")]
        public string Ambiente { get; set; } = "00";

        [JsonPropertyName("tipoModelo")]
        public int TipoModelo { get; set; } = 1;

        [JsonPropertyName("tipoOperacion")]
        public int TipoOperacion { get; set; } = 1;

        [JsonPropertyName("tipoEvento")]
        public string TipoEvento { get; set; } = "17";

        [JsonPropertyName("codigoGeneracion")]
        public string CodigoGeneracion { get; set; } = string.Empty;

        [JsonPropertyName("fecEmi")]
        public string FecEmi { get; set; } = string.Empty;

        [JsonPropertyName("horEmi")]
        public string HorEmi { get; set; } = string.Empty;

        [JsonPropertyName("tipoMoneda")]
        public string TipoMoneda { get; set; } = "USD";
    }

    public class EmisorOperacionEspecialDto
    {
        [JsonPropertyName("nit")]
        public string Nit { get; set; } = string.Empty;

        [JsonPropertyName("nombre")]
        public string Nombre { get; set; } = string.Empty;
    }

    public class CuerpoOperacionEspecialDto
    {
        [JsonPropertyName("numItem")]
        public int NumItem { get; set; }

        [JsonPropertyName("codigoGeneracionRef")]
        public string? CodigoGeneracionRef { get; set; }

        [JsonPropertyName("tipoDocumento")]
        public string TipoDocumento { get; set; } = string.Empty;

        [JsonPropertyName("numDocumento")]
        public string? NumDocumento { get; set; }

        [JsonPropertyName("fechaEmision")]
        public string? FechaEmision { get; set; }

        [JsonPropertyName("cantidad")]
        public int Cantidad { get; set; }

        [JsonPropertyName("descripcion")]
        public string Descripcion { get; set; } = string.Empty;

        [JsonPropertyName("docDel")]
        public string? DocDel { get; set; }

        [JsonPropertyName("docAl")]
        public string? DocAl { get; set; }

        [JsonPropertyName("precioUni")]
        public decimal PrecioUni { get; set; }

        [JsonPropertyName("ventaNoSuj")]
        public decimal VentaNoSuj { get; set; }

        [JsonPropertyName("ventaExenta")]
        public decimal VentaExenta { get; set; }

        [JsonPropertyName("ventaGravada")]
        public decimal VentaGravada { get; set; }

        /// <summary>Códigos de tributo (2 chars). Null si el ítem no lleva tributos.</summary>
        [JsonPropertyName("tributos")]
        public List<string>? Tributos { get; set; }
    }

    public class ResumenOperacionEspecialDto
    {
        [JsonPropertyName("totalNoSuj")]
        public decimal TotalNoSuj { get; set; }

        [JsonPropertyName("totalExenta")]
        public decimal TotalExenta { get; set; }

        [JsonPropertyName("totalGravada")]
        public decimal TotalGravada { get; set; }

        [JsonPropertyName("subTotal")]
        public decimal SubTotal { get; set; }

        [JsonPropertyName("tributos")]
        public List<TributoResumenOperacionEspecialDto>? Tributos { get; set; }

        [JsonPropertyName("total")]
        public decimal Total { get; set; }

        [JsonPropertyName("totalLetras")]
        public string? TotalLetras { get; set; }
    }

    public class TributoResumenOperacionEspecialDto
    {
        [JsonPropertyName("codigo")]
        public string Codigo { get; set; } = string.Empty;

        [JsonPropertyName("descripcion")]
        public string Descripcion { get; set; } = string.Empty;

        [JsonPropertyName("valor")]
        public decimal Valor { get; set; }
    }

    public class ApendiceOperacionEspecialDto
    {
        [JsonPropertyName("campo")]
        public string Campo { get; set; } = string.Empty;

        [JsonPropertyName("etiqueta")]
        public string Etiqueta { get; set; } = string.Empty;

        [JsonPropertyName("valor")]
        public string Valor { get; set; } = string.Empty;
    }
}
