namespace FraFactu.Application.DTOs.Suscripciones
{
    public class SuscripcionResponseDto
    {
        public int Id { get; set; }
        public int EmisorId { get; set; }
        public string EmisorNombre { get; set; } = string.Empty;
        public string EmisorCorreo { get; set; } = string.Empty;
        public string NombrePlan { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public string? Notas { get; set; }
        public bool Activo { get; set; }
        public DateTime FechaCreacion { get; set; }

        // Cobro mensual
        public DateTime FechaProximoCobro { get; set; }
        public decimal? PrecioProrrateado { get; set; }
        public int DiasGracia { get; set; }

        // Control de pagos
        public DateTime? FechaUltimoPago { get; set; }
        public bool PagoAlDia { get; set; }

        // Campos computados
        public int DiasRestantes { get; set; } // Días para el próximo cobro
        public string Estado { get; set; } = string.Empty; // Activa, Por vencer, Vencida, En gracia
    }
}
