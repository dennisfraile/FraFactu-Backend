namespace FraFactu.Application.DTOs.Suscripciones
{
    /// <summary>
    /// DTO para el endpoint GET /api/suscripciones/mi-suscripcion (EmisorAdmin)
    /// </summary>
    public class MiSuscripcionResponseDto
    {
        public bool TieneSuscripcion { get; set; }
        public string? NombrePlan { get; set; }
        public decimal? Precio { get; set; }
        public DateTime? FechaVencimiento { get; set; }
        public DateTime? FechaProximoCobro { get; set; }
        public int DiasParaCobro { get; set; }
        public int DiasGracia { get; set; }
        public string Estado { get; set; } = string.Empty; // Activa, PorVencer, EnGracia, Vencida
        public bool MostrarBanner { get; set; }
        public string? MensajeBanner { get; set; }
        public string? ColorBanner { get; set; } // warning, error
    }
}
