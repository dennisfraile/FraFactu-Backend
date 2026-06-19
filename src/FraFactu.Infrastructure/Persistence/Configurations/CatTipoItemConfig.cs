using FraFactu.Domain.Entities.Catalogos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraFactu.Infrastructure.Persistence.Configurations;

public class CatTipoItemConfig : IEntityTypeConfiguration<CatTipoItem>
{
    public void Configure(EntityTypeBuilder<CatTipoItem> builder)
    {
        builder.ToTable("cat_tipo_item");
        
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Codigo).HasMaxLength(2); 
        builder.Property(x => x.Valor).HasMaxLength(150).IsRequired();

        builder.HasData(
            new CatTipoItem {Id = 1, Codigo = "1", Valor = "Bienes" },
            new CatTipoItem {Id = 2, Codigo = "2", Valor = "Servicios" },
            new CatTipoItem {Id = 3, Codigo = "3", Valor = "Ambos (Bienes y Servicios, incluye los dos inherente a los Productos o servicios)" },
            new CatTipoItem {Id = 4, Codigo = "4", Valor = "Otros tributos por ítem" }
        );
    }
}