using FraFactu.Domain.Common;

namespace FraFactu.Domain.Entities
{
    /// <summary>
    /// Configuración singleton del proveedor del servicio (JD SmartCode).
    /// Datos que aparecen en el PDF de factura de suscripción.
    /// </summary>
    public class ConfiguracionProveedor : BaseEntity
    {
        public string Nombre { get; set; } = string.Empty;
        public string? NombreContacto { get; set; }
        public string? Email { get; set; }
        public string? Telefono { get; set; }
        public string? Website { get; set; }

        // Datos bancarios para transferencia
        public string? Banco { get; set; }
        public string? TipoCuenta { get; set; }
        public string? NumeroCuenta { get; set; }
        public string? TitularCuenta { get; set; }
    }
}
