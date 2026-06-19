using System.Text.Json;

namespace FraFactu.Application.DTOs.Hacienda
{
    public class TransmitirDteRequest
    {
        public int EmisorId { get; set; }
        public DteBaseDto Dte { get; set; } = new();
    }

    public class TransmitirLoteRequest
    {
        public int EmisorId { get; set; }
        public List<DteBaseDto> Dtes { get; set; } = new();
    }

    public class EventoContingenciaRequest
    {
        public int EmisorId { get; set; }
        public EventoContingenciaDto Evento { get; set; } = new();
    }

    public class AnularDteRequest
    {
        public int EmisorId { get; set; }
        public EventoInvalidacionDto EventoInvalidacion { get; set; } = new();
    }

    public class ConsultaDteRequest
    {
        public int EmisorId { get; set; }
        public string CodigoGeneracion { get; set; } = string.Empty;
        public string TipoDte { get; set; } = string.Empty;
    }
}
