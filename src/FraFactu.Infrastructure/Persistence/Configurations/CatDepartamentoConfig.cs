using FraFactu.Domain.Entities.Catalogos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraFactu.Infrastructure.Persistence.Configurations;

public class CatDepartamentoConfig : IEntityTypeConfiguration<CatDepartamento>
{
    public void Configure(EntityTypeBuilder<CatDepartamento> builder)
    {
        builder.ToTable("cat_departamento");
        
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Codigo).HasMaxLength(2); 
        builder.Property(x => x.Valor).HasMaxLength(50).IsRequired();

        builder.HasData(
            new CatDepartamento {Id = 1, Codigo = "00", Valor = "Otro (Para extranjeros)" },
            new CatDepartamento {Id = 2, Codigo = "01", Valor = "Ahuachapán" },
            new CatDepartamento {Id = 3, Codigo = "02", Valor = "Santa Ana" },
            new CatDepartamento {Id = 4, Codigo = "03", Valor = "Sonsonate" },
            new CatDepartamento {Id = 5, Codigo = "04", Valor = "Chalatenango" },
            new CatDepartamento {Id = 6, Codigo = "05", Valor = "La Libertad" },
            new CatDepartamento {Id = 7, Codigo = "06", Valor = "San Salvador" },
            new CatDepartamento {Id = 8, Codigo = "07", Valor = "Cuscatlán" },
            new CatDepartamento {Id = 9, Codigo = "08", Valor = "La Paz" },
            new CatDepartamento {Id = 10, Codigo = "09", Valor = "Cabañas" },
            new CatDepartamento {Id = 11, Codigo = "10", Valor = "San Vicente" },
            new CatDepartamento {Id = 12, Codigo = "11", Valor = "Usulután" },
            new CatDepartamento {Id = 13, Codigo = "12", Valor = "San Miguel" },
            new CatDepartamento {Id = 14, Codigo = "13", Valor = "Morazán" },
            new CatDepartamento {Id = 15, Codigo = "14", Valor = "La Unión" }
        );
    }
}