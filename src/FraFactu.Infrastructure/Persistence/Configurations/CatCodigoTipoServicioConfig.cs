using FraFactu.Domain.Entities.Catalogos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraFactu.Infrastructure.Persistence.Configurations;

public class CatTipoServicioConfig : IEntityTypeConfiguration<CatCodigoTipoServicio>
{
    public void Configure(EntityTypeBuilder<CatCodigoTipoServicio> builder)
    {
        builder.ToTable("cat_tipo_servicio");
        
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Codigo).HasMaxLength(2); 
        builder.Property(x => x.Valor).HasMaxLength(150).IsRequired();

        builder.HasData(
            new CatCodigoTipoServicio {Id = 1, Codigo = "01", Valor = "Cirugía" },
            new CatCodigoTipoServicio {Id = 2, Codigo = "02", Valor = "Operación" },
            new CatCodigoTipoServicio {Id = 3, Codigo = "03", Valor = "Tratamiento médico" },
            new CatCodigoTipoServicio {Id = 4, Codigo = "04", Valor = "Cirugía instituto salvadoreño de Bienestar Magisterial" },
            new CatCodigoTipoServicio {Id = 5, Codigo = "05", Valor = "Operación Instituto Salvadoreño de Bienestar Magisterial" },
            new CatCodigoTipoServicio {Id = 6, Codigo = "06", Valor = "Tratamiento médico Instituto Salvadoreño de Bienestar Magisterial" }
        );
    }
}