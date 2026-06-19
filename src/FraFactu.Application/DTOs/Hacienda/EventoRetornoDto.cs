using System.Text.Json.Serialization;

namespace FraFactu.Application.DTOs.Hacienda
{
    /// <summary>
    /// Evento de Retorno (tipoEvento "18") según el esquema fe-eret-v1.json del MH.
    /// Se serializa con nulls explícitos. ventaTercero y compraTercero son mutuamente excluyentes.
    /// </summary>
    public class EventoRetornoDto
    {
        [JsonPropertyName("identificacion")]
        public IdentificacionRetornoDto Identificacion { get; set; } = new();

        [JsonPropertyName("documentoRelacionado")]
        public List<DocumentoRelacionadoRetornoDto> DocumentoRelacionado { get; set; } = new();

        [JsonPropertyName("emisor")]
        public EmisorRetornoDto Emisor { get; set; } = new();

        [JsonPropertyName("documento")]
        public DocumentoReceptorRetornoDto? Documento { get; set; }

        [JsonPropertyName("ventaTercero")]
        public VentaTerceroRetornoDto? VentaTercero { get; set; }

        [JsonPropertyName("compraTercero")]
        public CompraTerceroRetornoDto? CompraTercero { get; set; }

        [JsonPropertyName("cuerpoDocumento")]
        public List<CuerpoRetornoDto> CuerpoDocumento { get; set; } = new();

        [JsonPropertyName("resumen")]
        public ResumenRetornoDto Resumen { get; set; } = new();

        [JsonPropertyName("apendice")]
        public List<ApendiceRetornoDto>? Apendice { get; set; }
    }

    public class IdentificacionRetornoDto
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
        public string TipoEvento { get; set; } = "18";

        [JsonPropertyName("tipoContingencia")]
        public int? TipoContingencia { get; set; }

        [JsonPropertyName("motivoContin")]
        public string? MotivoContin { get; set; }

        [JsonPropertyName("codigoGeneracion")]
        public string CodigoGeneracion { get; set; } = string.Empty;

        [JsonPropertyName("fecEmi")]
        public string FecEmi { get; set; } = string.Empty;

        [JsonPropertyName("horEmi")]
        public string HorEmi { get; set; } = string.Empty;

        [JsonPropertyName("fusion")]
        public string? Fusion { get; set; }

        [JsonPropertyName("tipoMoneda")]
        public string TipoMoneda { get; set; } = "USD";
    }

    public class DocumentoRelacionadoRetornoDto
    {
        [JsonPropertyName("tipoDocumento")]
        public string TipoDocumento { get; set; } = string.Empty;

        [JsonPropertyName("codigoGeneracion")]
        public string CodigoGeneracion { get; set; } = string.Empty;

        [JsonPropertyName("fechaEmision")]
        public string FechaEmision { get; set; } = string.Empty;
    }

    public class EmisorRetornoDto
    {
        [JsonPropertyName("nit")]
        public string Nit { get; set; } = string.Empty;

        [JsonPropertyName("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [JsonPropertyName("codEstableMH")]
        public string? CodEstableMH { get; set; }

        [JsonPropertyName("codEstable")]
        public string? CodEstable { get; set; }

        [JsonPropertyName("codPuntoVentaMH")]
        public string? CodPuntoVentaMH { get; set; }

        [JsonPropertyName("codPuntoVenta")]
        public string? CodPuntoVenta { get; set; }

        [JsonPropertyName("recintoFiscal")]
        public string? RecintoFiscal { get; set; }

        [JsonPropertyName("tipoRegimen")]
        public string? TipoRegimen { get; set; }

        [JsonPropertyName("regimen")]
        public string? Regimen { get; set; }

        [JsonPropertyName("tipoItemExpor")]
        public int? TipoItemExpor { get; set; }
    }

    public class DocumentoReceptorRetornoDto
    {
        [JsonPropertyName("tipoDocumento")]
        public string? TipoDocumento { get; set; }

        [JsonPropertyName("numDocumento")]
        public string? NumDocumento { get; set; }

        [JsonPropertyName("nombre")]
        public string? Nombre { get; set; }

        [JsonPropertyName("codPais")]
        public string? CodPais { get; set; }

        [JsonPropertyName("nombrePais")]
        public string? NombrePais { get; set; }

        [JsonPropertyName("telefono")]
        public string? Telefono { get; set; }

        [JsonPropertyName("correo")]
        public string? Correo { get; set; }
    }

    public class VentaTerceroRetornoDto
    {
        [JsonPropertyName("nit")]
        public string? Nit { get; set; }

        [JsonPropertyName("nombre")]
        public string? Nombre { get; set; }

        [JsonPropertyName("codDomiciliado")]
        public int? CodDomiciliado { get; set; }
    }

    public class CompraTerceroRetornoDto
    {
        [JsonPropertyName("numDocumento")]
        public string? NumDocumento { get; set; }

        [JsonPropertyName("nombre")]
        public string? Nombre { get; set; }
    }

    public class CuerpoRetornoDto
    {
        [JsonPropertyName("numItem")]
        public int NumItem { get; set; }

        [JsonPropertyName("tipoItem")]
        public int TipoItem { get; set; }

        [JsonPropertyName("codigoGeneracion")]
        public string CodigoGeneracion { get; set; } = string.Empty;

        [JsonPropertyName("cantidad")]
        public decimal Cantidad { get; set; }

        [JsonPropertyName("precioUni")]
        public decimal PrecioUni { get; set; }

        [JsonPropertyName("descripcion")]
        public string Descripcion { get; set; } = string.Empty;

        [JsonPropertyName("codigo")]
        public string? Codigo { get; set; }

        [JsonPropertyName("uniMedida")]
        public int UniMedida { get; set; }

        [JsonPropertyName("montoDescu")]
        public decimal MontoDescu { get; set; }

        [JsonPropertyName("codTributo")]
        public string? CodTributo { get; set; }

        [JsonPropertyName("ventaNoSuj")]
        public decimal VentaNoSuj { get; set; }

        [JsonPropertyName("ventaExenta")]
        public decimal VentaExenta { get; set; }

        [JsonPropertyName("ventaGravada")]
        public decimal VentaGravada { get; set; }

        [JsonPropertyName("compra")]
        public decimal Compra { get; set; }

        [JsonPropertyName("tributos")]
        public List<string>? Tributos { get; set; }

        [JsonPropertyName("psv")]
        public decimal Psv { get; set; }

        [JsonPropertyName("ivaItem")]
        public decimal IvaItem { get; set; }

        [JsonPropertyName("noGravado")]
        public decimal NoGravado { get; set; }

        [JsonPropertyName("seguro")]
        public decimal Seguro { get; set; }

        [JsonPropertyName("flete")]
        public decimal Flete { get; set; }

        [JsonPropertyName("ivaRete")]
        public decimal IvaRete { get; set; }

        [JsonPropertyName("reteRenta")]
        public decimal ReteRenta { get; set; }
    }

    public class ResumenRetornoDto
    {
        [JsonPropertyName("totalNoSuj")]
        public decimal TotalNoSuj { get; set; }

        [JsonPropertyName("totalExenta")]
        public decimal TotalExenta { get; set; }

        [JsonPropertyName("totalGravada")]
        public decimal TotalGravada { get; set; }

        [JsonPropertyName("totalCompraExcluidos")]
        public decimal TotalCompraExcluidos { get; set; }

        [JsonPropertyName("subTotalVentas")]
        public decimal SubTotalVentas { get; set; }

        [JsonPropertyName("tributos")]
        public List<TributoResumenRetornoDto>? Tributos { get; set; }

        [JsonPropertyName("totalSeguro")]
        public decimal? TotalSeguro { get; set; }

        [JsonPropertyName("totalFlete")]
        public decimal? TotalFlete { get; set; }

        [JsonPropertyName("montoTotalOperacion")]
        public decimal MontoTotalOperacion { get; set; }

        [JsonPropertyName("ivaRete")]
        public decimal IvaRete { get; set; }

        [JsonPropertyName("reteRenta")]
        public decimal? ReteRenta { get; set; }

        [JsonPropertyName("totalNoGravado")]
        public decimal TotalNoGravado { get; set; }

        [JsonPropertyName("totalPagar")]
        public decimal TotalPagar { get; set; }

        [JsonPropertyName("totalLetras")]
        public string? TotalLetras { get; set; }

        [JsonPropertyName("totalNoOnerosas")]
        public decimal TotalNoOnerosas { get; set; }

        [JsonPropertyName("totalIva")]
        public decimal TotalIva { get; set; }

        [JsonPropertyName("saldoFavor")]
        public decimal SaldoFavor { get; set; }
    }

    public class TributoResumenRetornoDto
    {
        [JsonPropertyName("codigo")]
        public string Codigo { get; set; } = string.Empty;

        [JsonPropertyName("descripcion")]
        public string Descripcion { get; set; } = string.Empty;

        [JsonPropertyName("valor")]
        public decimal Valor { get; set; }
    }

    public class ApendiceRetornoDto
    {
        [JsonPropertyName("campo")]
        public string Campo { get; set; } = string.Empty;

        [JsonPropertyName("etiqueta")]
        public string Etiqueta { get; set; } = string.Empty;

        [JsonPropertyName("valor")]
        public string Valor { get; set; } = string.Empty;
    }
}
