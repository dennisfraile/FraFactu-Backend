using FraFactu.Domain.Entities.Catalogos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraFactu.Infrastructure.Persistence.Configurations;

public class CatTipoGeneracionDocumentoConfig : IEntityTypeConfiguration<CatTipoGeneracionDocumento>
{
    public void Configure(EntityTypeBuilder<CatTipoGeneracionDocumento> builder)
    {
        builder.ToTable("cat_tipo_generacion");
        
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Codigo).HasMaxLength(2); 
        builder.Property(x => x.Valor).HasMaxLength(50).IsRequired();

        builder.HasData(
            new CatTipoGeneracionDocumento {Id = 1, Codigo = "1", Valor = "Físico" },
            new CatTipoGeneracionDocumento {Id = 2, Codigo = "2", Valor = "Electrónico" }
        );
    }
}