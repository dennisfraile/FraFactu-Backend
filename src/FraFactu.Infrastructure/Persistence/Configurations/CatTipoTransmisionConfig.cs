using FraFactu.Domain.Entities.Catalogos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraFactu.Infrastructure.Persistence.Configurations;

public class CatTipoTransmisionConfig : IEntityTypeConfiguration<CatTipoTransmision>
{
    public void Configure(EntityTypeBuilder<CatTipoTransmision> builder)
    {
        builder.ToTable("cat_tipo_transmision");
        
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Codigo).HasMaxLength(2); 
        builder.Property(x => x.Valor).HasMaxLength(100).IsRequired();

        builder.HasData(
            new CatTipoTransmision {Id = 1, Codigo = "01", Valor = "Transmisión normal" },
            new CatTipoTransmision {Id = 2, Codigo = "02", Valor = "Transmisión por contingencia" }
        );
    }
}