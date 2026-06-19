using FraFactu.Domain.Entities.Catalogos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraFactu.Infrastructure.Persistence.Configurations
{
    public class CatTipoDocumentoConfig: IEntityTypeConfiguration<CatTipoDocumento>
    {
        public void Configure(EntityTypeBuilder<CatTipoDocumento> builder)
        {
            builder.ToTable("cat_tipo_doc");
            
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Codigo).HasMaxLength(2); 
            builder.Property(x => x.Valor).HasMaxLength(150).IsRequired();

            builder.HasData(
                new CatTipoDocumento {Id = 1, Codigo = "01", Valor = "Factura" },
                new CatTipoDocumento {Id = 2, Codigo = "03", Valor = "Comprobante de crédito fiscal" },
                new CatTipoDocumento {Id = 3, Codigo = "04", Valor = "Nota de remisión" },
                new CatTipoDocumento {Id = 4, Codigo = "05", Valor = "Nota de crédito" },
                new CatTipoDocumento {Id = 5, Codigo = "06", Valor = "Nota de débito" },
                new CatTipoDocumento {Id = 6, Codigo = "07", Valor = "Comprobante de retención" },
                new CatTipoDocumento {Id = 7, Codigo = "08", Valor = "Comprobante de liquidación" },
                new CatTipoDocumento {Id = 8, Codigo = "09", Valor = "Documento contable de liquidación" },
                new CatTipoDocumento {Id = 9, Codigo = "11", Valor = "Facturas de exportación" },
                new CatTipoDocumento {Id = 10, Codigo = "14", Valor = "Factura de sujeto excluido" },
                new CatTipoDocumento {Id = 11, Codigo = "15", Valor = "Comprobante de donación" }
            );
        }
        
    }
}