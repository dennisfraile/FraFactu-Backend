using FraFactu.Domain.Entities.Catalogos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraFactu.Infrastructure.Persistence.Configurations;

public class CatTipoEstablecimientoConfig : IEntityTypeConfiguration<CatTipoEstablecimiento>
{
    public void Configure(EntityTypeBuilder<CatTipoEstablecimiento> builder)
    {
        builder.ToTable("cat_tipo_establecimiento");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Codigo).HasMaxLength(2);
        builder.Property(x => x.Valor).HasMaxLength(100).IsRequired();

        builder.HasData(
            new CatTipoEstablecimiento { Id = 1, Codigo = "01", Valor = "Casa Matriz" },
            new CatTipoEstablecimiento { Id = 2, Codigo = "02", Valor = "Sucursal" },
            new CatTipoEstablecimiento { Id = 3, Codigo = "04", Valor = "Bodega" },
            new CatTipoEstablecimiento { Id = 4, Codigo = "07", Valor = "Patio" },
            new CatTipoEstablecimiento { Id = 5, Codigo = "20", Valor = "Oficina Administrativa" },
            new CatTipoEstablecimiento { Id = 6, Codigo = "99", Valor = "Otros" }
        );
    }
}