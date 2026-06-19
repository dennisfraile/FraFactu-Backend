namespace FraFactu.Application.DTOs.Suscripciones
{
    public class ConfiguracionProveedorDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? NombreContacto { get; set; }
        public string? Email { get; set; }
        public string? Telefono { get; set; }
        public string? Website { get; set; }
        public string? Banco { get; set; }
        public string? TipoCuenta { get; set; }
        public string? NumeroCuenta { get; set; }
        public string? TitularCuenta { get; set; }
    }
}
