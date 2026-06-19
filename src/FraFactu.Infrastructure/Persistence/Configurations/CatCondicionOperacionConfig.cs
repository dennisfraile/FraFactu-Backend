using FraFactu.Domain.Entities.Catalogos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraFactu.Infrastructure.Persistence.Configurations;

public class CatCondicionOperacionConfig : IEntityTypeConfiguration<CatCondicionOperacion>
{
    public void Configure(EntityTypeBuilder<CatCondicionOperacion> builder)
    {
        builder.ToTable("cat_condicion_operacion");
        
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Codigo).HasMaxLength(2); 
        builder.Property(x => x.Valor).HasMaxLength(50).IsRequired();

        builder.HasData(
            new CatCondicionOperacion {Id = 1, Codigo = "1", Valor = "Contado" },
            new CatCondicionOperacion {Id = 2, Codigo = "2", Valor = "A crédito" },
            new CatCondicionOperacion {Id = 3, Codigo = "3", Valor = "Otro" }
        );
    }
}