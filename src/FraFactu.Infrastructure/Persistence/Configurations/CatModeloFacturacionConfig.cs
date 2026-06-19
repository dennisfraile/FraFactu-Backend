using FraFactu.Domain.Entities.Catalogos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraFactu.Infrastructure.Persistence.Configurations;

public class CatModeloFacturacionConfig : IEntityTypeConfiguration<CatModeloFacturacion>
{
    public void Configure(EntityTypeBuilder<CatModeloFacturacion> builder)
    {
        builder.ToTable("cat_modelo_fac");
        
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Codigo).HasMaxLength(2); 
        builder.Property(x => x.Valor).HasMaxLength(100).IsRequired();

        builder.HasData(
            new CatModeloFacturacion {Id = 1, Codigo = "01", Valor = "Modelo Facturación previo" },
            new CatModeloFacturacion {Id = 2, Codigo = "02", Valor = "Modelo Facturación diferido" }
        );
    }
}