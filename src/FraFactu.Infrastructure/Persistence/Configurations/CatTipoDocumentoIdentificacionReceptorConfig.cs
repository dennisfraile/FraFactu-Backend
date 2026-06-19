using FraFactu.Domain.Entities.Catalogos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraFactu.Infrastructure.Persistence.Configurations;

public class CatTipoDocumentoIdentificacionReceptorConfig : IEntityTypeConfiguration<CatTipoDocumentoIdentificacionReceptor>
{
    public void Configure(EntityTypeBuilder<CatTipoDocumentoIdentificacionReceptor> builder)
    {
        builder.ToTable("cat_tipo_documento_identificacion_receptor");
        
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Codigo).HasMaxLength(2); 
        builder.Property(x => x.Valor).HasMaxLength(100).IsRequired();

        builder.HasData(
            new CatTipoDocumentoIdentificacionReceptor {Id = 1, Codigo = "36", Valor = "NIT" },
            new CatTipoDocumentoIdentificacionReceptor {Id = 2, Codigo = "13", Valor = "DUI" },
            new CatTipoDocumentoIdentificacionReceptor {Id = 3, Codigo = "37", Valor = "Otro" },
            new CatTipoDocumentoIdentificacionReceptor {Id = 4, Codigo = "03", Valor = "Pasaporte" },        
            new CatTipoDocumentoIdentificacionReceptor {Id = 5, Codigo = "02", Valor = "Carnet de Residente" } 
        );
    }
}