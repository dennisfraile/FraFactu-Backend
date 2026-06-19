using System.Collections.Generic;

namespace FraFactu.Application.DTOs.Cuotas
{
    public class EstadoCuentaCuotaDto
    {
        public int Numero { get; set; }
        public DateTime FechaPactada { get; set; }
        public decimal Monto { get; set; }
        public string Estado { get; set; } = string.Empty;
        public DateTime? FechaPago { get; set; }
        public string? CodigoGeneracion { get; set; }
        public decimal InteresMora { get; set; }
    }

    public class EstadoCuentaPlanDto
    {
        public string EmisorNombre { get; set; } = string.Empty;
        public string ClienteNombre { get; set; } = string.Empty;
        public string ClienteDocumento { get; set; } = string.Empty;
        public DateTime FechaReporte { get; set; }
        public int PlanId { get; set; }
        public string Condicion { get; set; } = string.Empty;
        public decimal MontoTotal { get; set; }
        public decimal MontoPagado { get; set; }
        public decimal SaldoAdeudado { get; set; }
        public string EstadoCobro { get; set; } = string.Empty;
        public List<EstadoCuentaCuotaDto> Cuotas { get; set; } = new();
    }

    public class EstadoCuentaClienteDto
    {
        public string EmisorNombre { get; set; } = string.Empty;
        public string ClienteNombre { get; set; } = string.Empty;
        public string ClienteDocumento { get; set; } = string.Empty;
        public DateTime FechaReporte { get; set; }
        public decimal MontoTotal { get; set; }
        public decimal MontoPagado { get; set; }
        public decimal SaldoAdeudado { get; set; }
        public List<EstadoCuentaPlanDto> Planes { get; set; } = new();
    }
}
