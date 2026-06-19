using FraFactu.Domain.Entities.Catalogos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraFactu.Infrastructure.Persistence.Configurations;

public class CatTipoInvalidacionConfig : IEntityTypeConfiguration<CatTipoInvalidacion>
{
    public void Configure(EntityTypeBuilder<CatTipoInvalidacion> builder)
    {
        builder.ToTable("cat_tipo_invalidacion");
        
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Codigo).HasMaxLength(2); 
        builder.Property(x => x.Valor).HasMaxLength(150).IsRequired();

        builder.HasData(
            new CatTipoInvalidacion {Id = 1, Codigo = "01", Valor = "Error en la Información del Documento Tributario Electrónico a invalidar" },
            new CatTipoInvalidacion {Id = 2, Codigo = "02", Valor = "Rescindir de la operación realizada" },
            new CatTipoInvalidacion {Id = 3, Codigo = "03", Valor = "Otro" }
        );
    }
}