namespace FraFactu.Application.DTOs.Suscripciones
{
    public class UpdateSuscripcionDto
    {
        public string NombrePlan { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public string? Notas { get; set; }
        public bool Activo { get; set; } = true;
    }
}
