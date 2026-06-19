using FraFactu.Domain.Common;
namespace FraFactu.Domain.Entities
{
    public class FacturaExtencion: BaseEntity
    {
        public int FacturaId { get; set; }
        public FacturaElectronica Factura { get; set; } = null!;
        
        public string? NombEntrega { get; set; } 
        public string? DocuEntrega { get; set; } 
        
        public string? NombRecibe { get; set; }
        public string? DocuRecibe { get; set; }

        public string? PlacaVehiculo { get; set; }
        public string? Observaciones { get; set; }
        }
}