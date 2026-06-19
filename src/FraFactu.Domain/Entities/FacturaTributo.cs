using FraFactu.Domain.Common;
using FraFactu.Domain.Entities.Catalogos;
namespace FraFactu.Domain.Entities
{
    public class FacturaTributo : BaseEntity
    {
        // Relación con la Factura Padre
        public int FacturaId { get; set; }
        public FacturaElectronica Factura { get; set; } = null!;

        // Relación con el Catálogo (Para saber si es IVA, Turismo, etc.)
        public int CatTributoId { get; set; }
        public CatTributo Tributo { get; set; } = null!;

        // SNAPSHOTS: Guardamos copia del código y nombre
        // Por si mañana Hacienda cambia el nombre del impuesto, el historial no se rompe.
        public string CodigoAttribute { get; set; } = string.Empty; // Ej: "20"
        public string Descripcion { get; set; } = string.Empty; // Ej: "Impuesto al Valor Agregado 13%"
        
        // EL DINERO
        public decimal Valor { get; set; } // El monto calculado (Ej: $13.00)
            
        }
}