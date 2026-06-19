using FraFactu.Domain.Common;
using FraFactu.Domain.Entities.Catalogos;

namespace FraFactu.Domain.Entities
{
    public class Pago : BaseEntity
    {
        public int FacturaId { get; set; }
        public FacturaElectronica Factura { get; set; } = null!;

        public int CatFormaPagoId { get; set; }
        public CatFormaPago FormaPago { get; set; } = null!;

        public decimal Monto { get; set; }
        
        // Referencia: Número de cheque, autorización de tarjeta, wallet, etc.
        public string? Referencia { get; set; } 
        
        public int? CatPlazoId { get; set; } // Días, Meses (si es crédito)
        public CatPlazo? Plazo { get; set; }
        public decimal? Periodo { get; set; } // Cantidad de tiempo (ej: 30 días)
            
        }
}