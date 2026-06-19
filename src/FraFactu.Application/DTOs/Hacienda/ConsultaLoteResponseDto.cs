using System.Text.Json.Serialization;

namespace FraFactu.Application.DTOs.Hacienda
{
    public class ConsultaLoteResponseDto
    {
        [JsonPropertyName("codigoLote")]
        public string? CodigoLote { get; set; }

        [JsonPropertyName("estadoLote")]
        public string? EstadoLote { get; set; }

        [JsonPropertyName("procesados")]
        public List<DteProcesadoDto>? Procesados { get; set; }

        [JsonPropertyName("rechazados")]
        public List<DteRechazadoDto>? Rechazados { get; set; }
    }

    public class DteProcesadoDto
    {
        [JsonPropertyName("codigoGeneracion")]
        public string? CodigoGeneracion { get; set; }

        [JsonPropertyName("selloRecibido")]
        public string? SelloRecibido { get; set; }

        // MH a veces devuelve msg/estado aquí tambien
        [JsonPropertyName("estado")]
        public string? Estado { get; set; }
    }

    public class DteRechazadoDto
    {
        [JsonPropertyName("codigoGeneracion")]
        public string? CodigoGeneracion { get; set; }

        [JsonPropertyName("codigoMsg")]
        public string? CodigoMsg { get; set; }

        [JsonPropertyName("descripcionMsg")]
        public string? DescripcionMsg { get; set; }

        [JsonPropertyName("observaciones")]
        public List<string>? Observaciones { get; set; }
    }
}
