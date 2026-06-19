using FraFactu.Domain.Entities.Catalogos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraFactu.Infrastructure.Persistence.Configurations;

public class CatTipoDocumentoContingenciaConfig : IEntityTypeConfiguration<CatTipoDocumentoContingencia>
{
    public void Configure(EntityTypeBuilder<CatTipoDocumentoContingencia> builder)
    {
        builder.ToTable("cat_tipo_documento_contingencia");
        
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Codigo).HasMaxLength(2); 
        builder.Property(x => x.Valor).HasMaxLength(150).IsRequired();

        builder.HasData(
            new CatTipoDocumentoContingencia {Id = 1, Codigo = "01", Valor = "Factura Electrónica" }, 
            new CatTipoDocumentoContingencia {Id = 2, Codigo = "03", Valor = "Comprobante de Crédito Fiscal Electrónico" },
            new CatTipoDocumentoContingencia {Id = 3, Codigo = "04", Valor = "Nota de Remisión Electrónica" },
            new CatTipoDocumentoContingencia {Id = 4, Codigo = "05", Valor = "Nota de Crédito Electrónica" },
            new CatTipoDocumentoContingencia {Id = 5, Codigo = "06", Valor = "Nota de Débito Electrónica" },
            new CatTipoDocumentoContingencia {Id = 6, Codigo = "11", Valor = "Factura de Exportación Electrónica" },
            new CatTipoDocumentoContingencia {Id = 7, Codigo = "14", Valor = "Factura de Sujeto Excluido Electrónica" }
        );
    }
}