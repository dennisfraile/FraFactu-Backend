using FraFactu.Domain.Entities.Catalogos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraFactu.Infrastructure.Persistence.Configurations;

public class CatTipoContingenciaConfig : IEntityTypeConfiguration<CatTipoContingencia>
{
    public void Configure(EntityTypeBuilder<CatTipoContingencia> builder)
    {
        builder.ToTable("cat_tipo_contingencia");
        
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Codigo).HasMaxLength(2); 
        builder.Property(x => x.Valor).HasMaxLength(250).IsRequired();

        builder.HasData(
            new CatTipoContingencia {Id = 1, Codigo = "1", Valor = "No disponibilidad de sistema del MH" },
            new CatTipoContingencia {Id = 2, Codigo = "2", Valor = "No disponibilidad de sistema del emisor" },
            new CatTipoContingencia {Id = 3, Codigo = "3", Valor = "Falla en el suministro de servicio de Internet del Emisor" },
            new CatTipoContingencia {Id = 4, Codigo = "4", Valor = "Falla en el suministro de servicio de energía eléctrica del emisor que impida la transmisión de los DTE" },
            new CatTipoContingencia {Id = 5, Codigo = "5", Valor = "Otro" } 
        );
    }
}