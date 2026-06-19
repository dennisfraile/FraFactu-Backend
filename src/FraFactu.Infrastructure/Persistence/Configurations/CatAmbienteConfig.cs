using FraFactu.Domain.Entities.Catalogos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraFactu.Infrastructure.Persistence.Configurations
{
    public class CatAmbienteConfig: IEntityTypeConfiguration<CatAmbienteDestino>
    {
        public void Configure(EntityTypeBuilder<CatAmbienteDestino> builder)
        {
            builder.ToTable("cat_ambiente_destino");
            
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Codigo).HasMaxLength(2); 
            builder.Property(x => x.Valor).HasMaxLength(50).IsRequired();

            builder.HasData(
                new CatAmbienteDestino {Id = 1, Codigo = "00", Valor = "Modo prueba" },
                new CatAmbienteDestino {Id = 2, Codigo = "01", Valor = "Modo producción" }
            );
        }
        
    }
}