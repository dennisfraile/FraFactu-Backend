using FraFactu.Domain.Common;
namespace FraFactu.Domain.Entities
{
    public class FacturaApendice: BaseEntity
    {
        public int FacturaId { get; set; }
        public FacturaElectronica Factura { get; set; } = null!;
        
        public string Campo { get; set; } = string.Empty; 
        public string Etiqueta { get; set; } = string.Empty; 
        public string Valor { get; set; } = string.Empty;
        }
}