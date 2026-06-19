using FraFactu.Domain.Common;
using FraFactu.Domain.Entities.Catalogos;
namespace FraFactu.Domain.Entities
{
    public class FacturaDocumentoRelacionado: BaseEntity
    {
        public int FacturaId { get; set; }
        public FacturaElectronica Factura { get; set; } = null!;
        
        public int CatTipoDocumentoId { get; set; }
        public CatTipoDocumento TipoDocumento { get; set; } = null!;

        // ¿Fue físico o electrónico?
        public int CatTipoGeneracionDocumentoId { get; set; }
        public CatTipoGeneracionDocumento TipoGeneracion { get; set; } = null!;

        public string NumeroDocumento { get; set; } = string.Empty; // El Correlativo o UUID relacionado
        public DateTime FechaEmision { get; set; } // La fecha del documento viejo
            
        }
}