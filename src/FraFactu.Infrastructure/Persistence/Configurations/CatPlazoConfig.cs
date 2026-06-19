using FraFactu.Domain.Entities.Catalogos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraFactu.Infrastructure.Persistence.Configurations;

public class CatPlazoConfig : IEntityTypeConfiguration<CatPlazo>
{
    public void Configure(EntityTypeBuilder<CatPlazo> builder)
    {
        builder.ToTable("cat_plazo");
        
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Codigo).HasMaxLength(2); 
        builder.Property(x => x.Valor).HasMaxLength(20).IsRequired();

        builder.HasData(
            new CatPlazo {Id = 1, Codigo = "01", Valor = "Días" },
            new CatPlazo {Id = 2, Codigo = "02", Valor = "Meses" },
            new CatPlazo {Id = 3, Codigo = "03", Valor = "Años" }
        );
    }
}