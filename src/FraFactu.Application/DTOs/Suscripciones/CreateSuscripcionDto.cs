namespace FraFactu.Application.DTOs.Suscripciones
{
    public class CreateSuscripcionDto
    {
        public int EmisorId { get; set; }
        public string NombrePlan { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public string? Notas { get; set; }
    }
}
